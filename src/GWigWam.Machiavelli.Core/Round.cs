using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("Round {Number}")]
public class Round(Game game, int number)
{
    public event Action? BeforeCharacterPicks;
    public event Action<Player, Character>? OnPlayerTurn;
    public event Action<Player>? OnGetGoldAction;
    public event Action<Player, BuildingCardInstance[]>? OnGetCardsAction;
    public event Action<Player, BuildingColor, int>? OnGetBuildingsGoldAction;
    public event Action<Player, BuildingCardInstance>? OnBuild;
    public event Action<Player, Character>? OnAssassinateAction;
    public event Action<Player, Character>? OnRobAction;
    public event Action<Player, Player>? OnMagicianSwapWithPlayerAction;
    public event Action<Player, int>? OnMagicianSwapWithDeckAction;
    public event Action<Player>? OnClaimKingship;
    public event Action<Player>? OnMerchantGetExtraGoldAction;
    public event Action<Player, BuildingCardInstance[]>? OnArchitectGetBuildingCardsAction;
    public event Action<Player, Player, BuildingCardInstance>? OnCondottieroDestroyBuildingAction;

    public int Number { get; } = number;

    public Character? ClosedCharacter { get; private set; }
    public Character[]? OpenCharacters { get; private set; }

    private Dictionary<Player, Character> _PlayerPicks = [];
    public IReadOnlyDictionary<Player, Character> PlayerPick => _PlayerPicks;
    public Dictionary<Character, Player> CharacterPlayerMap => _PlayerPicks.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public Character? Assassinated { get; private set; }
    public Character? Robbed { get; private set; }

    public Player RoundKing { get; } = game.ActingKing;
    public Player[] PickOrder { get; private set; } = [];

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

