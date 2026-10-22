# 확정된 방향과 기준선

> **문서 종류**: 작업 지시서 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `6527034` (내용은 2026-09-20 기준 `work_plan.md`에서 옮김)
> 이 문서는 확정된 방향, 현재 기준선, 목표 아키텍처, 위험 요소, 빠른 참조를 담습니다. 진행 상태는 [`roadmap.md`](roadmap.md)와 각 마일스톤 폴더를 보십시오.

## 1. 확정된 방향과 범위

### 1.1 구현 순서

```text
이전 증강 시스템 조사
    ↓
권위와 무관한 순수 C# 게임 코어
    ↓
일반 요트 로컬 핫시트 완성
    ↓
일반/증강 게임 모드 분리
    ↓
대표 증강 수직 구현
    ↓
증강 55개 전체 이식
    ↓
증강 시스템 그래픽 요소 완성
    ↓
증강 요트 로컬 핫시트 완성
    ↓
네트워크 지연·중복·재동기화 검증
    ↓
EOS 또는 Steam 기반 호스트 온라인 연결
```

### 1.2 현재 확정 사항

1. 일반 요트가 기본 규칙이며 증강 요트는 기본 규칙에 증강 규칙을 합성한다.
2. 외부 권위 서버보다 로컬 핫시트 구현을 먼저 완료한다.
3. 레이팅, MMR, 순위표, 승패 기반 서버 보상은 현재 범위에서 제외한다.
4. 온라인 초기 버전은 별도 권위 서버 없이 플레이어 호스트 권위를 사용할 수 있게 준비한다.
5. 게임 규칙은 `UnityEngine`, `MonoBehaviour`, 씬 오브젝트에 의존하지 않는 순수 C#으로 작성한다.
6. Unity 클라이언트는 명령을 보내고 결과를 연출하는 역할을 맡는다.
7. 주사위 값과 베이킹 프리셋 선택 결과는 하나의 명령 결과로 함께 확정한다.
8. 현재는 일반/증강 모드가 하나의 공유 씬을 사용하는 구조를 우선한다.
9. 두 모드의 아트와 배치가 크게 달라질 때만 공통 프리팹을 기반으로 얇은 별도 씬을 만든다.
10. 이전 JavaScript 코드를 줄 단위로 번역하지 않고, 동작과 테스트를 기준으로 C#에 재설계한다.

### 1.3 현재 제외하거나 연기한 기능

| 기능 | 상태 | 다시 검토하는 시점 |
|---|---|---|
| 레이팅/MMR | `OUT` | 경쟁 모드 기획 확정 시 |
| 신뢰 가능한 원격 권위 서버 | `DEFERRED` | 레이팅·재화 보상 도입 시 |
| 서버 전용 Unity 인스턴스 | `OUT` | 실시간 고빈도 게임 규칙이 생길 때 |
| 일반/증강 씬 완전 복제 | `DEFERRED` | 두 모드의 시각 구성이 실제로 달라질 때 |
| 호스트 마이그레이션 | `DEFERRED` | 온라인 기본 매치가 안정화된 뒤 |
| `M17-T13`~`M17-T15` 그래픽 폴리시 (사운드·촉각, 그래픽 폴백, 해상도·성능 검증; 구 `M7-T10`·`T11`·`T12`) | `DEFERRED` | `M17` 핵심 완주 뒤 (`D-038`) |
| M16 셀 셰이딩 (재료 단계 셀 램프, Baseline/Cel A/B) | `DEFERRED` | 그래픽 패스 재개 시. 완료 조건 미충족·전제 오류로 채택 보류, 인프라는 유지 (`D-037`) |

---

## 2. 현재 프로젝트 기준선

### 2.1 Tessera에서 확인된 상태

| 영역 | 상태 | 근거/비고 |
|---|---|---|
| 기본 2인 요트 점수 계산 | 구현됨 | `YachtGameRules.cs` (LAYOUT-T1에서 삭제) |
| 12라운드 턴 진행 | 구현됨 | `YachtGameSession` |
| 기본 규칙 EditMode 테스트 | 일부 구현됨 | `Assets/Editor/Tests/Games/Yacht/YachtGameRulesTests.cs` |
| 베이킹 프리셋 로딩/재생 | 구현됨 | `DicePresetCatalog`, `BakedDiceController` |
| 주사위 값 결정 | 컨트롤러에 결합됨 | `AugmentedYachtController`가 `UnityEngine.Random` 사용 |
| 베이킹 프리셋 선택 | 컨트롤러에 결합됨 | 일반 5주사위 프리셋을 직접 선택 |
| 주사위 킵 상태 | 프레젠테이션 계층에 일부 존재 | 권위 상태로 이동 필요 |
| 증강 정의 데이터 | 55개 존재 | `Assets/StreamingAssets/WebSource/data/augments.json` (2026-09-19 디렉터리 재편에서 삭제. 코드에서 읽지 않던 웹 잔재였음) |
| 증강 카드 트레이 비주얼 | 구현됨 | `Assets/02.Scripts/Tabletop/AugmentCardTray.cs` |
| 실제 증강 런타임 | 미구현 | 트리거, 상태, 효과 처리기 필요 |
| 네트워크 모듈 | 비어 있음 | `Assets/02.Scripts/Network` |
| 일반/증강 모드 전환 | 미구현 | 공유 씬 모드 분기 필요 |

