﻿namespace GWigWam.Machiavelli.Core;

public record BuildingCard(string Id, string Description, BuildingColor Color, int Cost, int Count)
{
    public BuildingCardInstance[] Instantiate()
        => [.. Enumerable.Range(0, Count).Select(i => new BuildingCardInstance(this, i))];

    public static implicit operator BuildingCard(BuildingCardInstance inst) => inst.Card;
}

public record BuildingCardInstance(BuildingCard Card, int Id);

public enum BuildingColor : byte
{
    Blue    = 0b_00000001,
    Green   = 0b_00000010,
    Red     = 0b_00000100,
    Yellow  = 0b_00001000,
    Purple  = 0b_00010000,

    None    = 0b_00000000,
    All     = Blue | Green | Red | Yellow | Purple,
}

public static class BuildingCardIds
{
    public const string Observatory = "P_Observatory";
    public const string Library = "P_Library";
    public const string School = "P_School";
    public const string CourtOfWonders = "P_CourtOfWonders";
    public const string DragonGate = "P_DragonGate";
    public const string University = "P_University";
}