        BeforeCharacterPicks?.Invoke();
        var kingIx = Array.IndexOf(game.Players, RoundKing);
        PickOrder = [.. Enumerable.Range(0, game.NoPlayers).Select(i => (i + kingIx) % game.NoPlayers).Select(ix => game.Players[ix])];
        foreach (var (player, turn) in PickOrder.Select((p, ix) => (p, ix)))
        {
            if (game.NoPlayers == 7 && turn == 6)
            {
                characters.Add(ClosedCharacter!); // 7th player picks from remaining card in deck + closed card
                ClosedCharacter = null;
            }

            var pick = game.Controllers[player].PickCharacter(this, characters, turn);
            characters.Remove(pick);
            _PlayerPicks[player] = pick;
        }
    }

    public void Play()
    {
        Player? nextKing = null;
        Action<Player, PlayerController>[] turns = [RunAssassinTurn, RunThiefTurn, RunMagicianTurn, RunKingTurn, RunPreacherTurn, RunMerchantTurn, RunArchitectTurn, RunCondottieroTurn];
        for (int i = 0; i < 8; i++)
        {
            var pick = game.Characters[i];
            var player = CharacterPlayerMap.TryGetValue(pick, out var fnd) ? fnd : null;
            if (player != null)
            {
                OnPlayerTurn?.Invoke(player, pick);
                if (pick != Assassinated)
                {
                    if (pick == Robbed)
                    {
                        var thiefPlayer = PlayerPick.First(kvp => kvp.Value.Type == CharacterType.Known.Thief).Key;
                        thiefPlayer.Gold += player.Gold;
                        player.Gold = 0;
                    }

                    var controller = game.Controllers[player];
                    turns[i](player, controller);
                }
                else if (pick.Type == CharacterType.Known.King) // Assassinated should still be acting king next round
                {
                    nextKing = player;
                }
            }
        }

        game.ActingKing = nextKing ?? game.ActingKing;
    }

    private Action GetGetGoldAction(Player player) => () =>
    {
        player.Gold += 2;
        OnGetGoldAction?.Invoke(player);
    };

    private Action GetGetCardsAction(Player player) => () =>
    {
        var noCards = Gameplay.GetPlayerNoCardsToDraw(player);
        var noToPick = Gameplay.GetPlayerNoCardsToPick(player, noCards);
        var cards = game.Deck.Draw(noCards).ToArray();
        var picked = game.Controllers[player].PickBuildingCards(cards, noToPick).Take(noToPick).ToArray();
        player.Hand.AddRange(picked);
        OnGetCardsAction?.Invoke(player, picked);
    };

    private Action GetGetBuildingsGoldAction(Player player, BuildingColor color) => () =>
    {
        if (Gameplay.CalcBuildingIncome(player.City, color) is var g and > 0)
        {
            player.Gold += g;
            OnGetBuildingsGoldAction?.Invoke(player, color, g);
        }
    };

    private Func<BuildingCardInstance, bool> GetBuildAction(Player player) => (BuildingCardInstance card) =>
    {
        if (player.Hand.Contains(card) && player.Gold >= card.Card.Cost)
        {
            player.Gold -= card.Card.Cost;
            player.Hand.Remove(card);
            player.City.Add(card);
            OnBuild?.Invoke(player, card);
            if (player.City.Count == 8) // Game ends when one player has 8 buildings
            {
                game.Finished.Add(player);
            }
            return true;
        }
        return false;
    };

    private void RunAssassinTurn(Player player, PlayerController controller)
    {
        void assassinate(Character target)
        {
            Assassinated = target;
            OnAssassinateAction?.Invoke(player, Assassinated);
        }
        controller.PlayAssassin(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), assassinate));
    }

    private void RunThiefTurn(Player player, PlayerController controller)
    {
        void rob(Character target)
        {
            if (target.Type != CharacterType.Known.Assassin && target != Assassinated)
            {
                Robbed = target;
                OnRobAction?.Invoke(player, Robbed);
            }
        }
        controller.PlayThief(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), rob));
    }

    private void RunMagicianTurn(Player player, PlayerController controller)
    {
        void swapWithPlayer(Player other)
        {
            BuildingCardInstance[] hSelf = [.. player.Hand], hOther = [.. other.Hand];
            player.Hand.Clear();
            player.Hand.AddRange(hOther);
            other.Hand.Clear();
            other.Hand.AddRange(hSelf);
            OnMagicianSwapWithPlayerAction?.Invoke(player, other);
        }
        void swapWithDeck(IEnumerable<BuildingCardInstance> toSwap)
        {
            var rem = player.Hand.Intersect(toSwap).ToArray();
            player.Hand.RemoveAll(c => rem.Contains(c));
            game.Deck.Discard(rem);
            var drawn = game.Deck.Draw(rem.Length).ToArray();
            player.Hand.AddRange(drawn);
            OnMagicianSwapWithDeckAction?.Invoke(player, drawn.Length);
        }
        controller.PlayMagician(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), swapWithPlayer, swapWithDeck));
    }

    private void RunKingTurn(Player player, PlayerController controller)
    {
        game.ActingKing = player;
        OnClaimKingship?.Invoke(player);
        controller.PlayKing(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), GetGetBuildingsGoldAction(player, BuildingColor.Yellow)));
    }

    private void RunPreacherTurn(Player player, PlayerController controller)
    {
        controller.PlayPreacher(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), GetGetBuildingsGoldAction(player, BuildingColor.Blue)));
    }

    private void RunMerchantTurn(Player player, PlayerController controller)
    {
        void getExtraGold()
        {
            player.Gold += 1;
            OnMerchantGetExtraGoldAction?.Invoke(player);
        }
        controller.PlayMerchant(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), GetGetBuildingsGoldAction(player, BuildingColor.Green), getExtraGold));
    }

    private void RunArchitectTurn(Player player, PlayerController controller)
    {
        void getTwoBuildingCards()
        {
            var cards = game.Deck.Draw(2).ToArray();
            player.Hand.AddRange(cards);
            OnArchitectGetBuildingCardsAction?.Invoke(player, cards);
        }
        controller.PlayArchitect(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), getTwoBuildingCards));
    }

    private void RunCondottieroTurn(Player player, PlayerController controller)
    {
        bool destroyBuilding(BuildingCardInstance building)
        {
            var target = game.Players.First(p => p.City.Contains(building));
            var destCost = building.Card.Cost - 1;
            if (player.Gold >= destCost && PlayerPick[target].Type != CharacterType.Known.Preacher)
            {
                player.Gold -= destCost;
                target.City.Remove(building);
                game.Deck.Discard(building);
                OnCondottieroDestroyBuildingAction?.Invoke(player, target, building);
                return true;
            }
            return false;
        }
        controller.PlayCondottiero(this, new(GetGetGoldAction(player), GetGetCardsAction(player), GetBuildAction(player), GetGetBuildingsGoldAction(player, BuildingColor.Red), destroyBuilding));
    }
}
