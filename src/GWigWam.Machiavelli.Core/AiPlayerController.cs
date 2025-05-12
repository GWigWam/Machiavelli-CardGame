namespace GWigWam.Machiavelli.Core;
public class AiPlayerController(Game game, Player player, AiPlayerController.StrategyValues? strategy = null) : PlayerController
{
    public StrategyValues Strategy { get; set; } = strategy ?? new();

    private PickPositionInfo PickInfo { get; set; } = null!;

    public override Character PickCharacter(Round round, List<Character> characters, int turn)
    {
        var pi = PickInfo = new(game, round, characters, turn);
        var rgtCnt = pi.RightHand.Length;
        var lftCnt = pi.LeftHand.Length;

        var gold = player.Gold;
        var cardCnt = player.Hand.Count;
        var fstPick = turn == 0;
        var lstPick = turn == game.NoPlayers - 1;
        var is7pg = game.NoPlayers == 7;
        var assassinOpen = round.OpenCharacters!.Any(c => c.Type == CharacterType.Known.Assassin);
        var thiefOpen = round.OpenCharacters!.Any(c => c.Type == CharacterType.Known.Thief);
        var mageOpen = round.OpenCharacters!.Any(c => c.Type == CharacterType.Known.Magician);
        var canPickAssassin = characters.Any(c => c.Type == CharacterType.Known.Assassin);
        var canPickThief = characters.Any(c => c.Type == CharacterType.Known.Thief);
        var competitor = GetCompetitor();

        var assassinMaybePicked = !assassinOpen && !(canPickAssassin && lstPick);
        var canGetRobbed = !thiefOpen && !(canPickThief && lstPick);
        var protectGoldScore = canGetRobbed ? gold * Strategy.Pick_ProtectGoldMult : 0;

        var maxHandDiff = game.Players.Max(p => p.Hand.Count) - game.Players.Min(p => p.Hand.Count);
        var mayGetHandStolen = !mageOpen && maxHandDiff > 0 && game.Players.Max(p => p.Hand.Count) == player.Hand.Count;
        var protectHandScore = mayGetHandStolen ? maxHandDiff * Strategy.Pick_ProtectHandMult : 0;

        // Assassin
        var assassinateCompetitorChance = 1.0 / ((pi.RightHand.Contains(competitor) ? rgtCnt : lftCnt) + 1);
        var assassinateCompetitorScore = assassinateCompetitorChance * Strategy.Pick_Assassin_KillCompetitorMult;
        var assassinateScore = Math.Max(protectHandScore, assassinateCompetitorScore);

        // Thief
        var expStealAssassinMod = assassinMaybePicked ? 0.8 : 1; // When assassin is in game stealing will be less successful since neither he nor his victim can be robbed.
        var expStealRightGold = pi.RightHand.Select(p => p.Gold * (1.0 / (rgtCnt + 1)) * expStealAssassinMod).Sum();
        expStealRightGold += is7pg && !lstPick ? pi.LeftHand.Last().Gold * 0.5 * expStealAssassinMod : 0;
        var expStealLeftGold = pi.LeftHand.Select(p => p.Gold * (1.0 / (lftCnt + 1)) * expStealAssassinMod).Sum();
        var expStealGold = Math.Max(expStealLeftGold, expStealRightGold);
        var expStealGoldScore = expStealGold * Strategy.Pick_Thief_ExpectedGoldMult;
        var protectHandByBuildingBeforeMageScore = player.Hand.Any(c => c.Card.Cost < player.Gold + 2) ? 0.5 * protectHandScore : 0; // Half 'protectHandScore' b/c not whole hand will be stolen

        // Mage
        var needCards = cardCnt == 0 || player.Hand.Sum(c => c.Card.Cost) <= gold + 2;
        var tradeCardsWin = Math.Max(0, game.Players.Where(p => p != player).Max(p => p.Hand.Count) - player.Hand.Count);
        var tradeCardsScore = tradeCardsWin * Strategy.Pick_Mage_CardTradeWinMult * (needCards ? Strategy.Pick_Mage_NeedCardsMult : Strategy.Pick_Mage_DontNeedCardsMult);

        // King / colored
        var extraGoldNeededMult = player.Gold < 4 ? 1 : 0;
        var kingGoldScore = Gameplay.CalcBuildingIncome(player.City, BuildingColor.Yellow) * Strategy.Pick_ExtraGoldMult * extraGoldNeededMult;
        var preacherGoldScore = Gameplay.CalcBuildingIncome(player.City, BuildingColor.Blue) * Strategy.Pick_ExtraGoldMult * extraGoldNeededMult;
        var merchantGoldScore = (Gameplay.CalcBuildingIncome(player.City, BuildingColor.Green) + 1) * Strategy.Pick_ExtraGoldMult * extraGoldNeededMult;
        var condottieroGoldScore = Gameplay.CalcBuildingIncome(player.City, BuildingColor.Red) * Strategy.Pick_ExtraGoldMult * extraGoldNeededMult;

        // Preacher
        var protect1CostBuildingsScore = player.City.Any(c => c.Card.Cost <= 1) ? Strategy.Pick_Preacher_Protect1CostBuildingsScore : 0;

        // Architect
        var architectCanBuildCnt = player.Hand
            .OrderBy(c => c.Card.Cost)
            .Aggregate((b: 0, g: player.Gold + 2), (agg, cur) => agg.g >= cur.Card.Cost ? (agg.b + 1, agg.g - cur.Card.Cost) : agg).b;
        var architectBuildMultipleScore = architectCanBuildCnt > 1 ? (architectCanBuildCnt - 1) * Strategy.Pick_Architect_BuildMultipleMult : 0;
        var architectNeedCardsScore = needCards ? Strategy.Pick_Architect_NeedCardsScore : 0;

        // Condottiero
        var competitorHas1CostBuildingsScore = competitor.City.Any(c => c.Card.Cost <= 1) ? Strategy.Pick_Condottiero_CompetitorHas1CostBuildingsScore : 0;
        var competitorHasColorBonusScore = competitor.HasAllColorsBonus ? Strategy.Pick_Condottiero_CompetitorHasColorBonusScore : 0;
        var destroyScore = competitorHas1CostBuildingsScore + competitorHasColorBonusScore;

        var scores = new Dictionary<CharacterType, double>() {
            [CharacterType.Known.Assassin] = Strategy.Pick_AssassinBaseScore + protectGoldScore + assassinateScore,
            [CharacterType.Known.Thief] = Strategy.Pick_ThiefBaseScore + protectGoldScore + protectHandByBuildingBeforeMageScore + expStealGoldScore,
            [CharacterType.Known.Magician] = Strategy.Pick_MageBaseScore + tradeCardsScore + protectHandScore,
            [CharacterType.Known.King] = Strategy.Pick_KingBaseScore + kingGoldScore,
            [CharacterType.Known.Preacher] = Strategy.Pick_PreacherBaseScore + preacherGoldScore + protect1CostBuildingsScore,
            [CharacterType.Known.Merchant] = Strategy.Pick_MerchantBaseScore + merchantGoldScore,
            [CharacterType.Known.Architect] = Strategy.Pick_ArchitectBaseScore + architectNeedCardsScore + architectBuildMultipleScore,
            [CharacterType.Known.Condottiero] = Strategy.Pick_CondottieroBaseScore + condottieroGoldScore + protect1CostBuildingsScore + destroyScore,
        };

        var pick = scores.OrderByDescending(kvp => kvp.Value).Select(kvp => characters.FirstOrDefault(c => c.Type == kvp.Key)).First(c => c != null)!;
        return pick;
    }

