# LAYOUT 설계: 스크립트·에셋 디렉터리 재편

> **문서 종류**: 태스크 설계 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `9ce41c3`
> 상태는 [트랙 README](README.md) 태스크 표에 있습니다.

## 1. 참조 프로젝트 대조

`C:\project\2d-team04\Assets`를 2026-10-22에 조사했습니다. 참조 프로젝트의 최상위는 번호 붙은 타입 폴더(`01.Scenes`~`09.VFX`)입니다. 각 타입 폴더의 둘째 단은 도메인(`Player`, `Enemy` 등)으로 나뉩니다. `02.Scripts`는 도메인 15개로 나뉘고, 도메인마다 asmdef와 `Tests/EditMode|PlayMode/`가 있습니다.

| 참조 프로젝트 관행 | Tessera 판단 | 사유 |
|---|---|---|
| 스크립트는 도메인 우선이고, 도메인 안에 `Core`·`Data`·`Presentation`을 둠 | 채택 | 이미 모듈 우선입니다. `Games/Yacht` 안을 `Rules`·`Augments`·`Presentation`으로 나눕니다(§2) |
| 타입 폴더의 둘째 단을 같은 도메인 이름으로 통일 (`03.Prefabs/Enemy`, `05.Animations/Enemy`) | 채택 | 게임 전용 에셋에 `Games/<게임>/` 도메인을 둡니다(§6) |
| 최상위 번호 접두사 | 채택 안 함 | 아래 「번호 접두사의 비용」 |
| 도메인별 asmdef와 테스트 동거 | 보류 (`ARCH-05`) | asmdef 없이 기능 폴더 안에 테스트를 두려면 폴더마다 `Editor` 특수 폴더를 만들어야 해 테스트가 오히려 흩어집니다 |
| GUID를 유지하는 이동 | 채택 | `AssetDatabase.MoveAsset`만 씁니다 |
| 서드파티 혼재 (`09.VFX/Consumable/JMO Assets`, 최상위 `TextMesh Pro`) | 피함 | `Art/ThirdParty/` 규칙을 유지합니다 |
| `Resources`가 두 곳에 있고, 같은 카탈로그가 GUID만 다르게 중복됨 | 피함 | Resources 위치 규칙을 §6.1에 명시합니다 |
| 네임스페이스 이탈 (Enemy 70개 중 56개가 전역) | 피함 | 폴더↔네임스페이스 대응의 원본을 `architecture_overview.md` §3 하나로 유지합니다 |

### 번호 접두사의 비용

최상위 폴더 이름이 바뀌면 다음이 전부 깨집니다.

- `.claude/rules/`의 `paths` 글롭 8줄: serialization 2, module-boundary 1, editor-tests 1, art-direction 4
- `tessera-balance-check`의 경로 2줄
- 코드의 `"Assets/..."` 문자열 78건: Editor 50, 테스트 22, 런타임 6
- 문서의 `Assets/` 언급 약 590건
- 씬 경로를 쓰는 에디터 도구 4개와 `AGENTS.md`의 메인 씬 경로

반면 얻는 것은 프로젝트 창의 정렬 순서뿐입니다. Tessera의 최상위는 14개 안팎이라 알파벳 정렬로 충분합니다.

## 2. Scripts 이동 (`LAYOUT-T1`)

### 목표 트리

```text
Assets/Scripts/
├── Core/ Dice/ Rendering/ Tabletop/   # 그대로
├── Network/                           # 그대로 (빈 폴더)
└── Games/
    └── Yacht/
        ├── Rules/            # 일반 요트 규칙·권위 (Tessera.Games.Yacht)
        ├── Augments/         # 증강 규칙 (Tessera.Games.Yacht)
        │   ├── Framework/    #   런타임·디스패처·카탈로그·점수 엔진
        │   ├── Modification/ #   족보 변형 18종 (명세 §5.1)
        │   ├── Enhance/      #   시스템 강화 11종 (§5.2)
        │   ├── Quest/        #   퀘스트 11종 (§5.3)
        │   └── ManualAction/ #   수동 행동 6종 (§5.4)
        └── Presentation/     # 화면 표현 (Tessera.Games.Yacht.Presentation)
            ├── Flow/  AugmentViews/  Table/
            └── Dice/  Stage/  Hud/
```

