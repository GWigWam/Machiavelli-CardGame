namespace GWigWam.Machiavelli.Core;

public record BuildingCard(string Id, string Description, BuildingColor Color, int Cost);

public enum BuildingColor {
    Blue,
    Green,
    Red,
    Yellow,
    Purple
}