### 2.2 현재 구조에서 주의할 점

- `AugmentedYachtController.cs`가 게임 진행, 주사위 난수, 연출, 월드 생성, 입력을 폭넓게 담당한다.
- 일반 모드 씬에서 `AugmentCardTray` 오브젝트만 삭제해도 컨트롤러가 런타임에 다시 생성한다.
- `YachtGameSession`은 점수와 턴을 관리하지만 주사위 값·종류·킵 상태를 완전히 소유하지 않는다.
- 온라인 이전 가능성을 확보하려면 주사위와 킵 상태도 직렬화 가능한 게임 상태로 이동해야 한다.
- 증강 이식은 큰 컨트롤러에 조건문을 추가하는 방식으로 진행하지 않는다.
- 기존 사용자 변경과 무관한 코드 및 아트는 수정하지 않는다.

### 2.3 이전 프로젝트 레퍼런스

로컬 경로:

```text
C:\Users\dpcks\OneDrive\문서\yec\augmented-dice
```

GitHub:

```text
https://github.com/dpcks9677/augmented-dice
```

핵심 파일:

| 파일 | 참고할 내용 |
|---|---|
| `src/augments.json` | 55개 증강 표시 데이터 |
| `src/scoreEngine.js` | 증강 적용 점수 계산 |
| `src/augmentRules.js` | 충돌, 획득, 진행도 계산 |
| `src/authoritativeGame.js` | 상태, 턴, 굴림, 드래프트, 증강 행동 |
| `src/game/augments/augmentDraft.js` | 증강 선택지 생성 |
| `src/localDiceResultProvider.js` | 로컬 주사위 결과와 프리셋 키 선택 |
| `src/dicePresetRules.js` | 주사위 구성별 프리셋 파일 결정 |
| `src/server/DiceServer.js` | 명령 검증, 결과 및 애니메이션 브로드캐스트 |
| `tests/augmentRules.test.mjs` | 증강 규칙 기준 테스트 |
| `tests/authoritativeGame.test.mjs` | 전체 권위 상태 기준 테스트 |

이전 구현은 증강 로직이 `scoreEngine.js`, `augmentRules.js`, `authoritativeGame.js`에 분산되고 일부 증강 ID가 조건문에 직접 들어가 있다. 동작 기준으로는 활용하되 구조를 그대로 복제하지 않는다.

---

## 3. 목표 아키텍처

### 3.1 모듈 관계

```text
Tessera.Games.Yacht
├─ 기본 게임 상태
├─ 기본 점수 계산
├─ 명령과 결과
├─ 일반 요트 규칙
└─ 로컬 권위 구현

Tessera.Games.AugmentedYacht
├─ 증강 정의
├─ 증강 런타임 상태
├─ 드래프트 규칙
├─ 증강 효과 처리
└─ 기본 요트 규칙과 합성

Tessera.Dice
├─ 베이킹 프리셋 카탈로그
├─ 주사위 프레젠테이션
└─ 결과 재생

Tessera.Tabletop
├─ 공통 테이블 오브젝트
└─ 증강 전용 테이블 오브젝트

Tessera.Network
├─ 명령 전송
├─ 상태 스냅샷
├─ 호스트 권위 어댑터
└─ EOS/Steam 어댑터
```

위 구조는 책임 경계다. 실제 폴더 배치는 `D-028`에서 확정했다: 증강 관련 코드는 전부 `Assets/Scripts/Games/AugmentedYacht/` 아래에 두되, 순수 C# 로직은 `Logic/`, MonoBehaviour 프레젠테이션은 `Presentation/`으로 나눈다. 네임스페이스는 `Tessera.Games.Yacht`를 유지한다(`D-021`). asmdef 분리는 아직 하지 않는다.

이 배치는 LAYOUT 트랙에서 다시 바뀌었다. 현재 위치는 `Assets/02.Scripts/Games/Yacht/` 아래 `Rules/`·`Augments/`(네임스페이스 `Tessera.Games.Yacht`)와 `Presentation/`(네임스페이스 `Tessera.Games.Yacht.Presentation`)이다. 근거는 [LAYOUT 설계](tracks/layout/design.md)에 있다.

### 3.2 핵심 처리 흐름

```text
UI 입력
  ↓
GameCommand
  ↓
IGameAuthority.ExecuteAsync
  ↓
게임 상태 및 규칙 검증
  ↓
GameCommandResult
  ├─ 새 상태 스냅샷
  ├─ 게임 이벤트
  ├─ 최종 주사위 값
  └─ 베이킹 프리셋 정보
  ↓
Unity 프레젠테이션
```