    /// <summary>
    /// Competitor is next-best player, or runner-up to 1st place
    /// </summary>
    private Player GetCompetitor()
    {
        var playersByScore = game.Players.OrderByDescending(p => p.Score).ThenBy(_ => Random.Shared.NextDouble() /* In case of equal score order randomly */).ToArray();
        var ownStanding = Array.IndexOf(playersByScore, player);
        var competitor = ownStanding == 0 ? playersByScore[1] : playersByScore[ownStanding - 1];
        return competitor;
    }

    /// <summary>
    /// Assign a score to a <paramref name="card"/> based on how good it is, higher is better. 
    /// Takes into account cost, special abilities, color bonus, and destructibility.
    /// </summary>
    /// <param name="availableGold">(Expected) Gold available for building. Buildings closer to <paramref name="availableGold"/> in cost will score higher.</param>
    /// <param name="evalColorsInHand">For color bonus calculation: <c>true</c> if colors in hand count as already 'owned'.<br />USE: <c>true</c> when considering what card to pick from deck, <c>false</c> when considering what to build.</param>
    private double ScoreBuilding(BuildingCard card, int availableGold, bool evalColorsInHand = false)
    {
        var diff = availableGold - card.Cost + Strategy.Building_ScoreCalcCostAdj;
        var diffAbs = Math.Abs(diff);

        var score = 0 - (diff > 0 ? diffAbs * Strategy.Building_SurplusPenaltyMult : diffAbs * Strategy.Building_DeficitPenaltyMult);

        score += Strategy.Building_PreferColorScore[card.Color];
        score += Strategy.Building_IdBonusMap.TryGetValue(card.Id, out var bonus) ? bonus : 0;

        var buildings = evalColorsInHand ? player.City.Concat(player.Hand) : player.City;
        var hasColors = buildings.Aggregate(BuildingColor.None, (acc, cur) => acc | cur.Card.Color);
        var isMissingColor = (hasColors & card.Color) == BuildingColor.None;
        score += isMissingColor ? Strategy.Building_MissingColorScore / (5 - byte.PopCount((byte)hasColors)) : 0;

        score -= card.Cost <= 1 ? Strategy.Building_FreeDestructionPenalty : 0;

        return score;
    }