### 이동표

경로는 `Assets/Scripts/Games/` 기준입니다. `AY`는 `AugmentedYacht`의 줄임입니다.

| 현재 | 새 위치 | 파일 수 |
|---|---|---|
| `Yacht/` (`LocalGameAuthority`, `ScoreCategoryNames`, `YachtGameCore`, `YachtGameStateView`) | `Yacht/Rules/` | 4 |
| `AY/Logic/YachtDieFaces.cs` | `Yacht/Rules/` | 1 |
| `AY/Logic/` (`YachtAugmentRuntime`, `YachtAugmentScoreEngine`) | `Yacht/Augments/Framework/` | 2 |
| `AY/Logic/Augments/Core/` 전체 | `Yacht/Augments/Framework/` | 10 |
| `AY/Logic/Augments/Modification/` 전체 | `Yacht/Augments/Modification/` | 19 |
| `AY/Logic/Augments/Enhance/` 중 `EnhanceAugment`와 강화 11종 | `Yacht/Augments/Enhance/` | 12 |
| `AY/Logic/Augments/Enhance/` 중 `TableFlip`, `EquivalentExchange`, `Gambit`, `DoubleDown`, `DiceAlchemy`, `CoinToss` | `Yacht/Augments/ManualAction/` | 6 |
| `AY/Logic/Augments/Quest/` 전체 | `Yacht/Augments/Quest/` | 12 |
| `AY/Presentation/` 전체 | `Yacht/Presentation/<하위>/` (아래 표) | 26 |
| `AY/YachtGameRules.cs` (GUID 보존용 스텁) | 삭제 | 1 |

이동 92개, 삭제 1개입니다. 수동 행동 6종은 `EnhanceAugment`를 상속한 채로 폴더만 옮깁니다.

### 프레젠테이션 하위 폴더

| 폴더 | 파일 |
|---|---|
| `Flow/` | `AugmentedYachtController`, `YachtTurnFlowPresenter`, `YachtDiceRoundPresenter`, `YachtInputRouter`, `YachtSceneAssembler`, `PresentationPhase` |
| `AugmentViews/` | `AugmentCardView`, `AugmentParchmentVisuals`, `AugmentScrollModel`, `AugmentStickerCatalog`, `AugmentStickerTexture`, `AugmentTrayCardView`, `AugmentTrayPresenter`, `AugmentVfxPlanner` |
| `Table/` | `ParchmentScoreSheet`, `YachtRunicPresenter`, `TabletopSurfaceBuilder` |
| `Dice/` | `YachtDieVisuals`, `DiceVisualPool`, `DiceSmokePuffVfx` |
| `Stage/` | `YachtCameraRig`, `YachtLightingRig`, `YachtAudioService` |
| `Hud/` | `YachtHudFactory`, `RoundedRectSprite`, `YachtDebugPanel` |

### 절차

1. tessera-verifier가 기준선을 잽니다: EditMode 통과 수와 콘솔 오류 0. 옮길 파일의 `.meta` guid 목록도 저장합니다.
2. tessera-unity-operator가 `AssetDatabase.CreateFolder`와 `AssetDatabase.MoveAsset`으로 일괄 이동합니다. 에디터가 열린 상태에서 셸 `mv`·`git mv`는 쓰지 않습니다. `.cs`와 `.meta`가 갈라지면 GUID가 다시 발급되어 Missing Script가 생깁니다.
3. 스텁을 삭제하기 전에 그 GUID를 참조하는 씬·프리팹·에셋이 0인지 확인합니다. 대조 요령은 tessera-unity-operator 정의의 「GUID 대조는 인덱스를 한 번만」 절을 따릅니다.
4. 빈 `Games/AugmentedYacht/**` 폴더를 삭제합니다.
5. tessera-verifier로 컴파일 오류 0을 확인하고, 1단계 guid 목록이 새 경로에 그대로 있는지 대조합니다.

