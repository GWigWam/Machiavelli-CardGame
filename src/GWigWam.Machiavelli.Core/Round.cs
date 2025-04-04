namespace GWigWam.Machiavelli.Core;
public class Round(Game game)
{
    public Character? ClosedCharacter { get; private set; }
    public Character[]? OpenCharacters { get; private set; }

    public Character[] Picks { get; } = new Character[game.NoPlayers];

    public CharacterType? Assassinated { get; private set; }

    public void DistributeCharacters()
    {
        var noOpenCharacters = game.NoPlayers switch {
            >= 6 => 0,
            _ => 6 - game.NoPlayers
        };

        var characters = new List<Character>(game.Characters);
        ClosedCharacter = characters.RemoveRandomItem();
        while (true)
        {
            OpenCharacters = [.. characters.RemoveRandomItems(noOpenCharacters)];
            if (!OpenCharacters.Any(oc => oc.Type == CharacterType.Known.King)) // King may not be among open cards
            {
                break;
            }
            else
            {
                characters.AddRange(OpenCharacters);
            }
        }

        var kingIx = Array.IndexOf(game.Players, game.ActingKing);
        for (int i = 0; i < game.NoPlayers; i++)
        {
            var c = (i + kingIx) % game.NoPlayers;
            var cur = game.Players[c];

            var pick = game.Controllers[cur].PickCharacter(characters, i);
            characters.Remove(pick);
            Picks[c] = pick;
        }
    }

    public void Play()
    {
        if (ClosedCharacter == null) throw new InvalidOperationException($"Cannot run {nameof(Play)} before {nameof(DistributeCharacters)}");

        Action<Player, PlayerController>[] turns = [RunAssassinTurn, RunThiefTurn, RunMagicianTurn, RunKingTurn, RunPreacherTurn, RunMerchantTurn, RunArchitectTurn, RunCondottieroTurn];
        for (int i = 0; i < 8; i++)
        {
            (Character, Player)? curTurn = Array.FindIndex(Picks, p => p.Type.Id == i + 1) is int ix and >= 0 ? (Picks[ix], game.Players[ix]) : null;
            if (curTurn is (var pick, var player))
            {
                if (pick.Type != Assassinated)
                {
                    var controller = game.Controllers[player];
                    turns[i](player, controller);
                }
            }
        }
    }

    private Action GetGetGoldAction(Player player) => () => player.Gold += 2;
    private Action GetGetCardsAction(Player player) => () =>
    {
        var noCards = player.City.Any(c => c.Card.Id == "P_Observatory" /*TODO: consts? event?*/) ? 3 : 2;
        var noToPick = player.City.Any(c => c.Card.Id == "P_Library" /*TODO: consts? event?*/) ? noCards /* Rules say 'keep both', unclear what this means in conjunction with Observatory. Executive descision: keep all. */ : 1;
        var cards = game.Deck.Draw(noCards).ToArray();
        var picked = game.Controllers[player].PickBuildingCards(cards, noToPick).Take(noToPick).ToArray();
        player.Hand.AddRange(picked);
    };

    private void RunAssassinTurn(Player player, PlayerController controller)
    {
        void assassinate(CharacterType type) => Assassinated = type;
        controller.PlayAssassin(this, new(GetGetGoldAction(player), GetGetCardsAction(player), assassinate));
    }

    private void RunThiefTurn(Player player, PlayerController controller) {
    
    }

    private void RunMagicianTurn(Player player, PlayerController controller) {
    
    }

    private void RunKingTurn(Player player, PlayerController controller) {
    
    }

    private void RunPreacherTurn(Player player, PlayerController controller) {
    
    }

    private void RunMerchantTurn(Player player, PlayerController controller) {
    
    }

    private void RunArchitectTurn(Player player, PlayerController controller) {
    
    }

    private void RunCondottieroTurn(Player player, PlayerController controller) { 
    
    }
}
