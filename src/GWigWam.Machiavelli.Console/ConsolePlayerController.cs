using GWigWam.Machiavelli.Core;
using Spectre.Console;
using PAct = (string desc, bool once, System.Action action);

namespace GWigWam.Machiavelli.Console;
internal class ConsolePlayerController : PlayerController
{
    private Game Game { get; }
    private Player Self { get; }

    public ConsolePlayerController(Game game, Player player)
    {
        Game = game;
        Self = player;

        Game.OnNewRound += r => {
            r.BeforeCharacterPicks += () => {
                AnsiConsole.MarkupLine($"[[[blue]YOUR STATUS[/]]] Gold: {Self.Gold}:coin: Hand: {string.Join(" ", Self.Hand.Select(c => c.Card.ToMarkup()))}");
                if(Game.ActingKing == Self)
                {
                    AnsiConsole.MarkupLine($"Closed card: {r.ClosedCharacter!.ToMarkup()}");
                }
            };

            r.OnAssassinateAction += (p, c) =>
            {
                if (c == r.PlayerPick[Self])
                {
                    AnsiConsole.MarkupLine($"\t:skull_and_crossbones: [red]You have been assassinated![/]");
                }
            };
            r.OnRobAction += (p, c) =>
            {
                if (c == r.PlayerPick[Self])
                {
                    AnsiConsole.MarkupLine($"\t:money_with_wings: [red]You have been robbed![/]");
                }
            };
            r.OnCondottieroDestroyBuildingAction += (s, t, c) =>
            {
                if(t == Self)
                {
                    AnsiConsole.MarkupLine($"\t:fire: [red]Your building has been destroyed![/]");
                }
            };
        };
    }

    private static string Nth(int n) => n switch { 1 => "1st", 2 => "2nd", var i => $"{i}th" };

    public override Character PickCharacter(Round round, List<Character> characters, int turn) => AnsiConsole.Prompt(
        new SelectionPrompt<Character>()
            .Title($"Pick your character ({Nth(turn + 1)} pick)")
            .AddChoices(characters)
            .WrapAround()
            .UseConverter(c => c.ToMarkup()));

