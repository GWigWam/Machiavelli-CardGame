namespace GWigWam.Machiavelli.Core;
public abstract class PlayerController
{
    public abstract Character PickCharacter(List<Character> characters, int turn);

    public abstract IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count);

    public abstract void PlayAssassin(Round round, AssassinActions actions);
    public abstract void PlayThief(Round round, ThiefActions actions);
    public abstract void PlayMagician(Round round, MagicianActions actions);
    public abstract void PlayKing(Round round, KingActions actions);
    public abstract void PlayPreacher(Round round, PreacherActions actions);
    public abstract void PlayMerchant(Round round, MerchantActions actions);
    public abstract void PlayArchitect(Round round, ArchitectActions actions);
    public abstract void PlayCondottiero(Round round, CondottieroActions actions);
}