### 3.3 핵심 데이터 초안

```text
YachtGameState
├─ Mode
├─ Phase
├─ CurrentPlayerIndex
├─ CurrentRound
├─ RollsRemaining
├─ Dice[]
├─ Players[]
├─ DraftState
├─ AugmentStates[]
└─ Revision

YachtDieState
├─ Id
├─ Type
├─ Value
├─ IsKept
└─ PromotionLevel

GameCommand
├─ CommandId
├─ ExpectedRevision
├─ PlayerId/PlayerIndex
├─ Type
└─ Payload

GameCommandResult
├─ Accepted
├─ ErrorCode
├─ State
├─ Events[]
└─ RollPresentation

RollPresentation
├─ PresetFile
├─ PresetIndex
├─ IsMirrored
├─ FinalValues[]
└─ Duration
```

이 이름은 구현 과정에서 조정할 수 있지만 책임을 다시 프레젠테이션 계층으로 되돌리면 안 된다.

### 3.4 증강 처리 원칙

- JSON에는 ID, 이름, 설명, 대상, 표시 정보 같은 정적 데이터만 둔다.
- 실제 규칙은 컴파일되고 테스트되는 C# 처리기에 둔다.
- 범용 이벤트 버스를 과도하게 만들지 않고 고정된 처리 지점을 사용한다.
- 예상 처리 지점은 다음과 같다.

```text
OnAugmentSelected
OnTurnStarted
BeforeRoll
AfterRoll
BeforeScorePreview
BeforeScoreCommit
AfterScoreCommit
OnTurnEnded
OnGameEnded
ManualAction
```

- 증강 효과 처리 순서가 결과에 영향을 준다면 우선순위를 명시하고 테스트한다.
- 한 증강의 진행 상태는 다른 증강의 임의 딕셔너리에 섞지 않는다.

---

## 4. 위험 요소와 대응

| ID | 위험 | 영향 | 대응 |
|---|---|---|---|
| `R-001` | 큰 컨트롤러에 규칙을 계속 추가 | 테스트와 온라인 이전이 어려워짐 | 상태와 명령부터 단계적으로 추출 |
| `R-002` | 씬을 완전 복제 | 두 씬의 조명·UI·오브젝트가 달라짐 | 공유 씬 우선, 필요 시 공통 프리팹 사용 |
| `R-003` | 이전 JS 구조를 그대로 번역 | 하드코딩과 상태 분산을 재현 | 동작 매트릭스와 테스트 기준 이식 |
| `R-004` | 주사위 값과 프리셋이 별도 난수 | 양쪽 클라이언트 연출 불일치 | 하나의 `RollPresentation` 결과로 확정 |
| `R-005` | 증강 처리 순서 불명확 | 조합에 따라 결과가 달라짐 | 고정 처리 지점·우선순위·조합 테스트 |
| `R-006` | 핫시트 드래프트 정보 노출 | 상대 선택지를 미리 확인 | 로컬 핫시트는 수용된 위험으로 둠(`M17-T1` `DROPPED`, 2026-09-13). `M19` 온라인은 플레이어별 화면 분리로 자연 해소 |
| `R-007` | 네트워크부터 구현 | 규칙과 전송 오류를 동시에 디버깅 | 로컬 완성 후 지연 시뮬레이션 진행 |
| `R-008` | 호스트 권위를 신뢰 서버로 오해 | 호스트 치팅 가능 | 비레이팅 캐주얼 범위로 명시 |
| `R-009` | 특수 주사위 프리셋 누락 | 연출 실패 또는 잘못된 주사위 수 | 프리셋 호환성 자동 테스트 |
| `R-010` | 기존 사용자 변경과 충돌 | 작업 손실 가능 | 수정 전 `git status`, 정밀 수정, 관련 없는 파일 보존 |
| `R-011` | 증강 그래픽이 게임 규칙과 직접 결합 | 연출 중단이 상태 오류로 이어짐 | 그래픽은 확정된 이벤트와 상태를 읽기만 하도록 유지 |

---

## 5. 빠른 참조

### 현재 메인 씬

```text
Assets/01.Scenes/Augmented Dice.unity
```

### 현재 기본 규칙

```text
YachtGameRules.cs (LAYOUT-T1에서 삭제)
```

### 현재 게임 컨트롤러

```text
Assets/02.Scripts/Games/Yacht/Presentation/Flow/AugmentedYachtController.cs
```

### 현재 증강 데이터

```text
Assets/02.Scripts/Games/Yacht/Augments/Framework/YachtAugmentCatalog.cs
```

### 원칙 한 줄 요약

```text
일반 요트를 순수 C# 코어로 먼저 완성하고, 증강을 합성한 뒤, 마지막에 네트워크 전송 계층을 연결한다.
```
