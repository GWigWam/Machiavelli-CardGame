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

public class ColoredPlayerActions : BasePlayerActions
{
    public bool GotBuildingsGold { get; private set; }
    public Action GetBuildingsGold { get; }

    public ColoredPlayerActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold) : base(getGold, getCards, build)
    {
        GetBuildingsGold = () =>
        {
            if (!GotBuildingsGold)
            {
                getBuildingsGold();
                GotBuildingsGold = true;
            }
        };
    }
}

public class AssassinActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action<Character> assassinate) : BasePlayerActions(getGold, getCards, build)
{
    public Action<Character> Assassinate => assassinate;
}

public class ThiefActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action<Character> steal) : BasePlayerActions(getGold, getCards, build)
{
    public Action<Character> Steal => steal;
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

public class MerchantActions : ColoredPlayerActions
{
    public bool GotExtraGold { get; private set; }
    public Action GetExtraGold { get; }

    public MerchantActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold, Action getExtraGold) : base(getGold, getCards, build, getBuildingsGold)
    {
        GetExtraGold = () =>
        {
            if (!GotExtraGold)
            {
                getExtraGold();
                GotExtraGold = true;
            }
        };
    }
}

public class ArchitectActions : BasePlayerActions
{
    public bool GotTwoBuildingCards { get; private set; }
    public Action GetTwoBuildingCards { get; }

    public ArchitectActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getTwoBuildingCards) : base(getGold, getCards, build, maxBuiltBuildings: 3)
    {
        GetTwoBuildingCards = () =>
        {
            if (!GotTwoBuildingCards)
            {
                getTwoBuildingCards();
                GotTwoBuildingCards = true;
            }
        };
    }
}

public class CondottieroActions : ColoredPlayerActions
{
    public bool DidDestroyBuilding { get; private set; }
    public Action<BuildingCardInstance> DestroyBuilding { get; }

    public CondottieroActions(Action getGold, Action getCards, Func<BuildingCardInstance, bool> build, Action getBuildingsGold, Func<BuildingCardInstance, bool> destroyBuilding) : base(getGold, getCards, build, getBuildingsGold)
    {
        DestroyBuilding = b =>
        {
            if (!DidDestroyBuilding)
            {
                DidDestroyBuilding = destroyBuilding(b);
            }
        };
    }
}
