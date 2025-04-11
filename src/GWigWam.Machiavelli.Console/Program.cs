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
game.Controllers = game.Players.Select(p => (p, c: new RandomPlayerController(game, p))).ToDictionary(t => t.p, t => (PlayerController)t.c);

game.OnNewRound += r => {
    AnsiConsole.MarkupLine($"\nRound {r.Number}");
};

string[] pCol = ["blue", "red", "green", "yellow", "fuchsia", "cyan", "darkorange"];
string pMarkup(Player p) => $"[{pCol[Array.IndexOf(game.Players, p)]}]P{p.Id}[/]";
static string bcs(BuildingColor c) => c switch { BuildingColor.Blue => "blue", BuildingColor.Green => "green", BuildingColor.Red => "red", BuildingColor.Yellow => "yellow", _ => "fuchsia" };
string bMarkup(BuildingCard c) => $"[[[{bcs(c.Color)}]{c.Description}[/] [bold]{c.Cost}[/]]]";
string cIcon(CharacterType t) =>
    t == CharacterType.Known.Assassin ? ":skull:" :
    t == CharacterType.Known.Thief ? ":ninja:" :
    t == CharacterType.Known.Magician ? ":sparkles:" :
    t == CharacterType.Known.King ? ":crown:" :
    t == CharacterType.Known.Preacher ? ":folded_hands:" :
    t == CharacterType.Known.Merchant ? ":money_bag:" :
    t == CharacterType.Known.Architect ? ":classical_building:" :
    t == CharacterType.Known.Condottiero ? ":crossed_swords:" : "";
string cMarkup(Character c) => $"[white]{cIcon(c)} {c.Description}[/]";
void sumPlayer(int ix, Player p)
{
    AnsiConsole.MarkupLine($":bust_in_silhouette: {pMarkup(p)} {p.Gold}:coin: {p.Hand.Count}:flower_playing_cards:{(game.ActingKing == p ? " :crown:" : "")} {string.Join(" ", p.City.Select(i => bMarkup(i)))}");
}
void sumAllP()
{
    for (int px = 0; px < game.Players.Length; px++)
    {
        sumPlayer(px, game.Players[px]);
    }
}
game.OnRoundStart += r => {
    AnsiConsole.MarkupLine($"Unavailable cards | Open: {string.Join(" ", r.OpenCharacters!.Select(c => $"[[{cMarkup(c)}]]"))} | Closed: [[[white]:flower_playing_cards:???[/]]]");
    sumAllP();
    r.OnPlayerTurn += (p, c) => AnsiConsole.MarkupLine($"Turn {cMarkup(c)} {pMarkup(p)}");
    r.OnGetGoldAction += p => AnsiConsole.MarkupLine($"{pMarkup(p)} receives +2:coin: ({p.Gold}:coin:)");
    r.OnGetCardsAction += (p, c) => AnsiConsole.MarkupLine($"{pMarkup(p)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
    r.OnGetBuildingsGoldAction += (p, c, g) => AnsiConsole.MarkupLine($"{pMarkup(p)} receives +{g}:coin: ({c} buildings) ({p.Gold}:coin:)");
    r.OnBuild += (p, b) => AnsiConsole.MarkupLine($"{pMarkup(p)} builds {bMarkup(b)} ({p.Gold}:coin: {p.Hand.Count}:flower_playing_cards: left)");

    r.OnAssassinateAction += (p, c) => AnsiConsole.MarkupLine($"{pMarkup(p)} assassinates {cMarkup(c)}");
    r.OnRobAction += (p, c) => AnsiConsole.MarkupLine($"{pMarkup(p)} robs {cMarkup(c)}");
    r.OnMagicianSwapWithPlayerAction += (pSelf, pOther) => AnsiConsole.MarkupLine($"{pMarkup(pSelf)} swaps hand cards with {pMarkup(pOther)}");
    r.OnMagicianSwapWithDeckAction += (p, cards) => AnsiConsole.MarkupLine($"{pMarkup(p)} swaps {cards} with deck");
    r.OnClaimKingship += p => AnsiConsole.MarkupLine($"{pMarkup(p)} is the new king!");
    r.OnMerchantGetExtraGoldAction += p => AnsiConsole.MarkupLine($"{pMarkup(p)} receives +1:coin: extra ({p.Gold}:coin:)");
    r.OnArchitectGetBuildingCardsAction += (p, c) => AnsiConsole.MarkupLine($"{pMarkup(p)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
    r.OnCondottieroDestroyBuildingAction += (pSelf, pOther, b) => AnsiConsole.MarkupLine($"{pMarkup(pSelf)} destroys {pMarkup(pOther)}'s {bMarkup(b)}");
};
game.GameOver += () => {
    AnsiConsole.MarkupLine($"\n[red]Game over![/]");
    sumAllP();
    foreach (var (p, ix) in game.Players.OrderByDescending(t => t.Score).Select((t, ix) => (t, ix)))
    {
        AnsiConsole.MarkupLine($"#{ix+1} {pMarkup(p)} Score: [bold white]{p.Score}[/]");
    }
};

while (!game.NextRound()) { }
