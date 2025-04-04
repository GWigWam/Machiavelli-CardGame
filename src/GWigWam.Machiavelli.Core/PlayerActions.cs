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

public class AssassinActions(Action getGold, Action getCards, Action<CharacterType> assassinate) : BasePlayerActions(getGold, getCards)
{
    public Action<CharacterType> Assassinate => assassinate;
}
