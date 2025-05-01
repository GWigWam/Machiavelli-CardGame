namespace GWigWam.Machiavelli.Console;

using Spectre.Console.Rendering;
using PlayerPredicate = Func<Analysis.GameStats, Player, bool>;
public static class Analysis
{
    public static void Run(Func<Resources> resFactory)
    {
        var localRes = resFactory();
        var nog = AnsiConsole.Prompt(new TextPrompt<int>("No games:"));

        var stats = new GameStats[nog];
        AnsiConsole.Progress()
            .Start(ctx => {
                var task = ctx.AddTask("Simulate games", maxValue: nog);
                Parallel.For(0, nog, gix => {
                    var noPlayers = (gix % 5) + 3;
                    var (deck, chars) = resFactory();
                    var game = new Game(deck, chars, noPlayers);
                    game.Setup();
                    game.Controllers = game.Players.Select(p => (p, c: new AiPlayerController(game, p))).ToDictionary(t => t.p, t => (PlayerController)t.c);

                    stats[gix] = RecordGame(game);
                    task.Value++;
                });
                task.Value = nog;
        });

        TOut playerStat<TAgr, TOut>(PlayerPredicate playerPredicate, Func<GameStats, Player, TAgr> statSelector, Func<IEnumerable<TAgr>, TOut> statAggr)
        {
            var vals = stats
                .SelectMany(gs => gs.Game.Players
                    .Where(ap => playerPredicate(gs, ap))
                    .Select(p => statSelector(gs, p)));
            return statAggr(vals);
        }

        static bool isFst(GameStats gs, Player p) => gs.Standings[0] == p;
        static bool isLst(GameStats gs, Player p) => gs.Standings[gs.Standings.Length - 1] == p;
        static bool isTop(GameStats gs, Player p) => Array.IndexOf(gs.Standings, p) < gs.Standings.Length / 2;
        static bool isBot(GameStats gs, Player p) => Array.IndexOf(gs.Standings, p) >= gs.Standings.Length / 2;

        Func<GameStats, Player, double> kingFrac = (gs, p) => gs.PlayerKingCount[p] / (double)gs.Game.Rounds.Count;
        Func<GameStats, Player, int> fstKing = (gs, p) => gs.Game.Rounds.First().RoundKing == p ? 1 : 0;

        string fPcnt(double v, double max = 1, bool inv = false) => $"[{(Math.Abs((inv ? 1 : 0) - (v / max)) is var f ? f > 0.8 ? "red" : f > 0.6 ? "darkorange" : f > 0.3 ? "yellow" : "green" : "")}]{v:P2}[/]";

        var tableKing = new Table();
        tableKing.AddColumns("Position", "King rounds", "Game start player");
        tableKing.AddRow("Frst", fPcnt(playerStat(isFst, kingFrac, v => v.Average()), 0.3, true), fPcnt(playerStat(isFst, fstKing, v => v.Sum() / (double)v.Count()), 0.3, true));
        tableKing.AddRow(">50%", fPcnt(playerStat(isTop, kingFrac, v => v.Average()), 0.3, true), fPcnt(playerStat(isTop, fstKing, v => v.Sum() / (double)v.Count()), 0.3, true));
        tableKing.AddRow("<50%", fPcnt(playerStat(isBot, kingFrac, v => v.Average()), 0.3, true), fPcnt(playerStat(isBot, fstKing, v => v.Sum() / (double)v.Count()), 0.3, true));
        tableKing.AddRow("Last", fPcnt(playerStat(isLst, kingFrac, v => v.Average()), 0.3, true), fPcnt(playerStat(isLst, fstKing, v => v.Sum() / (double)v.Count()), 0.3, true));
        AnsiConsole.Write(tableKing);

        var tableChar = new Table();
        tableChar.AddColumns("Position", "Assassin", "Thief", "Mage", "King", "Preacher", "Merchant", "Architect", "Condottiero");
        foreach (var (cat, pred) in new (string, PlayerPredicate)[] { ("First", isFst), (">50%", isTop), ("<50%", isBot), ("Last", isLst) })
        {
            var row = new List<IRenderable> { new Markup(cat) };
            foreach (var c in CharacterType.Known.All)
            {
                var charCount = playerStat(pred, (gs, p) => gs.PlayerPickCount[(p, c)] / (double)gs.Game.Rounds.Count, v => v.Average());
                row.Add(new Markup(fPcnt(charCount, 0.2, true)));
            }
            tableChar.AddRow(row);
        }
        AnsiConsole.Write(tableChar);

        var tableVictims = new Table();
        tableVictims.AddColumns("Character", "Assassination target", "Assassination hit", "Thief target", "Thief hit");
        foreach (var ch in CharacterType.Known.All)
        {
            var dsc = localRes.Characters.First(c => c.Type == ch).Description;
            var at = fPcnt(stats.Average(s => s.AssassinTargets[ch].target), 0.25);
            var ah = fPcnt(stats.Average(s => s.AssassinTargets[ch].hit), 0.20);
            var tt = fPcnt(stats.Average(s => s.ThiefTargets[ch].target), 0.25);
            var th = fPcnt(stats.Average(s => s.ThiefTargets[ch].hit), 0.20);
            tableVictims.AddRow(dsc, at, ah, tt, th);
        }
        AnsiConsole.Write(tableVictims);
    }

