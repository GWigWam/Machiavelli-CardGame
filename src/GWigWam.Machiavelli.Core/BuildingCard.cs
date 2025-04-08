namespace GWigWam.Machiavelli.Core;

public record BuildingCard(string Id, string Description, BuildingColor Color, int Cost)
{
    public BuildingCardInstance[] Instantiate(int count)
        => [.. Enumerable.Range(0, count).Select(i => new BuildingCardInstance(this, count, i))];
}

public record BuildingCardInstance(BuildingCard Card, int TotalNumber, int Id);

public enum BuildingColor {
    Blue,
    Green,
    Red,
    Yellow,
    Purple
}

public static class BuildingCardIds
{
    public const string Observatory = "P_Observatory";
    public const string Library = "P_Library";
    public const string School = "P_School";
}
