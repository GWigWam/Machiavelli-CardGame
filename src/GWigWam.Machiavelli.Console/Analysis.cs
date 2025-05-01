namespace GWigWam.Machiavelli.Console;
public static class Analysis
{
    public static void Run(Func<Resources> resFactory)
    {
        var nog = AnsiConsole.Prompt(new TextPrompt<int>("No games:"));

        var stats = new GameStats[nog];
        AnsiConsole.Progress()
            .Start(ctx => {
                var task = ctx.AddTask("Simulate games", maxValue: nog);
                Parallel.For(0, nog, gix => {
                    var noPlayers = Random.Shared.Next(3, 8);
                    var (deck, chars) = resFactory();
                    var game = new Game(deck, chars, noPlayers);
                    game.Setup();
                    game.Controllers = game.Players.Select(p => (p, c: new AiPlayerController(game, p))).ToDictionary(t => t.p, t => (PlayerController)t.c);
                    
                    while (!game.NextRound()) { }

                    stats[gix] = new() {
                        Game = game
                    };

                    task.Value++;
                });
                task.Value = nog;
        });
    }

    private class GameStats
    {
        public Game Game { get; set; }
    }
}
