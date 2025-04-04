
using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("Player {Id}; {Gold} Gold, {Hand.Count} Cards, {City.Count} Built")]
public class Player(Game game, int id)
{
    public int Id => id;

    public int Gold { get; set; } = 0;

    public List<BuildingCardInstance> Hand { get; } = [];
    public List<BuildingCardInstance> City { get; } = [];

    public void Setup(IEnumerable<BuildingCardInstance> cards, int startingGold)
    {
        Hand.AddRange(cards);
        Gold = startingGold;
    }
}
