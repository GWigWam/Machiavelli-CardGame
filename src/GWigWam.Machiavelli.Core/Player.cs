
using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("Player {Id}; {Gold} Gold, {Hand.Count} Cards, {City.Count} Built")]
public class Player(Game game, int id)
{
    public int Id => id;

    public int Gold { get; set; } = 0;

    public List<BuildingCardInstance> Hand { get; } = [];
    public List<BuildingCardInstance> City { get; } = [];

    public bool HasAllColorsBonus {
        get => City.Select(c => c.Card.Color).Distinct().Count() == 5 ||
            (City.Any(b => b.Card.Id == BuildingCardIds.CourtOfWonders) && City.Where(c => c.Card.Id != BuildingCardIds.CourtOfWonders).Select(c => c.Card.Color).Distinct().Count() == 4); // Court of Wonders bonus: counts as any 1 color at the end of the game;
    }

    public int Score => CalcScore();

    public void Setup(IEnumerable<BuildingCardInstance> cards, int startingGold)
    {
        Hand.AddRange(cards);
        Gold = startingGold;
    }

    private int CalcScore()
    {
        const int finishFirstBonus = 4;
        const int finishLaterBonus = 2;
        const int allColorsBonus = 3;

        var fBonus = game.Finished.FirstOrDefault() == this ? finishFirstBonus : game.Finished.Contains(this) ? finishLaterBonus : 0;
        var cBonus = HasAllColorsBonus ? allColorsBonus : 0;
        var city = City.Sum(b => b.Card.Id == BuildingCardIds.DragonGate || b.Card.Id == BuildingCardIds.University ? b.Card.Cost + 2 : b.Card.Cost);

        return fBonus + cBonus + city;
    }
}