    private static GameStats RecordGame(Game game)
    {
        Player[] standings = null!;
        game.GameOver += s => standings = s;

        var aVict = CharacterType.Known.All.ToDictionary(c => c, _ => (target: 0, hit: 0));
        var tVict = CharacterType.Known.All.ToDictionary(c => c, _ => (target: 0, hit: 0));
        game.OnNewRound += r => {
            r.OnAssassinateAction += (_, t) => aVict[t.Type] = (aVict[t.Type].target + 1, aVict[t.Type].hit + (r.CharacterPlayerMap.ContainsKey(t) ? 1 : 0));
            r.OnRobAction += (_, t) => tVict[t.Type] = (tVict[t.Type].target + 1, tVict[t.Type].hit + (r.CharacterPlayerMap.ContainsKey(t) ? 1 : 0));
        };

        while (!game.NextRound()) { }

        var playerPickCount = game.Rounds
            .SelectMany(r => r.PlayerPick)
            .SelectMany(kvp => CharacterType.Known.All
                .Select(c => (character: c, player: kvp.Key, count: kvp.Value.Type == c ? 1 : 0)))
            .GroupBy(t => (t.player, t.character))
            .Select(g => (g.Key, cnt: g.Sum(g => g.count)))
            .ToDictionary(g => g.Key, g => g.cnt);

        var playerKingCount = game.Players
            .ToDictionary(p => p, p => game.Rounds.Where(r => r.RoundKing == p).Count());

        var aCnt = aVict.Values.Sum(t => t.target);
        var tCnt = tVict.Values.Sum(t => t.target);
        var aFrac = aVict.ToDictionary(kvp => kvp.Key, kvp => (aCnt == 0 ? 0 : kvp.Value.target / (double)aCnt, aCnt == 0 ? 0 : kvp.Value.hit / (double)aCnt));
        var tFrac = tVict.ToDictionary(kvp => kvp.Key, kvp => (tCnt == 0 ? 0 : kvp.Value.target / (double)tCnt, tCnt == 0 ? 0 : kvp.Value.hit / (double)tCnt));

        return new(game, standings, playerPickCount, playerKingCount, aFrac, tFrac);
    }

    public record GameStats(
        Game Game,
        Player[] Standings,
        Dictionary<(Player, CharacterType), int> PlayerPickCount,
        Dictionary<Player, int> PlayerKingCount,
        Dictionary<CharacterType, (double target, double hit)> AssassinTargets,
        Dictionary<CharacterType, (double target, double hit)> ThiefTargets
    );
}
