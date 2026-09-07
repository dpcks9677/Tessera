namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 화면이 지금 무엇을 하고 있는가(M10-T6).
    ///
    /// 예전에는 <c>turnTransitionInProgress</c>, <c>hasCompletedRoll</c>, <c>isArranging</c>와
    /// 코루틴 핸들 두 개의 null 검사가 흩어져 두 번째 상태 머신을 손으로 굴렸다. 조합에 따라
    /// 존재할 수 없는 상태(예: 굴리는 중이면서 정렬 중)가 표현 가능했고 동기화 보장이 없었다.
    /// 이 열거형은 그 조합을 실제로 존재하는 여섯 가지로 좁힌다.
    ///
    /// 권위 계층의 <see cref="Tessera.Games.Yacht.YachtGamePhase"/>와는 다른 축이다.
    /// 저쪽은 규칙상 무엇을 할 수 있는지, 이쪽은 연출이 끝났는지를 말한다.
    /// </summary>
    public enum PresentationPhase
    {
        /// <summary>게임 시작 전, 증강 드래프트 중, 게임 종료 후. 굴림은 못 하지만 UI는 살아 있다.</summary>
        Idle,

        /// <summary>턴 넘김 연출이 진행 중. 입력을 받지 않는다.</summary>
        TurnTransition,

        /// <summary>턴이 시작됐고 아직 굴리지 않았다.</summary>
        AwaitingRoll,

        /// <summary>굴림 궤적과 그 뒤의 정렬 애니메이션이 진행 중.</summary>
        Rolling,

        /// <summary>킵 토글에 따른 재정렬 애니메이션이 진행 중. 굴림은 이미 끝난 상태다.</summary>
        Arranging,

        /// <summary>굴림이 끝나 눈이 확정됐고 킵·점수 선택 입력을 기다린다.</summary>
        Settled
    }

    public static class PresentationPhaseExtensions
    {
        /// <summary>
        /// 화면이 입력을 받을 수 있는 안정 상태인가.
        /// 굴림 버튼, 증강 행동 버튼, 판 뒤집기가 모두 이 조건을 본다.
        /// </summary>
        public static bool IsInteractive(this PresentationPhase phase)
        {
            return phase == PresentationPhase.Idle
                || phase == PresentationPhase.AwaitingRoll
                || phase == PresentationPhase.Settled;
        }

        /// <summary>주사위가 굴러 멈춘 뒤인가. 굴림 결과를 읽어도 되는 시점이다.</summary>
        public static bool HasCompletedRoll(this PresentationPhase phase)
        {
            return phase == PresentationPhase.Settled || phase == PresentationPhase.Arranging;
        }
    }
}
