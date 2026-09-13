namespace Tessera.Games.Yacht
{
    /// <summary>줄 판정에 필요한 것만 담은 조회 쿼리입니다. UI는 이 쿼리를 몰라도 됩니다.</summary>
    public readonly struct AugmentProgressQuery
    {
        public AugmentProgressQuery(IReadOnlyYachtAugmentPlayerState player, IReadOnlyPlayerScoreData scores)
        {
            Player = player;
            Scores = scores;
        }

        public readonly IReadOnlyYachtAugmentPlayerState Player;
        public readonly IReadOnlyPlayerScoreData Scores;
    }

    /// <summary>
    /// 진행 상태를 카드 배지에 짧게 보고할 수 있는 증강 상태입니다.
    /// 진행도 개념이 없는 상태(<see cref="IAugmentState"/>만 구현하는 경우)는 이 인터페이스를
    /// 구현하지 않아도 됩니다.
    /// </summary>
    public interface IAugmentProgressText
    {
        AugmentProgress DescribeProgress(in AugmentProgressQuery query);
    }
}
