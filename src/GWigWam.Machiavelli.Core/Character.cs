namespace GWigWam.Machiavelli.Core;

public class Character
{
    public int Id { get; }
    public CharacterColor Color { get; }

    private Character(int id, CharacterColor color)
    {
        Id = id;
        Color = color;
    }

    public static class Known
    {
        public static Character Assassin { get; } = new(1, CharacterColor.None);
        public static Character Thief { get; } = new(2, CharacterColor.None);
        public static Character Magician { get; } = new(3, CharacterColor.None);
        public static Character King { get; } = new(4, CharacterColor.Yellow);
        public static Character Preacher { get; } = new(5, CharacterColor.Blue);
        public static Character Merchant { get; } = new(6, CharacterColor.Green);
        public static Character Architect { get; } = new(7, CharacterColor.None);
        public static Character Condottiero { get; } = new(8, CharacterColor.Red);

        public static Character[] All { get; } = [Assassin, Thief, Magician, King, Preacher, Merchant, Architect, Condottiero];
    }
}

public enum CharacterColor {
    None,
    Blue,
    Green,
    Red,
    Yellow
}
