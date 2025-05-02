global using GWigWam.Machiavelli.Console;
global using GWigWam.Machiavelli.Core;
global using GWigWam.Machiavelli.Res;
global using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;
AnsiConsole.Write(new FigletText("GWigWam").Centered().Color(Color.Yellow));
AnsiConsole.Write(new FigletText("Machiavelli").Centered().Color(Color.Red));

var res = new ResourceFiles(Path.Combine(new FileInfo(Environment.ProcessPath!).Directory!.FullName, "../../../../../res"));
var load = res.Load("nl");
var resFactory = await AnsiConsole.Status()
    .StartAsync("Loading...", _ => load);

Action single = () => ConsoleGame.Run(resFactory);
Action<int?> anl = i => Analysis.Run(resFactory, i);

if (Array.FindIndex(args, a => a.TrimStart('-', '/').Equals("anl", StringComparison.OrdinalIgnoreCase)) is int i and >= 0)
{
    var cnt = i + 1 < args.Length && int.TryParse(args[i + 1], out var n) ? n : (int?)null;
    anl(cnt);
}
else
{
    AnsiConsole.Prompt(new SelectionPrompt<(string, Action)>()
        .AddChoices(
            ("Single game", single),
            ("Analysis", () => anl(null)))
        .UseConverter(t => t.Item1))
        .Item2();
}
