using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tessera.Games.Yacht
{
    public enum YachtGameMode { Normal, Augmented }

    public enum YachtGamePhase
    {
        WaitingToStart,
        Draft,
        TurnReady,
        ScoreSelection,
        TurnTransition,
        GameOver
    }

    public enum ScoreCategory
    {
        Aces = 0,
        Deuces = 1,
        Threes = 2,
        Fours = 3,
        Fives = 4,
        Sixes = 5,
        Bonus = 6,
        Choice = 7,
        FourOfAKind = 8,
        FullHouse = 9,
        SmallStraight = 10,
        LargeStraight = 11,
        Yacht = 12,
        Total = 13
    }

    public enum YachtDieType { Normal, Heavy, Golden, Octahedron, Weird, Promotion, Couple, Sevens }
    public enum YachtCommandType
    {
        StartGame,
        SelectAugment,
        RollDice,
        SetDieKept,
        CommitScore,
        ResolveTimeout,
        UseAugmentAction,
        AdvanceTurn
    }

    public enum YachtCommandErrorCode
    {
        None,
        InvalidCommand,
        DuplicateCommand,
        RevisionMismatch,
        InvalidPlayer,
        NotCurrentPlayer,
        InvalidPhase,
        NoRollsRemaining,
        AllDiceKept,
        DieNotFound,
        RollRequired,
        CategoryAlreadyFilled,
        CategoryUnavailable,
        NotDrafting,
        AugmentNotOffered,
        AugmentUnavailable,
        AugmentConflict,
        AugmentRequired,
        AugmentAlreadyUsed
    }

    public enum YachtGameEventType
    {
        GameStarted,
        DraftStarted,
        AugmentSelected,
        AugmentReplaced,
        AugmentTriggered,
        AugmentActionUsed,
        DiceRolled,
        DieKeepChanged,
        ScoreCommitted,
        TurnAdvanced,
        GameEnded,
        TimeoutResolved
    }

    [Serializable]
    public sealed class PlayerScoreData
    {
        public int[] upperScores = { -1, -1, -1, -1, -1, -1 };
        public int[] upperBaseScores = { -1, -1, -1, -1, -1, -1 };
        public bool[] upperFilled = new bool[6];
        public bool hasBonus;
        public int bonusScore;
        public int upperBonusThreshold = 63;
        public bool stepBonusGranted;
        public int augmentBonusScore;
        public int[] lowerScores = { -1, -1, -1, -1, -1, -1 };
        public int[] lowerBaseScores = { -1, -1, -1, -1, -1, -1 };
        public bool[] lowerFilled = new bool[6];
        public int totalScore;

        public void Reset()
        {
            EnsureArrays();
            for (int i = 0; i < 6; i++) upperScores[i] = -1;
            for (int i = 0; i < 6; i++) upperBaseScores[i] = -1;
            Array.Clear(upperFilled, 0, upperFilled.Length);
            for (int i = 0; i < 6; i++) lowerScores[i] = -1;
            for (int i = 0; i < 6; i++) lowerBaseScores[i] = -1;
            Array.Clear(lowerFilled, 0, lowerFilled.Length);
            hasBonus = false;
            bonusScore = 0;
            upperBonusThreshold = 63;
            stepBonusGranted = false;
            augmentBonusScore = 0;
            totalScore = 0;
        }

        public int CalculateUpperSum()
        {
            EnsureArrays();
            int sum = 0;
            for (int i = 0; i < 6; i++)
            {
                if (upperFilled[i] || upperScores[i] != -1) sum += upperScores[i];
            }
            return sum;
        }

        public void RecalculateTotal()
        {
            EnsureArrays();
            int upperSum = CalculateUpperSum();
            hasBonus = stepBonusGranted || upperSum >= upperBonusThreshold;
            bonusScore = hasBonus ? (stepBonusGranted ? 55 : 35) : 0;
            int sum = upperSum + bonusScore + augmentBonusScore;
            for (int i = 0; i < 6; i++)
            {
                if (lowerFilled[i] || lowerScores[i] != -1) sum += lowerScores[i];
            }
            totalScore = sum;
        }

        public PlayerScoreData Clone()
        {
            EnsureArrays();
            return new PlayerScoreData
            {
                upperScores = (int[])upperScores.Clone(),
                upperBaseScores = (int[])upperBaseScores.Clone(),
                upperFilled = (bool[])upperFilled.Clone(),
                hasBonus = hasBonus,
                bonusScore = bonusScore,
                upperBonusThreshold = upperBonusThreshold,
                stepBonusGranted = stepBonusGranted,
                augmentBonusScore = augmentBonusScore,
                lowerScores = (int[])lowerScores.Clone(),
                lowerBaseScores = (int[])lowerBaseScores.Clone(),
                lowerFilled = (bool[])lowerFilled.Clone(),
                totalScore = totalScore
            };
        }

        private void EnsureArrays()
        {
            if (upperScores == null || upperScores.Length != 6) upperScores = new[] { -1, -1, -1, -1, -1, -1 };
            if (lowerScores == null || lowerScores.Length != 6) lowerScores = new[] { -1, -1, -1, -1, -1, -1 };
            if (upperBaseScores == null || upperBaseScores.Length != 6) upperBaseScores = new[] { -1, -1, -1, -1, -1, -1 };
            if (lowerBaseScores == null || lowerBaseScores.Length != 6) lowerBaseScores = new[] { -1, -1, -1, -1, -1, -1 };
            if (upperFilled == null || upperFilled.Length != 6) upperFilled = new bool[6];
            if (lowerFilled == null || lowerFilled.Length != 6) lowerFilled = new bool[6];
        }
    }

    [Serializable]
    public sealed class YachtDieState
    {
        public int Id;
        public YachtDieType Type;
        public int Value;
        public bool IsKept;
        public int KeepSlotIndex = -1;
        public int PromotionLevel;
        public YachtDieState Clone() => (YachtDieState)MemberwiseClone();
    }

    [Serializable]
    public sealed class YachtScoreCandidate
    {
        public ScoreCategory Category;
        public int Score;
        public int BaseScore;
        public int DiceBonusScore;
        public bool IsEnhanced;
        public string EnhancementSource;
        public YachtScoreCandidate Clone() => (YachtScoreCandidate)MemberwiseClone();
    }

    [Serializable]
    public sealed class YachtGameState
    {
        public int Version = 1;
        public long Revision;
        public YachtGameMode Mode;
        public YachtGamePhase Phase;
        public int CurrentPlayerIndex;
        public int CurrentRound = 1;
        public int RollsRemaining = YachtGameSession.MaxRolls;
        public bool HasRolled;
        public YachtDieState[] Dice = Array.Empty<YachtDieState>();
        public PlayerScoreData[] Players = Array.Empty<PlayerScoreData>();
        public YachtScoreCandidate[] Candidates = Array.Empty<YachtScoreCandidate>();
        public YachtDraftState Draft = new();
        public YachtAugmentPlayerState[] AugmentPlayers = Array.Empty<YachtAugmentPlayerState>();
        public string[] GlobalAugmentIds = Array.Empty<string>();
        public bool IsExtraTurnPhase;
        public int[] RoundScores = { int.MinValue, int.MinValue };
        public int RoundScoresRound;

        public YachtGameState Clone()
        {
            var clone = (YachtGameState)MemberwiseClone();
            clone.Dice = CloneDice(Dice);
            clone.Candidates = CloneCandidates(Candidates);
            clone.Draft = Draft?.Clone() ?? new YachtDraftState();
            clone.AugmentPlayers = new YachtAugmentPlayerState[AugmentPlayers?.Length ?? 0];
            for (int i = 0; i < clone.AugmentPlayers.Length; i++)
                clone.AugmentPlayers[i] = AugmentPlayers[i]?.Clone() ?? new YachtAugmentPlayerState();
            clone.GlobalAugmentIds = (string[])(GlobalAugmentIds?.Clone() ?? Array.Empty<string>());
            clone.RoundScores = (int[])(RoundScores?.Clone() ?? Array.Empty<int>());
            clone.Players = new PlayerScoreData[Players.Length];
            for (int i = 0; i < Players.Length; i++) clone.Players[i] = Players[i]?.Clone();
            return clone;
        }

        internal static YachtDieState[] CloneDice(IReadOnlyList<YachtDieState> source)
        {
            var clone = new YachtDieState[source?.Count ?? 0];
            for (int i = 0; i < clone.Length; i++) clone[i] = source[i]?.Clone();
            return clone;
        }

        private static YachtScoreCandidate[] CloneCandidates(IReadOnlyList<YachtScoreCandidate> source)
        {
            var clone = new YachtScoreCandidate[source?.Count ?? 0];
            for (int i = 0; i < clone.Length; i++) clone[i] = source[i]?.Clone();
            return clone;
        }
    }

    [Serializable]
    public sealed class YachtGameOptions
    {
        public const float DefaultTurnDurationSeconds = 60f;

        public YachtGameMode Mode = YachtGameMode.Normal;
        public int PlayerCount = YachtGameSession.PlayerCount;
        public int DiceCount = 5;
        public int PresetClipCount = 20;
        public float TurnDurationSeconds = DefaultTurnDurationSeconds;
        public YachtGameOptions Clone() => (YachtGameOptions)MemberwiseClone();
    }

    [Serializable]
    public sealed class YachtGameCommand
    {
        public string CommandId;
        public long ExpectedRevision;
        public int PlayerIndex;
        public YachtCommandType Type;
        public int DieId;
        public bool IsKept;
        public ScoreCategory Category;
        public string AugmentId;
    }

    [Serializable]
    public sealed class YachtGameEvent
    {
        public YachtGameEventType Type;
        public int PlayerIndex;
        public ScoreCategory Category;
        public int Score;
        public int DieId;
        public string AugmentId;
        public string RelatedAugmentId;
        public string Message;
    }

    [Serializable]
    public sealed class YachtDieResult
    {
        public int Id;
        public YachtDieType Type;
        public int Value;
    }

    [Serializable]
    public sealed class RollPresentation
    {
        public string PresetFile;
        public int PresetIndex;
        public bool IsMirrored;
        public YachtDieResult[] FinalValues = Array.Empty<YachtDieResult>();
        public float DurationSeconds;
    }

    [Serializable]
    public sealed class YachtGameCommandResult
    {
        public bool Accepted;
        public YachtCommandErrorCode ErrorCode;
        public string ErrorMessage;
        public YachtGameState State;
        public YachtGameEvent[] Events = Array.Empty<YachtGameEvent>();
        public RollPresentation RollPresentation;
    }

    public readonly struct YachtTurnResult
    {
        public YachtTurnResult(int scoredPlayerIndex, ScoreCategory category, int score, bool gameEnded)
        {
            ScoredPlayerIndex = scoredPlayerIndex;
            Category = category;
            Score = score;
            GameEnded = gameEnded;
        }
        public int ScoredPlayerIndex { get; }
        public ScoreCategory Category { get; }
        public int Score { get; }
        public bool GameEnded { get; }
    }

    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);
        bool NextBool();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;
        public SystemRandomSource() : this(Environment.TickCount) { }
        public SystemRandomSource(int seed) => random = new Random(seed);
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return random.Next(minInclusive, maxExclusive);
        }
        public bool NextBool() => random.Next(0, 2) == 1;
    }

    public interface IYachtRuleSet
    {
        YachtGameMode Mode { get; }
        YachtDieState[] CreateInitialDice(int diceCount);
        int RollValue(YachtDieState die, IRandomSource random);
        Dictionary<ScoreCategory, int> CalculateScores(IReadOnlyList<YachtDieState> dice);
        string SelectPresetFile(IReadOnlyList<YachtDieState> dice);
    }

    public sealed class NormalYachtRuleSet : IYachtRuleSet
    {
        public YachtGameMode Mode => YachtGameMode.Normal;
        public YachtDieState[] CreateInitialDice(int diceCount)
        {
            var dice = new YachtDieState[diceCount];
            for (int i = 0; i < dice.Length; i++)
            {
                dice[i] = new YachtDieState { Id = i + 1, Type = YachtDieType.Normal, Value = i + 1 };
            }
            return dice;
        }
        public int RollValue(YachtDieState die, IRandomSource random) => random.NextInt(1, 7);
        public Dictionary<ScoreCategory, int> CalculateScores(IReadOnlyList<YachtDieState> dice)
        {
            var values = new int[dice.Count];
            for (int i = 0; i < dice.Count; i++) values[i] = dice[i].Value;
            return YachtScoreCalculator.Calculate(values);
        }
        public string SelectPresetFile(IReadOnlyList<YachtDieState> dice) => $"dice_presets_normal_{dice.Count}.json";
    }

    /// <summary>M5부터 증강 처리기를 추가할 기본 규칙 합성 지점입니다.</summary>
    public sealed class AugmentedYachtRuleSet : IYachtRuleSet
    {
        private readonly IYachtRuleSet baseRules;
        public AugmentedYachtRuleSet(IYachtRuleSet baseRules) => this.baseRules = baseRules ?? throw new ArgumentNullException(nameof(baseRules));
        public YachtGameMode Mode => YachtGameMode.Augmented;
        public IYachtRuleSet BaseRules => baseRules;
        public YachtDieState[] CreateInitialDice(int diceCount) => baseRules.CreateInitialDice(diceCount);
        public int RollValue(YachtDieState die, IRandomSource random) => baseRules.RollValue(die, random);
        public Dictionary<ScoreCategory, int> CalculateScores(IReadOnlyList<YachtDieState> dice) => baseRules.CalculateScores(dice);
        public string SelectPresetFile(IReadOnlyList<YachtDieState> dice) => baseRules.SelectPresetFile(dice);
    }

    public static class YachtRuleSetFactory
    {
        public static IYachtRuleSet Create(YachtGameMode mode)
        {
            IYachtRuleSet normal = new NormalYachtRuleSet();
            return mode == YachtGameMode.Augmented ? new AugmentedYachtRuleSet(normal) : normal;
        }
    }

    public interface IGameAuthority
    {
        YachtGameState CurrentState { get; }
        Task<YachtGameCommandResult> ExecuteAsync(YachtGameCommand command);
    }

    public static class YachtScoreCalculator
    {
        public static readonly ScoreCategory[] ScorableCategories =
        {
            ScoreCategory.Aces, ScoreCategory.Deuces, ScoreCategory.Threes,
            ScoreCategory.Fours, ScoreCategory.Fives, ScoreCategory.Sixes,
            ScoreCategory.Choice, ScoreCategory.FourOfAKind, ScoreCategory.FullHouse,
            ScoreCategory.SmallStraight, ScoreCategory.LargeStraight, ScoreCategory.Yacht
        };

        public static Dictionary<ScoreCategory, int> Calculate(IReadOnlyList<int> dice)
        {
            if (dice == null || dice.Count != 5) throw new ArgumentException("기본 요트 점수 계산에는 주사위 5개가 필요합니다.", nameof(dice));
            int[] counts = new int[7];
            int sum = 0;
            for (int i = 0; i < dice.Count; i++)
            {
                int value = dice[i];
                if (value < 1 || value > 6) throw new ArgumentOutOfRangeException(nameof(dice), "기본 주사위 눈은 1~6이어야 합니다.");
                counts[value]++;
                sum += value;
            }

            bool fourOfAKind = false;
            bool yacht = false;
            bool hasThree = false;
            bool hasTwo = false;
            for (int value = 1; value <= 6; value++)
            {
                fourOfAKind |= counts[value] >= 4;
                yacht |= counts[value] == 5;
                hasThree |= counts[value] == 3;
                hasTwo |= counts[value] == 2;
            }

            bool fullHouse = yacht || (hasThree && hasTwo);
            bool smallStraight = HasSequence(counts, 1, 4) || HasSequence(counts, 2, 5) || HasSequence(counts, 3, 6);
            bool largeStraight = HasSequence(counts, 1, 5) || HasSequence(counts, 2, 6);
            return new Dictionary<ScoreCategory, int>
            {
                [ScoreCategory.Aces] = counts[1], [ScoreCategory.Deuces] = counts[2] * 2,
                [ScoreCategory.Threes] = counts[3] * 3, [ScoreCategory.Fours] = counts[4] * 4,
                [ScoreCategory.Fives] = counts[5] * 5, [ScoreCategory.Sixes] = counts[6] * 6,
                [ScoreCategory.Choice] = sum, [ScoreCategory.FourOfAKind] = fourOfAKind ? sum : 0,
                [ScoreCategory.FullHouse] = fullHouse ? sum : 0,
                [ScoreCategory.SmallStraight] = smallStraight ? 15 : 0,
                [ScoreCategory.LargeStraight] = largeStraight ? 30 : 0,
                [ScoreCategory.Yacht] = yacht ? 50 : 0
            };
        }

        public static bool IsScorable(ScoreCategory category) =>
            category >= ScoreCategory.Aces && category <= ScoreCategory.Sixes
            || category >= ScoreCategory.Choice && category <= ScoreCategory.Yacht;

        private static bool HasSequence(IReadOnlyList<int> counts, int first, int last)
        {
            for (int value = first; value <= last; value++) if (counts[value] == 0) return false;
            return true;
        }
    }
}
