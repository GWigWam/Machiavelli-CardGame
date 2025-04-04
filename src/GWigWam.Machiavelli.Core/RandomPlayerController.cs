
namespace GWigWam.Machiavelli.Core;
public class RandomPlayerController(Player player) : PlayerController(player)
{
    public override Character PickCharacter(List<Character> characters, int turn)
    {
        var pick = characters.RandomItem();
        return pick;
    }

    public override IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count)
        => cards.OrderBy(static _ => Random.Shared.Next()).Take(count);

    private void PlayGeneric(BasePlayerActions actions)
    {
        if (Random.Shared.NextDouble() > 0.5)
        {
            actions.GetGold();
        }
        else
        {
            actions.GetCards();
        }
    }

    public override void PlayAssassin(Round round, AssassinActions actions)
    {
        var kill = CharacterType.Known.All
            .Where(c => c != CharacterType.Known.Assassin)
            .RandomItem();
        actions.Assassinate(kill);
        PlayGeneric(actions);
    }
}
