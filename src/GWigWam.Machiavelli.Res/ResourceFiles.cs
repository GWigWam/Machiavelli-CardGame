using GWigWam.Machiavelli.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GWigWam.Machiavelli.Res;

public class ResourceFiles(string directoryPath)
{
    private readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Func<Resources>> Load(string langCode)
    {
        var lang = await LoadLang(langCode);
        var buildingCards = await LoadBuildings(lang);

        Resources factory()
        {
            var buildings = buildingCards.SelectMany(t => t.Instantiate()).ToArray();
            var deck = new Deck(buildings);

            var chars = CharacterType.Known.All
                .Select(c => new Character(c, lang.Characters.TryGetValue($"{c.Id}", out var trans) ? trans : $"{c.Id}")).ToArray();

            return new(deck, chars);
        }
        return factory;
    }

    private Task<LangModel> LoadLang(string code)
        => ReadJsonFile<LangModel>($"lang_{code}");

    private async Task<BuildingCard[]> LoadBuildings(LangModel lang)
    {
        var json = await ReadJsonFile<JsonArray>("buildings");
        var parsed = json
            .Select(e => e!.AsObject())
            .Select(o => new
            {
                id = o["id"]!.GetValue<string>(),
                cost = o["cost"]!.GetValue<int>(),
                qty = o["qty"]!.GetValue<int>(),
                color = o["color"]!.GetValue<string>() switch
                {
                    "blue" => BuildingColor.Blue,
                    "green" => BuildingColor.Green,
                    "red" => BuildingColor.Red,
                    "yellow" => BuildingColor.Yellow,
                    "purple" => BuildingColor.Purple,
                    var other => throw new Exception($"Unkown color '{other}' in buildings file")
                }
            });

        if (parsed.GroupBy(d => d.id).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault() is string duplicate)
        {
            throw new Exception($"Duplicate building key '{duplicate}'");
        }

        return [.. parsed.Select(m => new BuildingCard(m.id, cardDescOrId(m.id), m.color, m.cost, m.qty))];

        string cardDescOrId(string id)
            => lang.Buildings.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase))?.Desc
                    is string desc ? desc : id;
    }

    private async Task<TModel> ReadJsonFile<TModel>(string name)
    {
        var fileName = $"{name}.json";
        var fullPath = Path.Combine(directoryPath, fileName);
        if (File.Exists(fullPath))
        {
            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var res = await JsonSerializer.DeserializeAsync<TModel>(fs, JsonOptions);
            return res ?? throw new Exception($"Parsed '{name}' content is null");
        }
        throw new Exception($"Could not find file '{fullPath}'");
    }
}
