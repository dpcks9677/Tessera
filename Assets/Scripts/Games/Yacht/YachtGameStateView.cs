using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 프레젠테이션 계층이 권위 상태를 읽을 때 쓰는 읽기 전용 뷰다.
    ///
    /// <see cref="LocalGameAuthority"/>는 명령·revision·중복 방지로 상태 변경 경로를 하나로 모은다.
    /// 그런데 <see cref="YachtGameSession.State"/>가 내부 <see cref="YachtGameState"/> 객체를
    /// 그대로 돌려주면, 필드가 전부 public이라 화면 쪽 아무 데서나
    /// <c>session.State.Dice[0].Value = 6</c> 같은 쓰기가 가능해진다. 그러면 "누가 상태를 바꿨는가"를
    /// 추적할 수 없고, 재동기화를 넣는 M18에서 그대로 문제가 된다.
    ///
    /// 이 인터페이스들은 그 경로를 컴파일 시점에 막는다. 스냅샷 복사가 아니라 같은 객체를 가리키는
    /// 뷰이므로 읽기 비용은 예전과 같다.
    ///
    /// 노출 범위는 실제로 화면이 읽는 것만 담는다. 새 값이 필요해지면 여기에 한 줄 추가하는 것이
    /// 곧 "이 값을 화면에 넘긴다"는 명시적 결정이 된다.
    ///
    /// <see cref="LocalGameAuthority.CurrentState"/>는 의도적으로 구체 타입을 유지한다.
    /// 권위 계층 자신과 그 테스트 하네스는 상태의 안쪽이고, 테스트가 시나리오를 만들려면
    /// 상태를 직접 조립할 수 있어야 한다. 경계는 화면과 만나는 <see cref="YachtGameSession"/>에 둔다.
    /// </summary>
    public interface IReadOnlyYachtDieState
    {
        int Id { get; }
        YachtDieType Type { get; }
        int Value { get; }
        bool IsKept { get; }
        int KeepSlotIndex { get; }
        int PromotionLevel { get; }
    }

    /// <summary>드래프트 진행 상황의 읽기 전용 뷰다.</summary>
    public interface IReadOnlyYachtDraftState
    {
        bool IsActive { get; }
        int PlayerIndex { get; }
        IReadOnlyList<string> Options { get; }
        IReadOnlyList<int> OptionCardPresetIds { get; }
        IReadOnlyList<int> SelectionCounts { get; }
    }

    /// <summary>
    /// 점수표의 읽기 전용 뷰다.
    ///
    /// <c>PlayerScoreData</c>의 필드는 Unity 직렬화 관례를 따라 소문자로 시작하는 public 필드다.
    /// 여기서는 프로퍼티 이름을 대문자로 두어 필드와 구분한다.
    /// </summary>
    public interface IReadOnlyPlayerScoreData
    {
        IReadOnlyList<int> UpperScores { get; }
        IReadOnlyList<int> LowerScores { get; }
        bool HasBonus { get; }
        int BonusScore { get; }
        int TotalScore { get; }
        int CalculateUpperSum();
    }

    /// <summary>플레이어별 증강 보유 상황의 읽기 전용 뷰다.</summary>
    public interface IReadOnlyYachtAugmentPlayerState
    {
        IReadOnlyList<string> OwnedIds { get; }
        IReadOnlyList<int> OwnedCardPresetIds { get; }
        int ExtraTurns { get; }
        int TurnsTaken { get; }
    }

    /// <summary>권위 상태 전체의 읽기 전용 뷰다.</summary>
    public interface IReadOnlyYachtGameState
    {
        long Revision { get; }
        YachtGameMode Mode { get; }
        YachtGamePhase Phase { get; }
        int CurrentPlayerIndex { get; }
        int CurrentRound { get; }
        int RollsRemaining { get; }
        bool HasRolled { get; }
        bool IsExtraTurnPhase { get; }
        IReadOnlyList<IReadOnlyYachtDieState> Dice { get; }
        IReadOnlyList<IReadOnlyPlayerScoreData> Players { get; }
        IReadOnlyYachtDraftState Draft { get; }
        IReadOnlyList<IReadOnlyYachtAugmentPlayerState> AugmentPlayers { get; }
        IReadOnlyList<string> GlobalAugmentIds { get; }
    }
}
