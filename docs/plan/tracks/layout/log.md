# 레이아웃 트랙 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 양식은 [`process.md`](../../process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-10-22 — Claude Opus (LAYOUT 트랙 완료 게이트)

- 작업 ID: `LAYOUT-T1`~`LAYOUT-T7`
- 변경:
  - `/tessera-gate LAYOUT` 실행 (review_mode lean)
  - TEST-COVERAGE 지적에 따라 증강 아이콘 회귀 테스트 추가. `Assets/Editor/Tests/Games/Yacht/AugmentCardViewTests.cs`의 `AugmentIcons_LoadForEveryCatalogAugmentExceptKnownMissing`. `YachtAugmentCatalog.All`의 46종을 `Resources.Load<Sprite>("AugmentIcons/{id}")`로 로드하고, 로드되지 않는 목록이 `coin-toss` 1건과 같은지 단언
  - CODE-REVIEW 낮음 지적 반영. `.claude/skills/tessera-balance-check/SKILL.md`의 `git log` 경로에 이동 전 규칙 폴더 `Assets/Scripts/Games/Yacht` 추가. 옛 경로를 남긴 이유: 이동 전 커밋 이력을 함께 조회하기 위함
  - 무관한 미추적 폴더 `Claude outputs/`는 커밋 범위에서 제외
  - graphify 커뮤니티 이름 283개 재생성(`--backend=claude-cli`). Gemini 백엔드는 401 `API key not valid`로 실패. 라벨 파일은 별도 커밋
- 검증:
  - 게이트 판정 CONCERNS (CODE-REVIEW PASS, 낮음 3건 / TEST-COVERAGE CONCERNS)
  - EditMode 371/371 통과, 실패 0, 스킵 0 (기준선 370 + 새 테스트 1). 컴파일 오류 0
  - 플레이 모드: 증강 드래프트 → 굴림 → 첫 턴 점수 기록, 콘솔 에러·예외 0, 종료 후 씬 dirty 아님
  - 이동한 LFS 바이너리 97개 전부 새 위치에서 sha256이 HEAD LFS oid와 일치
  - `CONCERNS 수락 2026-10-22 — 베이커·생성기 경로 상수(AugmentScrollAssetGenerator, RuntimeAssetLibraryBaker, ParchmentTextureGenerator)를 검증하는 테스트 없음. 재편 전부터 없던 공백`
  - 그대로 둔 낮음 지적: `ProjectSettings/ProjectSettings.asset`의 `templateDefaultScene: Assets/Scenes/SampleScene.unity`(새 프로젝트 템플릿 기본값, 영향 없음)
- 다음 작업: LAYOUT 커밋·푸시 후 M17 남은 `TODO`(`M17-T5`, `M17-T6`, `M17-T9`, `M17-T25`)

---

### 2026-10-22 — Claude Opus (LAYOUT-T6 문서·설정 갱신)

- 작업 ID: `LAYOUT-T6`
- 변경:
  - 옛 경로·네임스페이스를 새 구조(`Assets/0N.*`, `Games/Yacht/{Rules,Augments,Presentation/*}`)로 갱신. 문서 20개와 루트 `README.md`
  - reference: `architecture_overview.md`, `control_manifest.md`, `augments_specification_and_status.md`, `augment_balance_analysis.md`, `art_style_guide.md`, `art/cel_shading_pixel_plan.md`, `art/pixel_edge_filter_plan.md`, `orchestration.md`, `docs/README.md`
  - plan: `direction.md`, `m17/README.md`, `m17/T09`·`T18`·`T23`·`T25`, `tracks/solid/README.md`, `tracks/improvement/README.md`, `tracks/layout/design.md`
  - 작업 중 발견한 기존 문서 오류도 함께 수정(`TableFlip`·`DiceAlchemy` 경로, `T25` 테스트 평면 경로, `m17/README.md`의 quill FBX 경로)
  - `improvement`: `ARCH-05` §2 의존 순서를 `Games.Yacht → Games.Yacht.Presentation`으로 변경
  - graphify: `PYTHONHASHSEED=0 graphify update .` 실행. 노드 4717, 엣지 9666, 커뮤니티 280개. 재라벨링은 사용자 결정 대기
- 검증:
  - tessera-reader 링크 점검(파일 69개): 깨진 링크 0, 옛 경로 잔존 0. 남은 매치는 이동 표·완료 기록 등 예외 범주
- 다음 작업: `/tessera-gate LAYOUT` 실행 후 커밋 허가 요청

---

### 2026-10-22 — Claude Opus (LAYOUT-T7 번호 접두사 적용)

