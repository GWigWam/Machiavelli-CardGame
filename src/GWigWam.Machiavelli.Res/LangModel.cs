namespace GWigWam.Machiavelli.Res;
internal class LangModel
{
    public Building[] Buildings { get; set; } = [];
    public Dictionary<string, string> Characters { get; set; } = [];

    public record Building(string Id, string Desc);
}
