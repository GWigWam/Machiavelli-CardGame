namespace GWigWam.Machiavelli.Core;
public class BasePlayerActions
{
    public bool GotGold { get; private set; }
    public bool GotCards { get; private set; }

    public Action GetGold { get; }
    public Action GetCards { get; }

    public BasePlayerActions(Action getGold, Action getCards)
    {
        GetGold = () =>
        {
            if (!GotCards && !GotGold)
            {
                getGold();
                GotGold = true;
            }
        };

        GetCards = () =>
        {
            if (!GotGold && !GotCards)
            {
                getCards();
                GotCards = true;
            }
        };
    }
}

public class ColoredPlayerActions : BasePlayerActions
{
    public Action GetBuildingsGold { get; }

    public ColoredPlayerActions(Action getGold, Action getCards, Action getBuildingsGold) : base(getGold, getCards)
    {
        GetBuildingsGold = getBuildingsGold;
    }
}

public class AssassinActions(Action getGold, Action getCards, Action<CharacterType> assassinate) : BasePlayerActions(getGold, getCards)
{
    public Action<CharacterType> Assassinate => assassinate;
}

public class ThiefActions(Action getGold, Action getCards, Action<CharacterType> steal) : BasePlayerActions(getGold, getCards)
{
    public Action<CharacterType> Steal => steal;
}

public class MagicianActions : BasePlayerActions
{
    public bool SwappedHandWithPlayer { get; private set; }
    public bool SwappedCardsWithDeck { get; private set; }

    public Action<Player> SwapHandWithPlayer { get; }
    public Action<IEnumerable<BuildingCardInstance>> SwapCardsWithDeck { get; }

    public MagicianActions(Action getGold, Action getCards, Action<Player> swapHandWithPlayer, Action<IEnumerable<BuildingCardInstance>> swapCardsWithDeck) : base(getGold, getCards)
    {
        SwapHandWithPlayer = (player) =>
        {
            if (!SwappedCardsWithDeck && !SwappedHandWithPlayer)
            {
                swapHandWithPlayer(player);
                SwappedHandWithPlayer = true;
            }
        };

        SwapCardsWithDeck = (cards) =>
        {
            if (!SwappedHandWithPlayer && !SwappedCardsWithDeck)
            {
                swapCardsWithDeck(cards);
                SwappedCardsWithDeck = true;
            }
        };
    }
}

public class KingActions(Action getGold, Action getCards, Action getBuildingsGold) : ColoredPlayerActions(getGold, getCards, getBuildingsGold);

public class PreacherActions(Action getGold, Action getCards, Action getBuildingsGold) : ColoredPlayerActions(getGold, getCards, getBuildingsGold);

public class MerchantActions(Action getGold, Action getCards, Action getBuildingsGold, Action getExtraGold) : ColoredPlayerActions(getGold, getCards, getBuildingsGold)
{
    public Action GetExtraGold { get; } = getExtraGold;
}

public class ArchitectActions(Action getGold, Action getCards, Action getTwoBuildingCards) : BasePlayerActions(getGold, getCards)
{
    public Action GetTwoBuildingCards { get; } = getTwoBuildingCards;
}

public class CondottieroActions(Action getGold, Action getCards, Action getBuildingsGold, Action<BuildingCardInstance> destroyBuilding) : ColoredPlayerActions(getGold, getCards, getBuildingsGold)
{
    public Action<BuildingCardInstance> DestroyBuilding { get; } = destroyBuilding;
}
