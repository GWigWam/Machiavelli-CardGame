namespace GWigWam.Machiavelli.Core;
public class Game
{
    public int NoPlayers => Players.Length;

    public Player[] Players { get; }
    public Player ActingKing { get; private set; }

    public Deck Deck { get; }

    public Game(Deck deck, int numOfPlayers)
    {
        Deck = deck;
        Players = [.. Enumerable.Range(0, numOfPlayers).Select(i => new Player(this, i + 1))];
        ActingKing = Players.RandomItem();
    }

    public void Setup()
    {
        const int startingGold = 2;
        const int startingHand = 4;

        Deck.Shuffle();

        foreach (var player in Players)
        {
            var cards = Deck.Draw(startingHand);
            player.Setup(cards, startingGold);
        }
    }
}
