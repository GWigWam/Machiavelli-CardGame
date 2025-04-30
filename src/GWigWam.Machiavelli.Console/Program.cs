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

AnsiConsole.Prompt(new SelectionPrompt<(string txt, Action act)>()
    .AddChoices(
        ("Single game", () => ConsoleGame.Run(resFactory)))
    .UseConverter(t => t.txt))
    .act();
