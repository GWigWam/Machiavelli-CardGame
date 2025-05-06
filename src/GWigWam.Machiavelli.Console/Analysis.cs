namespace GWigWam.Machiavelli.Console;

using Spectre.Console.Rendering;
using PlayerPredicate = Func<Analysis.GameStats, Player, bool>;
public static class Analysis
{
    public static void Run(Func<Resources> resFactory, int? noGames = null)
    {
        var localRes = resFactory();
        var nog = noGames ?? AnsiConsole.Prompt(new TextPrompt<int>("No games:"));

        List<IRenderable> renderables = [];
        AnsiConsole.Progress()
            .Start(ctx => {
                // T1: Simulate
                var stats = new GameStats[nog];
                var t1 = ctx.AddTask("Simulate games", maxValue: nog);
                Parallel.For(0, nog, gix => {
                    var noPlayers = (gix % 5) + 3;
                    var (deck, chars) = resFactory();
                    var game = new Game(deck, chars, noPlayers);
                    game.Setup();
                    game.Controllers = game.Players.Select(p => (p, c: new AiPlayerController(game, p))).ToDictionary(t => t.p, t => (PlayerController)t.c);

                    stats[gix] = RecordGame(game, localRes);
                    t1.Value++;
                });
                t1.Value = nog;

                TOut playerStat<TAgr, TOut>(PlayerPredicate playerPredicate, Func<GameStats, Player, TAgr> statSelector, Func<IEnumerable<TAgr>, TOut> statAggr)
                {
                    var vals = stats
                        .SelectMany(gs => gs.Game.Players
                            .Where(ap => playerPredicate(gs, ap))
                            .Select(p => statSelector(gs, p)));
                    return statAggr(vals);
                }

                // T2: Analyze
                var t2 = ctx.AddTask("Analyze", maxValue: 4);

                static bool isFst(GameStats gs, Player p) => gs.Standings[0] == p;
                static bool isLst(GameStats gs, Player p) => gs.Standings[gs.Standings.Length - 1] == p;
                static bool isTop(GameStats gs, Player p) => Array.IndexOf(gs.Standings, p) < gs.Standings.Length / 2;
                static bool isBot(GameStats gs, Player p) => Array.IndexOf(gs.Standings, p) >= gs.Standings.Length / 2;

                Func<GameStats, Player, double> kingFrac = (gs, p) => gs.PlayerKingCount[p] / (double)gs.Game.Rounds.Count;
                Func<GameStats, Player, int> fstKing = (gs, p) => gs.Game.Rounds.First().RoundKing == p ? 1 : 0;

                string fPcnt(double v, double min = 0, double max = 1, bool inv = false) => $"[{(Math.Abs((inv ? 1 : 0) - ((v - min) / (max - min))) is var f ? f > 0.8 ? "red" : f > 0.6 ? "darkorange" : f > 0.4 ? "yellow" : f > 0.2 ? "green" : "lime" : "")}]{v:P2}[/]";

                var tableKing = new Table();
                tableKing.AddColumns("Position", "King rounds", "Game start player");
                tableKing.AddRow("Frst", fPcnt(playerStat(isFst, kingFrac, v => v.Average()), 0.175, 0.25, true), fPcnt(playerStat(isFst, fstKing, v => v.Sum() / (double)v.Count()), 0.15, 0.3, true));
                tableKing.AddRow(">50%", fPcnt(playerStat(isTop, kingFrac, v => v.Average()), 0.175, 0.25, true), fPcnt(playerStat(isTop, fstKing, v => v.Sum() / (double)v.Count()), 0.15, 0.3, true));
                tableKing.AddRow("<50%", fPcnt(playerStat(isBot, kingFrac, v => v.Average()), 0.175, 0.25, true), fPcnt(playerStat(isBot, fstKing, v => v.Sum() / (double)v.Count()), 0.15, 0.3, true));
                tableKing.AddRow("Last", fPcnt(playerStat(isLst, kingFrac, v => v.Average()), 0.175, 0.25, true), fPcnt(playerStat(isLst, fstKing, v => v.Sum() / (double)v.Count()), 0.15, 0.3, true));
                renderables.Add(tableKing);
                t2.Value++;

                var tableChar = new Table();
                tableChar.AddColumns("Position", "Assassin", "Thief", "Mage", "King", "Preacher", "Merchant", "Architect", "Condottiero");
                foreach (var (cat, pred) in new (string, PlayerPredicate)[] { ("First", isFst), (">50%", isTop), ("<50%", isBot), ("Last", isLst) })
                {
                    var row = new List<IRenderable> { new Markup(cat) };
                    foreach (var c in CharacterType.Known.All)
                    {
                        var charCount = playerStat(pred, (gs, p) => gs.PlayerPickCount[(p, c)] / (double)gs.Game.Rounds.Count, v => v.Average());
                        row.Add(new Markup(fPcnt(charCount, 0.0, 0.2, true)));
                    }
                    tableChar.AddRow(row);
                }
                renderables.Add(tableChar);
                t2.Value++;

                var tableVictims = new Table();
                tableVictims.AddColumns("Character", "Assassination target", "Assassination hit", "Thief target", "Thief hit");
                foreach (var ch in CharacterType.Known.All)
                {
                    var dsc = localRes.Characters.First(c => c.Type == ch).Description;
                    var at = fPcnt(stats.Average(s => s.AssassinTargets[ch].target), 0.0, 0.25);
                    var aht = stats.Select(s => s.AssassinTargets[ch].hit).Where(d => d != null).ToArray();
                    var ah = aht.Length > 0 ? fPcnt(aht.Average(s => s!.Value), 0.6, 0.8) : "-";
                    var tt = fPcnt(stats.Average(s => s.ThiefTargets[ch].target), 0, 0.35);
                    var tht = stats.Select(s => s.ThiefTargets[ch].hit).Where(d => d != null).ToArray();
                    var th = tht.Length > 0 ? fPcnt(tht.Average(s => s!.Value), 0.5, 0.8) : "-";
                    tableVictims.AddRow(dsc, at, ah, tt, th);
                }
                var aha = fPcnt(stats.SelectMany(s => s.AssassinTargets.Values.Where(t => t.hit != null).Select(t => t.hit!.Value)).Average(), 0.75);
                var tha = fPcnt(stats.SelectMany(s => s.ThiefTargets.Values.Where(t => t.hit != null).Select(t => t.hit!.Value)).Average(), 0.75);
                tableVictims.AddRow("Avg", "-", aha, "-", tha);
                renderables.Add(tableVictims);
                t2.Value++;

                var builtStat = stats.SelectMany(gs => gs.BuiltCards)
                    .GroupBy(t => t.card)
                    .Select(g => (g.Key, bFrac: g.Sum(t => t.builtFrac) / g.Count(), wFrac: g.Sum(t => t.winnerBuilt) / g.Count(), lFrac: g.Sum(t => t.loserBuilt) / g.Count(), wsFrac: g.Sum(t => t.winerStart) / g.Count()))
                    .Select(t => (card: t.Key, t.bFrac, t.wFrac, wDiff: t.wFrac / t.bFrac, t.lFrac, lDiff: t.lFrac / t.bFrac, t.wsFrac))
                    .OrderByDescending(t => t.wDiff)
                    .ToArray();

                var tableBuilt = new Table();
                tableBuilt.AddColumns("Building", "Cnt", "Was built", "Winner built", "Diff", "Loser built", "Diff", "Winner start hand");
                foreach (var tpl in builtStat)
                {
                    tableBuilt.AddRow(
                        tpl.card.ToMarkup(),
                        $"{tpl.card.Count}",
                        fPcnt(tpl.bFrac, 0.4, 0.6, true),
                        fPcnt(tpl.wFrac, 0.1, 0.15, true),
                        fPcnt(tpl.wDiff, 0.2, 0.35, true),
                        fPcnt(tpl.lFrac, 0.0, 0.15),
                        fPcnt(tpl.lDiff, 0.05, 0.2),
                        fPcnt(tpl.wsFrac, 0.04, 0.08, true));
                }
                renderables.Add(tableBuilt);
                t2.Value++;
            });

        foreach (var r in renderables) { AnsiConsole.Write(r); }
    }

