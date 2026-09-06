# Tessera 아키텍처 결정 기록 (ADR)

구조적 판단과 그 근거를 남기는 문서다. 짧은 결정은 [`docs/augmented_yacht_work_plan.md`](augmented_yacht_work_plan.md) §11 결정 기록에 한 줄로 두고, 근거 설명이 필요한 것만 여기에 ADR로 쓴다.

| ID | 제목 | 상태 | 날짜 |
|---|---|---|---|
| `ADR-001` | 게임 상태 소유권과 싱글턴 | 채택 | 2026-09-04 |

---

## ADR-001. 게임 상태 소유권과 싱글턴

**상태**: 채택 · 2026-09-04 · 관련 결정 `D-030`

### 질문

게임 메인 데이터(주사위 값, 애니메이션, 점수, 변경 내역)를 싱글턴으로 관리하면 나중에 권위 서버로 옮기는 작업이 쉬워지는가?

### 결정

**아니다.** `LocalGameAuthority`와 `YachtGameSession`은 정적 싱글턴으로 바꾸지 않고 인스턴스로 유지한다.

### 배경 — 두 개념의 구분

이 질문은 서로 다른 두 가지를 섞고 있다.

- **단일 진실 공급원(Single Source of Truth)**: 상태를 한 곳이 배타적으로 소유하고, 변경 경로가 하나뿐인 설계. 권위 서버 이관에 필요한 것은 이쪽이다.
- **싱글턴 패턴**: 인스턴스가 하나뿐이고 전역 정적 접근점을 제공하는 구현 기법. 위를 얻는 여러 방법 중 하나이며, 여기서는 가장 비용이 큰 방법이다.

### 근거 1 — 단일 진실 공급원은 이미 구현돼 있다

`M2`에서 만든 구조가 정확히 그것이다.

| 요구 | 현재 구현 |
|---|---|
| 상태 배타 소유 | `LocalGameAuthority`가 `YachtGameState`를 단독 소유 |
| 변경 경로 단일화 | 모든 변경이 `Execute(YachtGameCommand)` → `YachtGameCommandResult`를 통과 |
| 중복 명령 방지 | `CommandId` 기반 dedupe |
| 낙관적 동시성 | `YachtGameCore.cs:206` `Revision` + `:271` `ExpectedRevision`, 불일치 시 `RevisionMismatch` |
| 변경 내역 보유 | `YachtGameEvent[]` |
| 원격 전환 대비 | `YachtGameCore.cs:409-413` `IGameAuthority.ExecuteAsync`가 `Task<>` 반환 |
| 난수 주입 | `IRandomSource` (테스트에서 결정론적 시드 주입 가능) |

주사위 값 결정, 점수 저장, 변경사항 보유는 전부 이미 한 객체에 모여 있다. 부족한 것은 상태 소유권이 아니라 **프레젠테이션 계층에서의 접근 편의성**이다. 그것은 배선 문제이며 전역화로 풀 문제가 아니다.

### 근거 2 — 정적 싱글턴이 권위 서버 이관을 오히려 방해한다

1. **서버는 매치를 동시에 여러 개 돌린다.** 정적 인스턴스 하나는 프로세스당 게임 하나를 뜻한다. 데디케이티드 서버나 호스트가 방을 둘 이상 들면 그 시점에 재작성이 필요하다. 현재 구조는 `new LocalGameAuthority()`를 방 개수만큼 만들면 끝난다.
2. **권위 모델은 객체가 둘이어야 한다.** 서버에는 *권위 상태*, 클라이언트에는 *복제본*이 있고 신뢰 수준이 다르다. 전역 이름 하나로 부르면 지금 만지는 것이 권위인지 복제본인지 코드에서 구분할 수 없다. 이것이 클라이언트 권위 버그의 표준 발생 경로다.
3. **테스트 격리가 깨진다.** 현재 에디터 테스트 80개는 각자 새 권위 인스턴스를 만든다. 정적화하면 테스트 간 상태가 공유되어 `Reset()` 호출이 강제되고, 실행 순서에 의존하는 산발적 실패가 생긴다. `M8`에서 확보한 회귀 안전망이 약해진다.
4. **Unity 도메인 리로드와 충돌한다.** Enter Play Mode Options로 도메인 리로드를 끄면 정적 필드가 플레이 모드 종료 후에도 살아남는다. 파괴된 `GameObject` 참조를 든 채로 남는다.
5. **전역 쓰기 가능은 권위의 반대말이다.** Command 패턴의 가치는 "변경 경로가 하나뿐"이라는 보장에 있다. 아무 스크립트나 상태를 직접 쓸 수 있으면 그 보장이 사라진다.

