using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("{Type.Id}: {Description}")]
public class Character(CharacterType type, string description)
{
    public CharacterType Type { get; } = type;
    public string Description { get; } = description;

    public override int GetHashCode() => Type.Id;
    public override bool Equals(object? obj) => obj is Character ch && ch.Type.Id == Type.Id;
}

public class CharacterType
{
    public int Id { get; }
    public CharacterColor Color { get; }

    private CharacterType(int id, CharacterColor color)
    {
        Id = id;
        Color = color;
    }

    public static class Known
    {
        public static CharacterType Assassin { get; } = new(1, CharacterColor.None);
        public static CharacterType Thief { get; } = new(2, CharacterColor.None);
        public static CharacterType Magician { get; } = new(3, CharacterColor.None);
        public static CharacterType King { get; } = new(4, CharacterColor.Yellow);
        public static CharacterType Preacher { get; } = new(5, CharacterColor.Blue);
        public static CharacterType Merchant { get; } = new(6, CharacterColor.Green);
        public static CharacterType Architect { get; } = new(7, CharacterColor.None);
        public static CharacterType Condottiero { get; } = new(8, CharacterColor.Red);

        public static CharacterType[] All { get; } = [Assassin, Thief, Magician, King, Preacher, Merchant, Architect, Condottiero];
    }
}

public enum CharacterColor {
    None,
    Blue,
    Green,
    Red,
    Yellow
}
