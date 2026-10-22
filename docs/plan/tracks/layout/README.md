# LAYOUT. 스크립트·에셋 디렉터리 재편

> **문서 종류**: 트랙 계획 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `9ce41c3`

## 목표

게임과 게임 모드를 폴더 경로만 보고 구분할 수 있어야 하고, 증강 하나를 이름으로 바로 찾을 수 있어야 합니다. 이를 위해 `Assets/Scripts/Games/`, `Assets/Editor/`, 게임 전용 에셋을 재편합니다. 모든 이동에서 `.meta` GUID를 유지해 씬·프리팹 바인딩을 깨지 않습니다.

## 범위

- 포함
  - `Assets/Scripts/Games/` 전체와 프레젠테이션 네임스페이스
  - `Assets/Editor/`의 요트 도메인 폴더와 루트 파일 2개
  - 게임 전용 에셋: 증강 아이콘, 증강 두루마리, 족보 아이콘
  - `Assets/InputSystem_Actions.inputactions`
  - 옛 경로를 가리키는 살아 있는 문서와 `.claude/` 설정
- 범위 밖
  - 클래스 이름 변경
  - asmdef 도입(`ARCH-05`), Addressables(`ARCH-03`)
  - 공통 모듈(`Core`·`Dice`·`Tabletop`·`Rendering`) 스크립트 폴더
  - `Rendering/Shaders`·`Settings`·`StreamingAssets`·`Scenes`·`TextMesh Pro`의 위치
  - 최상위 번호 접두사
  - 증강 정의가 여러 파일(ID 상수, 스티커 카탈로그, 아이콘)에 흩어진 문제
  - `docs/archive/`와 세션 로그에 남은 옛 경로

## 확정 결정 (2026-10-22, 사용자 수락)

| 항목 | 결정 |
|---|---|
| 게임 폴더 | `Games/AugmentedYacht/`를 없애고 `Games/Yacht/{Rules, Augments, Presentation}` 하나로 합칩니다. 다른 게임은 `Games/<게임>/`에 둡니다. 요트의 새 변형 모드는 `Games/Yacht/` 아래에 규칙 폴더 하나를 추가하는 방식이며, `Augments/`가 그 첫 사례입니다 |
| 증강 폴더 | `Augments/{Framework, Modification, Enhance, Quest, ManualAction}`. 증강 명세 §5.1~§5.4의 묶음과 1:1로 대응합니다. 옛 `Augments/Core`는 `Scripts/Core`와 이름이 겹쳐 `Framework`로 바꿉니다 |
| 네임스페이스 | `Rules/`·`Augments/**`는 `Tessera.Games.Yacht`를 유지합니다. `Presentation/**`은 `Tessera.Games.AugmentedYacht`에서 `Tessera.Games.Yacht.Presentation`으로 바꿉니다. 하위 폴더별 네임스페이스는 두지 않습니다 |
| 프레젠테이션 하위 폴더 | `Flow`·`AugmentViews`·`Table`·`Dice`·`Stage`·`Hud` 6개 |
| 증강 클래스 이름 | 바꾸지 않습니다. 대신 명세 §5 표의 클래스 칸을 파일 링크로 바꾸고, `YachtAugmentCatalog.Handlers` 항목마다 한국어 이름 주석을 답니다 |
| 착수 시점 | M17 완료를 기다리지 않고 지금 진행합니다 |
| 커밋 | 트랙이 끝날 때 한 번 합니다. 매번 사용자 허가를 받습니다 |

## 제안 결정 (사용자 확인 대기)

근거는 [`design.md`](design.md) §1입니다. 확인되면 위 표로 옮기고 `D-046`으로 기록합니다.

| 항목 | 제안 |
|---|---|
| 번호 접두사 (`01.Scenes` 등) | 채택하지 않습니다. 고칠 곳은 많은데 얻는 것은 정렬 순서뿐입니다 |
| 에셋 도메인 | 타입·출처 기준의 최상위 루트는 그대로 둡니다. 게임 전용 에셋만 각 루트 안의 `Games/<게임>/` 아래로 옮깁니다 |
| Editor 대칭 | Editor 하위 경로를 Scripts 하위 경로와 같게 맞춥니다. `Editor/Yacht`는 `Editor/Games/Yacht`로, `Editor/Tests/Yacht`는 `Editor/Tests/Games/Yacht`로 옮깁니다 |
| 테스트 동거 | 기능 폴더 안에 테스트를 두는 방식은 asmdef 도입(`ARCH-05`)과 함께 합니다. 지금은 `Editor/Tests/` 대칭을 유지합니다 |
| `ARCH-02` | 이 트랙으로 흡수하고 `VOID`로 처리합니다(`ARCH-01`이 `ARCH-05`로 흡수된 선례) |

