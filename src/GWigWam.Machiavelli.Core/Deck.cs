namespace GWigWam.Machiavelli.Core;
public class Deck(IEnumerable<BuildingCardInstance> cards)
{
    public readonly BuildingCardInstance[] AllCards = [.. cards];

    private List<BuildingCardInstance> Pile { get; set; } = [.. cards];
    private List<BuildingCardInstance> DiscardPile { get; set; } = [];

    public IEnumerable<BuildingCardInstance> ClosedCards => Pile;
    public bool CanDraw => Pile.Count > 0 || DiscardPile.Count > 0;

    public void Shuffle() => Pile = [.. Pile.OrderBy(_ => Random.Shared.NextDouble())];

    public BuildingCardInstance Draw()
    {
        if (!CanDraw)
        {
            throw new InvalidOperationException($"Out of cards! Check {nameof(Deck)}.{nameof(CanDraw)} before drawing.");
        }

        if (Pile.Count == 0)
        {
            // Reshuffle
            Pile = DiscardPile;
            DiscardPile = [];
            Shuffle();
        }

        var draw = Pile[0];
        Pile.RemoveAt(0);
        return draw;
    }

    public IEnumerable<BuildingCardInstance> Draw(int count)
    {
        while (CanDraw && count > 0)
        {
            yield return Draw();
            count--;
        }
    }

    public void Discard(BuildingCardInstance card) => DiscardPile.Add(card);

    public void Discard(IEnumerable<BuildingCardInstance> cards)
    {
        foreach (var card in cards)
        {
            Discard(card);
        }
    }
}
