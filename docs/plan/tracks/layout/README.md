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
  - 최상위 폴더 번호 접두사 (`01.Scenes`~`07.Settings`, `LAYOUT-T7`)
  - 옛 경로를 가리키는 살아 있는 문서와 `.claude/` 설정
- 범위 밖
  - 클래스 이름 변경
  - asmdef 도입(`ARCH-05`), Addressables(`ARCH-03`)
  - 공통 모듈(`Core`·`Dice`·`Tabletop`·`Rendering`) 스크립트 폴더
  - `Rendering/Shaders`·`Settings`·`StreamingAssets`·`Scenes`·`TextMesh Pro`의 위치 (번호 접두사만 `LAYOUT-T7`에서 붙임)
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
| 번호 접두사 | 도입합니다(제안을 뒤집은 결정). 일반 폴더만 번호를 붙입니다: `01.Scenes`, `02.Scripts`, `03.Prefabs`, `04.Art`, `05.Audio`, `06.Rendering`, `07.Settings`(`LAYOUT-T7`). 바꾸지 않는 폴더: `Editor`·`Resources`·`StreamingAssets`는 Unity 특수 폴더라 이름이 정확히 일치해야 동작합니다(`01.Editor`면 에디터 스크립트가 런타임 어셈블리로 들어가 빌드가 깨지고, `Resources`를 바꾸면 `Resources.Load`가 전부 null이 됩니다). `TextMesh Pro`는 패키지 임포트 폴더, `Temp`·`_Recovery`는 gitignore 대상이라 그대로 둡니다. 비용은 [`design.md`](design.md) §1 |
| 에셋 도메인 | 타입·출처 기준의 최상위 루트는 그대로 둡니다. 게임 전용 에셋만 각 루트 안의 `Games/<게임>/` 아래로 옮깁니다 |
| Editor 대칭 | Editor 하위 경로를 Scripts 하위 경로와 같게 맞춥니다. `Editor/Yacht`는 `Editor/Games/Yacht`로, `Editor/Tests/Yacht`는 `Editor/Tests/Games/Yacht`로 옮깁니다 |
| 테스트 동거 | 기능 폴더 안에 테스트를 두는 방식은 asmdef 도입(`ARCH-05`)과 함께 합니다. 지금은 `Editor/Tests/` 대칭을 유지합니다 |
| `ARCH-02` | 이 트랙으로 흡수하고 `VOID`로 처리합니다(`ARCH-01`이 `ARCH-05`로 흡수된 선례). 「에셋 도메인 유지」가 `ARCH-02`의 목표 구조를 대체하므로 반대 방향 계획이 남지 않게 합니다. `D-046` |

## 완료 게이트

- 모든 태스크가 `DONE`이거나, 사유가 적힌 `DROPPED`/`DEFERRED`
- 컴파일 오류 0. EditMode 테스트가 착수 전 기준선과 같은 수로 전부 통과. 기준선: 370건 전부 통과, 실패 0, 무시 0 (HEAD `821c808`, 2026-10-22, 필터 `Tessera.Editor.Tests`)
- 옮긴 파일의 `.meta` GUID가 전부 그대로이고, 메인 씬과 프리팹의 Missing Script가 0
- 플레이 모드에서 증강 드래프트부터 첫 턴 점수 기록까지 진행할 때 콘솔 예외 0
- 살아 있는 문서와 `.claude/` 설정에서 옛 경로 0건: `Games/AugmentedYacht`, `Resources/AugmentIcons`, `Editor/Yacht`, `Editor/Tests/Yacht`, 접두사 없는 최상위 7개(`Assets/Scenes/`, `Assets/Scripts/`, `Assets/Prefabs/`, `Assets/Art/`, `Assets/Audio/`, `Assets/Rendering/`, `Assets/Settings/`)

## 태스크