    private static GameStats RecordGame(Game game, Resources localRes)
    {
        var startingHand = game.Players.ToDictionary(p => p, p => p.Hand.ToArray());

        Player[] standings = null!;
        game.GameOver += s => standings = s;

        var aVict = CharacterType.Known.All.ToDictionary(c => c, _ => (target: 0, hit: 0));
        var tVict = CharacterType.Known.All.ToDictionary(c => c, _ => (target: 0, hit: 0));
        var builtl = new List<(Player player, BuildingCard building)>();
        game.OnNewRound += r => {
            r.OnAssassinateAction += (_, t) => aVict[t.Type] = (aVict[t.Type].target + 1, aVict[t.Type].hit + (r.CharacterPlayerMap.ContainsKey(t) ? 1 : 0));
            r.OnRobAction += (_, t) => tVict[t.Type] = (tVict[t.Type].target + 1, tVict[t.Type].hit + (r.CharacterPlayerMap.ContainsKey(t) ? 1 : 0));

            r.OnBuild += (p, b) => builtl.Add((p, b));
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
        var aFrac = aVict.ToDictionary(kvp => kvp.Key, kvp => (aCnt == 0 ? 0 : kvp.Value.target / (double)aCnt, kvp.Value.target == 0 ? (double?)null : kvp.Value.hit / (double)kvp.Value.target));
        var tFrac = tVict.ToDictionary(kvp => kvp.Key, kvp => (tCnt == 0 ? 0 : kvp.Value.target / (double)tCnt, kvp.Value.target == 0 ? (double?)null : kvp.Value.hit / (double)kvp.Value.target));

        var built = localRes.Deck.AllCards.Select(c => c.Card).DistinctBy(c => c.Id)
            .Select(c =>
            {
                var bc = builtl.Where(t => t.building == c).Count() / (double)c.Count;
                var win = builtl.Where(t => t.building == c && t.player == standings.First()).Count() / (double)c.Count;
                var los = builtl.Where(t => t.building == c && t.player == standings.Last()).Count() / (double)c.Count;
                var winStart = startingHand[standings.First()].Where(s => s == c).Count() / (double)c.Count;
                return (c, bc, win, los, winStart);
            }).ToArray();

        return new(game, standings, playerPickCount, playerKingCount, aFrac, tFrac, built);
    }

    public record GameStats(
        Game Game,
        Player[] Standings,
        Dictionary<(Player, CharacterType), int> PlayerPickCount,
        Dictionary<Player, int> PlayerKingCount,
        Dictionary<CharacterType, (double target, double? hit)> AssassinTargets,
        Dictionary<CharacterType, (double target, double? hit)> ThiefTargets,
        (BuildingCard card, double builtFrac, double winnerBuilt, double loserBuilt, double winerStart)[] BuiltCards
    );
}
