namespace GWigWam.Machiavelli.Core;
public class Game
{
    public event Action? AfterSetup;
    public event Action<Round>? OnNewRound;
    public event Action<Round>? OnRoundStart;
    public event Action<Round>? AfterRound;
    public event Action? GameOver;

    public int NoPlayers => Players.Length;

    public Player[] Players { get; }
    public Player ActingKing { get; set; }

    private IReadOnlyDictionary<Player, PlayerController>? controllers;
    public IReadOnlyDictionary<Player, PlayerController> Controllers {
        get => controllers ?? throw new InvalidOperationException($"get {nameof(Game)}.{nameof(Controllers)} called, but it has not yet been initialized: do so before starting the game.");
        set => controllers = controllers == null ? value : throw new InvalidOperationException($"Cannot change {nameof(Controllers)} afer it has been set.");
    }

    public Deck Deck { get; }
    public Character[] Characters { get; }

    public List<Player> Finished { get; } = [];

    public List<Round> Rounds { get; } = [];

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

        AfterSetup?.Invoke();
    }

    /// <returns>Boolean indicating game end</returns>
    public bool NextRound()
    {
        var num = Rounds.Count + 1;
        var round = new Round(this, num);
        Rounds.Add(round);
        OnNewRound?.Invoke(round);
        round.DistributeCharacters();
        OnRoundStart?.Invoke(round);
        round.Play();
        AfterRound?.Invoke(round);

        if (Finished.Count > 0)
        {
            GameOver?.Invoke();
            return true;
        }
        return false;
    }
}
