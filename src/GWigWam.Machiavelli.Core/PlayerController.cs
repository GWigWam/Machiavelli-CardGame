namespace GWigWam.Machiavelli.Core;
public abstract class PlayerController(Player player)
{
    public abstract Character PickCharacter(List<Character> characters, int turn);
}
