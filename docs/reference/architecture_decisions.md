# Tessera 아키텍처 결정 기록 (ADR)

> **문서 종류**: 기준 문서 (사람 대상)
> **코드 대조 기준**: 2026-09-14 · 커밋 `0a2f770`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.
> 작성 원칙은 [`docs/README.md`](../README.md)를 보십시오.

구조적 판단과 그 근거를 남기는 문서다. 짧은 결정은 [`docs/decisions.md`](../decisions.md)에 한 줄로 두고, 근거 설명이 필요한 것만 여기에 ADR로 쓴다. ADR이 정한 레이어별 해야 할 것·하지 말 것은 [`control_manifest.md`](control_manifest.md)에 옮겨 적고, 구현 지시서에는 그쪽을 싣는다.

## ADR 양식

이 절이 ADR 필드 형식의 원본이다. 작성과 상태 전이는 `/tessera-adr` 로 하고, 승인 점검 기준은 [`.claude/gates/ADR.md`](../../.claude/gates/ADR.md)다.

### 상태 수명주기

| 상태 | 뜻 | 들어오는 경로 |
|---|---|---|
| `Proposed` | 초안. 이 ADR에 기대는 태스크는 준비 점검에서 `CONCERNS` | `/tessera-adr new` |
| `Accepted` | 채택. 매니페스트에 반영됨 | `/tessera-adr accept` — `ADR` 게이트 통과와 사용자 확인 후에만 |
| `Deprecated` | 폐기. 대체 없이 더는 따르지 않음 | 사용자 결정 |
| `Superseded by ADR-nnn` | 다른 ADR로 대체됨 | 대체 ADR이 `Accepted` 가 될 때 |

- `Deprecated`·`Superseded` 가 된 ADR은 되살리지 않는다. 다시 필요하면 새 ADR을 쓴다.
- 번호는 재사용하지 않는다.

### 필드

각 ADR은 제목 바로 아래에 다음 메타 표를 둔다.

| 필드 | 값 |
|---|---|
| 상태 | 위 수명주기의 값 하나 |
| 날짜 | 마지막 상태 전이 날짜 `YYYY-MM-DD` |
| 레이어 | 영향을 받는 레이어. [`control_manifest.md`](control_manifest.md)의 레이어 이름(공통·게임 규칙·프레젠테이션)을 쓴다 |
| 관련 결정 | `D-nnn`, 태스크 ID. 없으면 `—` |
| 대체 | 이 ADR이 대체한 `ADR-nnn`. 없으면 `—` |
| 매니페스트 반영 | 이 ADR을 출처로 한 매니페스트 절. 없으면 `—` |

본문 절은 이 순서로 쓴다.

1. **맥락** — 무엇이 문제였는가
2. **결정** — 무엇을 정했는가
3. **검토한 대안** — 버린 대안과 버린 이유. 최소 하나
4. **근거** — 결정을 뒷받침하는 사실. 코드 위치는 대조 기준 시점의 것
5. **결과와 후속 작업** — 결정이 남긴 작업과 그 상태

## 목록

| ID | 제목 | 상태 | 레이어 | 날짜 |
|---|---|---|---|---|
| `ADR-001` | 게임 상태 소유권과 싱글턴 | `Accepted` | 게임 규칙, 프레젠테이션 | 2026-09-04 |

---

## ADR-001. 게임 상태 소유권과 싱글턴

| 필드 | 값 |
|---|---|
| 상태 | `Accepted` |
| 날짜 | 2026-09-04 |
| 레이어 | 게임 규칙, 프레젠테이션 |
| 관련 결정 | `D-030` |
| 대체 | — |
| 매니페스트 반영 | 게임 규칙, 프레젠테이션 |

### 맥락

게임 메인 데이터(주사위 값, 애니메이션, 점수, 변경 내역)를 싱글턴으로 관리하면 나중에 권위 서버로 옮기는 작업이 쉬워지는가?

이 질문은 서로 다른 두 가지를 섞고 있다.

- **단일 진실 공급원(Single Source of Truth)**: 상태를 한 곳이 배타적으로 소유하고, 변경 경로가 하나뿐인 설계. 권위 서버 이관에 필요한 것은 이쪽이다.
- **싱글턴 패턴**: 인스턴스가 하나뿐이고 전역 정적 접근점을 제공하는 구현 기법. 위를 얻는 여러 방법 중 하나이며, 여기서는 가장 비용이 큰 방법이다.

### 결정

**아니다.** `LocalGameAuthority`와 `YachtGameSession`은 정적 싱글턴으로 바꾸지 않고 인스턴스로 유지한다.

싱글턴이 적절한 대상은 매치 수명과 무관한 **불변 카탈로그와 서비스**다. 이미 그렇게 되어 있으며 유지한다.

- `YachtAugmentCatalog` — 정적 `IAugmentHandler[]` 배열
- `DicePaletteCatalog` (`DicePaletteCatalog.cs:40`) — 정적 클래스 + 머티리얼 캐시
- 오디오 서비스, 환경설정 등

**판단 기준: 매치가 끝나면 버려져야 하는 것은 인스턴스, 프로세스 내내 바뀌지 않는 것은 정적.**

### 검토한 대안

| 대안 | 판정 | 이유 |
|---|---|---|
| 게임 상태 정적 싱글턴 | 기각 | 서버 다중 매치·권위/복제본 구분·테스트 격리·도메인 리로드·단일 변경 경로를 모두 해친다(근거 2) |
| 합성 루트 + 읽기·쓰기 인터페이스 분리 | 접근 편의 문제가 생기면 채택할 방향 | 프레젠테이션이 상태를 몰래 고치는 사고를 컴파일 단계에서 막는다 |
| 매치 스코프 앰비언트 | 조건부 허용 | 읽기 전용·매치 수명 한정이면 안전선 안. 서버 다중 매치 문제가 남아 클라이언트 전용 편의 레이어로만 쓴다 |