| ID | 작업 | 상태 | 수락 기준 | 문서 |
|---|---|---|---|---|
| `LAYOUT-T1` | Scripts 게임 폴더 이동 | `DONE` | 이동표의 파일 92개가 새 위치에 있고 GUID가 그대로임. 스텁 `YachtGameRules.cs` 삭제, `Games/AugmentedYacht/` 폴더 없음, 컴파일 오류 0 | [design §2](design.md#2-scripts-이동-layout-t1) |
| `LAYOUT-T2` | 프레젠테이션 네임스페이스 변경 | `DONE` | `Tessera.Games.AugmentedYacht`의 선언·`using` 0건, 컴파일 오류 0, EditMode 기준선 수 통과, Missing Script 0 | [design §3](design.md#3-네임스페이스-변경-layout-t2) |
| `LAYOUT-T3` | Editor 폴더 대칭 | `DONE` | `Assets/Editor` 루트에 `.cs` 0개, `Editor/Games/Yacht/`·`Editor/Tests/Games/Yacht/`가 있고 옛 폴더는 없음, EditMode 기준선 수 통과 | [design §4](design.md#4-editor-대칭-layout-t3) |
| `LAYOUT-T4` | 증강 찾기 보조 | `DONE` | 명세 §5.1~§5.4 46행의 클래스 칸이 실제 파일 링크이고 깨진 링크 0, `Handlers` 46항목 모두에 한국어 이름 주석 | [design §5](design.md#5-증강-찾기-보조-layout-t4) |
| `LAYOUT-T5` | 게임 전용 에셋 분리와 루트 정리 | `DONE` | 이동 전후 `Resources.Load` 결과의 null 개수가 같음. 관련 베이커를 다시 돌려도 옛 경로 폴더가 생기지 않음 | [design §6](design.md#6-에셋-재편-layout-t5) |
| `LAYOUT-T6` | 문서·설정 경로 갱신 | `DONE` | 대상 문서와 설정에서 옛 경로 0건, tessera-reader 링크 점검에서 깨진 링크 0, `graphify update .` 성공 | [design §8](design.md#8-문서설정-갱신-layout-t6) |
| `LAYOUT-T7` | 최상위 폴더 번호 접두사 적용 | `DONE` | 7개 최상위 폴더가 번호 이름으로 옮겨지고 `.meta` GUID가 그대로임. 컴파일 오류 0, EditMode 기준선 수 통과, 옛 최상위 이름이 코드·`.claude/rules/`에 0건 | [design §7](design.md#7-번호-접두사-적용-layout-t7) |

- 순서는 T1 → T2 → (T3 ∥ T4) → T5 → T7 → T6입니다. T6이 문서 경로를 한 번에 고치도록 T7을 T6 앞에 둡니다. 단계별 담당은 [design §9](design.md#9-실행-순서와-담당)에 있습니다.
- 트랙 등록(`STATUS.md` 트랙 줄, `process.md` §1 트랙 목록, `D-045`)과 `D-046` 기록, `ARCH-02`의 `VOID` 처리는 2026-10-22에 마쳤습니다.

## 완료 기록

### `LAYOUT-T1` Scripts 게임 폴더 이동 — 2026-10-22
- `Assets/Scripts/Games/AugmentedYacht/**`와 `Games/Yacht/` 루트의 `.cs` 92개를 `AssetDatabase.MoveAsset`으로 옮김. 이동표([design §2](design.md#2-scripts-이동-layout-t1))와 실제 배치가 일치함
- GUID 보존용 스텁 `AugmentedYacht/YachtGameRules.cs`(참조 0건 확인) 삭제, `Games/AugmentedYacht/` 폴더 삭제
- 코드 내용 변경 없음. 네임스페이스는 `LAYOUT-T2`에서 변경
- 검증: 컴파일 오류 0, 새 경고 0. `.cs.meta` 대조로 92개 GUID 전부 보존, 새 GUID 0, 중복 0. EditMode 370/370 통과(기준선과 같음). 메인 씬 `Augmented Dice`와 `Assets/Prefabs/` 프리팹 17개의 Missing Script 0
- 게이트: READY PASS, DONE-CRITERIA PASS. 플레이 모드 흐름 확인은 트랙 완료 게이트에서 함. 새 규칙 테스트는 해당 없음

### `LAYOUT-T2` 프레젠테이션 네임스페이스 변경 — 2026-10-22
- `Assets/Scripts/Games/Yacht/Presentation/**` 선언 26개를 `Tessera.Games.AugmentedYacht`에서 `Tessera.Games.Yacht.Presentation`으로 변경
- `Assets/Editor` 쪽 `using` 17개, 정규화 이름 1곳(`TabletopSceneMigrator.cs`), 주석 2곳(cref 1, `CoinTossVfx.cs` 1) 갱신. 총 46개 파일
- 씬·프리팹의 `m_EditorClassIdentifier`에 옛 네임스페이스 문자열 남음: 씬 `Augmented Dice` 2곳, `3D Layered Parchment Score Sheet.prefab` 1곳, `AugmentScrollPreset_0~3.prefab` 각 1곳. 스크립트 바인딩은 GUID 기준이라 영향 없음(Missing Script 0으로 확인). 수정하지 않음
- 검증: 컴파일 오류 0, 새 경고 0. `Assets/**/*.cs`의 `Tessera.Games.AugmentedYacht` 0건. EditMode 370/370 통과(기준선과 같음). 메인 씬 Transform 556개, `Assets/Prefabs/` 프리팹 17개, `AugmentScrollPreset_0~3`의 Missing Script 0
- 게이트: READY PASS, DONE-CRITERIA PASS

### `LAYOUT-T4` 증강 찾기 보조 — 2026-10-22
- `YachtAugmentCatalog.Handlers` 46항목 모두에 한국어 이름 주석 추가(예: `new CoinToss() // 코인 토스`). 동작 변경 없음
- `docs/reference/augments_specification_and_status.md` §5.1~§5.4 클래스 칸 46/46을 `Assets/Scripts/Games/Yacht/Augments/<폴더>/<클래스>.cs` 파일 링크로 변경. 대응 불가 행 0
- 같은 문서의 다른 절에 남은 옛 `AugmentedYacht` 경로 링크는 `LAYOUT-T6`에서 일괄 수정
- 검증: 리플렉션으로 `Handlers` 46항목 확인, 컴파일 오류 0, EditMode 370/370
- 게이트: READY PASS, DONE-CRITERIA PASS

### `LAYOUT-T3` Editor 폴더 대칭 — 2026-10-22
- `Assets/Editor/Yacht/` → `Assets/Editor/Games/Yacht/`, `Assets/Editor/Tests/Yacht/` → `Assets/Editor/Tests/Games/Yacht/`로 이동. 루트의 `CoinSpinPresetBaker.cs`는 `Editor/Dice/`, `CoinSpinPresetTests.cs`는 `Editor/Tests/Dice/`로 이동. `AssetDatabase.MoveAsset` 사용, `.cs` 22개
- `Assets/Editor` 루트 `.cs` 0개, 옛 폴더와 `.meta` 잔재 없음
- 코드 내용 변경 없음. `Editor/Yacht` 등 옛 경로 문자열은 코드·`.claude/`에 0건
- 검증: GUID 22개 전부 보존(변경 0, 신규 0). 컴파일 오류 0, 새 경고 0. EditMode 370/370 통과(기준선과 같음)
- 게이트: READY PASS, DONE-CRITERIA PASS

### `LAYOUT-T5` 게임 전용 에셋 분리와 루트 정리 — 2026-10-22
- `Assets/Resources/AugmentIcons/`(45개)를 `Assets/Art/Source/Games/Yacht/Resources/AugmentIcons/`로 이동
- `Assets/Art/Generated/AugmentScrolls/Resources/AugmentScrolls/`를 `Assets/Art/Generated/Games/Yacht/Resources/AugmentScrolls/`로 이동
- 족보 아이콘 확인(주사위 눈 6종 + 특수 족보 6종). `Assets/Art/Source/Parchment/Icons/`(SVG 12개와 `svgicons.js`)를 `Assets/Art/Source/Games/Yacht/ScoreIcons/`로, `Assets/Art/Generated/Parchment/Icons/`(PNG 12개)를 `Assets/Art/Generated/Games/Yacht/ScoreIcons/`로 이동
- `Assets/InputSystem_Actions.inputactions`를 `Assets/Settings/`로 이동
- 빈 `Assets/Art/Source/Parchment/`, `Assets/Art/Generated/AugmentScrolls/` 삭제. `Assets/Art/Generated/Parchment/`에는 양피지 PNG 3개가 남음
- 경로 상수 수정 5개 파일: `AugmentIconGalleryWindow`(IconDirectory), `AugmentScrollAssetGenerator`(RootFolder, EnsureFolder 체인), `RuntimeAssetLibraryBaker`(IconsDir), `RuntimeAssetLibraryTests`(IconsDir), `ParchmentTextureGenerator`(아이콘 출력 경로, 임포터 설정 경로)
- 생성기·베이커 재실행으로 바뀐 두루마리 에셋 12개(프리팹 4, 머티리얼 4, 프리뷰 PNG 4)는 비결정적 재생성 노이즈라 사용자 승인으로 HEAD 내용으로 복원
- 검증: GUID 이동 전 107개 전부 보존, 재실행 전후 두루마리 폴더 GUID 36개 보존. 옛 경로 폴더 재생성 없음. `Resources.Load` 56키가 이동 전과 완전히 같음(`AugmentIcons` found 45, null 1은 원래 파일이 없는 `coin-toss`. 프리셋 4/4, 프리뷰 4/4, `parchment_scroll_albedo` found). `RuntimeAssetLibrary` scoreIcons 12/12. 같은 키 `Resources` 폴더 중복 0. 컴파일 오류 0, EditMode 370/370. 메인 씬과 프리팹 17개의 Missing Script 0, Missing 참조 0
- 프리뷰 PNG 4개는 LFS 포인터로 잘못 복원돼 `LAYOUT-T7` 검증 중 `git lfs smudge`로 바로잡음
- 게이트: READY PASS, DONE-CRITERIA PASS

### `LAYOUT-T7` 번호 접두사 적용 — 2026-10-22
- `AssetDatabase.MoveAsset`으로 최상위 폴더 7개 이동: `Assets/Scenes` → `Assets/01.Scenes`, `Scripts` → `02.Scripts`, `Prefabs` → `03.Prefabs`, `Art` → `04.Art`, `Audio` → `05.Audio`, `Rendering` → `06.Rendering`, `Settings` → `07.Settings`. `Editor`·`Resources`·`StreamingAssets`·`TextMesh Pro`·`Screenshots`는 그대로 둠
- GUID 보존: `.meta` 551개(폴더 meta 포함) 전부 유지. 변경 0, 신규 0, 삭제 0
- `ProjectSettings/EditorBuildSettings.asset`: Unity가 메모리상 씬 경로만 갱신하고 디스크 파일은 옛 경로를 유지함. 다시 설정·저장해 `Assets/01.Scenes/Augmented Dice.unity`로 맞춤(guid 동일)
- URP 기본·품질 레벨은 `Assets/07.Settings/{PC,Mobile}_RPAsset.asset`, `InputSystem.actions`는 `Assets/07.Settings/InputSystem_Actions.inputactions`를 가리킴
- 코드: `.cs` 30개의 에셋 경로 문자열·주석·메시지 수정(에디터 도구, EditMode 테스트, 런타임 `InkwellAndQuill`·`AugmentedYachtController`·`TabletopSurfaceBuilder`, 주석 포함). `EnsureFolder("Assets", "...")`와 `Path.Combine(Application.dataPath, "...")` 분할 형태도 포함. `Assets/**/*.cs`에 남은 옛 최상위 접두사 0건
- `.claude/`: `rules/art-direction.md`·`rules/module-boundary.md`·`rules/serialization.md`의 경로 glob 수정. art-direction Presentation glob은 `Assets/02.Scripts/Games/Yacht/Presentation/**/{YachtLightingRig,YachtCameraRig,TabletopSurfaceBuilder}.cs`를 가리켜 [design §8](design.md#8-문서설정-갱신-layout-t6)의 T1 메모를 해소. `skills/tessera-balance-check/SKILL.md`의 경로를 `Games/Yacht/Rules`·`Augments`로 수정(git log 줄의 `Assets/Scripts/Games/AugmentedYacht/Logic`은 이동 전 이력이라 의도적으로 유지). `agents/tessera-implementer.md`, `skills/tessera-task/references/implement-verify.md`, `AGENTS.md`의 메인 씬 경로도 수정
- 검증: 컴파일 오류 0, EditMode 370/370. 메인 씬과 프리팹 17개의 Missing Script 0, Missing 참조 0. `Resources.Load` 결과가 `LAYOUT-T5` 스냅샷과 동일. 재로드 후 옛 최상위 폴더 재생성 없음. 문서·설정에 적힌 경로가 모두 실재
- 발견·수정: `LAYOUT-T5`에서 두루마리 에셋 12개를 `git cat-file blob`으로 복원하면서 Git LFS smudge 필터를 우회함. `AugmentScrollPreview_0~3.png`에 131바이트 LFS 포인터 텍스트가 기록돼 `Resources.Load<Sprite>`가 null을 반환하고, `AugmentCardViewTests.Parchment_StaticPrefabAndPreviewProvideFourPresets`가 4회 실패(366/370). `git lfs smudge`로 복원해 sha256이 HEAD의 LFS oid와 일치함을 확인. 재임포트 후 370/370

### `LAYOUT-T6` 문서·설정 갱신 — 2026-10-22
- 옛 경로·네임스페이스를 새 구조(`Assets/0N.*`, `Games/Yacht/{Rules,Augments,Presentation/*}`)로 갱신. 문서 20개와 루트 `README.md`
  - reference: `architecture_overview.md`(§3 트리, 폴더·네임스페이스 노트를 「하위 폴더별 네임스페이스 없음」으로 교체, §3.1·§3.2, 레시피 1), `control_manifest.md` 적용 폴더 3줄, `augments_specification_and_status.md`, `augment_balance_analysis.md`, `art_style_guide.md`, `art/cel_shading_pixel_plan.md`, `art/pixel_edge_filter_plan.md`, `orchestration.md`, `docs/README.md`
  - plan: `direction.md`, `m17/README.md`, `m17/T09`·`T18`·`T23`·`T25`, `tracks/solid/README.md`, `tracks/improvement/README.md`, `tracks/layout/design.md`
- 작업 중 발견한 기존 문서 오류도 함께 고침
  - `TableFlip`·`DiceAlchemy` 경로 → `Augments/ManualAction/`
  - `T25`의 테스트 평면 경로 → `Editor/Tests/Games/Yacht/`
  - `m17/README.md`의 quill FBX 경로 → `Assets/04.Art/Source/Quill/quill_pen_low.fbx`
- `improvement`: `ARCH-05` §2 의존 순서를 `Games.Yacht → Games.Yacht.Presentation`으로 변경. 세부안 트리 위에 「LAYOUT 이후 트리 기준으로 다시 설계」 노트 추가. `ARCH-02`의 VOID 사유는 이미 `D-046`을 언급하므로 그대로 둠
- `direction.md`: §3.1 개념도와 `D-028` 이력 문장은 유지. 그 뒤에 현재 구조를 설명하는 문장 1개 추가
- 의도적으로 남긴 줄: 이동 표·완료 기록, 취소선 줄, VOID 제안, 이동 전 이력이 필요한 balance-check `git log` 줄의 옛 경로
- 아직 없는 계획 파일은 매핑된 위치로 적음. `YachtBankKeepZoneVfx`·`YachtAugmentVfxPresenter` → `Presentation/AugmentViews/`, `IAugmentRuntime` → `Augments/Framework/`, `TabletopPropsFacade` → `Presentation/Flow/`, `ParchmentMeshBuilder` → `Presentation/Table/`. 각 태스크 시작 시 확인
- graphify: `PYTHONHASHSEED=0 graphify update .` 실행. 노드 4717, 엣지 9666, 커뮤니티 280개(라벨 붙은 300개에서 감소). 추적 중인 라벨 파일은 복원함. 재라벨링은 사용자 결정 대기
- 검증(tessera-reader, 파일 69개, `Assets/` 토큰 494개): 깨진 링크 0, 옛 경로 잔존 0. 남은 매치는 전부 예외 범주

## 관련 결정

- `D-002`: 일반 요트가 기본 규칙이고, 증강 규칙은 그 위에 조합합니다. `Games/Yacht` 하나로 합치는 근거입니다
- `D-021`: 증강 3카테고리와 폴더. `ManualAction/`은 명세 §5.4 「수동 행동」 묶음에 맞춘 폴더이며 카테고리를 추가하는 것이 아닙니다. 보완 내용은 `D-045`에 적습니다
- `D-045`: 위 「확정 결정」
- `D-046`(2026-10-22): 위 「확정 결정」의 번호 접두사·에셋 도메인·Editor 대칭·테스트 동거·`ARCH-02` 처리 5건. 사용자가 확인해 확정했습니다. 번호 접두사는 제안(미채택)을 뒤집은 결정입니다
- `ARCH-02`는 흡수합니다. `ARCH-05`의 목표 의존 순서는 `Games.Yacht → Games.Yacht.Presentation`으로 갱신합니다
