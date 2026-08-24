using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    public enum YachtAugmentKind
    {
        ScoreReplacement,
        Dice,
        Enhancement,
        Quest,
        ManualAction,
        RandomReplacement
    }

    [Flags]
    public enum YachtAugmentHook
    {
        None = 0,
        OnSelected = 1 << 0,
        BeforeRoll = 1 << 1,
        BeforeScorePreview = 1 << 2,
        AfterScoreCommit = 1 << 3,
        ManualAction = 1 << 4,
        AfterDraft = 1 << 5
    }

    [Serializable]
    public sealed class YachtAugmentDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string Target;
        public YachtAugmentKind Kind;
        public YachtAugmentHook Hooks;
        public string[] Conflicts = Array.Empty<string>();
        public bool IsQuest;
        public bool PhaseOneOnly;
        public bool IsGlobal;

        public YachtAugmentDefinition Clone()
        {
            var clone = (YachtAugmentDefinition)MemberwiseClone();
            clone.Conflicts = (string[])(Conflicts?.Clone() ?? Array.Empty<string>());
            return clone;
        }
    }

    [Serializable]
    public sealed class YachtDraftState
    {
        public bool IsActive;
        public int PlayerIndex = -1;
        public string[] Options = Array.Empty<string>();
        public int[] SelectionCounts = Array.Empty<int>();

        public YachtDraftState Clone() => new()
        {
            IsActive = IsActive,
            PlayerIndex = PlayerIndex,
            Options = (string[])(Options?.Clone() ?? Array.Empty<string>()),
            SelectionCounts = (int[])(SelectionCounts?.Clone() ?? Array.Empty<int>())
        };
    }

    [Serializable]
    public sealed class YachtAugmentPlayerState
    {
        public string[] OwnedIds = Array.Empty<string>();
        public int ExtraTurns;
        public int NoTimeRemaining;
        public bool NoTimeFailed;
        public bool NoTimeRewarded;
        public int StepCategoryIndex;
        public bool StepFailed;
        public bool StepRewarded;
        public bool TableFlipUsed;
        public string RandomBoxAwardId;
        public int TurnsTaken;
        public bool FastSmallScored;
        public bool FastLargeScored;
        public bool FastRewarded;
        public bool HoldoutRewarded;
        public bool CautiousSmallScored;
        public bool CautiousFailed;
        public bool CautiousRewarded;
        public int EveryLittleCount;
        public bool EveryLittleRewarded;
        public int CopycatCount;
        public bool CopycatRewarded;
        public int[] RecordedBaseScores = Array.Empty<int>();
        public bool DoublingRewarded;
        public int NozdormuTargetTurn;
        public bool NozdormuRewarded;
        public int MomentumState;
        public int YachtBankRemainingTurns;
        public int YachtBankBalance;
        public bool YachtBankPayoutPending;
        public bool YachtBankPaid;
        public int PromotionValue;
        public bool PromotionActive;
        public bool PromotionSkipNextGrowth;
        public int EquivalentExchangeUses;
        public int BountyTargetCategory = -1;
        public int BountySuccesses;
        public int BountyScratches;
        public bool BountyRewarded;
        public int DuelRound;
        public bool DuelResolved;
        public int ProphetTurnsRemaining;
        public int[] ProphetTargets = Array.Empty<int>();
        public int GambitState;
        public bool DoubleDownUsed;
        public bool DoubleDownActive;
        public int PiggyBankBalance;
        public bool DiceAlchemyUsed;

        public YachtAugmentPlayerState Clone() => new()
        {
            OwnedIds = (string[])(OwnedIds?.Clone() ?? Array.Empty<string>()),
            ExtraTurns = ExtraTurns,
            NoTimeRemaining = NoTimeRemaining,
            NoTimeFailed = NoTimeFailed,
            NoTimeRewarded = NoTimeRewarded,
            StepCategoryIndex = StepCategoryIndex,
            StepFailed = StepFailed,
            StepRewarded = StepRewarded,
            TableFlipUsed = TableFlipUsed,
            RandomBoxAwardId = RandomBoxAwardId,
            TurnsTaken = TurnsTaken,
            FastSmallScored = FastSmallScored,
            FastLargeScored = FastLargeScored,
            FastRewarded = FastRewarded,
            HoldoutRewarded = HoldoutRewarded,
            CautiousSmallScored = CautiousSmallScored,
            CautiousFailed = CautiousFailed,
            CautiousRewarded = CautiousRewarded,
            EveryLittleCount = EveryLittleCount,
            EveryLittleRewarded = EveryLittleRewarded,
            CopycatCount = CopycatCount,
            CopycatRewarded = CopycatRewarded,
            RecordedBaseScores = (int[])(RecordedBaseScores?.Clone() ?? Array.Empty<int>()),
            DoublingRewarded = DoublingRewarded,
            NozdormuTargetTurn = NozdormuTargetTurn,
            NozdormuRewarded = NozdormuRewarded,
            MomentumState = MomentumState,
            YachtBankRemainingTurns = YachtBankRemainingTurns,
            YachtBankBalance = YachtBankBalance,
            YachtBankPayoutPending = YachtBankPayoutPending,
            YachtBankPaid = YachtBankPaid,
            PromotionValue = PromotionValue,
            PromotionActive = PromotionActive,
            PromotionSkipNextGrowth = PromotionSkipNextGrowth,
            EquivalentExchangeUses = EquivalentExchangeUses,
            BountyTargetCategory = BountyTargetCategory,
            BountySuccesses = BountySuccesses,
            BountyScratches = BountyScratches,
            BountyRewarded = BountyRewarded,
            DuelRound = DuelRound,
            DuelResolved = DuelResolved,
            ProphetTurnsRemaining = ProphetTurnsRemaining,
            ProphetTargets = (int[])(ProphetTargets?.Clone() ?? Array.Empty<int>()),
            GambitState = GambitState,
            DoubleDownUsed = DoubleDownUsed,
            DoubleDownActive = DoubleDownActive,
            PiggyBankBalance = PiggyBankBalance,
            DiceAlchemyUsed = DiceAlchemyUsed
        };
    }

    /// <summary>
    /// M5 대표 증강의 고정 처리 지점을 담당합니다. Unity 표현 계층과 분리되어
    /// 동일한 상태와 명령을 로컬·온라인 권위 구현에서 재사용할 수 있습니다.
    /// </summary>
    public sealed class YachtAugmentRuntime
    {
        public const string LuckySevensId = "lucky-sevens";
        public const string PerfectSquaresId = "perfect-squares";
        public const string GamblerId = "gambler";
        public const string ThreeOfAKindId = "three-of-a-kind";
        public const string TinyHouseId = "tiny-house";
        public const string TwoPairId = "two-pair";
        public const string HeadAndTailId = "head-and-tail";
        public const string EvensId = "evens";
        public const string OddsId = "odds";
        public const string DoubleLargeStraightId = "double-large-straight";
        public const string PrimeCollectionId = "prime-collection";
        public const string DuplexHouseId = "duplex-house";
        public const string MountainId = "mountain";
        public const string HighDiceId = "high-dice";
        public const string SecondChoiceId = "2nd-choice";
        public const string FibonacciId = "fibonacci-numbers";
        public const string ReverseChoiceId = "reverse-choice";
        public const string YachtBankId = "yacht-bank";
        public const string BlackjackId = "blackjack-21";
        public const string FastStraightId = "fast-straight";
        public const string OctahedronId = "8-sided";
        public const string NoTimeToWasteId = "no-time-to-waste";
        public const string StepByStepId = "step-by-step";
        public const string HoldoutId = "holdout";
        public const string CautiousStraightId = "cautious-straight";
        public const string EveryLittleId = "every-little";
        public const string CopycatId = "copycat";
        public const string DoublingId = "doubling";
        public const string NozdormuId = "nozdormu";
        public const string WeightedDiceId = "weighted-dice";
        public const string MomentumId = "momentum";
        public const string GoldenDieId = "golden-die";
        public const string PromotionDieId = "promotion-die";
        public const string CoupleDiceId = "couple-dice";
        public const string SevensDiceId = "sevens-dice";
        public const string TableFlipId = "table-flip";
        public const string EquivalentExchangeId = "equivalent-exchange";
        public const string BountyHunterId = "bounty-hunter";
        public const string DuelId = "duel";
        public const string RandomBoxId = "random-box";
        public const string ProphetId = "prophet";
        public const string GambitId = "gambit";
        public const string DoubleDownId = "double-down";
        public const string PiggyBankId = "piggy-bank";
        public const string DiceAlchemyId = "dice-alchemy";
        public const int StepByStepUpperBonusThreshold = 58;
        public const int DraftOptionCount = 3;

        private static readonly YachtAugmentDefinition[] Definitions =
        {
            Score(LuckySevensId, "럭키 세븐", ScoreCategory.Aces),
            Score(PerfectSquaresId, "퍼펙트 스퀘어", ScoreCategory.Aces),
            Score(GamblerId, "갬블러", ScoreCategory.Choice),
            Score(ThreeOfAKindId, "쓰리 오브 어 카인드", ScoreCategory.FourOfAKind),
            Score(TinyHouseId, "타이니 하우스", ScoreCategory.FullHouse),
            Score(TwoPairId, "투 페어", ScoreCategory.FullHouse),
            Score(HeadAndTailId, "머리와 몸통", ScoreCategory.FullHouse),
            Score(EvensId, "에번스", ScoreCategory.SmallStraight),
            Score(OddsId, "오즈", ScoreCategory.SmallStraight),
            Score(DoubleLargeStraightId, "더블 라지 스트레이트", ScoreCategory.SmallStraight),
            Score(PrimeCollectionId, "프라임 컬렉션", ScoreCategory.LargeStraight),
            Score(DuplexHouseId, "땅콩주택", ScoreCategory.LargeStraight),
            Score(MountainId, "마운틴", ScoreCategory.LargeStraight),
            Score(HighDiceId, "하이 다이스", ScoreCategory.LargeStraight),
            Score(SecondChoiceId, "두 번째 초이스", ScoreCategory.Yacht, true),
            Score(FibonacciId, "피보나치 넘버즈", ScoreCategory.Yacht, true),
            Score(ReverseChoiceId, "리버스 초이스", ScoreCategory.Yacht, true),
            Score(BlackjackId, "블랙잭 21", ScoreCategory.Yacht, true),
            Enhance(YachtBankId, "요트 뱅크"),
            Quest(FastStraightId, "재빠른 스트레이트", true),
            Quest(NoTimeToWasteId, "낭비할 시간 없다"),
            Quest(StepByStepId, "차근차근", true),
            Quest(HoldoutId, "알박기"),
            Quest(CautiousStraightId, "신중한 스트레이트"),
            Quest(EveryLittleId, "티끌 모아 태산"),
            Quest(CopycatId, "카피캣"),
            Quest(DoublingId, "더블링"),
            Quest(NozdormuId, "노즈도르무"),
            Dice(WeightedDiceId, "묵직한 주사위"),
            Enhance(MomentumId, "추진력"),
            Dice(GoldenDieId, "황금 주사위"),
            Dice(OctahedronId, "8면 주사위", new[] { TableFlipId }),
            Dice(PromotionDieId, "프로모션 주사위"),
            Dice(CoupleDiceId, "커플 주사위"),
            Dice(SevensDiceId, "세븐스 다이스"),
            Action(TableFlipId, "판 뒤집기", new[] { OctahedronId }),
            Action(EquivalentExchangeId, "등가교환"),
            Quest(BountyHunterId, "현상금 사냥꾼"),
            Enhance(DuelId, "결투"),
            new YachtAugmentDefinition { Id = RandomBoxId, DisplayName = "랜덤 박스", Description = Describe(RandomBoxId), Kind = YachtAugmentKind.RandomReplacement, Hooks = YachtAugmentHook.OnSelected | YachtAugmentHook.AfterDraft },
            Quest(ProphetId, "예지자"),
            Action(GambitId, "갬빗"),
            Action(DoubleDownId, "더블 다운"),
            Enhance(PiggyBankId, "저금통"),
            Action(DiceAlchemyId, "주사위 연금술")
        };

        private static YachtAugmentDefinition Score(string id, string name, ScoreCategory target, bool phaseOneOnly = false) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Target = target.ToString(),
            Kind = YachtAugmentKind.ScoreReplacement,
            Hooks = YachtAugmentHook.OnSelected | YachtAugmentHook.BeforeScorePreview,
            PhaseOneOnly = phaseOneOnly
        };

        private static YachtAugmentDefinition Quest(string id, string name, bool phaseOneOnly = false) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Target = "Quest",
            Kind = YachtAugmentKind.Quest,
            Hooks = YachtAugmentHook.OnSelected | YachtAugmentHook.AfterScoreCommit,
            IsQuest = true,
            PhaseOneOnly = phaseOneOnly
        };

        private static YachtAugmentDefinition Dice(string id, string name, string[] conflicts = null) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.Dice,
            Hooks = YachtAugmentHook.OnSelected | YachtAugmentHook.BeforeRoll,
            Conflicts = conflicts ?? Array.Empty<string>()
        };

        private static YachtAugmentDefinition Action(string id, string name, string[] conflicts = null) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.ManualAction,
            Hooks = YachtAugmentHook.ManualAction,
            Conflicts = conflicts ?? Array.Empty<string>()
        };

        private static YachtAugmentDefinition Enhance(string id, string name) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.Enhancement,
            Hooks = YachtAugmentHook.OnSelected | YachtAugmentHook.BeforeScorePreview | YachtAugmentHook.AfterScoreCommit
        };

        private static string Describe(string id) => id switch
        {
            LuckySevensId => "Aces를 합 7·17·27이면 15점인 족보로 바꿉니다.",
            PerfectSquaresId => "Aces를 합 9·16·25이면 12점인 족보로 바꿉니다.",
            GamblerId => "Choice를 합 24 이상이면 합계+7점인 족보로 바꿉니다.",
            ThreeOfAKindId => "Four of a Kind를 같은 눈 3개 이상이면 합계 점수인 족보로 바꿉니다.",
            TinyHouseId => "Full House를 1~4만 사용해 완성하면 28점인 족보로 바꿉니다.",
            TwoPairId => "Full House를 서로 다른 두 쌍 또는 포카드면 15점인 족보로 바꿉니다.",
            HeadAndTailId => "Full House를 연속 3개와 같은 눈 2개면 합계+10점인 족보로 바꿉니다.",
            EvensId => "Small Straight를 모든 눈이 2·4·6이면 20점인 족보로 바꿉니다.",
            OddsId => "Small Straight를 모든 눈이 1·3·5·7이면 20점인 족보로 바꿉니다.",
            DoubleLargeStraightId => "Small Straight를 Large Straight 조건 30점으로 바꾸고 상단 기준을 60으로 낮춥니다.",
            PrimeCollectionId => "Large Straight를 모든 눈이 2·3·5·7이고 2·3·5를 포함하면 35점인 족보로 바꿉니다.",
            DuplexHouseId => "Large Straight를 연속한 두 눈의 2+3 Full House면 35점인 족보로 바꿉니다.",
            MountainId => "Large Straight를 2·3·4·5·6이면 40점인 족보로 바꿉니다.",
            HighDiceId => "Large Straight를 4~7만 사용하고 합 26 이상이면 35점인 족보로 바꿉니다.",
            SecondChoiceId => "Yacht를 조건 없이 합계의 절반을 얻는 족보로 바꿉니다.",
            FibonacciId => "Yacht를 1·1·2·3·5이면 25점인 족보로 바꿉니다.",
            ReverseChoiceId => "Yacht를 조건 없이 30-합계 점수로 바꾸며 음수도 허용합니다.",
            BlackjackId => "Yacht를 합계가 21이면 21점인 족보로 바꿉니다.",
            YachtBankId => "3턴 동안 가장 왼쪽 킵 주사위를 점수에서 제외해 최대 15까지 저축하고 다음 내 턴에 받습니다.",
            FastStraightId => "8번째 내 턴까지 두 Straight를 모두 기입하면 +15점입니다.",
            NoTimeToWasteId => "연속 3턴 첫 굴림 직후 기입하면 +15점입니다.",
            StepByStepId => "Aces부터 Sixes까지 순서대로 기입하면 상단 기준 58과 보너스 55를 적용합니다.",
            HoldoutId => "9번째 내 턴 이후 Full House를 기입하면 +7점입니다.",
            CautiousStraightId => "Small Straight 뒤 Large Straight를 기입하면 +7점입니다.",
            EveryLittleId => "점수 기입에 사용한 눈 1을 누적 7개 모으면 +15점입니다.",
            CopycatId => "상대가 쓴 족보를 3회 따라 쓰거나 같은 하단 점수를 따라 쓰면 +10점입니다.",
            DoublingId => "0점이 아닌 같은 기본 점수를 두 번 기입하면 +10점입니다.",
            NozdormuId => "다음 드래프트 전까지 내 턴을 15초로 진행하고 완료하면 +9점입니다.",
            WeightedDiceId => "주사위 하나의 면을 4·4·5·5·6·6으로 바꿉니다.",
            MomentumId => "기본 점수 0점 다음 턴의 양수 기본 점수를 한 번 1.5배로 강화합니다.",
            GoldenDieId => "황금 주사위가 1·2·3이면 최종 점수에 +2점을 더합니다.",
            OctahedronId => "주사위 둘을 1·2·3·4·4·5·5·6의 8면 주사위로 바꿉니다.",
            PromotionDieId => "눈 1로 시작해 내 턴마다 성장하고 눈 6으로 기입하면 일반 주사위가 됩니다.",
            CoupleDiceId => "커플 주사위 둘의 눈이 같으면 최종 점수에 +3점을 더합니다.",
            SevensDiceId => "주사위 둘을 2~7 면으로 바꾸고 눈 7과 확장 Straight를 허용합니다.",
            TableFlipId => "게임당 한 번 비킵 주사위를 굴림 소모 없이 다시 굴립니다.",
            EquivalentExchangeId => "기본 굴림 소진 후 최대 3회, -5점씩 내고 추가 굴림을 합니다.",
            BountyHunterId => "매 턴 무작위 빈 족보를 목표로 3회 기입하면 최대 +15점입니다. 스크래치마다 3점 감소합니다.",
            DuelId => "획득 라운드 점수를 비교해 승리 +10점, 동점 +5점을 받습니다.",
            RandomBoxId => "상단 기준을 58로 낮추고 양쪽 선택 후 퀘스트가 아닌 무작위 증강으로 교체됩니다.",
            ProphetId => "3턴 동안 제시된 숫자와 같은 기본 점수를 기입할 때마다 +7점입니다.",
            GambitId => "한 번 선언해 이번 턴은 일반 주사위 4개, 다음 내 턴은 6개를 사용합니다.",
            DoubleDownId => "9번째 내 턴부터 한 번 기본 점수를 1.5배, 추진력과 함께면 2배로 강화합니다.",
            PiggyBankId => "남은 굴림마다 3을 저축하고 12에 도달할 때마다 +12점을 받습니다.",
            DiceAlchemyId => "게임당 한 번 첫 굴림 후 비킵 주사위 눈을 최저 1까지 1씩 낮춥니다.",
            _ => string.Empty
        };

        public IReadOnlyList<YachtAugmentDefinition> GetDefinitions()
        {
            var result = new YachtAugmentDefinition[Definitions.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Definitions[i].Clone();
            return result;
        }

        public YachtAugmentDefinition FindDefinition(string augmentId)
        {
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].Id, augmentId, StringComparison.Ordinal)) return Definitions[i].Clone();
            return null;
        }

        public void Initialize(YachtGameState state, int playerCount)
        {
            state.Draft = new YachtDraftState { SelectionCounts = new int[playerCount] };
            state.GlobalAugmentIds = Array.Empty<string>();
            state.AugmentPlayers = new YachtAugmentPlayerState[playerCount];
            for (int i = 0; i < playerCount; i++) state.AugmentPlayers[i] = new YachtAugmentPlayerState();
        }

        public bool TryBeginDraft(YachtGameState state, IRandomSource random, out YachtGameEvent gameEvent)
        {
            gameEvent = null;
            if (state.Mode != YachtGameMode.Augmented || !IsDraftRound(state.CurrentRound)) return false;

            int expected = ExpectedSelectionCount(state.CurrentRound);
            int playerIndex = FindNextDraftPlayer(state, expected);
            if (playerIndex < 0) return false;

            state.Draft.IsActive = true;
            state.Draft.PlayerIndex = playerIndex;
            state.Draft.Options = CreateDraftOptions(state, playerIndex, random);
            state.Phase = YachtGamePhase.Draft;
            gameEvent = new YachtGameEvent
            {
                Type = YachtGameEventType.DraftStarted,
                PlayerIndex = playerIndex,
                Message = $"P{playerIndex + 1} 증강 선택"
            };
            return true;
        }

        public bool TrySelectAugment(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            IRandomSource random,
            out YachtGameEvent[] events,
            out YachtCommandErrorCode errorCode,
            out string errorMessage)
        {
            events = Array.Empty<YachtGameEvent>();
            errorCode = YachtCommandErrorCode.None;
            errorMessage = null;
            if (!state.Draft.IsActive || state.Phase != YachtGamePhase.Draft || state.Draft.PlayerIndex != playerIndex)
                return Fail(YachtCommandErrorCode.NotDrafting, "현재 플레이어가 증강을 선택할 차례가 아닙니다.", out errorCode, out errorMessage);
            if (!Contains(state.Draft.Options, augmentId))
                return Fail(YachtCommandErrorCode.AugmentNotOffered, "제시되지 않은 증강입니다.", out errorCode, out errorMessage);
            if (!CanAcquire(state, playerIndex, augmentId))
                return Fail(HasConflict(state.AugmentPlayers[playerIndex], augmentId)
                    ? YachtCommandErrorCode.AugmentConflict
                    : YachtCommandErrorCode.AugmentUnavailable,
                    "이미 보유했거나 충돌하는 증강입니다.", out errorCode, out errorMessage);

            var emitted = new List<YachtGameEvent>();
            ApplyAugment(state, playerIndex, augmentId, emitted, false);
            state.Draft.SelectionCounts[playerIndex]++;
            emitted.Add(new YachtGameEvent
            {
                Type = YachtGameEventType.AugmentSelected,
                PlayerIndex = playerIndex,
                AugmentId = augmentId,
                Message = $"{FindDefinition(augmentId)?.DisplayName ?? augmentId} 선택"
            });

            int expected = ExpectedSelectionCount(state.CurrentRound);
            int nextPlayer = FindNextDraftPlayer(state, expected);
            if (nextPlayer >= 0)
            {
                state.Draft.PlayerIndex = nextPlayer;
                state.Draft.Options = CreateDraftOptions(state, nextPlayer, random);
                emitted.Add(new YachtGameEvent
                {
                    Type = YachtGameEventType.DraftStarted,
                    PlayerIndex = nextPlayer,
                    Message = $"P{nextPlayer + 1} 증강 선택"
                });
            }
            else
            {
                ResolveRandomBoxes(state, random, emitted);
                state.Draft.IsActive = false;
                state.Draft.PlayerIndex = -1;
                state.Draft.Options = Array.Empty<string>();
                state.Phase = YachtGamePhase.TurnReady;
            }

            events = emitted.ToArray();
            return true;
        }

        public bool Owns(YachtGameState state, int playerIndex, string augmentId)
        {
            YachtAugmentDefinition definition = FindDefinition(augmentId);
            if (definition?.IsGlobal == true) return Contains(state?.GlobalAugmentIds, augmentId);
            return state?.AugmentPlayers != null
            && playerIndex >= 0
            && playerIndex < state.AugmentPlayers.Length
            && Contains(state.AugmentPlayers[playerIndex].OwnedIds, augmentId);
        }

        public void ConfigureDice(YachtGameState state, int playerIndex, YachtDieState[] dice)
        {
            if (dice == null) return;
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (player.GambitState == 1 || player.GambitState == 2) return;
            int next = 0;
            AssignDice(state, playerIndex, dice, WeightedDiceId, YachtDieType.Heavy, 1, ref next);
            AssignDice(state, playerIndex, dice, GoldenDieId, YachtDieType.Golden, 1, ref next);
            AssignDice(state, playerIndex, dice, OctahedronId, YachtDieType.Octahedron, 2, ref next);
            if (Owns(state, playerIndex, PromotionDieId) && player.PromotionActive && next < dice.Length)
            {
                dice[next].Type = YachtDieType.Promotion;
                dice[next].PromotionLevel = Math.Max(1, player.PromotionValue);
                dice[next].Value = dice[next].PromotionLevel;
                next++;
            }
            AssignDice(state, playerIndex, dice, CoupleDiceId, YachtDieType.Couple, 2, ref next);
            AssignDice(state, playerIndex, dice, SevensDiceId, YachtDieType.Sevens, 2, ref next);
        }

        public int RollValue(YachtDieState die, IRandomSource random, Func<int> baseRoll)
        {
            int[] faces = die.Type switch
            {
                YachtDieType.Heavy => new[] { 4, 4, 5, 5, 6, 6 },
                YachtDieType.Octahedron => new[] { 1, 2, 3, 4, 4, 5, 5, 6 },
                YachtDieType.Sevens => new[] { 2, 3, 4, 5, 6, 7 },
                _ => null
            };
            if (die.Type == YachtDieType.Promotion) return Math.Max(1, die.PromotionLevel);
            return faces == null ? baseRoll() : faces[random.NextInt(0, faces.Length)];
        }

        public int GetDiceCount(YachtGameState state, int playerIndex, int defaultCount)
        {
            int gambit = state.AugmentPlayers[playerIndex].GambitState;
            return gambit == 1 ? 4 : gambit == 2 ? 6 : defaultCount;
        }

        public string SelectPresetFile(IReadOnlyList<YachtDieState> dice, bool tableFlip)
        {
            if (tableFlip) return $"dice_presets_flip_{dice?.Count ?? 5}.json";
            int octahedrons = 0;
            int normals = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                if (dice[i].Type == YachtDieType.Octahedron) octahedrons++;
                else normals++;
            }
            return octahedrons > 0
                ? $"dice_presets_mixed_{normals}normal_{octahedrons}octa.json"
                : $"dice_presets_normal_{normals}.json";
        }

        public void ModifyScorePreview(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice,
            IDictionary<ScoreCategory, int> scores)
        {
            Dictionary<ScoreCategory, int> calculated = YachtAugmentScoreEngine.CalculateBaseScores(
                GetScoringDice(state, playerIndex, dice),
                state.AugmentPlayers[playerIndex].OwnedIds);
            foreach (KeyValuePair<ScoreCategory, int> pair in calculated) scores[pair.Key] = pair.Value;
        }

        public YachtScoreCandidate[] CreateScoreCandidates(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice)
        {
            IReadOnlyList<YachtDieState> scoringDice = GetScoringDice(state, playerIndex, dice);
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            Dictionary<ScoreCategory, int> baseScores = YachtAugmentScoreEngine.CalculateBaseScores(scoringDice, player.OwnedIds);
            int diceBonus = YachtAugmentScoreEngine.CalculateDiceBonus(scoringDice);
            var result = new YachtScoreCandidate[YachtScoreCalculator.ScorableCategories.Length];
            for (int i = 0; i < result.Length; i++)
            {
                ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                int baseScore = baseScores[category];
                bool momentum = player.MomentumState == 1 && baseScore > 0;
                bool doubleDown = player.DoubleDownActive && baseScore > 0;
                float multiplier = momentum && doubleDown ? 2f : momentum || doubleDown ? 1.5f : 1f;
                int enhanced = (int)Math.Floor(baseScore * multiplier);
                int finalScore = baseScore == 0 ? 0 : enhanced + diceBonus;
                result[i] = new YachtScoreCandidate
                {
                    Category = category,
                    BaseScore = baseScore,
                    DiceBonusScore = diceBonus,
                    Score = finalScore,
                    IsEnhanced = multiplier > 1f,
                    EnhancementSource = momentum && doubleDown ? "Momentum+DoubleDown"
                        : momentum ? "Momentum" : doubleDown ? "DoubleDown" : null
                };
            }
            return result;
        }

        public YachtGameEvent[] AfterScoreCommit(
            YachtGameState state,
            int playerIndex,
            int normalRollCount)
        {
            return AfterScoreCommit(state, playerIndex, normalRollCount, default);
        }

        public YachtGameEvent[] AfterScoreCommit(
            YachtGameState state,
            int playerIndex,
            int normalRollCount,
            ScoreCategory category)
        {
            var events = new List<YachtGameEvent>();
            if (Owns(state, playerIndex, NoTimeToWasteId))
            {
                YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
                if (!progress.NoTimeRewarded && !progress.NoTimeFailed)
                {
                    if (normalRollCount != 1)
                    {
                        progress.NoTimeFailed = true;
                        events.Add(new YachtGameEvent
                        {
                            Type = YachtGameEventType.AugmentTriggered,
                            PlayerIndex = playerIndex,
                            AugmentId = NoTimeToWasteId,
                            Message = "낭비할 시간 없다 실패"
                        });
                    }
                    else
                    {
                        progress.NoTimeRemaining = Math.Max(0, progress.NoTimeRemaining - 1);
                        if (progress.NoTimeRemaining > 0)
                        {
                            events.Add(new YachtGameEvent
                            {
                                Type = YachtGameEventType.AugmentTriggered,
                                PlayerIndex = playerIndex,
                                AugmentId = NoTimeToWasteId,
                                Score = progress.NoTimeRemaining,
                                Message = $"낭비할 시간 없다: {progress.NoTimeRemaining}턴 남음"
                            });
                        }
                        else
                        {
                            progress.NoTimeRewarded = true;
                            state.Players[playerIndex].augmentBonusScore += 15;
                            state.Players[playerIndex].RecalculateTotal();
                            events.Add(new YachtGameEvent
                            {
                                Type = YachtGameEventType.AugmentTriggered,
                                PlayerIndex = playerIndex,
                                AugmentId = NoTimeToWasteId,
                                Score = 15,
                                Message = "낭비할 시간 없다 완료: +15점"
                            });
                        }
                    }
                }
            }

            if (Owns(state, playerIndex, StepByStepId))
            {
                YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
                if (!progress.StepFailed && !progress.StepRewarded)
                {
                    int expectedCategory = progress.StepCategoryIndex;
                    if ((int)category != expectedCategory)
                    {
                        progress.StepFailed = true;
                        events.Add(new YachtGameEvent
                        {
                            Type = YachtGameEventType.AugmentTriggered,
                            PlayerIndex = playerIndex,
                            AugmentId = StepByStepId,
                            Message = "차근차근 실패"
                        });
                    }
                    else
                    {
                        progress.StepCategoryIndex++;
                        if (progress.StepCategoryIndex >= 6)
                        {
                            progress.StepRewarded = true;
                            state.Players[playerIndex].upperBonusThreshold = Math.Min(
                                state.Players[playerIndex].upperBonusThreshold,
                                StepByStepUpperBonusThreshold);
                            TryGrantStepBonus(state, playerIndex);
                            events.Add(new YachtGameEvent
                            {
                                Type = YachtGameEventType.AugmentTriggered,
                                PlayerIndex = playerIndex,
                                AugmentId = StepByStepId,
                                Score = state.Players[playerIndex].stepBonusGranted ? 55 : 0,
                                Message = state.Players[playerIndex].stepBonusGranted
                                    ? "차근차근 완료: 상단 보너스 +55점"
                                    : "차근차근 완료: 상단 보너스 기준 58점"
                            });
                        }
                        else
                        {
                            events.Add(new YachtGameEvent
                            {
                                Type = YachtGameEventType.AugmentTriggered,
                                PlayerIndex = playerIndex,
                                AugmentId = StepByStepId,
                                Score = progress.StepCategoryIndex,
                                Message = $"차근차근: {progress.StepCategoryIndex}/6"
                            });
                        }
                    }
                }
                else if (progress.StepRewarded)
                {
                    TryGrantStepBonus(state, playerIndex);
                }
            }

            return events.ToArray();
        }

        public YachtGameEvent[] AfterScoreCommit(
            YachtGameState state,
            int playerIndex,
            int normalRollCount,
            ScoreCategory category,
            int baseScore,
            int finalScore,
            IReadOnlyList<YachtDieState> dice,
            IRandomSource random)
        {
            var events = new List<YachtGameEvent>(AfterScoreCommit(state, playerIndex, normalRollCount, category));
            YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
            int turnNumber = progress.TurnsTaken + 1;

            if (Owns(state, playerIndex, FastStraightId) && !progress.FastRewarded && turnNumber <= 8)
            {
                if (category == ScoreCategory.SmallStraight && baseScore > 0) progress.FastSmallScored = true;
                if (category == ScoreCategory.LargeStraight && baseScore > 0) progress.FastLargeScored = true;
                if (progress.FastSmallScored && progress.FastLargeScored)
                {
                    progress.FastRewarded = true;
                    GrantBonus(state, playerIndex, FastStraightId, 15, events);
                }
            }

            if (Owns(state, playerIndex, HoldoutId) && !progress.HoldoutRewarded
                && turnNumber >= 9 && category == ScoreCategory.FullHouse && baseScore > 0)
            {
                progress.HoldoutRewarded = true;
                GrantBonus(state, playerIndex, HoldoutId, 7, events);
            }

            if (Owns(state, playerIndex, CautiousStraightId) && !progress.CautiousFailed && !progress.CautiousRewarded)
            {
                if (category == ScoreCategory.LargeStraight && baseScore > 0 && !progress.CautiousSmallScored)
                    progress.CautiousFailed = true;
                else if (category == ScoreCategory.SmallStraight && baseScore > 0)
                    progress.CautiousSmallScored = true;
                else if (category == ScoreCategory.LargeStraight && baseScore > 0 && progress.CautiousSmallScored)
                {
                    progress.CautiousRewarded = true;
                    GrantBonus(state, playerIndex, CautiousStraightId, 7, events);
                }
            }

            if (Owns(state, playerIndex, EveryLittleId) && !progress.EveryLittleRewarded)
            {
                progress.EveryLittleCount += CountUsedOnes(category, baseScore, dice);
                if (progress.EveryLittleCount >= 7)
                {
                    progress.EveryLittleRewarded = true;
                    GrantBonus(state, playerIndex, EveryLittleId, 15, events);
                }
            }

            if (Owns(state, playerIndex, CopycatId) && !progress.CopycatRewarded)
            {
                int opponent = playerIndex == 0 ? 1 : 0;
                if (IsFilled(state.Players[opponent], category))
                {
                    int opponentBase = GetBaseScore(state.Players[opponent], category);
                    bool immediate = (int)category >= (int)ScoreCategory.Choice && opponentBase == baseScore;
                    progress.CopycatCount++;
                    if (immediate || progress.CopycatCount >= 3)
                    {
                        progress.CopycatRewarded = true;
                        GrantBonus(state, playerIndex, CopycatId, 10, events);
                    }
                }
            }

            if (Owns(state, playerIndex, DoublingId) && !progress.DoublingRewarded && baseScore != 0)
            {
                if (Contains(progress.RecordedBaseScores, baseScore))
                {
                    progress.DoublingRewarded = true;
                    GrantBonus(state, playerIndex, DoublingId, 10, events);
                }
                else progress.RecordedBaseScores = Append(progress.RecordedBaseScores, baseScore);
            }

            if (Owns(state, playerIndex, NozdormuId) && !progress.NozdormuRewarded
                && turnNumber >= progress.NozdormuTargetTurn)
            {
                progress.NozdormuRewarded = true;
                GrantBonus(state, playerIndex, NozdormuId, 9, events);
            }

            if (Owns(state, playerIndex, MomentumId))
            {
                if (progress.MomentumState == 0 && baseScore == 0) progress.MomentumState = 1;
                else if (progress.MomentumState == 1 && baseScore > 0) progress.MomentumState = 2;
            }

            ProcessYachtBank(state, playerIndex, dice);
            if (progress.PromotionActive && progress.PromotionValue >= 6) progress.PromotionActive = false;

            if (Owns(state, playerIndex, BountyHunterId) && !progress.BountyRewarded
                && progress.BountyTargetCategory == (int)category)
            {
                progress.BountySuccesses++;
                if (baseScore == 0) progress.BountyScratches++;
                if (progress.BountySuccesses >= 3)
                {
                    progress.BountyRewarded = true;
                    GrantBonus(state, playerIndex, BountyHunterId, Math.Max(0, 15 - progress.BountyScratches * 3), events);
                }
                progress.BountyTargetCategory = -1;
            }

            if (Owns(state, playerIndex, ProphetId) && progress.ProphetTurnsRemaining > 0)
            {
                if (Contains(progress.ProphetTargets, baseScore)) GrantBonus(state, playerIndex, ProphetId, 7, events);
                progress.ProphetTurnsRemaining--;
                progress.ProphetTargets = Array.Empty<int>();
            }

            if (progress.GambitState == 1) progress.GambitState = 2;
            else if (progress.GambitState == 2) progress.GambitState = 3;
            progress.DoubleDownActive = false;

            if (Owns(state, playerIndex, PiggyBankId))
            {
                progress.PiggyBankBalance += state.RollsRemaining * 3;
                while (progress.PiggyBankBalance >= 12)
                {
                    progress.PiggyBankBalance -= 12;
                    GrantBonus(state, playerIndex, PiggyBankId, 12, events);
                }
            }

            RecordAndResolveDuels(state, playerIndex, finalScore, events);
            progress.TurnsTaken++;
            return events.ToArray();
        }

        public void PrepareTurn(YachtGameState state, int playerIndex, IRandomSource random, bool growPromotion)
        {
            YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
            if (progress.YachtBankPayoutPending && !progress.YachtBankPaid)
            {
                progress.YachtBankPaid = true;
                progress.YachtBankPayoutPending = false;
                state.Players[playerIndex].augmentBonusScore += progress.YachtBankBalance;
                state.Players[playerIndex].RecalculateTotal();
            }
            if (progress.PromotionActive)
            {
                if (progress.PromotionSkipNextGrowth) progress.PromotionSkipNextGrowth = false;
                else if (growPromotion) progress.PromotionValue = Math.Min(6, Math.Max(1, progress.PromotionValue) + 1);
            }
            if (Owns(state, playerIndex, BountyHunterId) && !progress.BountyRewarded && progress.BountyTargetCategory < 0)
                progress.BountyTargetCategory = SelectEmptyCategory(state, playerIndex, random);
            if (Owns(state, playerIndex, ProphetId) && progress.ProphetTurnsRemaining > 0 && progress.ProphetTargets.Length == 0)
            {
                progress.ProphetTargets = new[]
                {
                    random.NextInt(1, 31), random.NextInt(1, 31), random.NextInt(1, 31)
                };
            }
        }

        public float GetTurnDuration(YachtGameState state, int playerIndex, float defaultSeconds)
        {
            YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
            return Owns(state, playerIndex, NozdormuId) && !progress.NozdormuRewarded
                && progress.TurnsTaken < progress.NozdormuTargetTurn ? 15f : defaultSeconds;
        }

        public void RecalculateStepBonus(YachtGameState state, int playerIndex)
        {
            if (Owns(state, playerIndex, StepByStepId)) TryGrantStepBonus(state, playerIndex);
        }

        private static void TryGrantStepBonus(YachtGameState state, int playerIndex)
        {
            YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
            PlayerScoreData scores = state.Players[playerIndex];
            if (progress.StepRewarded)
            {
                scores.upperBonusThreshold = Math.Min(scores.upperBonusThreshold, StepByStepUpperBonusThreshold);
                if (!scores.stepBonusGranted && scores.CalculateUpperSum() >= StepByStepUpperBonusThreshold)
                    scores.stepBonusGranted = true;
            }
            scores.RecalculateTotal();
        }

        public bool CanUseTableFlip(YachtGameState state, int playerIndex, out YachtCommandErrorCode code, out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            if (!Owns(state, playerIndex, TableFlipId))
                return Fail(YachtCommandErrorCode.AugmentRequired, "판 뒤집기 증강을 보유하지 않았습니다.", out code, out message);
            if (state.AugmentPlayers[playerIndex].TableFlipUsed)
                return Fail(YachtCommandErrorCode.AugmentAlreadyUsed, "판 뒤집기를 이미 사용했습니다.", out code, out message);
            if (!state.HasRolled)
                return Fail(YachtCommandErrorCode.RollRequired, "첫 굴림 후 판 뒤집기를 사용할 수 있습니다.", out code, out message);
            return true;
        }

        public void MarkTableFlipUsed(YachtGameState state, int playerIndex) =>
            state.AugmentPlayers[playerIndex].TableFlipUsed = true;

        public bool CanUseEquivalentExchange(YachtGameState state, int playerIndex, out YachtCommandErrorCode code, out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (!Owns(state, playerIndex, EquivalentExchangeId))
                return Fail(YachtCommandErrorCode.AugmentRequired, "등가교환 증강을 보유하지 않았습니다.", out code, out message);
            if (player.EquivalentExchangeUses >= 3)
                return Fail(YachtCommandErrorCode.AugmentAlreadyUsed, "등가교환을 모두 사용했습니다.", out code, out message);
            if (!state.HasRolled || state.RollsRemaining > 0)
                return Fail(YachtCommandErrorCode.NoRollsRemaining, "기본 굴림을 모두 사용한 뒤 등가교환을 사용할 수 있습니다.", out code, out message);
            return true;
        }

        public void MarkEquivalentExchangeUsed(YachtGameState state, int playerIndex)
        {
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            player.EquivalentExchangeUses++;
            state.Players[playerIndex].augmentBonusScore -= 5;
            state.Players[playerIndex].RecalculateTotal();
        }

        public bool TryActivateBeforeRoll(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            out YachtCommandErrorCode code,
            out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            if (!Owns(state, playerIndex, augmentId))
                return Fail(YachtCommandErrorCode.AugmentRequired, "해당 증강을 보유하지 않았습니다.", out code, out message);
            if (state.HasRolled)
                return Fail(YachtCommandErrorCode.InvalidPhase, "첫 굴림 전에만 사용할 수 있습니다.", out code, out message);

            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (augmentId == GambitId)
            {
                if (player.GambitState != 0)
                    return Fail(YachtCommandErrorCode.AugmentAlreadyUsed, "갬빗을 이미 사용했습니다.", out code, out message);
                player.GambitState = 1;
                return true;
            }
            if (augmentId == DoubleDownId)
            {
                if (player.DoubleDownUsed)
                    return Fail(YachtCommandErrorCode.AugmentAlreadyUsed, "더블 다운을 이미 사용했습니다.", out code, out message);
                if (player.TurnsTaken < 8)
                    return Fail(YachtCommandErrorCode.AugmentUnavailable, "더블 다운은 아홉 번째 내 턴부터 사용할 수 있습니다.", out code, out message);
                player.DoubleDownUsed = true;
                player.DoubleDownActive = true;
                return true;
            }
            return Fail(YachtCommandErrorCode.AugmentUnavailable, "굴림 전 발동 증강이 아닙니다.", out code, out message);
        }

        public bool TryUseDiceAlchemy(
            YachtGameState state,
            int playerIndex,
            out YachtCommandErrorCode code,
            out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (!Owns(state, playerIndex, DiceAlchemyId))
                return Fail(YachtCommandErrorCode.AugmentRequired, "주사위 연금술 증강을 보유하지 않았습니다.", out code, out message);
            if (player.DiceAlchemyUsed)
                return Fail(YachtCommandErrorCode.AugmentAlreadyUsed, "주사위 연금술을 이미 사용했습니다.", out code, out message);
            if (!state.HasRolled)
                return Fail(YachtCommandErrorCode.RollRequired, "첫 굴림 후 사용할 수 있습니다.", out code, out message);
            for (int i = 0; i < state.Dice.Length; i++)
                if (!state.Dice[i].IsKept) state.Dice[i].Value = Math.Max(1, state.Dice[i].Value - 1);
            player.DiceAlchemyUsed = true;
            return true;
        }

        private static bool IsDraftRound(int round) => round == 1 || round == 6 || round == 9;
        private static int ExpectedSelectionCount(int round) => round >= 9 ? 3 : round >= 6 ? 2 : 1;

        private static int FindNextDraftPlayer(YachtGameState state, int expected)
        {
            for (int i = 0; i < state.Draft.SelectionCounts.Length; i++)
                if (state.Draft.SelectionCounts[i] < expected) return i;
            return -1;
        }

        private string[] CreateDraftOptions(YachtGameState state, int playerIndex, IRandomSource random)
        {
            var candidates = new List<string>();
            for (int i = 0; i < Definitions.Length; i++)
                if (CanAcquire(state, playerIndex, Definitions[i].Id)) candidates.Add(Definitions[i].Id);
            Shuffle(candidates, random);
            int count = Math.Min(DraftOptionCount, candidates.Count);
            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = candidates[i];
            return result;
        }

        private bool CanAcquire(YachtGameState state, int playerIndex, string augmentId)
        {
            YachtAugmentDefinition definition = FindDefinition(augmentId);
            if (definition == null) return false;
            if (definition.PhaseOneOnly && state.CurrentRound != 1) return false;
            if (definition.IsGlobal && Contains(state.GlobalAugmentIds, augmentId)) return false;
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (Contains(player.OwnedIds, augmentId) || HasConflict(player, augmentId)) return false;
            if (definition.Kind == YachtAugmentKind.ScoreReplacement)
            {
                for (int i = 0; i < player.OwnedIds.Length; i++)
                {
                    YachtAugmentDefinition owned = FindDefinition(player.OwnedIds[i]);
                    if (owned?.Kind == YachtAugmentKind.ScoreReplacement
                        && string.Equals(owned.Target, definition.Target, StringComparison.Ordinal)) return false;
                }
            }
            int required = RequiredDiceSlots(augmentId);
            if (required > 0)
            {
                for (int i = 0; i < player.OwnedIds.Length; i++) required += RequiredDiceSlots(player.OwnedIds[i]);
                if (required > 5) return false;
            }
            return true;
        }

        private bool HasConflict(YachtAugmentPlayerState player, string candidateId)
        {
            YachtAugmentDefinition candidate = FindDefinition(candidateId);
            if (candidate == null) return false;
            for (int i = 0; i < player.OwnedIds.Length; i++)
            {
                string ownedId = player.OwnedIds[i];
                if (Contains(candidate.Conflicts, ownedId)) return true;
                YachtAugmentDefinition owned = FindDefinition(ownedId);
                if (owned != null && Contains(owned.Conflicts, candidateId)) return true;
            }
            return false;
        }

        private static int RequiredDiceSlots(string augmentId) => augmentId switch
        {
            WeightedDiceId => 1,
            GoldenDieId => 1,
            OctahedronId => 2,
            PromotionDieId => 1,
            CoupleDiceId => 2,
            SevensDiceId => 2,
            _ => 0
        };

        private void ApplyAugment(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            ICollection<YachtGameEvent> events,
            bool replacement)
        {
            YachtAugmentPlayerState runtime = state.AugmentPlayers[playerIndex];
            YachtAugmentDefinition definition = FindDefinition(augmentId);
            if (definition?.IsGlobal == true) state.GlobalAugmentIds = Append(state.GlobalAugmentIds, augmentId);
            else runtime.OwnedIds = Append(runtime.OwnedIds, augmentId);

            if (augmentId == LuckySevensId)
            {
                ResetFilledTarget(state, playerIndex, ScoreCategory.Aces, runtime);
            }
            else if (definition?.Kind == YachtAugmentKind.ScoreReplacement)
            {
                if (Enum.TryParse(definition.Target, out ScoreCategory target)) ResetFilledTarget(state, playerIndex, target, runtime);
                if (augmentId == DoubleLargeStraightId)
                {
                    state.Players[playerIndex].upperBonusThreshold = Math.Min(state.Players[playerIndex].upperBonusThreshold, 60);
                    state.Players[playerIndex].RecalculateTotal();
                }
            }
            else if (augmentId == YachtBankId)
            {
                runtime.YachtBankRemainingTurns = 3;
                runtime.YachtBankBalance = 0;
                runtime.YachtBankPayoutPending = false;
                runtime.YachtBankPaid = false;
            }
            else if (augmentId == FastStraightId)
            {
                runtime.FastSmallScored = false;
                runtime.FastLargeScored = false;
                runtime.FastRewarded = false;
            }
            else if (augmentId == NoTimeToWasteId)
            {
                runtime.NoTimeRemaining = 3;
                runtime.NoTimeFailed = false;
                runtime.NoTimeRewarded = false;
            }
            else if (augmentId == StepByStepId)
            {
                runtime.StepCategoryIndex = 0;
                runtime.StepFailed = false;
                runtime.StepRewarded = false;
            }
            else if (augmentId == NozdormuId)
            {
                runtime.NozdormuTargetTurn = state.CurrentRound < 6 ? 5 : state.CurrentRound < 9 ? 8 : 12;
                runtime.NozdormuRewarded = false;
            }
            else if (augmentId == MomentumId)
            {
                runtime.MomentumState = 0;
            }
            else if (augmentId == PromotionDieId)
            {
                runtime.PromotionValue = 1;
                runtime.PromotionActive = true;
                runtime.PromotionSkipNextGrowth = true;
            }
            else if (augmentId == BountyHunterId)
            {
                runtime.BountyTargetCategory = -1;
                runtime.BountySuccesses = 0;
                runtime.BountyScratches = 0;
                runtime.BountyRewarded = false;
            }
            else if (augmentId == DuelId)
            {
                runtime.DuelRound = state.CurrentRound;
                runtime.DuelResolved = false;
            }
            else if (augmentId == ProphetId)
            {
                runtime.ProphetTurnsRemaining = 3;
                runtime.ProphetTargets = Array.Empty<int>();
            }
            else if (augmentId == RandomBoxId)
            {
                state.Players[playerIndex].upperBonusThreshold = Math.Min(state.Players[playerIndex].upperBonusThreshold, 58);
                state.Players[playerIndex].RecalculateTotal();
                runtime.RandomBoxAwardId = null;
            }

            if (replacement)
            {
                events.Add(new YachtGameEvent
                {
                    Type = YachtGameEventType.AugmentReplaced,
                    PlayerIndex = playerIndex,
                    AugmentId = RandomBoxId,
                    RelatedAugmentId = augmentId,
                    Message = $"랜덤 박스 → {FindDefinition(augmentId)?.DisplayName ?? augmentId}"
                });
            }
        }

        private static void ResetFilledTarget(
            YachtGameState state,
            int playerIndex,
            ScoreCategory target,
            YachtAugmentPlayerState runtime)
        {
            int index = (int)target;
            PlayerScoreData scores = state.Players[playerIndex];
            bool filled;
            if (index <= 5)
            {
                filled = scores.upperFilled[index] || scores.upperScores[index] != -1;
                if (!filled) return;
                scores.upperScores[index] = -1;
                scores.upperBaseScores[index] = -1;
                scores.upperFilled[index] = false;
            }
            else
            {
                int lower = index - 7;
                filled = scores.lowerFilled[lower] || scores.lowerScores[lower] != -1;
                if (!filled) return;
                scores.lowerScores[lower] = -1;
                scores.lowerBaseScores[lower] = -1;
                scores.lowerFilled[lower] = false;
            }
            scores.RecalculateTotal();
            runtime.ExtraTurns++;
        }

        private void ResolveRandomBoxes(YachtGameState state, IRandomSource random, ICollection<YachtGameEvent> events)
        {
            for (int playerIndex = 0; playerIndex < state.AugmentPlayers.Length; playerIndex++)
            {
                YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
                if (!Contains(player.OwnedIds, RandomBoxId) || !string.IsNullOrEmpty(player.RandomBoxAwardId)) continue;
                player.OwnedIds = Remove(player.OwnedIds, RandomBoxId);

                var candidates = new List<string>();
                for (int i = 0; i < Definitions.Length; i++)
                {
                    YachtAugmentDefinition definition = Definitions[i];
                    if (definition.Id == RandomBoxId || definition.IsQuest || !CanAcquire(state, playerIndex, definition.Id)) continue;
                    candidates.Add(definition.Id);
                }
                Shuffle(candidates, random);
                if (candidates.Count == 0) continue;

                string awarded = candidates[0];
                player.RandomBoxAwardId = awarded;
                ApplyAugment(state, playerIndex, awarded, events, true);
            }
        }

        private static void Shuffle<T>(IList<T> values, IRandomSource random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int index = random.NextInt(0, i + 1);
                (values[index], values[i]) = (values[i], values[index]);
            }
        }

        private void AssignDice(
            YachtGameState state,
            int playerIndex,
            IList<YachtDieState> dice,
            string augmentId,
            YachtDieType type,
            int count,
            ref int next)
        {
            if (!Owns(state, playerIndex, augmentId)) return;
            for (int assigned = 0; assigned < count && next < dice.Count; assigned++, next++)
                dice[next].Type = type;
        }

        private IReadOnlyList<YachtDieState> GetScoringDice(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice)
        {
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (!Owns(state, playerIndex, YachtBankId) || player.YachtBankRemainingTurns <= 0) return dice;
            int excludedIndex = -1;
            int lowestSlot = int.MaxValue;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                if (!dice[i].IsKept) continue;
                int slot = dice[i].KeepSlotIndex >= 0 ? dice[i].KeepSlotIndex : i;
                if (slot >= lowestSlot) continue;
                lowestSlot = slot;
                excludedIndex = i;
            }
            if (excludedIndex < 0) return dice;
            var result = new List<YachtDieState>(dice.Count - 1);
            for (int i = 0; i < dice.Count; i++) if (i != excludedIndex) result.Add(dice[i]);
            return result;
        }

        private static void GrantBonus(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            int score,
            ICollection<YachtGameEvent> events)
        {
            state.Players[playerIndex].augmentBonusScore += score;
            state.Players[playerIndex].RecalculateTotal();
            events.Add(new YachtGameEvent
            {
                Type = YachtGameEventType.AugmentTriggered,
                PlayerIndex = playerIndex,
                AugmentId = augmentId,
                Score = score,
                Message = $"{augmentId}: {(score >= 0 ? "+" : string.Empty)}{score}점"
            });
        }

        private static int CountUsedOnes(
            ScoreCategory category,
            int baseScore,
            IReadOnlyList<YachtDieState> dice)
        {
            // 스크래치는 어떤 주사위도 족보 계산에 사용한 것으로 보지 않는다.
            if (baseScore == 0) return 0;
            int count = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
                if (dice[i].Value == 1) count++;
            return count;
        }

        private static bool IsFilled(PlayerScoreData scores, ScoreCategory category)
        {
            int index = (int)category;
            return index <= 5
                ? scores.upperFilled[index] || scores.upperScores[index] != -1
                : scores.lowerFilled[index - 7] || scores.lowerScores[index - 7] != -1;
        }

        private static int GetBaseScore(PlayerScoreData scores, ScoreCategory category)
        {
            int index = (int)category;
            if (index <= 5)
                return scores.upperBaseScores[index] != -1 ? scores.upperBaseScores[index] : scores.upperScores[index];
            int lower = index - 7;
            return scores.lowerBaseScores[lower] != -1 ? scores.lowerBaseScores[lower] : scores.lowerScores[lower];
        }

        private void ProcessYachtBank(YachtGameState state, int playerIndex, IReadOnlyList<YachtDieState> dice)
        {
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            if (!Owns(state, playerIndex, YachtBankId) || player.YachtBankRemainingTurns <= 0) return;

            YachtDieState banked = null;
            int lowestSlot = int.MaxValue;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                YachtDieState die = dice[i];
                if (!die.IsKept) continue;
                int slot = die.KeepSlotIndex >= 0 ? die.KeepSlotIndex : i;
                if (slot >= lowestSlot) continue;
                lowestSlot = slot;
                banked = die;
            }
            if (banked != null) player.YachtBankBalance = Math.Min(15, player.YachtBankBalance + banked.Value);
            player.YachtBankRemainingTurns--;
            if (player.YachtBankRemainingTurns <= 0) player.YachtBankPayoutPending = true;
        }

        private static int SelectEmptyCategory(YachtGameState state, int playerIndex, IRandomSource random)
        {
            var empty = new List<int>();
            for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
            {
                ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                if (!IsFilled(state.Players[playerIndex], category)) empty.Add((int)category);
            }
            return empty.Count == 0 ? -1 : empty[random.NextInt(0, empty.Count)];
        }

        private void RecordAndResolveDuels(
            YachtGameState state,
            int playerIndex,
            int finalScore,
            ICollection<YachtGameEvent> events)
        {
            if (state.RoundScoresRound != state.CurrentRound)
            {
                state.RoundScoresRound = state.CurrentRound;
                state.RoundScores = new[] { int.MinValue, int.MinValue };
            }
            state.RoundScores[playerIndex] = finalScore;
            if (state.RoundScores[0] == int.MinValue || state.RoundScores[1] == int.MinValue) return;

            for (int owner = 0; owner < state.AugmentPlayers.Length; owner++)
            {
                YachtAugmentPlayerState duel = state.AugmentPlayers[owner];
                if (!Owns(state, owner, DuelId) || duel.DuelResolved || duel.DuelRound != state.CurrentRound) continue;
                int opponent = owner == 0 ? 1 : 0;
                int bonus = state.RoundScores[owner] > state.RoundScores[opponent] ? 10
                    : state.RoundScores[owner] == state.RoundScores[opponent] ? 5 : 0;
                duel.DuelResolved = true;
                if (bonus > 0) GrantBonus(state, owner, DuelId, bonus, events);
            }
        }

        private static bool Contains(IReadOnlyList<int> values, int target)
        {
            for (int i = 0; i < (values?.Count ?? 0); i++) if (values[i] == target) return true;
            return false;
        }

        private static int[] Append(IReadOnlyList<int> values, int item)
        {
            var result = new int[(values?.Count ?? 0) + 1];
            for (int i = 0; i < result.Length - 1; i++) result[i] = values[i];
            result[result.Length - 1] = item;
            return result;
        }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            for (int i = 0; i < (values?.Count ?? 0); i++)
                if (string.Equals(values[i], target, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string[] Append(IReadOnlyList<string> values, string item)
        {
            var result = new string[(values?.Count ?? 0) + 1];
            for (int i = 0; i < result.Length - 1; i++) result[i] = values[i];
            result[result.Length - 1] = item;
            return result;
        }

        private static string[] Remove(IReadOnlyList<string> values, string item)
        {
            var result = new List<string>(values?.Count ?? 0);
            for (int i = 0; i < (values?.Count ?? 0); i++)
                if (!string.Equals(values[i], item, StringComparison.Ordinal)) result.Add(values[i]);
            return result.ToArray();
        }

        private static bool Fail(
            YachtCommandErrorCode failure,
            string failureMessage,
            out YachtCommandErrorCode code,
            out string message)
        {
            code = failure;
            message = failureMessage;
            return false;
        }
    }
}