- 작업 ID: `LAYOUT-T7`
- 변경:
  - `AssetDatabase.MoveAsset`으로 최상위 폴더 7개 이동: `Scenes` → `01.Scenes`, `Scripts` → `02.Scripts`, `Prefabs` → `03.Prefabs`, `Art` → `04.Art`, `Audio` → `05.Audio`, `Rendering` → `06.Rendering`, `Settings` → `07.Settings`. `Editor`·`Resources`·`StreamingAssets`·`TextMesh Pro`·`Screenshots`는 그대로 둠
  - `.meta` 551개(폴더 meta 포함) 전부 유지
  - `EditorBuildSettings.asset`의 씬 경로를 다시 설정·저장해 `Assets/01.Scenes/Augmented Dice.unity`로 맞춤(guid 동일)
  - 코드: `.cs` 30개의 에셋 경로 문자열·주석·메시지 수정. `Assets/**/*.cs`에 남은 옛 최상위 접두사 0건
  - `.claude/`: `rules/art-direction.md`·`rules/module-boundary.md`·`rules/serialization.md`의 경로 glob 수정. `skills/tessera-balance-check/SKILL.md`, `agents/tessera-implementer.md`, `skills/tessera-task/references/implement-verify.md`, `AGENTS.md`의 경로도 수정
- 검증:
  - GUID 변경 0, 신규 0, 삭제 0. 컴파일 오류 0, EditMode 370/370 통과
  - 메인 씬과 프리팹 17개의 Missing Script 0, Missing 참조 0. `Resources.Load` 결과가 `LAYOUT-T5` 스냅샷과 동일. 옛 최상위 폴더 재생성 없음
- 발견·수정: `LAYOUT-T5`에서 두루마리 에셋을 `git cat-file blob`으로 복원하면서 Git LFS smudge 필터를 우회해 `AugmentScrollPreview_0~3.png`에 LFS 포인터 텍스트가 기록됨. `Resources.Load<Sprite>`가 null을 반환해 `AugmentCardViewTests.Parchment_StaticPrefabAndPreviewProvideFourPresets`가 4회 실패(366/370). `git lfs smudge`로 복원해 sha256이 HEAD의 LFS oid와 일치함을 확인. 재임포트 후 370/370
- 다음 작업: `LAYOUT-T6` 문서·설정 갱신

---

### 2026-10-22 — Claude Opus (LAYOUT-T5 게임 전용 에셋 분리와 루트 정리)

- 작업 ID: `LAYOUT-T5`
- 변경:
  - `Assets/Resources/AugmentIcons/`(45개)를 `Assets/Art/Source/Games/Yacht/Resources/AugmentIcons/`로 이동
  - `Assets/Art/Generated/AugmentScrolls/Resources/AugmentScrolls/`를 `Assets/Art/Generated/Games/Yacht/Resources/AugmentScrolls/`로 이동
  - 족보 아이콘을 `Assets/Art/Source/Games/Yacht/ScoreIcons/`(SVG 12개와 `svgicons.js`)와 `Assets/Art/Generated/Games/Yacht/ScoreIcons/`(PNG 12개)로 이동
  - `Assets/InputSystem_Actions.inputactions`를 `Assets/Settings/`로 이동
  - 빈 `Assets/Art/Source/Parchment/`, `Assets/Art/Generated/AugmentScrolls/` 삭제. `Assets/Art/Generated/Parchment/`에는 양피지 PNG 3개가 남음
  - 경로 상수 수정 5개 파일: `AugmentIconGalleryWindow`, `AugmentScrollAssetGenerator`, `RuntimeAssetLibraryBaker`, `RuntimeAssetLibraryTests`, `ParchmentTextureGenerator`
  - 생성기·베이커 재실행으로 바뀐 두루마리 에셋 12개(프리팹 4, 머티리얼 4, 프리뷰 PNG 4)는 비결정적 재생성 노이즈라 사용자 승인으로 HEAD 내용으로 복원
- 검증:
  - 이동 전 107개 GUID 전부 보존. 두루마리 폴더 GUID 36개 보존. 옛 경로 폴더 재생성 없음
  - `Resources.Load` 56키가 이동 전과 완전히 같음(`AugmentIcons` found 45, null 1은 원래 파일이 없는 `coin-toss`. 프리셋 4/4, 프리뷰 4/4, `parchment_scroll_albedo` found). `RuntimeAssetLibrary` scoreIcons 12/12. 같은 키 `Resources` 폴더 중복 0
  - 컴파일 오류 0, EditMode 370/370. 메인 씬과 프리팹 17개의 Missing Script 0, Missing 참조 0
