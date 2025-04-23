using System.Diagnostics;

namespace GWigWam.Machiavelli.Core;

[DebuggerDisplay("{Type.Id}: {Description}")]
public class Character(CharacterType type, string description)
{
    public CharacterType Type { get; } = type;
    public string Description { get; } = description;

    public override int GetHashCode() => Type.Id;
    public override bool Equals(object? obj) => obj is Character ch && ch.Type.Id == Type.Id;

    public static implicit operator CharacterType(Character character) => character.Type;
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

    public static class Ids
    {
        public const int Assassin = 1;
        public const int Thief = 2;
        public const int Magician = 3;
        public const int King = 4;
        public const int Preacher = 5;
        public const int Merchant = 6;
        public const int Architect = 7;
        public const int Condottiero = 8;
    }

    public static class Known
    {
        public static CharacterType Assassin { get; } = new(Ids.Assassin, CharacterColor.None);
        public static CharacterType Thief { get; } = new(Ids.Thief, CharacterColor.None);
        public static CharacterType Magician { get; } = new(Ids.Magician, CharacterColor.None);
        public static CharacterType King { get; } = new(Ids.King, CharacterColor.Yellow);
        public static CharacterType Preacher { get; } = new(Ids.Preacher, CharacterColor.Blue);
        public static CharacterType Merchant { get; } = new(Ids.Merchant, CharacterColor.Green);
        public static CharacterType Architect { get; } = new(Ids.Architect, CharacterColor.None);
        public static CharacterType Condottiero { get; } = new(Ids.Condottiero, CharacterColor.Red);

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
