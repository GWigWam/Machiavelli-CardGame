namespace GWigWam.Machiavelli.Core;
public abstract class PlayerController(Player player)
{
    public abstract Character PickCharacter(List<Character> characters, int turn);

    public abstract IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count);

    public abstract void PlayAssassin(Round round, AssassinActions actions);
}
