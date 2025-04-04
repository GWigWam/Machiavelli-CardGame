namespace GWigWam.Machiavelli.Core;
public class Game
{
    public int NoPlayers => Players.Length;

    public Player[] Players { get; }
    public Player ActingKing { get; private set; }

    private IReadOnlyDictionary<Player, PlayerController>? controllers;
    public IReadOnlyDictionary<Player, PlayerController> Controllers {
        get => controllers ?? throw new InvalidOperationException($"get {nameof(Game)}.{nameof(Controllers)} called, but it has not yet been initialized: do so before starting the game.");
        set => controllers = controllers == null ? value : throw new InvalidOperationException($"Cannot change {nameof(Controllers)} afer it has been set.");
    }

    public Deck Deck { get; }
    public Character[] Characters { get; }

    public Game(Deck deck, Character[] characters, int numOfPlayers)
    {
        Deck = deck;
        Characters = characters;
        Players = [.. Enumerable.Range(0, numOfPlayers).Select(i => new Player(this, id: i + 1))];
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