### 근거 3 — 애니메이션은 권위 데이터에 넣지 않는다

권위가 결정할 것과 클라이언트가 연출할 것은 분리되어야 하며, 현재 코드는 이미 분리해 두었다.

`YachtGameCore.cs:302-309`:

```csharp
public sealed class RollPresentation
{
    public string PresetFile;              // 어떤 클립
    public int PresetIndex;
    public bool IsMirrored;
    public YachtDieResult[] FinalValues;   // 권위가 정한 결과
    public float DurationSeconds;
}
```

권위는 결과값과 연출 시드만 내려보내고, 실제 재생은 클라이언트의 `BakedDiceController`가 담당한다. 서버는 애니메이션 프레임을 알 필요가 없다. 이 경계를 흐리면 나중에 서버 코드에서 Unity 의존성을 떼어내는 작업이 추가로 생긴다.

### 싱글턴이 적절한 대상

매치 수명과 무관한 **불변 카탈로그와 서비스**다. 이미 그렇게 되어 있으며 유지한다.

- `YachtAugmentCatalog` — 정적 `IAugmentHandler[]` 배열
- `DicePaletteCatalog` (`DicePaletteCatalog.cs:37`) — 정적 클래스 + 머티리얼 캐시
- 오디오 서비스, 환경설정 등

**판단 기준: 매치가 끝나면 버려져야 하는 것은 인스턴스, 프로세스 내내 바뀌지 않는 것은 정적.**

### 접근 편의성이 실제 문제라면

전역 정적이 아니라 **합성 루트(Composition Root)** 로 푼다.

```text
AugmentedYachtController (합성 루트, 매치당 1개)
  ├─ LocalGameAuthority 생성 → YachtGameSession
  └─ 각 프레젠터에 주입
        읽기 전용: IReadOnlyGameState   (프레젠터용)
        쓰기 전용: IGameCommandSink     (입력 라우터용)
```

읽기와 쓰기 인터페이스를 나누면 "프레젠터가 상태를 몰래 고치는" 사고가 컴파일 단계에서 막힌다. 싱글턴으로는 얻을 수 없는 이득이다.

그래도 전역 접근이 필요하면 **매치 스코프 앰비언트**까지가 안전선이다.

```csharp
public static class MatchRuntime
{
    public static IReadOnlyGameState Current { get; private set; }  // 읽기 전용만 노출
    internal static void Bind(...);   // 매치 시작 시 합성 루트만 호출
    internal static void Unbind();    // 매치 종료 시 반드시 해제
}
```

세터를 노출하지 않고, 수명을 매치에 묶고, 명령은 여전히 주입받은 `IGameCommandSink`로만 보낸다. 서버 다중 매치 문제는 남으므로 클라이언트 전용 편의 레이어로만 쓴다.

### 후속 작업

- `IReadOnlyGameState` / `IGameCommandSink` 인터페이스 분리 — `M11` 프레젠테이션 분해 시 재검토
- `AugmentedYachtController.cs:99`의 `augmentViewCatalog` 중복 인스턴스 제거 — 카드 표시용 정의 조회만을 위해 `YachtAugmentRuntime`을 두 번째로 생성하고 있다. 정적 정의 조회로 대체한다 (`M11-T7`)
- `AugmentStateStore` 직렬화 지원 — `{id, typeTag, payload}` 평탄화 (`M18-T8`)
