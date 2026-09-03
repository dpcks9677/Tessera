namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 증강 하나의 정의와 처리 규칙입니다. 구현체는 필요한 발동 시점 인터페이스만
    /// 함께 구현하며, 디스패처가 구현된 인터페이스를 보고 호출 대상을 정합니다.
    /// </summary>
    public interface IAugmentHandler
    {
        string Id { get; }

        /// <summary>같은 시점에서 여러 증강이 발동할 때의 처리 순서입니다. 작을수록 먼저 처리합니다.</summary>
        int Order { get; }

        YachtAugmentDefinition CreateDefinition();
    }

    /// <summary>증강 처리기의 공통 기본 구현입니다.</summary>
    public abstract class AugmentHandler : IAugmentHandler
    {
        public abstract string Id { get; }

        public virtual int Order => 0;

        public abstract YachtAugmentDefinition CreateDefinition();
    }

    /// <summary>증강을 획득했을 때 호출됩니다. 전용 상태의 초기화를 여기서 합니다.</summary>
    public interface IOnAugmentSelected
    {
        void OnSelected(AugmentSelectionContext context);
    }

    /// <summary>턴이 시작될 때 호출됩니다. 주사위를 만들기 전 단계입니다.</summary>
    public interface IOnTurnStarted
    {
        void OnTurnStarted(AugmentTurnContext context);
    }

    /// <summary>점수를 확정한 뒤 호출됩니다. 퀘스트 진행과 보상을 여기서 처리합니다.</summary>
    public interface IAfterScoreCommit
    {
        void AfterScoreCommit(AugmentCommitContext context);
    }

    /// <summary>턴이 끝날 때 호출됩니다. <see cref="IAfterScoreCommit"/> 다음에 처리합니다.</summary>
    public interface IOnTurnEnded
    {
        void OnTurnEnded(AugmentCommitContext context);
    }

    /// <summary>점수 미리보기를 만들 때 호출됩니다. 족보 점수를 교체하거나 보정합니다.</summary>
    public interface IBeforeScorePreview
    {
        void ModifyScores(AugmentScoreContext context);
    }

    /// <summary>이번 턴에 사용할 주사위 개수를 보정합니다.</summary>
    public interface IDiceCountModifier
    {
        int ModifyDiceCount(AugmentQueryContext context, int diceCount);
    }

    /// <summary>이번 턴에 사용할 주사위 종류를 배정합니다.</summary>
    public interface IDiceLayoutProvider
    {
        void ConfigureDice(AugmentDiceContext context);
    }

    /// <summary>턴 제한 시간을 보정합니다.</summary>
    public interface ITurnDurationModifier
    {
        float ModifyTurnDuration(AugmentQueryContext context, float seconds);
    }

    /// <summary>플레이어가 직접 발동하는 증강입니다.</summary>
    public interface IManualActionAugment
    {
        /// <summary>이 행동을 사용할 수 있는 단계입니다.</summary>
        YachtGamePhase RequiredPhase { get; }

        /// <summary>사용 시 주사위를 다시 굴려야 하면 true입니다.</summary>
        bool RerollsDice { get; }

        bool CanUse(AugmentActionContext context, out YachtCommandErrorCode code, out string message);

        /// <summary>사용을 확정합니다. <see cref="CanUse"/>가 true를 반환한 뒤에만 호출됩니다.</summary>
        void Use(AugmentActionContext context);
    }
}