## 3. 네임스페이스 변경 (`LAYOUT-T2`)

- 선언: 프레젠테이션 26개 파일의 `namespace Tessera.Games.AugmentedYacht`를 `namespace Tessera.Games.Yacht.Presentation`으로 바꿉니다.
- 사용처: `using Tessera.Games.AugmentedYacht;`를 쓰는 테스트 9개와 에디터 도구를 `using Tessera.Games.Yacht.Presentation;`으로 바꿉니다. 정규화된 이름(`Tessera.Games.AugmentedYacht.X`)으로 직접 참조한 곳도 함께 바꿉니다. 테스트 네임스페이스는 `Tessera.Editor.Tests`를 유지합니다.
- 씬 직렬화 영향
  - Tessera 타입을 가리키는 UnityEvent 바인딩(`m_TargetAssemblyTypeName`)은 0건입니다.
  - `m_EditorClassIdentifier` 7건은 정보용이라 동작에 영향이 없습니다.
  - 컴포넌트 바인딩은 GUID 기준입니다.
  - 직렬화 enum(`launchMode`)은 정수로 저장되어 타입의 네임스페이스와 무관합니다.
- 경계: `Rules/`·`Augments/`에 `using Tessera.Games.AugmentedYacht`가 있으면 레이어 위반입니다. 새 네임스페이스로 바꿔 주지 말고 보고합니다(`control_manifest.md` 게임 규칙 절).
- 이름 충돌: `Tessera.Games.Yacht` 안에서 `Presentation`이라는 식별자가 네임스페이스와 겹칠 수 있습니다. 컴파일에서 드러나며, 생기면 해당 줄만 고칩니다.

## 4. Editor 대칭 (`LAYOUT-T3`)

규칙은 「Editor 하위 경로 = Scripts 하위 경로」입니다. 공통 모듈(`Dice`·`Tabletop`·`Rendering`)은 이미 이 규칙대로라 바꾸지 않습니다.

| 현재 | 새 위치 | 근거 |
|---|---|---|
| `Editor/Yacht/` | `Editor/Games/Yacht/` | Scripts 대칭 |
| `Editor/Tests/Yacht/` | `Editor/Tests/Games/Yacht/` | Scripts 대칭 |
| `Editor/CoinSpinPresetBaker.cs` | `Editor/Dice/` | 동전 에디터 코드는 `Dice/`에 둔다는 `architecture_overview.md` §3.2 규칙 |
| `Editor/CoinSpinPresetTests.cs` | `Editor/Tests/Dice/` | 같음. 네임스페이스는 이미 `Tessera.Editor.Tests`입니다 |

`[MenuItem]` 메뉴 경로는 폴더 위치와 무관합니다(§3.2). 이동 방법은 §2 절차 2·5와 같습니다.

## 5. 증강 찾기 보조 (`LAYOUT-T4`)

- 명세 링크: [`augments_specification_and_status.md`](../../../reference/augments_specification_and_status.md) §5.1~§5.4 표 46행의 「C# 구현 클래스」 칸을 상대 링크로 바꿉니다. 예: `` [`TableFlip`](../../Assets/Scripts/Games/Yacht/Augments/ManualAction/TableFlip.cs) ``
- 카탈로그 주석: `YachtAugmentCatalog.Handlers`의 항목마다 줄 끝에 `// 판 뒤집기` 형태의 한국어 이름 주석을 답니다. 이름의 원본은 명세 §5 표의 「이름」 칸이며, 값을 그대로 옮깁니다.
- 두 작업은 `LAYOUT-T1` 뒤의 최종 경로를 기준으로 합니다.