    public override IEnumerable<BuildingCardInstance> PickBuildingCards(BuildingCardInstance[] cards, int count)
        => cards.OrderBy(bi => ScoreBuilding(bi, availableGold: player.Gold, evalColorsInHand: true)).Take(count);

    private void GetGoldOrCards(BasePlayerActions actions, BuildingColor? color = null)
    {
        var expGold = player.Gold + 2 + (color is BuildingColor col ? Gameplay.CalcBuildingIncome(player.City, col) : 0);
        var prioCards = player.Hand.Sum(b => b.Card.Cost) <= expGold;
        (prioCards ? actions.GetCards : actions.GetGold)();
    }

    /// <summary>
    /// Generic build action function for all characters except Architect.<br />
    /// May call <see cref="ColoredPlayerActions.GetBuildingsGold"/> but is not guaranteed to!
    /// </summary>
    private void MaybeBuild(BasePlayerActions actions, BuildingColor? playerColor = null)
    {
        if (player.Hand.Count > 0)
        {
            if (playerColor is BuildingColor col && actions is ColoredPlayerActions cpAct)
            {
                var extraNow = Gameplay.CalcBuildingIncome(player.City, col);
                var buildBeforeExtraGold = player.Hand
                    .Where(c => (c.Card.Color == col || c.Card.Id == BuildingCardIds.School) && c.Card.Cost <= player.Gold)
                    .Select(c => (c, score: ScoreBuilding(c.Card, availableGold: player.Gold)))
                    .OrderByDescending(t => t.score).FirstOrDefault().c;
                var buildAfterExtraGold = player.Hand
                    .Select(c => (c, score: ScoreBuilding(c.Card, availableGold: player.Gold + extraNow)))
                    .OrderByDescending(t => t.score).ThenBy(t => t.c.Card.Cost).ToArray();

                // Choose between 2 options
                //  1: Buy building in character's color, then collect character-color gold; this results in 1 more gold than the other way round.
                //  2: Collect character-color gold first to afford a more expensive (better building); if the built building is of the character's color no gold can be gained from it this turn. 
                if (buildBeforeExtraGold is BuildingCardInstance bb && (buildAfterExtraGold.First(t => t.c == bb).score + Strategy.Building_ImmediateExtraGoldScore) >= buildAfterExtraGold[0].score)
                {
                    actions.Build(bb);
                    cpAct.GetBuildingsGold();
                }
                else if (buildAfterExtraGold.Length > 0)
                {
                    cpAct.GetBuildingsGold();
                    if (buildAfterExtraGold[0].c.Card.Cost <= player.Gold)
                    {
                        actions.Build(buildAfterExtraGold[0].c);
                    }
                }
            }
            else
            {
                var ranked = player.Hand.OrderByDescending(c => ScoreBuilding(c.Card, availableGold: player.Gold)).ToArray();
                if (ranked.FirstOrDefault() is BuildingCardInstance build && build.Card.Cost <= player.Gold)
                {
                    actions.Build(build);
                }
            }
        }
    }

