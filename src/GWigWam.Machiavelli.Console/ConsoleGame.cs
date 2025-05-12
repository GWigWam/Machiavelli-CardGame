namespace GWigWam.Machiavelli.Console;
public static class ConsoleGame
{
    public static void Run(Func<Resources> resFactory, AiPlayerController.StrategyValues? strat = null)
    {
        var human = YNPrompt("Include human player?");
        for (int g = 1; true; g++)
        {
            var noPlayers = AnsiConsole.Prompt(new TextPrompt<int>("Number of players: ").Validate(i => i > 2 && i <= 7).DefaultValue(4));
            AnsiConsole.MarkupLine($" --- Game #{g} ---");

            var (deck, chars) = resFactory();
            var game = new Game(deck, chars, noPlayers);
            game.Setup();
            var controllerDict = game.Players.Select(p => (p, c: new AiPlayerController(game, p, strat))).ToDictionary(t => t.p, t => (PlayerController)t.c);
            if (human)
            {
                controllerDict[game.Players[0]] = new ConsolePlayerController(game, game.Players[0]);
            }
            game.Controllers = controllerDict;

            SubscribeConsoleOutpToGame(game);
            while (!game.NextRound()) { }

            if (!YNPrompt("Play again?")) { break; }
        }
    }

    public static void SubscribeConsoleOutpToGame(Game game)
    {
        void sumrPlayer(int ix, Player p)
            => AnsiConsole.MarkupLine($"{(game.ActingKing == p ? " :crown:" : " :bust_in_silhouette:")} {p.ToMarkup(game)} {p.Gold}:coin: {p.Hand.Count}:flower_playing_cards: | [{(p.City.Count >= 7 ? "orangered1": "default")}]{p.City.Count}[/]/{p.Score:D2}p: {string.Join(" ", p.City.Select(i => i.Card.ToMarkup()))}");

        void sumrAllPlayers()
        {
            for (int px = 0; px < game.Players.Length; px++)
            {
                sumrPlayer(px, game.Players[px]);
            }
        }

        game.OnNewRound += r => {
            AnsiConsole.MarkupLine($"\nRound {r.Number}");
            sumrAllPlayers();

            r.BeforeCharacterPicks += () => AnsiConsole.MarkupLine($"Unavailable cards | Open: {string.Join(" ", r.OpenCharacters!.Select(c => $"[[{c.ToMarkup()}]]"))} | Closed: [[[white]:flower_playing_cards:???[/]]]");
        };

        game.OnRoundStart += r => {
            r.OnPlayerTurn += (p, c) => AnsiConsole.MarkupLine($"Turn {c.ToMarkup()} {p.ToMarkup(game)}");
            r.OnGetGoldAction += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +2:coin: ({p.Gold}:coin:)");
            r.OnGetCardsAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
            r.OnGetBuildingsGoldAction += (p, c, g) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +{g}:coin: ({c} buildings) ({p.Gold}:coin:)");
            r.OnBuild += (p, b) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} builds {b.Card.ToMarkup()} ({p.Gold}:coin: {p.Hand.Count}:flower_playing_cards: left)");

            r.OnAssassinateAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} assassinates {c.ToMarkup()}");
            r.OnRobAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} robs {c.ToMarkup()}");
            r.OnMagicianSwapWithPlayerAction += (pSelf, pOther) => AnsiConsole.MarkupLine($"{pSelf.ToMarkup(game)} swaps hand cards with {pOther.ToMarkup(game)}");
            r.OnMagicianSwapWithDeckAction += (p, cards) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} swaps {cards} with deck");
            r.OnClaimKingship += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} is the new king!");
            r.OnMerchantGetExtraGoldAction += p => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} receives +1:coin: extra ({p.Gold}:coin:)");
            r.OnArchitectGetBuildingCardsAction += (p, c) => AnsiConsole.MarkupLine($"{p.ToMarkup(game)} draws +{c.Length}:flower_playing_cards: ({p.Hand.Count}:flower_playing_cards:)");
            r.OnCondottieroDestroyBuildingAction += (pSelf, pOther, b) => AnsiConsole.MarkupLine($"{pSelf.ToMarkup(game)} destroys {pOther.ToMarkup(game)}'s {b.Card.ToMarkup()} :fire:");
        };

        game.GameOver += standings => {
            AnsiConsole.MarkupLine($"\n[red]Game over![/]");
            sumrAllPlayers();

            foreach (var (p, ix) in standings.Select((t, ix) => (t, ix)))
            {
                AnsiConsole.MarkupLine($"#{ix+1} {p.ToMarkup(game)} Score: [bold white]{p.Score}[/] (Buildings: {p.CityScore}{(game.Finished.Contains(p) ? game.Finished.First() == p ? " + 4 (finished first)" : " + 2 (finished later)" : "")}{(p.HasAllColorsBonus ? " + 3 (colors bonus)" : "")})");
            }
        };
    }

    private static bool YNPrompt(string prompt) => AnsiConsole.Prompt(new TextPrompt<bool>(prompt).AddChoices([true, false]).DefaultValue(true).WithConverter(b => b ? "y" : "n"));
}
