
namespace GWigWam.Machiavelli.Core;
public class RandomPlayerController(Game game, Player player) : PlayerController(player)
{
    public override Character PickCharacter(List<Character> characters, int turn)
    {
        var pick = characters.RandomItem();
        return pick;
    }

    public override IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count)
        => cards.OrderBy(static _ => Random.Shared.Next()).Take(count);

    private void PlayGeneric(BasePlayerActions actions)
    {
        if (!game.Deck.CanDraw || Random.Shared.NextDouble() > 0.33)
        {
            actions.GetGold();
        }
        else
        {
            actions.GetCards();
        }

        if (player.Hand.Any())
        {
            var build = player.Hand.RandomItem();
            if (build.Card.Cost <= player.Gold)
            {
                actions.Build(build);
            }
        }
    }

    private void PlayGeneric(ColoredPlayerActions actions)
    {
        actions.GetBuildingsGold();
        PlayGeneric((BasePlayerActions)actions);
    }

    public override void PlayAssassin(Round round, AssassinActions actions)
    {
        var kill = CharacterType.Known.All
            .Where(c => c != CharacterType.Known.Assassin)
            .RandomItem();
        actions.Assassinate(kill);
        PlayGeneric(actions);
    }

    public override void PlayThief(Round round, ThiefActions actions)
    {
        var rob = CharacterType.Known.All
            .Where(c => c != CharacterType.Known.Assassin && c != CharacterType.Known.Thief && c != round.Assassinated)
            .RandomItem();
        actions.Steal(rob);
        PlayGeneric(actions);
    }

    public override void PlayMagician(Round round, MagicianActions actions)
    {
        void swapWithDeck()
        {
            var cnt = Random.Shared.Next(0, player.Hand.Count + 1);
            var toSwap = player.Hand.OrderBy(_ => Random.Shared.Next()).Take(cnt);
            actions.SwapCardsWithDeck(toSwap);
        }

        void swapWithPlayer()
        {
            var target = game.Players.Where(p => p != player).OrderBy(_ => Random.Shared.Next()).First();
            actions.SwapHandWithPlayer(target);
        }

        Action act = Random.Shared.NextDouble() switch
        {
            < 0.33 => swapWithDeck,
            < 0.66 => swapWithPlayer,
            _ => () => { }
        };
        act();
        PlayGeneric(actions);
    }

    public override void PlayKing(Round round, KingActions actions)
    {
        PlayGeneric(actions);
    }

    public override void PlayPreacher(Round round, PreacherActions actions)
    {
        PlayGeneric(actions);
    }

    public override void PlayMerchant(Round round, MerchantActions actions)
    {
        actions.GetExtraGold();
        PlayGeneric(actions);
    }

    public override void PlayArchitect(Round round, ArchitectActions actions)
    {
        actions.GetTwoBuildingCards();
        PlayGeneric(actions);
    }

    public override void PlayCondottiero(Round round, CondottieroActions actions)
    {
        PlayGeneric(actions);
        if (Random.Shared.NextDouble() > 0.5)
        {
            var targets = game.Players
                .Where(p => p != player && round.PlayerPick[p].Type != CharacterType.Known.Preacher)
                .SelectMany(p => p.City)
                .Where(b => b.Card.Cost <= player.Gold)
                .ToArray();
            if (targets.Any())
            {
                actions.DestroyBuilding(targets.RandomItem());
            }
        }
    }
}