    /// <summary>
    /// Infers which character <paramref name="target"/> player may have picked based on their position.
    /// </summary>
    private IEnumerable<(Character character, double likelyhoodScore)> GuessCharacterLikelihood(IEnumerable<Character> possiblePicks, Player target) => possiblePicks.Select(character =>
    {
        var score = 0.0;
        // Pick frequency
        if (game.Rounds.Count is int rCount and >= 3)
        {
            var pCnt = game.Rounds.Take(rCount - 1).Sum(r => r.PlayerPick[target].Type == character.Type ? 1 : 0);
            var freq = pCnt / (double)(rCount - 1);
            var freqNormal = freq * 8; // Baseline pick freq is 1/8 b/c of the no characters, normalize ~1
            score += freqNormal * Strategy.GuessCharacter_PrevFreqMult;
        }
        // Extra gold
        score += character.Type.Id switch
        {
            CharacterType.Ids.King => Gameplay.CalcBuildingIncome(target.City, BuildingColor.Yellow) * Strategy.GuessCharacter_ExtraGoldMult,
            CharacterType.Ids.Preacher => Gameplay.CalcBuildingIncome(target.City, BuildingColor.Blue) * Strategy.GuessCharacter_ExtraGoldMult,
            CharacterType.Ids.Merchant => (Gameplay.CalcBuildingIncome(target.City, BuildingColor.Green) + 1) * Strategy.GuessCharacter_ExtraGoldMult,
            CharacterType.Ids.Condottiero => Gameplay.CalcBuildingIncome(target.City, BuildingColor.Red) * Strategy.GuessCharacter_ExtraGoldMult,
            _ => 0
        };
        // Frew cards
        score += target.Hand.Count <= 1 ?
            character.Type == CharacterType.Known.Magician ? Strategy.GuessCharacter_FewCardsScore_Mage :
            character.Type == CharacterType.Known.Architect ? Strategy.GuessCharacter_FewCardsScore_Architect : 0 : 0;
        return (character, score);
    });

