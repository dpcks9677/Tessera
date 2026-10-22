namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 대상 족보 한 칸을 다른 규칙으로 교체하는 변형 증강의 공통 기반입니다.
    /// 대상 칸이 이미 채워진 상태에서 획득했을 때의 초기화와 추가 턴은
    /// 분류 전체에 적용되는 규칙이라 <see cref="YachtAugmentRuntime"/>이 담당합니다.
    /// </summary>
    public abstract class ModificationAugment : AugmentHandler, IBeforeScorePreview
    {
        public abstract string DisplayName { get; }

        public abstract string Description { get; }

        /// <summary>이 증강이 교체하는 족보 칸입니다.</summary>
        public abstract ScoreCategory Target { get; }

        /// <summary>1라운드에만 획득할 수 있으면 true입니다.</summary>
        public virtual bool PhaseOneOnly => false;

        public override YachtAugmentDefinition CreateDefinition() => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Description = Description,
            Target = Target.ToString(),
            Kind = YachtAugmentKind.Modification,
            PhaseOneOnly = PhaseOneOnly
        };

        public void ModifyScores(AugmentScoreContext context) =>
            context.Scores[Target] = CalculateScore(context.Facts);

        /// <summary>대상 칸에 넣을 점수입니다.</summary>
        protected abstract int CalculateScore(YachtDiceFacts facts);
    }
}