## 완료 게이트

- 모든 태스크가 `DONE`이거나, 사유가 적힌 `DROPPED`/`DEFERRED`
- 컴파일 오류 0. EditMode 테스트가 착수 전 기준선과 같은 수로 전부 통과
- 옮긴 파일의 `.meta` GUID가 전부 그대로이고, 메인 씬과 프리팹의 Missing Script가 0
- 플레이 모드에서 증강 드래프트부터 첫 턴 점수 기록까지 진행할 때 콘솔 예외 0
- 살아 있는 문서와 `.claude/` 설정에서 옛 경로 0건: `Games/AugmentedYacht`, `Resources/AugmentIcons`, `Editor/Yacht`, `Editor/Tests/Yacht`

## 태스크

| ID | 작업 | 상태 | 수락 기준 | 문서 |
|---|---|---|---|---|
| `LAYOUT-T1` | Scripts 게임 폴더 이동 | `TODO` | 이동표의 파일 92개가 새 위치에 있고 GUID가 그대로임. 스텁 `YachtGameRules.cs` 삭제, `Games/AugmentedYacht/` 폴더 없음, 컴파일 오류 0 | [design §2](design.md#2-scripts-이동-layout-t1) |
| `LAYOUT-T2` | 프레젠테이션 네임스페이스 변경 | `TODO` | `Tessera.Games.AugmentedYacht`의 선언·`using` 0건, 컴파일 오류 0, EditMode 기준선 수 통과, Missing Script 0 | [design §3](design.md#3-네임스페이스-변경-layout-t2) |
| `LAYOUT-T3` | Editor 폴더 대칭 | `BLOCKED` | `Assets/Editor` 루트에 `.cs` 0개, `Editor/Games/Yacht/`·`Editor/Tests/Games/Yacht/`가 있고 옛 폴더는 없음, EditMode 기준선 수 통과 | [design §4](design.md#4-editor-대칭-layout-t3) |
| `LAYOUT-T4` | 증강 찾기 보조 | `TODO` | 명세 §5.1~§5.4 46행의 클래스 칸이 실제 파일 링크이고 깨진 링크 0, `Handlers` 46항목 모두에 한국어 이름 주석 | [design §5](design.md#5-증강-찾기-보조-layout-t4) |
| `LAYOUT-T5` | 게임 전용 에셋 분리와 루트 정리 | `BLOCKED` | 이동 전후 `Resources.Load` 결과의 null 개수가 같음. 관련 베이커를 다시 돌려도 옛 경로 폴더가 생기지 않음 | [design §6](design.md#6-에셋-재편-layout-t5) |
| `LAYOUT-T6` | 문서·설정 경로 갱신 | `TODO` | 대상 문서와 설정에서 옛 경로 0건, tessera-reader 링크 점검에서 깨진 링크 0, `graphify update .` 성공 | [design §7](design.md#7-문서설정-갱신-layout-t6) |

- `LAYOUT-T3`·`LAYOUT-T5` 차단 사유: 위 「제안 결정」을 사용자가 확인해야 합니다(2026-10-22).
- 순서는 T1 → T2 → (T3 ∥ T4) → T5 → T6입니다. 단계별 담당은 [design §8](design.md#8-실행-순서와-담당)에 있습니다.
- 트랙 등록(`STATUS.md` 트랙 줄, `process.md` §1 트랙 목록, `D-045`)은 2026-10-22에 마쳤습니다. `D-046` 기록과 `ARCH-02`의 `VOID` 처리는 제안 결정이 확인된 뒤에 합니다.

## 완료 기록

## 관련 결정

- `D-002`: 일반 요트가 기본 규칙이고, 증강 규칙은 그 위에 조합합니다. `Games/Yacht` 하나로 합치는 근거입니다
- `D-021`: 증강 3카테고리와 폴더. `ManualAction/`은 명세 §5.4 「수동 행동」 묶음에 맞춘 폴더이며 카테고리를 추가하는 것이 아닙니다. 보완 내용은 `D-045`에 적습니다
- `D-045`(등록 예정): 위 「확정 결정」
- `D-046`(제안 확인 뒤): 위 「제안 결정」
- `ARCH-02`는 흡수합니다. `ARCH-05`의 목표 의존 순서는 `Games.Yacht → Games.Yacht.Presentation`으로 갱신합니다
