
namespace GWigWam.Machiavelli.Core;
public class RandomPlayerController(Player player) : PlayerController(player)
{
    public override Character PickCharacter(List<Character> characters, int turn)
    {
        var pick = characters.RandomItem();
        return pick;
    }
}