    public override IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count)
    {
        if (cards.Length > 0)
        {
            return AnsiConsole.Prompt(new MultiSelectionPrompt<BuildingCardInstance>()
                .Title($"Pick {count} building cards")
                .AddChoices(cards)
                .WrapAround()
                .UseConverter(c => c.Card.ToMarkup()));
        }
        AnsiConsole.MarkupLine("[red]No cards to choose from[/]");
        return [];
    }

    private void PlayTurn(Round round, BasePlayerActions baseActions, params IEnumerable<PAct> custActions)
    {
        bool stop = false;
        AnsiConsole.MarkupLine($"Round {round.Number}. Your turn! Gold: {Self.Gold}:coin: Hand: {string.Join(" ", Self.Hand.Select(c => c.Card.ToMarkup()))}");

        var drawNoCards = Gameplay.GetPlayerNoCardsToDraw(Self);
        var drawNoToPick = Gameplay.GetPlayerNoCardsToPick(Self, drawNoCards);

        List<PAct> actions =
        [
            ("Get Gold", once: true, () =>
            {
                baseActions.GetGold();
                AnsiConsole.MarkupLine($"You now have {Self.Gold}:coin:");
            }),
            ($"Get Cards (keep {drawNoToPick}/{drawNoCards})", once: true, () =>
            {
                baseActions.GetCards();
                AnsiConsole.MarkupLine($"You now have {Self.Hand.Count}:flower_playing_cards: Hand: {string.Join(" ", Self.Hand.Select(c => c.Card.ToMarkup()))}");
            }),
            ("Build", once: false, () =>
            {
                var choice = AnsiConsole.Prompt(new SelectionPrompt<BuildingCardInstance>()
                    .Title("Building: ")
                    .AddChoices([..Self.Hand, null!])
                    .UseConverter(i => i is BuildingCardInstance bci ? bci.Card.ToMarkup() : "None"));
                if(choice != null)
                {
                    baseActions.Build(choice);
                }
            }),
            .. custActions,
            ("End turn", once: true, () => stop = true),
        ];

        while (!stop)
        {
            var act = AnsiConsole.Prompt(new SelectionPrompt<PAct>()
                .Title("Do action: ")
                .AddChoices(actions)
                .WrapAround()
                .UseConverter(p => p.desc));
            if (act.once) { actions.Remove(act); }
            act.action();
        }
    }

    public override void PlayAssassin(Round round, AssassinActions actions)
    {
        void kill()
        {
            var k = AnsiConsole.Prompt(new SelectionPrompt<Character>()
                .Title("[red]Kill[/]: ")
                .AddChoices(Game.Characters.Where(c => c.Type != CharacterType.Known.Assassin))
                .WrapAround()
                .UseConverter(c => c.ToMarkup()));
            actions.Assassinate(k);
        }
        PlayTurn(round, actions, ("Assassinate", once: true, kill));
    }

    public override void PlayThief(Round round, ThiefActions actions)
    {
        void rob()
        {
            var k = AnsiConsole.Prompt(new SelectionPrompt<Character>()
                .Title("[red]Rob[/]: ")
                .AddChoices(Game.Characters.Where(c => c.Type != CharacterType.Known.Assassin && c.Type != CharacterType.Known.Thief && c != round.Assassinated))
                .WrapAround()
                .UseConverter(c => c.ToMarkup()));
            actions.Steal(k);
        }
        PlayTurn(round, actions, ("Steal", once: true, rob));
    }

    public override void PlayMagician(Round round, MagicianActions actions)
    {
        void printCards() => AnsiConsole.MarkupLine($"You now have {Self.Hand.Count}:flower_playing_cards: Hand: {string.Join(" ", Self.Hand.Select(c => c.Card.ToMarkup()))}");
        void swapPlayer()
        {
            var p = AnsiConsole.Prompt(new SelectionPrompt<Player>()
                .Title("Swap with: ")
                .AddChoices(Game.Players.Where(p => p != Self))
                .UseConverter(p => $"{p.ToMarkup(Game)} ({p.Hand.Count}:flower_playing_cards:)"));
            actions.SwapHandWithPlayer(p);
            printCards();
        }
        void swapDeck()
        {
            var s = AnsiConsole.Prompt(new MultiSelectionPrompt<BuildingCardInstance>()
                .Title("Swap: ")
                .AddChoices(Self.Hand)
                .NotRequired()
                .UseConverter(c => c.Card.ToMarkup()));
            actions.SwapCardsWithDeck(s);
            printCards();
        }
        PlayTurn(round, actions, ("Swap hand with player", once: true, swapPlayer), ("Swap cards with deck", once: true, swapDeck));
    }

    private PAct GetGetExtraGoldAction(BuildingColor color, ColoredPlayerActions a) => ($"Get gold for {color} buildings", once: true, () => a.GetBuildingsGold());

    public override void PlayKing(Round round, KingActions actions) => PlayTurn(round, actions, GetGetExtraGoldAction(BuildingColor.Yellow, actions));
    public override void PlayPreacher(Round round, PreacherActions actions) => PlayTurn(round, actions, GetGetExtraGoldAction(BuildingColor.Blue, actions));
    public override void PlayMerchant(Round round, MerchantActions actions) => PlayTurn(round, actions, GetGetExtraGoldAction(BuildingColor.Green, actions), ("Get extra gold", once: true, actions.GetExtraGold));

    public override void PlayArchitect(Round round, ArchitectActions actions)
    {
        void getTwoCards()
        {
            actions.GetTwoBuildingCards();
            AnsiConsole.MarkupLine($"You now have {Self.Hand.Count}:flower_playing_cards: Hand: {string.Join(" ", Self.Hand.Select(c => c.Card.ToMarkup()))}");
        }
        PlayTurn(round, actions, ("Get two cards", once: true, getTwoCards));
    }

    public override void PlayCondottiero(Round round, CondottieroActions actions)
    {
        void destroy()
        {
            var p = AnsiConsole.Prompt(new SelectionPrompt<Player>()
                .Title("Player: ")
                .AddChoices(Game.Players.Where(p => p != Self && p.City.Count > 0 && round.PlayerPick[p].Type != CharacterType.Known.Preacher).Concat([null!]))
                .UseConverter(p => p != null ? $"{p.ToMarkup(Game)} (Buildings: {p.City.Count}; Score: {p.Score})" : "Cancel"));
            if (p != null)
            {
                var b = AnsiConsole.Prompt(new SelectionPrompt<BuildingCardInstance>()
                    .Title("Building: ")
                    .AddChoices(p.City.Concat([null!]))
                    .UseConverter(c => c != null ? $"{c.Card.ToMarkup()} ({c.Card.Cost - 1}:coin:)" : "Cancel"));
                if (b != null)
                {
                    actions.DestroyBuilding(b);
                }
            }
        }
        PlayTurn(round, actions, GetGetExtraGoldAction(BuildingColor.Red, actions), ("Destroy building", once: false, destroy));
    }
}
