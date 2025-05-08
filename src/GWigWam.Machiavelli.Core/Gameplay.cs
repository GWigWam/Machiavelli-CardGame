namespace GWigWam.Machiavelli.Core;
public static class Gameplay
{
    public static int CalcBuildingIncome(IEnumerable<BuildingCardInstance> city, BuildingColor color)
        => city.Where(b => b.Card.Color == color || b.Card.Id == BuildingCardIds.School).Count();

    /// <summary>
    /// Number of cards player may draw default is 2, can be increased to 3 by having an Observatory in the city.
    /// </summary>
    public static int GetPlayerNoCardsToDraw(Player player)
        => player.City.Any(c => c.Card.Id == BuildingCardIds.Observatory) ? 3 : 2;

    /// <summary>
    /// Normally player keeps 1 card of drawn cards, but if they have a Library in their city, they may keep 2 cards. <br />
    /// Edge case: Rules say 'keep both', unclear what this means in conjunction with Observatory. Executive descision: keep all.
    /// </summary>
    /// <param name="nrChooseFrom">Number of drawn cards the player is picking from</param>
    /// <returns></returns>
    public static int GetPlayerNoCardsToPick(Player player, int nrChooseFrom)
        => player.City.Any(c => c.Card.Id == BuildingCardIds.Library) ? nrChooseFrom : 1;

    public static int GetBuildingPoints(BuildingCard card)
        => card.Cost + (card.Id == BuildingCardIds.DragonGate || card.Id == BuildingCardIds.University ? 2 : 0);
    public static int GetBuildingPoints(IEnumerable<BuildingCard> cards) => cards.Sum(GetBuildingPoints);
    public static int GetBuildingPoints(IEnumerable<BuildingCardInstance> cards) => cards.Sum(b => GetBuildingPoints(b));
}