## 6. 에셋 재편 (`LAYOUT-T5`)

제안 단계입니다. 사용자가 확인하기 전까지 `LAYOUT-T5`는 `BLOCKED`입니다.

### 6.1 규칙 (`architecture_overview.md` §3.1 개정안)

- 최상위는 지금처럼 타입·출처 기준입니다: `Art`, `Audio`, `Prefabs`, `Resources`, `Scenes`, `Settings`, `StreamingAssets`, `Rendering`.
- 각 루트의 둘째 단은 도메인입니다.
  - 공통 에셋은 지금 도메인 이름을 그대로 씁니다: `Coin`, `Dice`, `Tabletop`, `Parchment`, `Tray`, `Wood`, `Quill`.
  - **게임 전용 에셋은 `Games/<게임>/`** 아래에 둡니다.
- 도메인 폴더는 그 도메인 에셋이 실제로 생길 때만 만듭니다(오디오 규칙과 같은 원칙). 이번에 `Games/Yacht/`가 생기는 곳은 `Art/Source`와 `Art/Generated` 두 곳뿐입니다.
- `Resources`는 두 자리만 씁니다.
  - (a) 최상위 `Assets/Resources/`: 여러 도메인이 함께 쓰는 런타임 카탈로그와 공용 VFX(`RuntimeAssetLibrary.asset`, `Vfx/`)
  - (b) 도메인 전용: `Art/{Source|Generated}/<도메인>/Resources/<이름>/`. `Source` 안의 중첩은 이번에 새로 허용합니다.
  - `Resources.Load` 키는 `<이름>/...`로 위치와 무관합니다. 같은 키 폴더를 두 곳에 만들지 않습니다. 참조 프로젝트에 카탈로그가 중복된 사례가 있습니다.
- `<종류>` 단은 종류가 둘 이상일 때만 둡니다. `Generated/Dice`·`Parchment`·`Tray`·`Wood`처럼 종류가 하나뿐인 폴더는 지금 그대로 둡니다. 규칙을 현실에 맞추는 것이라 이동은 0건입니다.
- `Prefabs/<도메인>/`은 씬이나 코드가 직접 참조하는 프리팹의 자리입니다. 베이커가 만든 프리팹도 여기에 둡니다(현행 유지).

### 6.2 이동표

| 현재 | 새 위치 | 비고 |
|---|---|---|
| `Resources/AugmentIcons/` (45개) | `Art/Source/Games/Yacht/Resources/AugmentIcons/` | 생성하는 코드가 없고 갤러리 창만 있어 `Source`로 분류 |
| `Art/Generated/AugmentScrolls/Resources/AugmentScrolls/` (프리셋 16개, `Previews/`, 두루마리 알베도) | `Art/Generated/Games/Yacht/Resources/AugmentScrolls/` | `AugmentScrollAssetGenerator` 산출물 |
| `Art/Source/Parchment/Icons/` | `Art/Source/Games/Yacht/ScoreIcons/` | 족보 아이콘인지 착수할 때 확인. 아니면 이 줄은 제외 |
| `Art/Generated/Parchment/Icons/` | `Art/Generated/Games/Yacht/ScoreIcons/` | 같음 |
| `Assets/InputSystem_Actions.inputactions` | `Assets/Settings/` | ProjectSettings가 GUID로 참조하고, 코드 참조는 0 |

이동이 끝나면 비는 `Art/Source/Parchment/`와 `Art/Generated/AugmentScrolls/`는 삭제합니다.

### 6.3 고칠 경로 상수

착수할 때 tessera-scout가 줄 번호를 다시 확인합니다. 아래 경로는 `LAYOUT-T3` 이후 기준입니다.

- `Editor/Games/Yacht/AugmentIconGalleryWindow.cs:10`의 `IconDirectory`
- `AugmentScrollAssetGenerator`: 9행, 33~36행
- `RuntimeAssetLibraryBaker`: 19~29행 중 아이콘 경로
- `ParchmentTextureGenerator`: 379행, 395행 (아이콘 경로일 때만)
- 위 경로를 문자열로 쓰는 테스트

