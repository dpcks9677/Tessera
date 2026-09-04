using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>기획 기준 증강 분류입니다. 이식 매트릭스 3.1~3.3절의 세 묶음과 같습니다.</summary>
    public enum YachtAugmentKind
    {
        /// <summary>족보·점수를 교체하는 변형 증강입니다.</summary>
        Modification,

        /// <summary>주사위·수동 행동·상시 효과를 포함한 강화 증강입니다.</summary>
        Enhance,

        /// <summary>조건을 달성하면 보상을 주는 퀘스트 증강입니다.</summary>
        Quest
    }

    [Serializable]
    public sealed class YachtAugmentDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string Target;
        public YachtAugmentKind Kind;
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
        public int[] OptionCardPresetIds = Array.Empty<int>();
        public int[] SelectionCounts = Array.Empty<int>();

        public YachtDraftState Clone() => new()
        {
            IsActive = IsActive,
            PlayerIndex = PlayerIndex,
            Options = (string[])(Options?.Clone() ?? Array.Empty<string>()),
            OptionCardPresetIds = (int[])(OptionCardPresetIds?.Clone() ?? Array.Empty<int>()),
            SelectionCounts = (int[])(SelectionCounts?.Clone() ?? Array.Empty<int>())
        };
    }

    [Serializable]
    public sealed class YachtAugmentPlayerState
    {
        public string[] OwnedIds = Array.Empty<string>();
        public int[] OwnedCardPresetIds = Array.Empty<int>();
        public int ExtraTurns;

        /// <summary>
        /// 증강별 전용 진행 상태입니다. M7.5 이후 새로 추가하는 증강은 아래의 개별 필드가 아니라
        /// 자신의 <see cref="IAugmentState"/> 구현체를 여기에 보관합니다.
        /// </summary>
        public AugmentStateStore States = new();
        public int TurnsTaken;

        public YachtAugmentPlayerState Clone() => new()
        {
            OwnedIds = (string[])(OwnedIds?.Clone() ?? Array.Empty<string>()),
            OwnedCardPresetIds = (int[])(OwnedCardPresetIds?.Clone() ?? Array.Empty<int>()),
            ExtraTurns = ExtraTurns,
            TurnsTaken = TurnsTaken,
            States = States?.Clone() ?? new AugmentStateStore()
        };

        // --- 구 상태 필드 호환성 프로퍼티 (States 위임) ---
        public int NoTimeRemaining
        {
            get => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).RemainingTurns;
            set => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).RemainingTurns = value;
        }
        public bool NoTimeFailed
        {
            get => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).Failed;
            set => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).Failed = value;
        }
        public bool NoTimeRewarded
        {
            get => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).Rewarded;
            set => States.GetOrCreate<NoTimeToWasteState>(YachtAugmentRuntime.NoTimeToWasteId).Rewarded = value;
        }
        public int StepCategoryIndex
        {
            get => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).CategoryIndex;
            set => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).CategoryIndex = value;
        }
        public bool StepFailed
        {
            get => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).Failed;
            set => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).Failed = value;
        }
        public bool StepRewarded
        {
            get => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).Rewarded;
            set => States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId).Rewarded = value;
        }
        public bool TableFlipUsed
        {
            get => States.GetOrCreate<TableFlipState>(YachtAugmentRuntime.TableFlipId).IsUsed;
            set => States.GetOrCreate<TableFlipState>(YachtAugmentRuntime.TableFlipId).IsUsed = value;
        }
        public string RandomBoxAwardId
        {
            get => States.GetOrCreate<RandomBoxState>(YachtAugmentRuntime.RandomBoxId).AwardId;
            set => States.GetOrCreate<RandomBoxState>(YachtAugmentRuntime.RandomBoxId).AwardId = value;
        }
        public bool FastSmallScored
        {
            get => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).SmallScored;
            set => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).SmallScored = value;
        }
        public bool FastLargeScored
        {
            get => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).LargeScored;
            set => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).LargeScored = value;
        }
        public bool FastRewarded
        {
            get => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).Rewarded;
            set => States.GetOrCreate<FastStraightState>(YachtAugmentRuntime.FastStraightId).Rewarded = value;
        }
        public bool HoldoutRewarded
        {
            get => States.GetOrCreate<HoldoutState>(YachtAugmentRuntime.HoldoutId).Rewarded;
            set => States.GetOrCreate<HoldoutState>(YachtAugmentRuntime.HoldoutId).Rewarded = value;
        }
        public bool CautiousSmallScored
        {
            get => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).SmallScored;
            set => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).SmallScored = value;
        }
        public bool CautiousFailed
        {
            get => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).Failed;
            set => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).Failed = value;
        }
        public bool CautiousRewarded
        {
            get => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).Rewarded;
            set => States.GetOrCreate<CautiousStraightState>(YachtAugmentRuntime.CautiousStraightId).Rewarded = value;
        }
        public int EveryLittleCount
        {
            get => States.GetOrCreate<EveryLittleCountsState>(YachtAugmentRuntime.EveryLittleId).Count;
            set => States.GetOrCreate<EveryLittleCountsState>(YachtAugmentRuntime.EveryLittleId).Count = value;
        }
        public bool EveryLittleRewarded
        {
            get => States.GetOrCreate<EveryLittleCountsState>(YachtAugmentRuntime.EveryLittleId).Rewarded;
            set => States.GetOrCreate<EveryLittleCountsState>(YachtAugmentRuntime.EveryLittleId).Rewarded = value;
        }
        public int CopycatCount
        {
            get => States.GetOrCreate<CopycatState>(YachtAugmentRuntime.CopycatId).Count;
            set => States.GetOrCreate<CopycatState>(YachtAugmentRuntime.CopycatId).Count = value;
        }
        public bool CopycatRewarded
        {
            get => States.GetOrCreate<CopycatState>(YachtAugmentRuntime.CopycatId).Rewarded;
            set => States.GetOrCreate<CopycatState>(YachtAugmentRuntime.CopycatId).Rewarded = value;
        }
        public int[] RecordedBaseScores
        {
            get => States.GetOrCreate<DoublingState>(YachtAugmentRuntime.DoublingId).RecordedBaseScores.ToArray();
            set => States.GetOrCreate<DoublingState>(YachtAugmentRuntime.DoublingId).RecordedBaseScores = value != null ? new System.Collections.Generic.List<int>(value) : new System.Collections.Generic.List<int>();
        }
        public bool DoublingRewarded
        {
            get => States.GetOrCreate<DoublingState>(YachtAugmentRuntime.DoublingId).Rewarded;
            set => States.GetOrCreate<DoublingState>(YachtAugmentRuntime.DoublingId).Rewarded = value;
        }
        public int NozdormuTargetTurn
        {
            get => States.GetOrCreate<NozdormuState>(YachtAugmentRuntime.NozdormuId).TargetTurn;
            set => States.GetOrCreate<NozdormuState>(YachtAugmentRuntime.NozdormuId).TargetTurn = value;
        }
        public bool NozdormuRewarded
        {
            get => States.GetOrCreate<NozdormuState>(YachtAugmentRuntime.NozdormuId).Rewarded;
            set => States.GetOrCreate<NozdormuState>(YachtAugmentRuntime.NozdormuId).Rewarded = value;
        }
        public int MomentumState
        {
            get => States.GetOrCreate<MomentumAugmentState>(YachtAugmentRuntime.MomentumId).State;
            set => States.GetOrCreate<MomentumAugmentState>(YachtAugmentRuntime.MomentumId).State = value;
        }
        public int YachtBankRemainingTurns
        {
            get => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).RemainingTurns;
            set => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).RemainingTurns = value;
        }
        public int YachtBankBalance
        {
            get => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).Balance;
            set => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).Balance = value;
        }
        public bool YachtBankPayoutPending
        {
            get => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).PayoutPending;
            set => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).PayoutPending = value;
        }
        public bool YachtBankPaid
        {
            get => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).Paid;
            set => States.GetOrCreate<YachtBankState>(YachtAugmentRuntime.YachtBankId).Paid = value;
        }
        public int PromotionValue
        {
            get => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).Value;
            set => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).Value = value;
        }
        public bool PromotionActive
        {
            get => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).IsActive;
            set => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).IsActive = value;
        }
        public bool PromotionSkipNextGrowth
        {
            get => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).SkipNextGrowth;
            set => States.GetOrCreate<PromotionDieState>(YachtAugmentRuntime.PromotionDieId).SkipNextGrowth = value;
        }
        public int EquivalentExchangeUses
        {
            get => States.GetOrCreate<EquivalentExchangeState>(YachtAugmentRuntime.EquivalentExchangeId).Uses;
            set => States.GetOrCreate<EquivalentExchangeState>(YachtAugmentRuntime.EquivalentExchangeId).Uses = value;
        }
        public int BountyTargetCategory
        {
            get => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).TargetCategory;
            set => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).TargetCategory = value;
        }
        public int BountySuccesses
        {
            get => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Successes;
            set => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Successes = value;
        }
        public int BountyScratches
        {
            get => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Scratches;
            set => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Scratches = value;
        }
        public bool BountyRewarded
        {
            get => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Rewarded;
            set => States.GetOrCreate<BountyHunterState>(YachtAugmentRuntime.BountyHunterId).Rewarded = value;
        }
        public int DuelRound
        {
            get => States.GetOrCreate<DuelState>(YachtAugmentRuntime.DuelId).DuelRound;
            set => States.GetOrCreate<DuelState>(YachtAugmentRuntime.DuelId).DuelRound = value;
        }
        public bool DuelResolved
        {
            get => States.GetOrCreate<DuelState>(YachtAugmentRuntime.DuelId).DuelResolved;
            set => States.GetOrCreate<DuelState>(YachtAugmentRuntime.DuelId).DuelResolved = value;
        }
        public int ProphetTurnsRemaining
        {
            get => States.GetOrCreate<ProphetState>(YachtAugmentRuntime.ProphetId).TurnsRemaining;
            set => States.GetOrCreate<ProphetState>(YachtAugmentRuntime.ProphetId).TurnsRemaining = value;
        }
        public int[] ProphetTargets
        {
            get => States.GetOrCreate<ProphetState>(YachtAugmentRuntime.ProphetId).Targets;
            set => States.GetOrCreate<ProphetState>(YachtAugmentRuntime.ProphetId).Targets = value ?? Array.Empty<int>();
        }
        public int GambitState
        {
            get => States.GetOrCreate<GambitState>(YachtAugmentRuntime.GambitId).State;
            set => States.GetOrCreate<GambitState>(YachtAugmentRuntime.GambitId).State = value;
        }
        public bool DoubleDownUsed
        {
            get => States.GetOrCreate<DoubleDownState>(YachtAugmentRuntime.DoubleDownId).IsUsed;
            set => States.GetOrCreate<DoubleDownState>(YachtAugmentRuntime.DoubleDownId).IsUsed = value;
        }
        public bool DoubleDownActive
        {
            get => States.GetOrCreate<DoubleDownState>(YachtAugmentRuntime.DoubleDownId).IsActive;
            set => States.GetOrCreate<DoubleDownState>(YachtAugmentRuntime.DoubleDownId).IsActive = value;
        }
        public int PiggyBankBalance
        {
            get => States.GetOrCreate<PiggyBankState>(YachtAugmentRuntime.PiggyBankId).Balance;
            set => States.GetOrCreate<PiggyBankState>(YachtAugmentRuntime.PiggyBankId).Balance = value;
        }
        public bool DiceAlchemyUsed
        {
            get => States.GetOrCreate<DiceAlchemyState>(YachtAugmentRuntime.DiceAlchemyId).IsUsed;
            set => States.GetOrCreate<DiceAlchemyState>(YachtAugmentRuntime.DiceAlchemyId).IsUsed = value;
        }
    }

    /// <summary>
    /// M5 대표 증강의 고정 처리 지점을 담당합니다. Unity 표현 계층과 분리되어
    /// 동일한 상태와 명령을 로컬·온라인 권위 구현에서 재사용할 수 있습니다.
    /// </summary>
    public sealed class YachtAugmentRuntime
    {
        public const int CardVisualPresetCount = 4;
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
            new YachtAugmentDefinition { Id = RandomBoxId, DisplayName = "랜덤 박스", Description = Describe(RandomBoxId), Kind = YachtAugmentKind.Enhance },
            Quest(ProphetId, "예지자"),
            Action(GambitId, "갬빗"),
            Action(DoubleDownId, "더블 다운"),
            Enhance(PiggyBankId, "저금통"),
            Action(DiceAlchemyId, "주사위 연금술")
        };

        /// <summary>
        /// 처리기로 이관된 증강의 정의와 아직 이관하지 않은 정의를 합친 전체 목록입니다.
        /// 카탈로그 등록 순서를 앞에 두어 이관 전후로 노출 순서가 바뀌지 않게 합니다.
        /// </summary>
        private static readonly YachtAugmentDefinition[] AllDefinitions = BuildAllDefinitions();

        private static YachtAugmentDefinition[] BuildAllDefinitions()
        {
            IReadOnlyList<IAugmentHandler> handlers = YachtAugmentCatalog.All;
            var result = new List<YachtAugmentDefinition>(handlers.Count + Definitions.Length);
            for (int i = 0; i < handlers.Count; i++) result.Add(handlers[i].CreateDefinition());
            for (int i = 0; i < Definitions.Length; i++)
                if (YachtAugmentCatalog.Find(Definitions[i].Id) == null) result.Add(Definitions[i]);
            return result.ToArray();
        }

        private static YachtAugmentDefinition Quest(string id, string name, bool phaseOneOnly = false) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Target = "Quest",
            Kind = YachtAugmentKind.Quest,
            IsQuest = true,
            PhaseOneOnly = phaseOneOnly
        };

        private static YachtAugmentDefinition Dice(string id, string name, string[] conflicts = null) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.Enhance,
            Conflicts = conflicts ?? Array.Empty<string>()
        };

        private static YachtAugmentDefinition Action(string id, string name, string[] conflicts = null) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.Enhance,
            Conflicts = conflicts ?? Array.Empty<string>()
        };

        private static YachtAugmentDefinition Enhance(string id, string name) => new()
        {
            Id = id,
            DisplayName = name,
            Description = Describe(id),
            Kind = YachtAugmentKind.Enhance
        };

        private static string Describe(string id) => id switch
        {
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
            var result = new YachtAugmentDefinition[AllDefinitions.Length];
            for (int i = 0; i < result.Length; i++) result[i] = AllDefinitions[i].Clone();
            return result;
        }

        public YachtAugmentDefinition FindDefinition(string augmentId) => Lookup(augmentId);

        /// <summary>
        /// 정의를 ID로 찾는다. 정의 목록은 정적이므로 인스턴스가 필요 없다.
        ///
        /// 프레젠테이션 쪽은 정의를 읽기만 하려고 런타임을 통째로 하나 더 만들고 있었다(M10-T7).
        /// </summary>
        public static YachtAugmentDefinition Lookup(string augmentId)
        {
            for (int i = 0; i < AllDefinitions.Length; i++)
                if (string.Equals(AllDefinitions[i].Id, augmentId, StringComparison.Ordinal)) return AllDefinitions[i].Clone();
            return null;
        }

        public void Initialize(YachtGameState state, int playerCount)
        {
            state.Draft = new YachtDraftState { SelectionCounts = new int[playerCount] };
            state.GlobalAugmentIds = Array.Empty<string>();
            state.AugmentPlayers = new YachtAugmentPlayerState[playerCount];
            for (int i = 0; i < playerCount; i++) state.AugmentPlayers[i] = new YachtAugmentPlayerState();
        }

        public bool TryBeginDraft(YachtGameState state, IRandomSource random, out YachtGameEvent gameEvent) =>
            TryBeginDraft(state, random, new SystemRandomSource(0), out gameEvent);

        public bool TryBeginDraft(
            YachtGameState state,
            IRandomSource random,
            IRandomSource visualRandom,
            out YachtGameEvent gameEvent)
        {
            gameEvent = null;
            if (state.Mode != YachtGameMode.Augmented || !IsDraftRound(state.CurrentRound)) return false;

            int expected = ExpectedSelectionCount(state.CurrentRound);
            int playerIndex = FindNextDraftPlayer(state, expected);
            if (playerIndex < 0) return false;

            state.Draft.IsActive = true;
            state.Draft.PlayerIndex = playerIndex;
            state.Draft.Options = CreateDraftOptions(state, playerIndex, random);
            state.Draft.OptionCardPresetIds = CreateCardPresetIds(state.Draft.Options.Length, visualRandom);
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
            out string errorMessage) =>
            TrySelectAugment(state, playerIndex, augmentId, random, new SystemRandomSource(0),
                out events, out errorCode, out errorMessage);

        public bool TrySelectAugment(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            IRandomSource random,
            IRandomSource visualRandom,
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

            int optionIndex = Array.IndexOf(state.Draft.Options, augmentId);
            int visualPreset = optionIndex >= 0 && optionIndex < (state.Draft.OptionCardPresetIds?.Length ?? 0)
                ? NormalizeCardPreset(state.Draft.OptionCardPresetIds[optionIndex])
                : visualRandom.NextInt(0, CardVisualPresetCount);
            var emitted = new List<YachtGameEvent>();
            ApplyAugment(state, playerIndex, augmentId, emitted, false, visualPreset, random);
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
                state.Draft.OptionCardPresetIds = CreateCardPresetIds(state.Draft.Options.Length, visualRandom);
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
                state.Draft.OptionCardPresetIds = Array.Empty<int>();
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

            var context = new AugmentDiceContext(state, playerIndex, null, dice);
            List<IDiceLayoutProvider> providers = YachtAugmentDispatcher.Collect<IDiceLayoutProvider>(state, playerIndex);
            for (int i = 0; i < providers.Count; i++)
            {
                context.BindAugment(((IAugmentHandler)providers[i]).Id);
                providers[i].ConfigureDice(context);
            }
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
            var query = new AugmentQueryContext(state, playerIndex);
            List<IDiceCountModifier> modifiers = YachtAugmentDispatcher.Collect<IDiceCountModifier>(state, playerIndex);
            for (int i = 0; i < modifiers.Count; i++)
            {
                query.BindAugment(((IAugmentHandler)modifiers[i]).Id);
                defaultCount = modifiers[i].ModifyDiceCount(query, defaultCount);
            }
            return defaultCount;
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

        /// <summary>기본 족보를 계산한 뒤 보유한 변형 증강의 족보 교체를 적용합니다.</summary>
        public Dictionary<ScoreCategory, int> CalculateScores(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice) =>
            CalculateScoringDiceScores(state, playerIndex, GetScoringDice(state, playerIndex, dice));

        private Dictionary<ScoreCategory, int> CalculateScoringDiceScores(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> scoringDice)
        {
            YachtDiceFacts facts = YachtAugmentScoreEngine.CreateFacts(
                scoringDice, state.AugmentPlayers[playerIndex].OwnedIds);
            Dictionary<ScoreCategory, int> scores = YachtAugmentScoreEngine.CalculateBaseScores(facts);
            var context = new AugmentScoreContext(state, playerIndex, scoringDice, scores, facts);
            List<IBeforeScorePreview> handlers = YachtAugmentDispatcher.Collect<IBeforeScorePreview>(state, playerIndex);
            for (int i = 0; i < handlers.Count; i++)
            {
                context.BindAugment(((IAugmentHandler)handlers[i]).Id);
                handlers[i].ModifyScores(context);
            }
            return scores;
        }

        public void ModifyScorePreview(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice,
            IDictionary<ScoreCategory, int> scores)
        {
            Dictionary<ScoreCategory, int> calculated = CalculateScores(state, playerIndex, dice);
            foreach (KeyValuePair<ScoreCategory, int> pair in calculated) scores[pair.Key] = pair.Value;
        }

        public YachtScoreCandidate[] CreateScoreCandidates(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice)
        {
            IReadOnlyList<YachtDieState> scoringDice = GetScoringDice(state, playerIndex, dice);
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            Dictionary<ScoreCategory, int> baseScores = CalculateScoringDiceScores(state, playerIndex, scoringDice);
            int diceBonus = YachtAugmentScoreEngine.CalculateDiceBonus(scoringDice);
            var result = new YachtScoreCandidate[YachtScoreCalculator.ScorableCategories.Length];
            var queryContext = new AugmentQueryContext(state, playerIndex);
            List<IScoreEnhancementModifier> modifiers = YachtAugmentDispatcher.Collect<IScoreEnhancementModifier>(state, playerIndex);

            for (int i = 0; i < result.Length; i++)
            {
                ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                int baseScore = baseScores[category];
                float multiplier = 1f;
                string enhancementSource = null;

                for (int m = 0; m < modifiers.Count; m++)
                {
                    queryContext.BindAugment(((IAugmentHandler)modifiers[m]).Id);
                    if (modifiers[m].TryGetEnhancement(queryContext, category, baseScore, out float mult, out string source))
                    {
                        multiplier = multiplier > 1f ? 2f : mult;
                        enhancementSource = enhancementSource != null ? $"{enhancementSource}+{source}" : source;
                    }
                }

                if (player.DoubleDownActive && baseScore > 0 && (enhancementSource == null || !enhancementSource.Contains("DoubleDown")))
                {
                    multiplier = multiplier > 1f ? 2f : 1.5f;
                    enhancementSource = enhancementSource != null ? $"{enhancementSource}+DoubleDown" : "DoubleDown";
                }

                int enhanced = (int)Math.Floor(baseScore * multiplier);
                int finalScore = baseScore == 0 ? 0 : enhanced + diceBonus;
                result[i] = new YachtScoreCandidate
                {
                    Category = category,
                    BaseScore = baseScore,
                    DiceBonusScore = diceBonus,
                    Score = finalScore,
                    IsEnhanced = multiplier > 1f,
                    EnhancementSource = enhancementSource
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
            return AfterScoreCommit(state, playerIndex, normalRollCount, category, 0, 0, Array.Empty<YachtDieState>(), new SystemRandomSource(0));
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
            var events = new List<YachtGameEvent>();
            var commitContext = new AugmentCommitContext(state, playerIndex, random, events, category, baseScore, finalScore, normalRollCount, dice);
            List<IAfterScoreCommit> commitHandlers = YachtAugmentDispatcher.Collect<IAfterScoreCommit>(state, playerIndex);
            for (int i = 0; i < commitHandlers.Count; i++)
            {
                commitContext.BindAugment(((IAugmentHandler)commitHandlers[i]).Id);
                commitHandlers[i].AfterScoreCommit(commitContext);
            }

            Duel.RecordAndResolve(state, playerIndex, finalScore, events);
            state.AugmentPlayers[playerIndex].TurnsTaken++;
            return events.ToArray();
        }

        public void PrepareTurn(YachtGameState state, int playerIndex, IRandomSource random, bool growPromotion)
        {
            var context = new AugmentTurnContext(state, playerIndex, random, null, growPromotion);
            List<IOnTurnStarted> handlers = YachtAugmentDispatcher.Collect<IOnTurnStarted>(state, playerIndex);
            for (int i = 0; i < handlers.Count; i++)
            {
                context.BindAugment(((IAugmentHandler)handlers[i]).Id);
                handlers[i].OnTurnStarted(context);
            }

        }

        public float GetTurnDuration(YachtGameState state, int playerIndex, float defaultSeconds)
        {
            var query = new AugmentQueryContext(state, playerIndex);
            List<ITurnDurationModifier> modifiers = YachtAugmentDispatcher.Collect<ITurnDurationModifier>(state, playerIndex);
            for (int i = 0; i < modifiers.Count; i++)
            {
                defaultSeconds = modifiers[i].ModifyTurnDuration(query, defaultSeconds);
            }
            return defaultSeconds;
        }

        public void RecalculateStepBonus(YachtGameState state, int playerIndex)
        {
            if (Owns(state, playerIndex, StepByStepId)) StepByStep.TryGrantStepBonus(state, playerIndex);
        }

        private static void TryGrantStepBonus(YachtGameState state, int playerIndex)
        {
            StepByStep.TryGrantStepBonus(state, playerIndex);
        }

        public bool CanUseTableFlip(YachtGameState state, int playerIndex, out YachtCommandErrorCode code, out string message)
        {
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(TableFlipId);
            return (YachtAugmentCatalog.Find(TableFlipId) as IManualActionAugment)?.CanUse(context, out code, out message)
                ?? Fail(YachtCommandErrorCode.AugmentUnavailable, "미지원", out code, out message);
        }

        public void MarkTableFlipUsed(YachtGameState state, int playerIndex)
        {
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(TableFlipId);
            (YachtAugmentCatalog.Find(TableFlipId) as IManualActionAugment)?.Use(context);
        }

        public bool CanUseEquivalentExchange(YachtGameState state, int playerIndex, out YachtCommandErrorCode code, out string message)
        {
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(EquivalentExchangeId);
            return (YachtAugmentCatalog.Find(EquivalentExchangeId) as IManualActionAugment)?.CanUse(context, out code, out message)
                ?? Fail(YachtCommandErrorCode.AugmentUnavailable, "미지원", out code, out message);
        }

        public void MarkEquivalentExchangeUsed(YachtGameState state, int playerIndex)
        {
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(EquivalentExchangeId);
            (YachtAugmentCatalog.Find(EquivalentExchangeId) as IManualActionAugment)?.Use(context);
        }

        public bool TryActivateBeforeRoll(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            out YachtCommandErrorCode code,
            out string message)
        {
            if (YachtAugmentCatalog.Find(augmentId) is not IManualActionAugment action)
                return Fail(YachtCommandErrorCode.AugmentUnavailable, "굴림 전 발동 증강이 아닙니다.", out code, out message);
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(augmentId);
            if (!action.CanUse(context, out code, out message)) return false;
            action.Use(context);
            return true;
        }

        public bool TryUseDiceAlchemy(
            YachtGameState state,
            int playerIndex,
            out YachtCommandErrorCode code,
            out string message)
        {
            if (YachtAugmentCatalog.Find(DiceAlchemyId) is not IManualActionAugment action)
                return Fail(YachtCommandErrorCode.AugmentUnavailable, "미지원", out code, out message);
            var context = new AugmentActionContext(state, playerIndex, null, null);
            context.BindAugment(DiceAlchemyId);
            if (!action.CanUse(context, out code, out message)) return false;
            action.Use(context);
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
            for (int i = 0; i < AllDefinitions.Length; i++)
                if (CanAcquire(state, playerIndex, AllDefinitions[i].Id)) candidates.Add(AllDefinitions[i].Id);
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
            if (definition.Kind == YachtAugmentKind.Modification)
            {
                for (int i = 0; i < player.OwnedIds.Length; i++)
                {
                    YachtAugmentDefinition owned = FindDefinition(player.OwnedIds[i]);
                    if (owned?.Kind == YachtAugmentKind.Modification
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

        private static int RequiredDiceSlots(string augmentId)
        {
            if (YachtAugmentCatalog.Find(augmentId) is IDiceLayoutProvider provider)
                return provider.RequiredDiceSlots;
            return 0;
        }

        private void ApplyAugment(
            YachtGameState state,
            int playerIndex,
            string augmentId,
            ICollection<YachtGameEvent> events,
            bool replacement,
            int visualPreset,
            IRandomSource random)
        {
            YachtAugmentPlayerState runtime = state.AugmentPlayers[playerIndex];
            YachtAugmentDefinition definition = FindDefinition(augmentId);
            if (definition?.IsGlobal == true) state.GlobalAugmentIds = Append(state.GlobalAugmentIds, augmentId);
            else if (replacement)
            {
                int replacedIndex = Array.IndexOf(runtime.OwnedIds, RandomBoxId);
                if (replacedIndex >= 0)
                {
                    runtime.OwnedIds[replacedIndex] = augmentId;
                    runtime.OwnedCardPresetIds = EnsureCardPresetCount(runtime.OwnedCardPresetIds, runtime.OwnedIds.Length);
                    runtime.OwnedCardPresetIds[replacedIndex] = NormalizeCardPreset(visualPreset);
                }
                else
                {
                    runtime.OwnedIds = Append(runtime.OwnedIds, augmentId);
                    runtime.OwnedCardPresetIds = Append(runtime.OwnedCardPresetIds, NormalizeCardPreset(visualPreset));
                }
            }
            else
            {
                runtime.OwnedIds = Append(runtime.OwnedIds, augmentId);
                runtime.OwnedCardPresetIds = Append(runtime.OwnedCardPresetIds, NormalizeCardPreset(visualPreset));
            }

            // 대상 칸 초기화와 추가 턴은 변형 분류 전체에 적용되는 규칙이므로 여기서 처리하고,
            // 증강 하나에만 해당하는 부수 효과는 아래에서 처리기에 위임한다.
            if (definition?.Kind == YachtAugmentKind.Modification)
            {
                if (Enum.TryParse(definition.Target, out ScoreCategory target)) ResetFilledTarget(state, playerIndex, target, runtime);
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

            if (YachtAugmentCatalog.Find(augmentId) is IOnAugmentSelected selected)
            {
                var selection = new AugmentSelectionContext(state, playerIndex, random, events, replacement);
                selection.BindAugment(augmentId);
                selected.OnSelected(selection);
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
                int randomBoxIndex = Array.IndexOf(player.OwnedIds, RandomBoxId);
                int inheritedPreset = randomBoxIndex >= 0 && randomBoxIndex < (player.OwnedCardPresetIds?.Length ?? 0)
                    ? NormalizeCardPreset(player.OwnedCardPresetIds[randomBoxIndex])
                    : 0;
                var candidates = new List<string>();
                for (int i = 0; i < AllDefinitions.Length; i++)
                {
                    YachtAugmentDefinition definition = AllDefinitions[i];
                    if (definition.Id == RandomBoxId || definition.IsQuest || !CanAcquire(state, playerIndex, definition.Id)) continue;
                    candidates.Add(definition.Id);
                }
                Shuffle(candidates, random);
                if (candidates.Count == 0) continue;

                string awarded = candidates[0];
                player.RandomBoxAwardId = awarded;
                ApplyAugment(state, playerIndex, awarded, events, true, inheritedPreset, random);
            }
        }

        private static int[] CreateCardPresetIds(int count, IRandomSource visualRandom)
        {
            int[] values = { 0, 1, 2, 3 };
            for (int i = values.Length - 1; i > 0; i--)
            {
                int index = visualRandom.NextInt(0, i + 1);
                (values[index], values[i]) = (values[i], values[index]);
            }
            var result = new int[Math.Min(count, values.Length)];
            Array.Copy(values, result, result.Length);
            return result;
        }

        public static int NormalizeCardPreset(int value) => value == 4
            ? CardVisualPresetCount - 1
            : value >= 0 && value < CardVisualPresetCount ? value : 0;

        private static void Shuffle<T>(IList<T> values, IRandomSource random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int index = random.NextInt(0, i + 1);
                (values[index], values[i]) = (values[i], values[index]);
            }
        }

        private IReadOnlyList<YachtDieState> GetScoringDice(
            YachtGameState state,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice)
        {
            var context = new AugmentQueryContext(state, playerIndex);
            List<IScoringDiceFilter> filters = YachtAugmentDispatcher.Collect<IScoringDiceFilter>(state, playerIndex);
            IReadOnlyList<YachtDieState> result = dice;
            for (int i = 0; i < filters.Count; i++)
            {
                context.BindAugment(((IAugmentHandler)filters[i]).Id);
                result = filters[i].FilterScoringDice(context, result);
            }
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

        private static int[] EnsureCardPresetCount(IReadOnlyList<int> values, int count)
        {
            var result = new int[Math.Max(0, count)];
            for (int i = 0; i < result.Length; i++)
                result[i] = i < (values?.Count ?? 0) ? NormalizeCardPreset(values[i]) : 0;
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