- 발견: 프리뷰 PNG 4개가 LFS 포인터로 잘못 복원됨. `LAYOUT-T7` 검증 중 `git lfs smudge`로 바로잡음
- 다음 작업: `LAYOUT-T7` 번호 접두사 적용

---

### 2026-10-22 — Claude Opus (LAYOUT-T3 Editor 폴더 대칭)

- 작업 ID: `LAYOUT-T3`
- 변경:
  - `Assets/Editor/Yacht/` → `Assets/Editor/Games/Yacht/`, `Assets/Editor/Tests/Yacht/` → `Assets/Editor/Tests/Games/Yacht/`로 이동
  - 루트의 `CoinSpinPresetBaker.cs`는 `Editor/Dice/`, `CoinSpinPresetTests.cs`는 `Editor/Tests/Dice/`로 이동
  - `AssetDatabase.MoveAsset` 사용, `.cs` 22개. 코드 내용 변경 없음
- 검증:
  - `Assets/Editor` 루트 `.cs` 0개, 옛 폴더와 `.meta` 잔재 없음. `Editor/Yacht` 등 옛 경로 문자열은 코드·`.claude/`에 0건
  - GUID 22개 전부 보존. 컴파일 오류 0, 새 경고 0. EditMode 370/370 통과
- 다음 작업: `LAYOUT-T5` 게임 전용 에셋 분리와 루트 정리

---

### 2026-10-22 — Claude Opus (LAYOUT-T4 증강 찾기 보조)

- 작업 ID: `LAYOUT-T4`
- 변경:
  - `YachtAugmentCatalog.Handlers` 46항목 모두에 한국어 이름 주석 추가. 동작 변경 없음
  - `docs/reference/augments_specification_and_status.md` §5.1~§5.4 클래스 칸 46/46을 `Assets/Scripts/Games/Yacht/Augments/<폴더>/<클래스>.cs` 파일 링크로 변경. 대응 불가 행 0
  - 같은 문서의 다른 절에 남은 옛 `AugmentedYacht` 경로 링크는 `LAYOUT-T6`에서 일괄 수정
- 검증:
  - 리플렉션으로 `Handlers` 46항목 확인. 컴파일 오류 0, EditMode 370/370
- 다음 작업: `LAYOUT-T3` Editor 폴더 대칭

---

### 2026-10-22 — Claude Opus (LAYOUT-T2 프레젠테이션 네임스페이스 변경)

- 작업 ID: `LAYOUT-T2`
- 변경:
  - `Assets/Scripts/Games/Yacht/Presentation/**` 선언 26개를 `Tessera.Games.AugmentedYacht`에서 `Tessera.Games.Yacht.Presentation`으로 변경
  - `Assets/Editor` 쪽 `using` 17개, 정규화 이름 1곳(`TabletopSceneMigrator.cs`), 주석 2곳 갱신. 총 46개 파일
  - 씬·프리팹의 `m_EditorClassIdentifier`에 옛 네임스페이스 문자열 남음: 씬 `Augmented Dice` 2곳, `3D Layered Parchment Score Sheet.prefab` 1곳, `AugmentScrollPreset_0~3.prefab` 각 1곳. 스크립트 바인딩은 GUID 기준이라 영향 없음. 수정하지 않음
- 검증:
  - `Assets/**/*.cs`의 `Tessera.Games.AugmentedYacht` 0건. 컴파일 오류 0, 새 경고 0. EditMode 370/370 통과
  - 메인 씬 Transform 556개, `Assets/Prefabs/` 프리팹 17개, `AugmentScrollPreset_0~3`의 Missing Script 0
- 다음 작업: `LAYOUT-T4` 증강 찾기 보조

---

### 2026-10-22 — Claude Opus (LAYOUT-T1 Scripts 게임 폴더 이동)

- 작업 ID: `LAYOUT-T1`
- 변경:
  - `Assets/Scripts/Games/AugmentedYacht/**`와 `Games/Yacht/` 루트의 `.cs` 92개를 `AssetDatabase.MoveAsset`으로 옮김
  - GUID 보존용 스텁 `AugmentedYacht/YachtGameRules.cs`(참조 0건 확인) 삭제, `Games/AugmentedYacht/` 폴더 삭제
  - 코드 내용 변경 없음. 네임스페이스는 `LAYOUT-T2`에서 변경
- 검증:
  - 컴파일 오류 0, 새 경고 0. `.cs.meta` 대조로 92개 GUID 전부 보존, 새 GUID 0, 중복 0
  - EditMode 370/370 통과(기준선과 같음). 메인 씬 `Augmented Dice`와 `Assets/Prefabs/` 프리팹 17개의 Missing Script 0
- 다음 작업: `LAYOUT-T2` 프레젠테이션 네임스페이스 변경
