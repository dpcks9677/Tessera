using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 증강 처리기가 게임 상태에 접근하는 통로입니다. 처리기는 <see cref="YachtGameState"/>를
    /// 직접 수정하지 않고 이 컨텍스트의 메서드로 상태를 바꾸고 이벤트를 발행합니다.
    /// </summary>
    public abstract class AugmentContext
    {
        private readonly ICollection<YachtGameEvent> events;

        protected AugmentContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            ICollection<YachtGameEvent> events)
        {
            Game = game;
            PlayerIndex = playerIndex;
            Random = random;
            this.events = events;
        }

        public YachtGameState Game { get; }
        public int PlayerIndex { get; }
        public IRandomSource Random { get; }

        /// <summary>현재 처리 중인 증강의 ID입니다. 디스패처가 처리기를 호출하기 직전에 지정합니다.</summary>
        public string AugmentId { get; private set; }

        public PlayerScoreData Score => Game.Players[PlayerIndex];

        public YachtAugmentPlayerState Player => Game.AugmentPlayers[PlayerIndex];

        /// <summary>플레이어가 특정 증강을 보유하고 있는지 확인합니다.</summary>
        public bool Owns(string augmentId)
        {
            if (Player?.OwnedIds != null)
            {
                for (int i = 0; i < Player.OwnedIds.Length; i++)
                    if (string.Equals(Player.OwnedIds[i], augmentId, System.StringComparison.Ordinal)) return true;
            }
            if (Game?.GlobalAugmentIds != null)
            {
                for (int i = 0; i < Game.GlobalAugmentIds.Length; i++)
                    if (string.Equals(Game.GlobalAugmentIds[i], augmentId, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>현재 증강의 전용 상태를 가져옵니다. 없으면 만들어집니다.</summary>
        public T State<T>() where T : class, IAugmentState, new() => Player.States.GetOrCreate<T>(AugmentId);

        /// <summary>증강 발동 이벤트를 발행합니다.</summary>
        public void Emit(string message, int score = 0)
        {
            events?.Add(new YachtGameEvent
            {
                Type = YachtGameEventType.AugmentTriggered,
                PlayerIndex = PlayerIndex,
                AugmentId = AugmentId,
                Score = score,
                Message = message
            });
        }

        /// <summary>증강 보너스 점수를 더하고 총점을 다시 계산한 뒤 이벤트를 발행합니다.</summary>
        public void AddBonus(int points, string message)
        {
            Score.augmentBonusScore += points;
            Score.RecalculateTotal();
            Emit(message, points);
        }

        internal void BindAugment(string augmentId) => AugmentId = augmentId;
    }

    /// <summary>증강을 획득한 시점입니다.</summary>
    public sealed class AugmentSelectionContext : AugmentContext
    {
        public AugmentSelectionContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            ICollection<YachtGameEvent> events,
            bool isRandomBoxReplacement)
            : base(game, playerIndex, random, events)
        {
            IsRandomBoxReplacement = isRandomBoxReplacement;
        }

        /// <summary>랜덤 박스가 이 증강으로 교체되어 획득된 경우 true입니다.</summary>
        public bool IsRandomBoxReplacement { get; }
    }

    /// <summary>턴이 시작된 시점입니다. 주사위를 만들기 전에 호출됩니다.</summary>
    public sealed class AugmentTurnContext : AugmentContext
    {
        public AugmentTurnContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            ICollection<YachtGameEvent> events,
            bool growPromotion)
            : base(game, playerIndex, random, events)
        {
            GrowPromotion = growPromotion;
        }

        /// <summary>이 턴에 성장형 증강의 값을 올릴지 여부입니다.</summary>
        public bool GrowPromotion { get; }
    }

    /// <summary>
    /// 이번 턴에 사용할 주사위를 구성하는 시점입니다. 슬롯 커서를 처리기들이 공유하므로
    /// 먼저 호출된 증강이 앞쪽 주사위를 차지합니다.
    /// </summary>
    public sealed class AugmentDiceContext : AugmentContext
    {
        private int nextSlot;

        public AugmentDiceContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            IList<YachtDieState> dice)
            : base(game, playerIndex, random, null)
        {
            Dice = dice;
        }

        public IList<YachtDieState> Dice { get; }

        /// <summary>남은 슬롯이 있는 만큼 주사위 종류를 배정하고 실제 배정한 개수를 반환합니다.</summary>
        public int Assign(YachtDieType type, int count)
        {
            int assigned = 0;
            while (assigned < count && nextSlot < Dice.Count)
            {
                Dice[nextSlot].Type = type;
                assigned++;
                nextSlot++;
            }
            return assigned;
        }

        /// <summary>다음 슬롯 하나를 배정하고 그 주사위를 반환합니다. 남은 슬롯이 없으면 null입니다.</summary>
        public YachtDieState AssignOne(YachtDieType type)
        {
            if (nextSlot >= Dice.Count) return null;
            YachtDieState die = Dice[nextSlot];
            die.Type = type;
            nextSlot++;
            return die;
        }
    }

    /// <summary>점수 미리보기를 만드는 시점입니다. 족보 점수를 교체하거나 보정합니다.</summary>
    public sealed class AugmentScoreContext : AugmentContext
    {
        public AugmentScoreContext(
            YachtGameState game,
            int playerIndex,
            IReadOnlyList<YachtDieState> dice,
            IDictionary<ScoreCategory, int> scores,
            YachtDiceFacts facts)
            : base(game, playerIndex, null, null)
        {
            Dice = dice;
            Scores = scores;
            Facts = facts;
        }

        /// <summary>점수 계산에 실제로 사용되는 주사위입니다.</summary>
        public IReadOnlyList<YachtDieState> Dice { get; }

        /// <summary>족보별 점수입니다. 처리기가 자기 대상 칸만 덮어씁니다.</summary>
        public IDictionary<ScoreCategory, int> Scores { get; }

        /// <summary>같은 주사위에 대해 한 번만 계산된 족보 판정 결과입니다.</summary>
        public YachtDiceFacts Facts { get; }
    }

    /// <summary>점수를 확정한 시점과 턴이 끝나는 시점에 공통으로 쓰입니다.</summary>
    public sealed class AugmentCommitContext : AugmentContext
    {
        public AugmentCommitContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            ICollection<YachtGameEvent> events,
            ScoreCategory category,
            int baseScore,
            int finalScore,
            int normalRollCount,
            IReadOnlyList<YachtDieState> dice)
            : base(game, playerIndex, random, events)
        {
            Category = category;
            BaseScore = baseScore;
            FinalScore = finalScore;
            NormalRollCount = normalRollCount;
            Dice = dice;
        }

        public ScoreCategory Category { get; }
        public int BaseScore { get; }
        public int FinalScore { get; }

        /// <summary>이번 턴에 사용한 기본 굴림 횟수입니다. 판 뒤집기 같은 추가 굴림은 세지 않습니다.</summary>
        public int NormalRollCount { get; }

        public IReadOnlyList<YachtDieState> Dice { get; }

        /// <summary>이번 턴이 이 플레이어의 몇 번째 턴인지입니다. 1부터 시작합니다.</summary>
        public int TurnNumber => Player.TurnsTaken + 1;
    }

    /// <summary>수동 행동 증강을 사용하는 시점입니다.</summary>
    public sealed class AugmentActionContext : AugmentContext
    {
        public AugmentActionContext(
            YachtGameState game,
            int playerIndex,
            IRandomSource random,
            ICollection<YachtGameEvent> events)
            : base(game, playerIndex, random, events)
        {
        }
    }

    /// <summary>상태를 바꾸지 않고 값만 보정할 때 쓰는 컨텍스트입니다.</summary>
    public sealed class AugmentQueryContext : AugmentContext
    {
        public AugmentQueryContext(YachtGameState game, int playerIndex)
            : base(game, playerIndex, null, null)
        {
        }
    }
}