#### 접근 편의성이 실제 문제라면

전역 정적이 아니라 **합성 루트(Composition Root)** 로 푼다.

```text
AugmentedYachtController (합성 루트, 매치당 1개)
  ├─ LocalGameAuthority 생성 → YachtGameSession
  └─ 각 프레젠터에 주입
        읽기 전용: IReadOnlyGameState   (프레젠터용)
        쓰기 전용: IGameCommandSink     (입력 라우터용)
```

**[현재 코드, 2026-10-22]** 위 그림은 제안 구조다. 지금은 `YachtTurnFlowPresenter.cs:263` 이 `new YachtGameSession(options)` 로 세션을 만들고, `LocalGameAuthority` 는 `YachtGameSession` 생성자(`LocalGameAuthority.cs:742`) 안에서 만들어진다. `AugmentedYachtController` 는 세션을 직접 만들지 않는다.

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

### 근거

#### 근거 1 — 단일 진실 공급원은 이미 구현돼 있다

`M2`에서 만든 구조가 정확히 그것이다.

| 요구 | 현재 구현 |
|---|---|
| 상태 배타 소유 | `LocalGameAuthority`가 `YachtGameState`를 단독 소유 |
| 변경 경로 단일화 | 모든 변경이 `Execute(YachtGameCommand)` → `YachtGameCommandResult`를 통과 |
| 중복 명령 방지 | `CommandId` 기반 dedupe |
| 낙관적 동시성 | `YachtGameCore.cs:229` `Revision` + `:309` `ExpectedRevision`, 불일치 시 `RevisionMismatch` |
| 변경 내역 보유 | `YachtGameEvent[]` |
| 원격 전환 대비 | `YachtGameCore.cs:454, 457` `IGameAuthority.ExecuteAsync`가 `Task<>` 반환 |
| 난수 주입 | `IRandomSource` (테스트에서 결정론적 시드 주입 가능) |

주사위 값 결정, 점수 저장, 변경사항 보유는 전부 이미 한 객체에 모여 있다. 부족한 것은 상태 소유권이 아니라 **프레젠테이션 계층에서의 접근 편의성**이다. 그것은 배선 문제이며 전역화로 풀 문제가 아니다.

#### 근거 2 — 정적 싱글턴이 권위 서버 이관을 오히려 방해한다

1. **서버는 매치를 동시에 여러 개 돌린다.** 정적 인스턴스 하나는 프로세스당 게임 하나를 뜻한다. 데디케이티드 서버나 호스트가 방을 둘 이상 들면 그 시점에 재작성이 필요하다. 현재 구조는 `new LocalGameAuthority()`를 방 개수만큼 만들면 끝난다.
2. **권위 모델은 객체가 둘이어야 한다.** 서버에는 *권위 상태*, 클라이언트에는 *복제본*이 있고 신뢰 수준이 다르다. 전역 이름 하나로 부르면 지금 만지는 것이 권위인지 복제본인지 코드에서 구분할 수 없다. 이것이 클라이언트 권위 버그의 표준 발생 경로다.
3. **테스트 격리가 깨진다.** 현재 에디터 테스트 80개는 각자 새 권위 인스턴스를 만든다. 정적화하면 테스트 간 상태가 공유되어 `Reset()` 호출이 강제되고, 실행 순서에 의존하는 산발적 실패가 생긴다. `M8`에서 확보한 회귀 안전망이 약해진다.
4. **Unity 도메인 리로드와 충돌한다.** Enter Play Mode Options로 도메인 리로드를 끄면 정적 필드가 플레이 모드 종료 후에도 살아남는다. 파괴된 `GameObject` 참조를 든 채로 남는다.
5. **전역 쓰기 가능은 권위의 반대말이다.** Command 패턴의 가치는 "변경 경로가 하나뿐"이라는 보장에 있다. 아무 스크립트나 상태를 직접 쓸 수 있으면 그 보장이 사라진다.

#### 근거 3 — 애니메이션은 권위 데이터에 넣지 않는다

권위가 결정할 것과 클라이언트가 연출할 것은 분리되어야 하며, 현재 코드는 이미 분리해 두었다.

`YachtGameCore.cs:340-347`:

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

### 결과와 후속 작업

- `IReadOnlyGameState` / `IGameCommandSink` 인터페이스 분리 — `M11` 프레젠테이션 분해 시 재검토
- `AugmentedYachtController.cs:99`의 `augmentViewCatalog` 중복 인스턴스 제거 — 카드 표시용 정의 조회만을 위해 `YachtAugmentRuntime`을 두 번째로 생성하고 있다. 정적 정의 조회로 대체한다 (`M11-T7`) — **[재확인 필요, 2026-09-14]** 지목된 `augmentViewCatalog` 식별자와 `AugmentedYachtController.cs:99`는 현재 코드에 존재하지 않는다(99번째 줄은 `TurnFlow` 프로퍼티). 카드 표시 경로(`AugmentTrayPresenter.cs`)는 `ownedCards[...].Definition`으로 조회하며 런타임을 중복 생성하지 않아, 원 항목이 지목한 문제는 해소된 것으로 보인다. 다만 `new YachtAugmentRuntime()` 중복 생성 자체는 `YachtDebugPanel.EnsureDefinitions()`(디버그 패널 전용)에 남아 있으며, 이를 원 항목과 같은 문제로 볼지는 미결이다.
- `AugmentStateStore` 직렬화 지원 — `{id, typeTag, payload}` 평탄화 (`M18-T8`)