### 6.4 검증

- 이동 전과 후에 tessera-verifier가 `unity command script eval`로 증강 ID 전체의 `AugmentIcons/<id>`와 스크롤 프리셋 키를 `Resources.Load`합니다. 두 번의 null 개수가 같아야 합니다.
- tessera-unity-operator가 두루마리 생성기와 `RuntimeAssetLibraryBaker`를 다시 돌립니다. 옛 경로 폴더가 다시 생기지 않아야 합니다.
- `RuntimeAssetLibrary.asset`은 GUID로 참조하므로 족보 아이콘 슬롯 12개가 null이 아니어야 합니다.

### 6.5 바꾸지 않는 것

| 대상 | 사유 |
|---|---|
| `Assets/Screenshots/` | unity-skills `scene_screenshot`이 저장 위치를 고정합니다(`unity-rest.md` 「스크린샷」). gitignore 대상입니다 |
| `Assets/_Recovery/` | Unity 크래시 복구 산출물입니다. gitignore 대상이며, 필요하면 사용자가 직접 비웁니다 |
| `Assets/Temp/` | 빈 폴더이고 gitignore 대상입니다. 생성 주체는 미확인이라 지우지 않습니다 |
| `Rendering/Shaders/` | 코드 경로 2곳(`BuildDiceGraphicsPoC`, `RegisterPixelEdgeFeature`)과 art-direction 규칙 글롭이 가리킵니다. 옮겨서 얻는 것이 없습니다 |
| `Settings/`, `StreamingAssets/WebSource/` | GUID와 코드 경로가 고정돼 있고, `ARCH-04`의 영역입니다 |
| `Resources/Vfx/` | 동전·주사위 연기 공용 VFX로, §6.1 (a)에 해당합니다 |
| `Prefabs/Dice/Die_Octahedron`, `Die_Sevens` | 증강용이지만 주사위 도메인 에셋입니다. `AugmentedYachtController`의 에디터 경로 상수도 그대로 둡니다 |
| 런타임 경로 상수 6건 | 전부 `#if UNITY_EDITOR` 안의 로드입니다(`InkwellAndQuill`은 262행 경고 문구에서만 런타임에 쓰임). 가리키는 `Wood`·`Quill`·`Dice` 폴더는 이동 대상이 아닙니다 |
| `TextMesh Pro/` | 패키지 임포트 폴더입니다 |

### 6.6 범위 밖 발견

- `AugmentScrollModel.cs:438`의 `Resources.Load<Texture2D>("Parchment/parchment_base")` 대체 경로는 항상 null입니다. `Parchment/`를 담은 `Resources` 폴더가 없기 때문입니다. 주 경로인 `AugmentScrolls/parchment_scroll_albedo`는 존재합니다. 이번 트랙에서는 고치지 않고 보고만 합니다.

## 7. 문서·설정 갱신 (`LAYOUT-T6`)

### 대상

- [`architecture_overview.md`](../../../reference/architecture_overview.md)
  - §3 트리
  - 폴더·네임스페이스 어긋남 설명(96~98행): 어긋남이 사라지므로 「하위 폴더별 네임스페이스 없음」으로 교체
  - §3.1(§6.1 개정 규칙), §3.2(Editor 트리)
  - 레시피 1(새 증강 경로)
  - 모듈별 상세의 파일 링크