    public override void PlayAssassin(Round round, AssassinActions actions)
    {
        var competitor = GetCompetitor();
        var rght = PickInfo.RightHand.Contains(competitor);
        var possibleChars = rght ? PickInfo.PossibleRightCharacters : [.. PickInfo.Available.Where(c => c.Type != CharacterType.Known.Assassin)];
        var hitChance = 1.0 / possibleChars.Length;
        var killCompetitorScore = hitChance * Strategy.Assassin_KillCompetitor_100pBaseScore * (competitor.Score > player.Score ? Strategy.Assassin_KillCompetitor_IsAheadMult : 1);

        var cardCnts = game.Players.Select(p => p.Hand.Count).Distinct().Order().ToArray();
        var haveMostCards = cardCnts.Last() == player.Hand.Count && cardCnts.Length > 1;
        var roundHasMage = PickInfo.PossibleCharacters.Any(c => c.Type == CharacterType.Known.Magician);
        var isMageOnDangerousSide = (PickInfo.PossibleRightCharacters.Any(c => c.Type == CharacterType.Known.Magician) ? PickInfo.RightHand : PickInfo.LeftHand).Any(p => p.Hand.Count == cardCnts.First());
        if (haveMostCards && roundHasMage && isMageOnDangerousSide && Strategy.Assassin_KillDangerousMage_Score > killCompetitorScore)
        {
            actions.Assassinate(game.Characters.First(c => c.Type == CharacterType.Known.Magician));
        }
        else
        {
            var scores = GuessCharacterLikelihood(possibleChars, competitor);
            var tgt = scores.Select(t => (t.character, score: t.likelyhoodScore)).OrderByDescending(t => t.score).ThenBy(_ => Random.Shared.Next()).First().character;
            actions.Assassinate(tgt);
        }

        GetGoldOrCards(actions);
        MaybeBuild(actions);
    }

    public override void PlayThief(Round round, ThiefActions actions)
    {
        Player[] possibleP(IEnumerable<Player> inp) => [.. inp.Where(p => p != player && round.PlayerPick[p].Type != CharacterType.Known.Assassin)];
        Character[] possibleC(IEnumerable<Character> inp) => [.. inp.Where(c => c != round.Assassinated && c.Type != CharacterType.Known.Assassin && c.Type != CharacterType.Known.Thief)];
        var playersLeft = possibleP(PickInfo.LeftHand);
        var playersRght = possibleP(PickInfo.RightHand);
        var charsLeft = possibleC(PickInfo.Available);
        var charsRght = possibleC(PickInfo.PossibleRightCharacters);

        var rght = playersLeft.Length == 0 || (playersRght.Length > 0 && playersRght.Average(p => p.Gold) >= playersLeft.Average(p => p.Gold)); 
        var richOrder = possibleP(game.Players)
            .OrderBy(p => playersRght.Contains(p) && rght ? 0 : 1) // Prefer to steal from side with highest avg gold
            .ThenByDescending(p => p.Gold) // Attempt to steal from richest player
            .ThenBy(_ => Random.Shared.NextDouble());
        foreach (var rich in richOrder)
        {
            var possibleChars = playersRght.Contains(rich) ? charsRght : charsLeft;
            possibleChars = game.NoPlayers == 7 && rich == PickInfo.LeftHand.LastOrDefault() ? [.. possibleChars, .. charsRght] : possibleChars; // In 7-player game last player to pick can also choose closed card
            if (possibleChars.Length > 0)
            {
                var scores = GuessCharacterLikelihood(possibleChars, rich);
                var tgt = scores
                    .Select(t => (t.character, score: t.likelyhoodScore))
                    .OrderByDescending(t => t.score).ThenBy(_ => Random.Shared.Next())
                    .First().character;
                actions.Steal(tgt);
                break;
            }
        }

        GetGoldOrCards(actions);
        MaybeBuild(actions);
    }

    public override void PlayMagician(Round round, MagicianActions actions)
    {
        var mostCards = game.Players.Where(p => p != player).OrderByDescending(p => p.Hand.Count).First();
        var tradeCardsRatio = player.Hand.Count > 0 ? (double)mostCards.Hand.Count / player.Hand.Count : double.PositiveInfinity;
        if (tradeCardsRatio >= Strategy.Mage_MinTradeCardsRatio)
        {
            MaybeBuild(actions);
            actions.SwapHandWithPlayer(mostCards);
        }
        else if (player.Hand.Count > 0)
        {
            var avgScore = game.Deck.AllCards.Except(game.Players.SelectMany(p => p.City)).Average(c => ScoreBuilding(c, availableGold: player.Gold + 2));
            var swap = player.Hand
                .Select(card => (card, score: ScoreBuilding(card, availableGold: player.Gold + 2)))
                .Where(t => t.score < avgScore).Select(t => t.card);
            actions.SwapCardsWithDeck(swap);
        }

        GetGoldOrCards(actions);
        MaybeBuild(actions);
    }

