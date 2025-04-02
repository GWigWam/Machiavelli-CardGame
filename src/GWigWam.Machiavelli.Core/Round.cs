namespace GWigWam.Machiavelli.Core;
public class Round(Game game)
{
    public Character? ClosedCharacter { get; private set; }
    public Character[]? OpenCharacters { get; private set; }

    public Character[] Picks { get; } = new Character[game.NoPlayers];

    public void DistributeCharacters()
    {
        var noOpenCharacters = game.NoPlayers switch {
            >= 6 => 0,
            _ => 6 - game.NoPlayers
        };

        var characters = new List<Character>(game.Characters);
        ClosedCharacter = characters.RemoveRandomItem();
        while (true)
        {
            OpenCharacters = [.. characters.RemoveRandomItems(noOpenCharacters)];
            if (!OpenCharacters.Any(oc => oc.Type == CharacterType.Known.King)) // King may not be among open cards
            {
                break;
            }
            else
            {
                characters.AddRange(OpenCharacters);
            }
        }

        var kingIx = Array.IndexOf(game.Players, game.ActingKing);
        for (int i = 0; i < game.NoPlayers; i++)
        {
            var c = (i + kingIx) % game.NoPlayers;
            var cur = game.Players[c];

            var pick = cur.PickCharacter(characters, i);
            characters.Remove(pick);
            Picks[c] = pick;
        }
    }
}