- [`control_manifest.md`](../../../reference/control_manifest.md) 레이어별 「적용 폴더」(23·37·56행). 37행은 `Games/Yacht/Rules/`·`Games/Yacht/Augments/`, 56행은 `Games/Yacht/Presentation/`과 새 네임스페이스로 바꿉니다
- `augments_specification_and_status.md`에 남은 옛 경로
- `docs/README.md:73`의 코드 스니펫 출처 예시 경로
- `.claude/rules/art-direction.md:6` 글롭: `Stage/YachtLightingRig`, `Stage/YachtCameraRig`, `Table/TabletopSurfaceBuilder` 세 파일을 가리키게 바꿉니다
- `.claude/skills/tessera-balance-check/SKILL.md:34-35`: `Assets/Scripts/Games/Yacht/Augments`로 바꿉니다
- `docs/plan/` 중 진행 중인 문서(`m17/`, `tracks/*/README.md`)의 옛 경로
- `improvement` 트랙
  - `ARCH-02`는 `VOID`로 처리하고 흡수 사유를 적습니다
  - `ARCH-05` §2 의존 순서를 `Games.Yacht → Games.Yacht.Presentation`으로 바꿉니다
  - `ARCH-05` 세부안 트리에 「LAYOUT 이후 트리 기준으로 다시 설계」 한 줄을 추가합니다
- `graphify update .` 실행

### 고치지 않는 것

다음은 시점 기록이라 고치지 않습니다.

- `docs/archive/`
- 모든 `log.md`
- `decisions.md`의 과거 항목
- 코드 주석 속 옛 ID

### 검증

- tessera-reader가 대상 문서의 `Assets/` 링크가 전부 실재하는지 점검합니다.
- 완료 게이트에 적힌 옛 경로 문자열의 검색 결과가 0건이어야 합니다.
- `graphify-out/.graphify_labels.json`의 커뮤니티 이름은 LLM 생성물이라 다시 만드는 데 비용이 듭니다. `update` 후 커뮤니티 구성이 크게 바뀌었는지 보고하고, 재생성 여부는 사용자에게 묻습니다.

## 8. 실행 순서와 담당

Unity 접근 에이전트는 한 번에 하나만 돌립니다.

| 단계 | 태스크 | 담당 | 비고 |
|---|---|---|---|
| 0 | 결정 기록 | tessera-scribe | 제안 결정이 확인된 뒤 `D-046` 기록과 `ARCH-02` `VOID` 처리. 트랙 등록과 `D-045`는 2026-10-22에 마침 |
| 0 | 기준선 | tessera-verifier | EditMode 통과 수, 콘솔 오류, guid 목록 |
| 1 | T1 | tessera-unity-operator → tessera-verifier | |
| 2 | T2 | tessera-implementer → tessera-verifier | |
| 3 | T3 ∥ T4 | T3: tessera-unity-operator / T4: tessera-implementer(카탈로그 주석), tessera-scribe(명세 링크) | 대상 파일이 겹치지 않습니다. T4가 끝나면 tessera-verifier가 컴파일을 확인합니다 |
| 4 | T5 | tessera-scout → tessera-unity-operator → tessera-implementer → tessera-verifier → tessera-unity-operator(베이커 재실행) | |
| 5 | T6 | tessera-scribe → tessera-reader → `graphify update .` | |
| 6 | 게이트 | `/tessera-gate LAYOUT` | `PASS`이면 커밋 허가를 요청합니다. 커밋은 `refactor(layout): …` 한 번입니다 |

### 위험과 대책

| 위험 | 대책 |
|---|---|
| 셸 이동으로 `.meta`가 갈라져 GUID가 다시 발급됨 | `MoveAsset`만 씁니다. 단계마다 guid 목록을 대조합니다 |
| 같은 `Resources` 키 폴더가 두 곳에 생김 | 옛 폴더를 삭제했는지 §6.4에서 확인합니다 |
| 베이커가 옛 경로에 다시 씀 | 경로 상수를 고친 뒤 베이커를 다시 돌려 확인합니다 |
| M17 남은 태스크 문서가 옛 경로를 가리킴 | T6 대상에 `m17/`을 넣었습니다 |
| 중간에 실패함 | 착수 시점의 작업 트리는 깨끗합니다(`9ce41c3`). 변경을 전부 폐기하고 다시 임포트하면 원상태로 돌아갑니다. 폐기는 사용자 허가 뒤에만 합니다 |