    public override void PlayKing(Round round, KingActions actions)
    {
        GetGoldOrCards(actions, BuildingColor.Yellow);
        MaybeBuild(actions, BuildingColor.Yellow);
        actions.GetBuildingsGold();
    }

    public override void PlayPreacher(Round round, PreacherActions actions)
    {
        GetGoldOrCards(actions, BuildingColor.Blue);
        MaybeBuild(actions, BuildingColor.Blue);
        actions.GetBuildingsGold();
    }

    public override void PlayMerchant(Round round, MerchantActions actions)
    {
        actions.GetExtraGold();
        GetGoldOrCards(actions, BuildingColor.Green);
        MaybeBuild(actions, BuildingColor.Green);
        actions.GetBuildingsGold();
    }

    public override void PlayArchitect(Round round, ArchitectActions actions)
    {
        actions.GetTwoBuildingCards();
        GetGoldOrCards(actions);

        var combos = calcBuildCombos() // All possible combinations player can afford to build (1 - 3 cards).
#if DEBUG
            // Distinct is not needed: highest scoring combo is picked, and duplicates will have the same score. It is only included to make debugging easier.
            .DistinctBy(c => c.Select(b => player.Hand.IndexOf(b)).OrderBy(i => i).Aggregate(new HashCode(), (acc, cur) => { acc.Add(cur); return acc; }).ToHashCode())
#endif
            .Select(c =>
            {
                var goldLeft = player.Gold - c.Sum(b => b.Card.Cost);
                var score = c.Average(b => ScoreBuilding(b, b.Card.Cost + goldLeft));
                var finish = player.City.Count + c.Length >= 8;
                var finishPoints = finish ? game.Finished.Any() ? 2 : 4 : 0;
                var gainColorBonus = !player.HasAllColorsBonus && player.City.Select(b => b.Card.Color).Concat(c.Select(b => b.Card.Color)).Aggregate((acc, cur) => acc | cur) == BuildingColor.All ? 3 : 0;
                var points = Gameplay.GetBuildingPoints(c) + finishPoints + gainColorBonus;
                return (combo: c, score, points, finish);
            })
            .ToArray();
        if (combos.Length > 0)
        {
            // Normally the combo with the most 'score' is picked (which takes into account long-term utility)
            // However, the architect can rush to finish to get the 4-points 'finish first' bonus, in such a case combos are ranked by objective point-value not score
            var bestScore = combos.OrderByDescending(t => t.score).First();
            var bestPoints = combos.OrderByDescending(t => t.points).First();
            var bestFinishPoints = combos.Where(t => t.finish).OrderByDescending(t => t.points).FirstOrDefault();
            var best = combos.Any(t => t.finish) ? bestFinishPoints.points >= bestPoints.points ? bestFinishPoints : bestScore : bestScore;
            foreach (var b in best.combo)
            {
                actions.Build(b);
            }
        }

        BuildingCardInstance[][] calcBuildCombos()
        {
            IEnumerable<BuildingCardInstance[]> r(BuildingCardInstance[] curBuild, IEnumerable<BuildingCardInstance> handLeft, int goldLeft)
            {
                var hasDeeper = false;
                foreach (var card in handLeft.Where(c => c.Card.Cost <= goldLeft))
                {
                    foreach (var inr in r([.. curBuild, card], handLeft.Where(c => c != card), goldLeft - card.Card.Cost))
                    {
                        yield return inr;
                        hasDeeper = true; // Do not return the shorter version of this combination which leaves gold unspent.
                    }
                }
                if (!hasDeeper && curBuild.Length > 0 && curBuild.Length <= 3)
                {
                    yield return curBuild;
                }
            }
            return [.. r([], player.Hand, player.Gold)];
        }
    }

