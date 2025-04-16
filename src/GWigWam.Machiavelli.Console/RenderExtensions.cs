using GWigWam.Machiavelli.Core;

namespace GWigWam.Machiavelli.Console;
public static class RenderExtensions
{
    private static readonly string[] PlayerColors = ["blue", "red", "green", "yellow", "fuchsia", "cyan", "darkorange"];

    public static string ToMarkup(this Player p, Game game) => $"[{PlayerColors[Array.IndexOf(game.Players, p)]}]P{p.Id}[/]";

    public static string ToMarkup(this BuildingColor c) => c switch { BuildingColor.Blue => "blue", BuildingColor.Green => "green", BuildingColor.Red => "red", BuildingColor.Yellow => "yellow", _ => "fuchsia" };

    public static string ToMarkup(this BuildingCard c) => $"[[[{c.Color.ToMarkup()}]{c.Description}[/] [bold]{c.Cost}[/]]]";

    public static string ToIcon(this CharacterType t) =>
        t == CharacterType.Known.Assassin ? ":skull:" :
        t == CharacterType.Known.Thief ? ":ninja:" :
        t == CharacterType.Known.Magician ? ":sparkles:" :
        t == CharacterType.Known.King ? ":crown:" :
        t == CharacterType.Known.Preacher ? ":folded_hands:" :
        t == CharacterType.Known.Merchant ? ":money_bag:" :
        t == CharacterType.Known.Architect ? ":classical_building:" :
        t == CharacterType.Known.Condottiero ? ":crossed_swords:" : "";

    public static string ToMarkup(this Character c) => $"[white]{c.Type.ToIcon()} {c.Description}[/]";
}
