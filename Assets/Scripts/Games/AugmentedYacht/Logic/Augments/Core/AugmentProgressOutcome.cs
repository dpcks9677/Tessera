namespace Tessera.Games.Yacht
{
    /// <summary>
    /// <see cref="IAugmentProgressText"/>가 보고하는 진행 상태의 결과 구분입니다.
    /// UI는 이 값으로 진행 중·성공·실패를 색으로 구분합니다.
    /// </summary>
    public enum AugmentProgressOutcome
    {
        InProgress,
        Succeeded,
        Failed,
    }
}