    public override void PlayCondottiero(Round round, CondottieroActions actions)
    {
        GetGoldOrCards(actions, BuildingColor.Red);
        MaybeBuild(actions, BuildingColor.Red);
        actions.GetBuildingsGold();

        // AI will only consider destroying opponents buildings after building in it's own city, meaning it will never not built in order to destroy a more expensive building.
        // This is a design choice, in my opinion spending to much on destruction is bad. True, it is a quicker way of closing a score gap with 1 opponent than building, but Machiavelli is not a 2-player game, while you are sabotaging your neighbor a third player may snatch victory.
        var competitor = GetCompetitor();
        foreach (var opponent in game.Players.Where(p => p != player && round.PlayerPick[p].Type != CharacterType.Known.Preacher).OrderBy(p => p == competitor ? 0 : 1).ThenByDescending(p => p.Score).ThenBy(_ => Random.Shared.NextDouble()))
        {
            var isCompetitor = opponent == competitor;
            if (opponent.HasAllColorsBonus)
            {
                var colCheapest = opponent.City
                    .Where(b => opponent.City.Where(bc => bc.Card.Color == b.Card.Color).Count() == 1)
                    .OrderBy(b => b.Card.Cost)
                    .FirstOrDefault();
                if(colCheapest?.Card.Cost - 1 is int colCost && colCost <= player.Gold && colCost < Strategy.Condottiero_DestroyCostMax * Strategy.Condottiero_DestroyCostMax_ColorBonusMult * (isCompetitor ? Strategy.Condottiero_DestroyCostMax_CompetitorMult : 1))
                {
                    actions.DestroyBuilding(colCheapest!);
                    break;
                }
            }
            var cheapest = opponent.City.OrderBy(b => b.Card.Cost).FirstOrDefault();
            if (cheapest?.Card.Cost - 1 is int cost && cost <= player.Gold && cost < Strategy.Condottiero_DestroyCostMax * (isCompetitor ? Strategy.Condottiero_DestroyCostMax_CompetitorMult : 1))
            {
                actions.DestroyBuilding(cheapest!);
                break;
            }
        }
    }

    public class StrategyValues
    {
        public double Pick_AssassinBaseScore { get; set; } = -1;
        public double Pick_ThiefBaseScore { get; set; } = 0;
        public double Pick_MageBaseScore { get; set; } = -0.5;
        public double Pick_KingBaseScore { get; set; } = 0;
        public double Pick_PreacherBaseScore { get; set; } = 0;
        public double Pick_MerchantBaseScore { get; set; } = 0;
        public double Pick_ArchitectBaseScore { get; set; } = 0;
        public double Pick_CondottieroBaseScore { get; set; } = 0;

        public double Pick_ProtectGoldMult { get; set; } = 0.5;
        public double Pick_ProtectHandMult { get; set; } = 0.05;
        public double Pick_ExtraGoldMult { get; set; } = 2;

        public double Pick_Assassin_KillCompetitorMult { get; set; } = 0.3;
        public double Pick_Thief_ExpectedGoldMult { get; set; } = 1;
        public double Pick_Mage_CardTradeWinMult { get; set; } = 1;
        public double Pick_Mage_DontNeedCardsMult { get; set; } = 0.1;
        public double Pick_Mage_NeedCardsMult { get; set; } = 2;
        public double Pick_Preacher_Protect1CostBuildingsScore { get; set; } = 1;
        public double Pick_Architect_NeedCardsScore { get; set; } = 1;
        public double Pick_Architect_BuildMultipleMult { get; set; } = 1;
        public double Pick_Condottiero_CompetitorHas1CostBuildingsScore { get; set; } = 2;
        public double Pick_Condottiero_CompetitorHasColorBonusScore { get; set; } = 2;

