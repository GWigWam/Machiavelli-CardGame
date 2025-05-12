
using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("Player {Id}; {Gold} Gold, {Hand.Count} Cards, {City.Count} Built")]
public class Player(Game game, int id)
{
    public int Id => id;

    public int Gold { get; set; } = 0;

    public List<BuildingCardInstance> Hand { get; } = [];

    private readonly List<BuildingCardInstance> _City = [];
    public IReadOnlyList<BuildingCardInstance> City => _City;

    public bool HasAllColorsBonus { get; private set; }
    public int CityScore { get; private set; } = 0;
    public int Score => CalcScore();

    private bool hasCOW = false;

    public void Setup(IEnumerable<BuildingCardInstance> cards, int startingGold)
    {
        Hand.AddRange(cards);
        Gold = startingGold;
    }

    public void AddBuilding(BuildingCardInstance building)
    {
        _City.Add(building);
        CityScore += Gameplay.GetBuildingPoints(building);
        HasAllColorsBonus = CalcHasColorBonus();
        hasCOW = hasCOW || building.Card.Id == BuildingCardIds.CourtOfWonders;
    }

    public void RemoveBuilding(BuildingCardInstance building)
    {
        if (_City.IndexOf(building) is int ix and >= 0)
        {
            _City.RemoveAt(ix);
            CityScore -= Gameplay.GetBuildingPoints(building);
            HasAllColorsBonus = CalcHasColorBonus();
            hasCOW = hasCOW && building.Card.Id != BuildingCardIds.CourtOfWonders;
        }
    }

    private int CalcScore()
    {
        const int finishFirstBonus = 4;
        const int finishLaterBonus = 2;
        const int allColorsBonus = 3;

        var fBonus = game.Finished.FirstOrDefault() == this ? finishFirstBonus : game.Finished.Contains(this) ? finishLaterBonus : 0;
        var cBonus = HasAllColorsBonus ? allColorsBonus : 0;
        var city = CityScore;

        return fBonus + cBonus + city;
    }

    private bool CalcHasColorBonus() => City.Aggregate(BuildingColor.None, (acc, cur) => acc | cur.Card.Color) == BuildingColor.All ||
        (hasCOW && City.Where(c => c.Card.Id != BuildingCardIds.CourtOfWonders).Select(c => c.Card.Color).Distinct().Count() == 4); // Court of Wonders bonus: counts as any 1 color at the end of the game;
}
