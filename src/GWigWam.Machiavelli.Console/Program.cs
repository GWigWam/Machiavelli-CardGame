using GWigWam.Machiavelli.Console;
using GWigWam.Machiavelli.Core;
using GWigWam.Machiavelli.Res;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;
AnsiConsole.Write(new FigletText("GWigWam").Centered().Color(Color.Yellow));
AnsiConsole.Write(new FigletText("Machiavelli").Centered().Color(Color.Red));

var res = new ResourceFiles(Path.Combine(new FileInfo(Environment.ProcessPath!).Directory!.FullName, "../../../../../res"));
var load = res.Load("nl");
var resFactory = await AnsiConsole.Status()
    .StartAsync("Loading...", _ => load);
var (deck, chars) = resFactory();

var noPlayers = AnsiConsole.Prompt(new TextPrompt<int>("Number of players: ").Validate(i => i > 2 && i <= 7).DefaultValue(4));

var game = new Game(deck, chars, noPlayers);
game.Setup();
var controllerDict = game.Players.Select(p => (p, c: new AiPlayerController(game, p))).ToDictionary(t => t.p, t => (PlayerController)t.c);
controllerDict[game.Players[0]] = new ConsolePlayerController(game, game.Players[0]);
game.Controllers = controllerDict;

game.OnNewRound += r => {
    AnsiConsole.MarkupLine($"\nRound {r.Number}");

    r.BeforeCharacterPicks += () => AnsiConsole.MarkupLine($"Unavailable cards | Open: {string.Join(" ", r.OpenCharacters!.Select(c => $"[[{c.ToMarkup()}]]"))} | Closed: [[[white]:flower_playing_cards:???[/]]]");
};

void sumPlayer(int ix, Player p)
{
    AnsiConsole.MarkupLine($"{(game.ActingKing == p ? " :crown:" : " :bust_in_silhouette:")} {p.ToMarkup(game)} {p.Gold}:coin: {p.Hand.Count}:flower_playing_cards: | [{(p.City.Count >= 7 ? "orangered1": "default")}]{p.City.Count}[/]/{p.Score:D2}p: {string.Join(" ", p.City.Select(i => i.Card.ToMarkup()))}");
}
void sumAllP()
{
    for (int px = 0; px < game.Players.Length; px++)
    {
        sumPlayer(px, game.Players[px]);
    }
}
game.OnNewRound += r => sumAllP();

game.OnRoundStart += r => {
    r.OnPlayerTurn += (p, c) => AnsiConsole.MarkupLine($"Turn {c.ToMarkup()} {p.ToMarkup(game)}");
    r.OnGetGoldAction += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +2:coin: ({p.Gold}:coin:)");
    r.OnGetCardsAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
    r.OnGetBuildingsGoldAction += (p, c, g) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +{g}:coin: ({c} buildings) ({p.Gold}:coin:)");
    r.OnBuild += (p, b) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} builds {b.Card.ToMarkup()} ({p.Gold}:coin: {p.Hand.Count}:flower_playing_cards: left)");

    r.OnAssassinateAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} assassinates {c.ToMarkup()}");
    r.OnRobAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} robs {c.ToMarkup()}");
    r.OnMagicianSwapWithPlayerAction += (pSelf, pOther) => AnsiConsole.MarkupLine($"{pSelf.ToMarkup(game)} swaps hand cards with {pOther.ToMarkup(game)}");
    r.OnMagicianSwapWithDeckAction += (p, cards) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} swaps {cards} with deck");
    r.OnClaimKingship += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} is the new king!");
    r.OnMerchantGetExtraGoldAction += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +1:coin: extra ({p.Gold}:coin:)");
    r.OnArchitectGetBuildingCardsAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
    r.OnCondottieroDestroyBuildingAction += (pSelf, pOther, b) => AnsiConsole.MarkupLine($"{pSelf.ToMarkup(game)} destroys {pOther.ToMarkup(game)}'s {b.Card.ToMarkup()} :fire:");
};
game.GameOver += () => {
    AnsiConsole.MarkupLine($"\n[red]Game over![/]");
    sumAllP();
    foreach (var (p, ix) in game.Players.OrderByDescending(t => t.Score).Select((t, ix) => (t, ix)))
    {
        AnsiConsole.MarkupLine($"#{ix+1} {p.ToMarkup(game)} Score: [bold white]{p.Score}[/]");
    }
};

while (!game.NextRound()) { }