        /// <summary>
        /// Value (pos or neg) added to available<->cost difference when scoring buildings.
        /// Negative to incentivize cheap buildings, positive for the reverse.
        /// </summary>
        public double Building_ScoreCalcCostAdj { get; set; } = 0;
        /// <summary>
        /// Score penalty for buildings which cannot be afforded.
        /// Multiplied with the difference, 0 for no effect. 
        /// </summary>
        public double Building_DeficitPenaltyMult { get; set; } = 1;
        /// <summary>
        /// Score penalty for buildings which are cheaper than can be afforded.
        /// Multiplied with the difference, 0 for no effect.
        /// </summary>
        public double Building_SurplusPenaltyMult { get; set; } = 0.5;
        /// <summary>
        /// Score penalty subtracted for buildings which can be destroyed for free (cost <= 1) by the condottiero.
        /// </summary>
        public double Building_FreeDestructionPenalty { get; set; } = 0.75;
        /// <summary>
        /// Bonus score applied to a building if it's color is missing (not in city or hand), bonus is divided by number of colors still missing. 
        /// </summary>
        public double Building_MissingColorScore { get; set; } = 2;
        /// <summary>
        /// Score bonus for buildings which result in an extra gold income this turn.
        /// </summary>
        public double Building_ImmediateExtraGoldScore { get; set; } = 1;

        public Dictionary<BuildingColor, double> Building_PreferColorScore { get; } = new() {
            [BuildingColor.Blue] =   1.25,
            [BuildingColor.Green] =  1.0,
            [BuildingColor.Red] =    1.30,
            [BuildingColor.Yellow] = 1.25,
            [BuildingColor.Purple] = 0.75
        };

        /// <summary>
        /// Flat score bonus applied to given special buildings.
        /// </summary>
        public Dictionary<string, double> Building_IdBonusMap { get; } = new() {
            [BuildingCardIds.Observatory] = 0,      /* draw more cards */
            [BuildingCardIds.Library] = 1.5,        /* keep more cards */
            [BuildingCardIds.School] = 2.5,         /* color income joker */
            [BuildingCardIds.CourtOfWonders] = 0,   /* color bonus joker */
            [BuildingCardIds.DragonGate] = 2.0,     /* +2 points */
            [BuildingCardIds.University] = 2.0,     /* +2 points */
        };

        public double GuessCharacter_PrevFreqMult { get; set; } = 1;
        public double GuessCharacter_ExtraGoldMult { get; set; } = 1;
        public double GuessCharacter_FewCardsScore_Mage { get; set; } = 1.5;
        public double GuessCharacter_FewCardsScore_Architect { get; set; } = 2.0;

        public double Assassin_KillDangerousMage_Score { get; set; } = 0.75;
        public double Assassin_KillCompetitor_100pBaseScore { get; set; } = 2;
        public double Assassin_KillCompetitor_IsAheadMult { get; set; } = 1.5;

        public double Mage_MinTradeCardsRatio { get; set; } = 4.0 / 3.0;

        public double Condottiero_DestroyCostMax { get; set; } = 0.75;
        public double Condottiero_DestroyCostMax_ColorBonusMult { get; set; } = 3;
        public double Condottiero_DestroyCostMax_CompetitorMult { get; set; } = 1.5;
    }

    private class PickPositionInfo
    {
        public Character[] Available { get; }

        public Player[] RightHand { get; }
        public Player[] LeftHand { get; }

        public Character[] PossibleRightCharacters { get; }
        public Character[] PossibleCharacters { get; }

        public PickPositionInfo(Game game, Round round, List<Character> available, int turn)
        {
            Available = [.. available];

            RightHand = [.. round.PickOrder.Take(turn)];
            LeftHand = [.. round.PickOrder.Skip(turn + 1)];

            PossibleRightCharacters = turn == 0 ? [] : [.. game.Characters.Except(available).Except(round.OpenCharacters!)];
            PossibleCharacters = [.. PossibleRightCharacters, .. available];
        }
    }
}
