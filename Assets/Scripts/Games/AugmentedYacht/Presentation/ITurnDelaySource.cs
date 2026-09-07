using System;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 턴 제한 시간을 재고, 턴을 넘겨도 되는 시점을 알리는 쪽(M10-T6b).
    ///
    /// 지금은 모래시계가 이 역할을 한다. 턴 흐름 코드가 모래시계를 직접 부르고 있어서
    /// 장식 프롭을 빼거나 다른 연출로 바꾸면 게임 진행이 멈추는 구조였다.
    /// 이 인터페이스를 사이에 두면 턴 흐름은 "시간을 재는 무언가"만 알면 된다.
    ///
    /// <see cref="Started"/>는 타이머가 켜졌다는 뜻이 아니라 <b>시작 연출까지 끝나</b>
    /// 턴을 진행해도 좋다는 신호다. 모래시계의 경우 뒤집기 애니메이션이 끝난 시점이다.
    /// </summary>
    public interface ITurnDelaySource
    {
        event Action Started;
        event Action<float, float> Ticked;
        event Action Expired;

        void Begin(float seconds, bool animate);
        void SetIdle(float seconds);
        void Reset(float seconds);
        void Pause();
        void Resume();

        /// <param name="hideVisual">
        /// 남은 시간 표현까지 감출지 여부. 턴이 끝났을 뿐 다음 턴이 이어질 때는 <c>false</c>로 두어
        /// 진행 연출을 유지하고, 게임이 끝났을 때만 <c>true</c>로 정리한다.
        /// </param>
        void Stop(bool hideVisual = true);
    }
}
