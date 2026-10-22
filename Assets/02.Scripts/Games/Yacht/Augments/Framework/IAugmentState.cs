namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 증강 하나가 소유하는 진행 상태입니다. 증강별 상태는 이 인터페이스를 구현한
    /// 자신의 클래스에 두며, 공용 <see cref="YachtAugmentPlayerState"/>에 필드를 추가하지 않습니다.
    /// 구현체는 <see cref="AugmentStateStore"/>가 만들 수 있도록 매개변수 없는 생성자를 가져야 합니다.
    /// </summary>
    public interface IAugmentState
    {
        IAugmentState Clone();
    }
}
