namespace GWigWam.Machiavelli.Core;
public class BasePlayerActions
{
    public bool GotGold { get; private set; }
    public bool GotCards { get; private set; }
    public int BuildingsBuilt { get; private set; }
    public int MaxBuiltBuildings { get; }

    public Action GetGold { get; }
    public Action GetCards { get; }
    public Action<BuildingCardInstance> Build { get; }

    public BasePlayerActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, int maxBuiltBuildings = 1)
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

        MaxBuiltBuildings = maxBuiltBuildings;
        Build = (BuildingCardInstance card) =>
        {
            if(BuildingsBuilt < MaxBuiltBuildings)
            {
                if (build(card))
                {
                    BuildingsBuilt++;
                }
            }
        };
    }
}

public class ColoredPlayerActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold) : BasePlayerActions(getGold, getCards, build)
{
    public Action GetBuildingsGold { get; } = getBuildingsGold;
}

public class AssassinActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action<CharacterType> assassinate) : BasePlayerActions(getGold, getCards, build)
{
    public Action<CharacterType> Assassinate => assassinate;
}

public class ThiefActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action<CharacterType> steal) : BasePlayerActions(getGold, getCards, build)
{
    public Action<CharacterType> Steal => steal;
}

public class MagicianActions : BasePlayerActions
{
    public bool SwappedHandWithPlayer { get; private set; }
    public bool SwappedCardsWithDeck { get; private set; }

    public Action<Player> SwapHandWithPlayer { get; }
    public Action<IEnumerable<BuildingCardInstance>> SwapCardsWithDeck { get; }

    public MagicianActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action<Player> swapHandWithPlayer, Action<IEnumerable<BuildingCardInstance>> swapCardsWithDeck) : base(getGold, getCards, build)
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

public class KingActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold) : ColoredPlayerActions(getGold, getCards, build, getBuildingsGold);

public class PreacherActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold) : ColoredPlayerActions(getGold, getCards, build, getBuildingsGold);

public class MerchantActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold, Action getExtraGold) : ColoredPlayerActions(getGold, getCards, build, getBuildingsGold)
{
    public Action GetExtraGold { get; } = getExtraGold;
}

public class ArchitectActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getTwoBuildingCards) : BasePlayerActions(getGold, getCards, build, maxBuiltBuildings: 3)
{
    public Action GetTwoBuildingCards { get; } = getTwoBuildingCards;
}

public class CondottieroActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold, Action<BuildingCardInstance> destroyBuilding) : ColoredPlayerActions(getGold, getCards, build, getBuildingsGold)
{
    public Action<BuildingCardInstance> DestroyBuilding { get; } = destroyBuilding;
}
