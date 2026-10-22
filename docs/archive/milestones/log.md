# M0~M16 세션 로그 (보관)

> **문서 종류**: 시점 기록 (보관)
> 2026-10-22 문서 재편 때 옛 `docs/agent/session_log.md`에서 `M0`~`M16` 시기 항목만 남긴 것입니다. 새 항목을 쓰지 않습니다.
> 과거 항목의 마일스톤 ID는 작성 당시 번호이며 현재 번호와 다를 수 있습니다(`D-029`, `D-035`).

---

### 2026-09-07 — Claude (M7-T5~T12를 M17로 이동)

- 작업 ID: 없음(문서 관리, `D-038`)
- 시작 상태: `D-036`으로 M7을 차단 최소분/폴리시분 태그로 나눴으나(미커밋), 사용자가 실제 태스크 이동을 요청
- 사용자 결정: `M7-T5`~`M7-T12`를 `M17`로 옮기고 `M7`은 `T1`~`T4`로 완료 처리
- 완료 내용:
  - `D-038` 추가, `D-036` 태그 분리 대체
  - §6: `M7` `DONE`, `M17` 선행 `M11`만, 결과물 갱신
  - §7 M7 절: 태그 분리 문단을 이동 문단으로 교체
  - §M17 절: `M17-T8`~`M17-T15`(구 `M7-T5`~`T12`) 추가, 완료 조건 보강. `M17-T13`~`T15` `DEFERRED`
  - §2 전체 상태 줄, §3.3 연기 테이블, §M16 매듭 절 참조 갱신
  - `m7_graphics_plan.md`: 표 상태·소속 갱신, §5 로그 추가
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`
- 실행한 검증: 두 문서 M7/M17 표기 일치, `M17` 선행 정합 확인. 코드·씬 변경 없음
- 새 결정/가정: `D-038`
- 남은 문제/차단 요소: 없음
- 다음 작업: `feature/yacht-dice-migration` → `main` 병합

### 2026-09-07 — Claude (M16 매듭 — 셀 셰이딩 채택 보류)

- 작업 ID: `M16` (매듭)
- 시작 상태: `M16`이 `DOING`으로 채택 판단 대기. `M16-T7`만 `TODO`, 나머지 완료. 코드는 커밋됨(26953db·9de4418·22fd80b·6dd46a0). `main` 병합 전 마일스톤을 확정해야 했음
- 사용자 결정: 채택 보류 + 인프라 유지. `m_RenderingMode`는 Forward 유지 + 문서화
- 완료 내용:
  - `D-037` 추가: 셀 셰이딩 채택 보류, 인프라·기본값 Baseline 유지, `M16-T7` `DEFERRED`, Forward 유지
  - §6 표 `M16` 행 `DOING` → `DEFERRED`, 결과물 갱신
  - §7 M16 절: `M16-T7` `DEFERRED`, "M16 매듭 (2026-09-07)" 기록 추가
  - §2 진행 포인터: 현재 마일스톤 없음(`main` 병합 준비), 전체 상태 줄에 M16 보류 반영
  - §3.3 연기 테이블에 M16 셀 셰이딩 행 추가
  - §15 최우선 작업을 `M16`에서 `main` 병합으로 교체
  - `cel_shading_pixel_plan.md` §12 매듭 절 추가, §6.4 표 주석 갱신
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/cel_shading_pixel_plan.md`
- 실행한 검증: 두 문서의 M16 상태 표기 일치 확인. 코드·씬·에셋 변경 없음(인프라 유지, `m_RenderingMode` 현행 유지)
- 새 결정/가정: `D-037`
- 남은 문제/차단 요소: 없음. Cel 육안 비교는 그래픽 패스로 이월
- 다음 작업: `feature/yacht-dice-migration` → `main` `--no-ff` 병합

### 2026-09-07 — Claude (M7 그래픽 후순위화 문서 정리)

- 작업 ID: 없음(문서 관리, `D-036`)
- 시작 상태: `M16` 매듭 작업 진행 중. 그래픽 폴리시를 `M17` 뒤로 미루고 싶으나 M7이 번호상 `M17` 선행에 물려 있었음. `work_plan` §2는 "M7-T1~T5 완료"였으나 `m7_graphics_plan` 표·로그상 `M7-T5`는 8면 주사위 도중 중단 상태로 두 문서가 어긋나 있었음
- 완료 내용:
  - `D-036` 추가: M7 잔여를 차단 최소분(T5·T7~T9 기능 레벨)과 폴리시분(T6·T11·T12)으로 분리, 폴리시분 `DEFERRED`, `M17` 선행을 차단 최소분으로 한정, 재번호 없음
  - §6 표: M7 행 결과물 갱신, `M17` 선행 `M7`, `M11` → `M7` 차단 최소분(T5·T7·T8·T9), `M11`
  - §2 전체 상태 줄: "M7-T1~T4 완료, M7-T5 중단", "M7 폴리시분 `DEFERRED`" 반영
  - §3.3 연기 테이블에 M7 폴리시분 행 추가
  - §7 M7 절: 차단 최소분/폴리시분 분류 문단 추가, `M7-T5` 정의 변경 이력(아이콘 이식 → 특수 주사위 외형) 명시
  - §15 최우선 작업 줄을 stale한 `M7-T1`에서 현재 `M16` 상태로 교체
  - `m7_graphics_plan.md`: 표 상태 갱신(T6·T11·T12 → `DEFERRED`), 분류 문단·완료 조건·§3 규칙 7 수정, stale한 §2 협업 포인터를 `work_plan` 참조로 축약
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`
- 실행한 검증: 두 문서의 M7 상태 표기 일치 확인, `M17` 선행 참조 정합 확인, 코드·씬·에셋 변경 없음
- 새 결정/가정: `D-036`
- 남은 문제/차단 요소: 없음
- 다음 작업: `M16` 채택 판단 재개

### 2026-09-06 — Claude (코스믹 큐브 머티리얼 null 오버라이드 수정)

- 작업 ID: 없음(마일스톤 밖 버그 수정)
- 시작 상태: 씬의 코스믹 큐브 프리팹 인스턴스에 렌더러 8개의 머티리얼과 테서랙트 메시를 null로 덮는 오버라이드 9개가 저장되어 있었음. 프롭이 통째로 기본 머티리얼로 보임. `HEAD`에는 없고 작업 트리에만 있던 변경
- 원인: `RollCosmicCube`가 `[ExecuteAlways]`라 에디트 모드에서도 연출을 만들고, 그때 `RuntimeAssetGuard`가 `HideFlags.DontSave` 사본을 `sharedMaterial`·`sharedMesh`에 끼운다. 사본은 씬에 직렬화되지 않으므로 그 상태로 씬을 저장하면 참조가 null로 기록되고, 프리팹 인스턴스에서는 그 null이 오버라이드로 굳는다
- 완료 내용:
  - 씬에서 null 오버라이드 9개를 제거해 프리팹의 구운 머티리얼·메시 참조를 되살림
  - `RuntimeAssetGuard`가 사본과 원본 에셋의 짝을 기억하도록 하고, 씬 저장 직전에 구운 에셋으로 되돌린 뒤 저장이 끝나면 같은 사본을 다시 끼우는 경로를 추가
  - 그 시점을 잡는 `RuntimeAssetGuardSceneHook`(`EditorSceneManager.sceneSaving`/`sceneSaved`)을 추가. 코스믹 큐브뿐 아니라 이 헬퍼를 쓰는 프롭 전부에 적용됨
- 변경 파일: `Scripts/Core/RuntimeAssetGuard.cs`, `Editor/RuntimeAssetGuardSceneHook.cs`(신규), `Editor/RuntimeAssetGuardTests.cs`(신규), `Scenes/Augmented Dice.unity`
- 실행한 검증: 씬을 다시 열고 저장해도 null 오버라이드가 생기지 않음(파일 해시 불변), 플레이 모드에서 크리스탈 셸·내부 성운·엣지 아웃라인·테서랙트 정상 렌더 확인, EditMode 795개 중 프로젝트 테스트 전부 통과
- 새 결정/가정: 없음
- 남은 문제/차단 요소: 없음
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-06 — Claude (M10.9 Unity 물리 기반 프리셋 베이커)

- 작업 ID: `M10.9-T1`~`M10.9-T6`
- 시작 상태: `M10.7` 트레이 확대가 작업 트리에 있었고, 프리셋은 여전히 웹 스튜디오에서 구운 것이라 착지 좌표가 트레이 좌표계와 다른 경로로 스케일되고 있었음
- 완료 내용:
  - `EditorSceneManager.NewPreviewScene()`과 `PhysicsScene.Simulate`로 플레이 모드 없이 물리를 스텝할 수 있음을 확인하고 그 경로를 채택
  - 트레이 내부 림 경계에 박스 물리벽 5장을 세우는 리그와 D6 박스·D8 볼록 메시 바디를 작성
  - 시뮬레이션 좌표계를 주사위 루트 로컬 프레임으로 두고, 저장 시 `BakedDiceController.TransformPresetDie`의 역함수로 소스 단위로 되돌리는 인코더 작성
  - 분포도·인식도·판정 시간 세 기준을 구현하고, 판정 시간 1.0~2.0초를 20구간으로 나눠 구간별 최고 분포 점수를 뽑는 층화 선별기 작성
  - 트레이 STL의 펠트 바닥 면을 실측해 플레이 경계를 바로잡음. 원본 웹 값을 그대로 옮겼더니 +Z 상한이 실제보다 낮아 착지가 화면 아래로 몰렸음
  - 회전량을 원본 웹 베이커와 같은 범위(일반 ±11, 판 뒤집기 ±40 rad/s)로 맞춤. 처음 값은 과해 저해상도에서 눈이 읽히지 않았음
  - 프리셋 20종을 다시 구움. 재생 코드와 JSON 포맷은 건드리지 않음
- 변경 파일: `Editor/DicePresetBakeRig.cs`, `Editor/DicePresetScoring.cs`, `Editor/DicePresetWriter.cs`, `Editor/DicePresetBaker.cs`, `Editor/DicePresetBakeTests.cs`(신규), `StreamingAssets/WebSource/presets/*.json`(21개 재생성)
- 실행한 검증: EditMode 793개 중 프로젝트 테스트 전부 통과, `Verify Pure Yaw Alignment` 600/600(최대 기울기 7.66도, 허용 25도), `Run Physics And Keep Validation` PASSED, 굴림 스크린샷 육안 확인
- 새 결정/가정: 프리셋 생산 경로를 `preset-studio`에서 Unity 에디터로 옮김. `preset-studio`는 트레이 STL 원본 생성 코드를 품고 있어 삭제하지 않고 그대로 둠
- 남은 문제/차단 요소: 판 뒤집기 프리셋 재생 길이가 원본 2.65초에서 2.0초 이하로 짧아짐. 판정 시간 상한을 맞추기 위한 의도된 변화지만 연출 강도는 실제 플레이에서 재확인 필요
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-06 — Claude (M10.8 셀 셰이딩 전환 구현)

- 작업 ID: `M10.8-T0`~`M10.8-T6`, `M10.8-T8`~`M10.8-T10`
- 시작 상태: `M10.7`·`M10.9` 커밋 완료. 화면이 여전히 "픽셀 필터를 씌운 3D 렌더"로 읽히고, M10.6의 포스트 색 양자화는 고유 색을 30색까지 줄이고도 이 문제를 풀지 못한 상태
- 완료 내용:
  - `Tessera/CelSurface` 셀 셰이더와 공유 include 신설. 노멀 축 스냅, 밴드 램프, 하드 그림자 계단, 하드 림
  - `PC_Renderer`를 Deferred에서 Forward로 전환. 셀 셰이더가 GBuffer를 채우지 않아 엣지 피처의 노멀 공급원이 `DepthNormals` 패스가 되기 때문
  - `RenderStyle { Baseline, Cel }` 런타임 토글을 `V` 키와 HUD `Style:` 버튼에 연결. 재료·조명·SSAO·렌더 타깃·엣지 임계값이 한 번에 따라감
  - 주사위 재료를 Lit 경로를 남긴 채 Cel 오버로드로 분기. 테이블·소품은 `CelStyleSwitcher`가 이미 만들어진 렌더러를 훑어 교체·복구
  - Cel에서 월드 카메라 렌더 타깃을 내부 해상도로 직접 생성. 가짜 픽셀화(1920 렌더 후 점 샘플링)를 실제 저해상도 렌더로 교체
  - `PixelReadabilityMetrics` 세 지표와 Baseline·Cel 동시 측정 도구 신설. M10.6의 "고유 색 수" 지표를 대체
  - 상세 문서 `docs/reference/art/cel_shading_pixel_plan.md` 작성
- 변경 파일: `Rendering/Shaders/CelSurface.shader`·`CelSurfaceShading.hlsl` 신규, `Scripts/Rendering/RenderStyle.cs`·`CelMaterialFactory.cs`·`CelStyleSwitcher.cs`·`PixelReadabilityMetrics.cs` 신규, `Editor/CelSurfaceTests.cs`·`PixelReadabilityMetricsTests.cs`·`RunPixelReadabilityValidation.cs` 신규, `Scripts/Rendering/TesseraPixelPalette.cs`·`PixelEdgeRendererFeature.cs`, `Scripts/Dice/DicePaletteCatalog.cs`, `Scripts/Games/AugmentedYacht/Presentation/`의 `AugmentedYachtController.cs`·`YachtCameraRig.cs`·`YachtLightingRig.cs`·`YachtInputRouter.cs`·`YachtSceneAssembler.cs`·`DiceVisualPool.cs`, `Settings/PC_Renderer.asset`, `docs/cel_shading_pixel_plan.md` 신규
- 실행한 검증: 정적 검토만. URP 패키지 소스에서 `unity_AmbientSky` 선언과 `DepthNormalsPass`의 비-OCT 출력 규약(`half4(normalWS, 0.0)`)을 확인해 셰이더 계약을 맞춤
- 새 결정/가정: 테이블·소품은 빌더에 분기를 넣지 않고 렌더러 순회 교체로 처리(롤백 지점을 한 곳으로 모으고 Baseline 경로를 무손상으로 유지). 밴드 수는 새 필드 없이 기존 `Metallic`에서 파생
- 남은 문제/차단 요소: **Unity 컴파일·테스트·플레이를 실행하지 않았다.** 셰이더 컴파일, 엣지 임계값 시작값, 화면 결과가 모두 미확인. `M10.8-T7` 특수 연출 셰이더 축소는 착수 전 확인 필요
- 다음 작업: `M10.8-T11` Unity 검증

### 2026-09-06 — Claude (M10.7 트레이 확대·상수 단일화, M10.8 계획 수립)

- 작업 ID: `M10.7-T1`~`M10.7-T4`, `M10.8` 계획
- 시작 상태: 작업 트리에 트레이 배율 확대 변경이 커밋되지 않은 채 쌓여 있었음. `DiceBoardMetrics`의 주석이 존재하지 않는 `Assets/Editor/DicePresetBaker.cs`를 참조했고, 신설한 `PlayBounds*` 상수는 소비처가 없었음
- 완료 내용:
  - 트레이 배율 0.05 → 0.06으로 올리고 프리팹 스케일·씬 인스턴스 Y를 맞춤
  - `AugmentedYachtController`의 중복 상수 `TrayScale`·`RollSurfaceY`·`TrayVisualY`를 제거하고 `DiceBoardMetrics` 참조로 교체
  - `TrayVisualY`를 `RollSurfaceY - PlayFloorSourceY * TrayScale`로 정의해 배율과 플레이 바닥 높이의 관계를 식으로 고정
  - 플레이 영역 경계 상수를 정의하고, 소스 원본과 Z 부호가 반대인 이유를 주석에 기록
  - 존재하지 않는 파일을 가리키던 주석을 사실에 맞게 고치고, 경계 상수에 아직 소비처가 없다는 사실을 명시
- 변경 파일: `Scripts/Core/DiceBoardMetrics.cs`, `Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs`, `Prefabs/Tabletop/Yacht Tray Visual.prefab`, `Scenes/Augmented Dice.unity`
- 실행한 검증: 파생 상수 값 손계산 검증(`TrayVisualY` 0.81701186이 씬 값 0.81701183과 일치, `TrayRimTopY` ~2.257, `ActiveArrangedY` ~2.81, `TrayOuterWidth` 9.30), 제거한 상수의 잔여 참조 없음 확인
- 새 결정/가정: `D-033`(그래픽 트랙 마일스톤을 소수점으로 연장), `D-034`(포스트 양자화 대신 재료 단계 셀 램프로 전환하고 A/B 토글을 구조에 넣음)
- 남은 문제/차단 요소: 프리셋 베이커 파일 5개(`DicePresetBaker`·`DicePresetBakeRig`·`DicePresetScoring`·`DicePresetWriter`·`DicePresetBakeTests`)가 세션 중 병행 작성되어 아직 미추적 상태. 커밋 범위 확정 전까지 대기
- 다음 작업: `M10.8-T0` 기준선 캡처와 토글 골격

### 2026-09-04 — Claude (M10-T6·M10-T8 컨트롤러 분해)

- 작업 ID: `M10-T6`, `M10-T8`
- 시작 상태: `M10-T1`~`M10-T5`와 `M10-T7`이 끝나 컨트롤러가 1,593줄로 줄어 있었으나, 턴 흐름 플래그 5개와 씬·HUD 생성 코드가 그대로 남아 있었고 `ITurnDelaySource`는 정의만 있고 모래시계 직접 호출이 9곳 남아 있었음
- 완료 내용:
  - `PresentationPhase` 열거형을 추가해 `turnTransitionInProgress`·`hasCompletedRoll`·`isArranging`과 코루틴 핸들 2개의 조합을 여섯 가지 명시적 상태(Idle/TurnTransition/AwaitingRoll/Rolling/Arranging/Settled)로 통합
  - `YachtTurnFlowPresenter`로 세션 생성, 턴 타이머 콜백, 드래프트 선택, 점수 확정, 턴 넘김, 게임 종료, 상태 문구·타이머 문구를 이관
  - `YachtDiceRoundPresenter`로 주사위 화면 사본과 굴림 궤적·킵 정렬 애니메이션을 이관
  - `YachtSceneAssembler`로 씬 레이아웃 해석, 최소 월드·프레젠테이션 구성, 게임 흐름 UI 생성, 디버그 버튼 배선을 이관
  - `YachtRunicPresenter`로 추가 턴 적립·소모, 증강 점수 덮어쓰기, 룬 디버그 버튼을 이관
  - `ITurnDelaySource`에 `Pause`·`Stop`을 추가해 턴 흐름의 모래시계 직접 호출을 제거
  - `AugmentCardValidationRunner`가 리플렉션 대신 `YachtTurnFlowPresenter`를 직접 호출하도록 수정
- 변경 파일: `Presentation/PresentationPhase.cs`·`YachtTurnFlowPresenter.cs`·`YachtDiceRoundPresenter.cs`·`YachtSceneAssembler.cs`·`YachtRunicPresenter.cs` 신규, `Presentation/AugmentedYachtController.cs`, `Presentation/ITurnDelaySource.cs`, `Tabletop/HourglassTimer.cs`, `Editor/AugmentCardValidationRunner.cs`
- 검증 도구: 점수표 클릭을 흉내 낼 수단이 없어 `YachtTurnFlowPresenter.CommitScore`를 공개 진입점으로 두고, `Tessera/Validation/Visual Check/Auto Play Augmented Game` 메뉴로 드래프트·굴림·기입을 끝까지 자동 진행하도록 만듦. `M11-T7` 반복 전체 게임 테스트에서 재사용함
- 실행한 검증: Unity 에셋 재임포트 후 재컴파일, 신규·수정 스크립트별 컴파일 진단 조회, EditMode 전체 테스트, 플레이 모드 진입 후 런타임 오류 관찰, 증강 요트 자동 완주 1회
- 검증 결과: 컴파일 오류 0건, 콘솔 오류 0건. EditMode 755/761 통과(실패 1건은 `unity-skills` 패키지 자체의 `SKILL.md` 바이트 예산 테스트로 이 저장소 코드와 무관, 건너뜀 5건). 자동 완주에서 드래프트·굴림·킵 정렬·점수 기입 24회·턴 넘김·게임 종료 오버레이가 모두 동작(P1 61점, P2 76점). 컨트롤러 1,593줄 → 604줄(M10 시작 시점 대비 3,096줄 → 604줄)
- 새 결정/가정: `D-032`(400줄 기준을 책임 기준으로 대체). 흐름 프레젠터는 갱신 직전 `TrayRebindRequested`로 증강 트레이 참조를 다시 맞춰, 늦게 채워지는 카메라·트레이 참조를 붙잡지 않게 유지함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 결정에 따라 `M11-T1` 또는 `M7` 잔여 그래픽 작업

### 2026-09-03 — Claude (M7.5-R2 변형 18개 이관)

- 작업 ID: `M7.5-R2`
- 시작 상태: 변형 18종의 점수 규칙이 `YachtAugmentScoreEngine.ApplyReplacementScores`의 else-if 체인에, 획득 시 부수 효과는 `YachtAugmentRuntime.ApplyAugment`의 분기에 나뉘어 있었음
- 완료 내용: `Augments/Modification/`에 공통 기반 `ModificationAugment`와 처리기 18개를 추가하고 카탈로그에 등록함. 족보 판정 헬퍼를 `Augments/Core/YachtDiceFacts.cs`로 추출해 기본 점수 계산과 처리기가 공유하게 함. `YachtAugmentScoreEngine`에서 교체 로직과 전용 판정 헬퍼를 제거해 기본 족보 계산만 남김. 점수 미리보기·후보 생성 경로가 `IBeforeScorePreview` 처리기를 호출하도록 바꾸고, 정의 목록·드래프트 후보·랜덤 박스 후보가 카탈로그와 잔여 정적 배열을 합친 `AllDefinitions`를 쓰도록 정리함
- 변경 파일: `Augments/Modification/` 19개 신규, `Augments/Core/YachtDiceFacts.cs` 신규, `Augments/Core/AugmentContexts.cs`, `Augments/Core/YachtAugmentCatalog.cs`, `YachtAugmentRuntime.cs`, `YachtAugmentScoreEngine.cs`, `YachtGameRulesTests.cs`, `YachtModificationAugmentTests.cs`
- 실행한 검증: 생성된 C# 처리기에서 점수 식을 추출해 이관 전 규칙과 전수 대조(증강 18종 × 주사위 5개 조합, 눈 1~6과 1~7 두 범위, 12,852건). Unity 재임포트 후 EditMode 결과를 지우고 전체 재실행
- 검증 결과: 전수 대조 불일치 **0건**, 컴파일 오류 0건, EditMode **83/83 통과**
- 새 결정/가정: `D-026`(분류 공통 규칙은 런타임에, 증강 고유 부수 효과만 처리기에), `D-027`(`YachtDiceFacts` 추출). 정의 노출 순서를 유지하려고 카탈로그 등록 순서를 기존 배열 순서와 일치시킴
- 남은 문제/차단 요소: 없음
- 다음 작업: `M7.5-R3` 강화 중 주사위·상시형 이관

### 2026-09-03 — Claude (M7.5-R1 검증 및 R2 특성화 테스트 선행 작성)

- 작업 ID: `M7.5-R1` 검증, `M7.5-R2` 사전 준비
- 시작 상태: R1 구현은 끝났으나 클라우드 환경에 C# 컴파일러를 설치할 수 없어 컴파일·테스트가 미실행 상태였음
- 완료 내용: 변형 18종의 실패·경계 분기를 덮는 특성화 테스트 23개를 `Assets/Editor/YachtModificationAugmentTests.cs`로 추가함. Unity 에디터를 직접 조작해 재컴파일과 EditMode 전체 실행을 수행하고, Kind 3종 축소로 깨진 기존 단정 1건을 수정함
- 변경 파일: `Assets/Editor/YachtModificationAugmentTests.cs`(신규), `Assets/Editor/AugmentCardViewTests.cs`
- 실행한 검증: Unity 6000.3.21f1에서 `Ctrl+R` 재임포트 후 Console 확인, Test Runner EditMode 전체 실행 2회
- 검증 결과: 컴파일 오류 0건. 1차 실행 82/83 (기존 `CommonCard_이름효과종류대상상태를_같은레이아웃에표시한다`가 종류 라벨 `"족보 교체"`를 기대해 실패). 라벨 기댓값을 `"변형"`으로 갱신한 뒤 2차 실행 **83/83 통과**
- 새 결정/가정: 없음. `D-021`의 Kind 축소가 카드 종류 라벨 표시에 미치는 영향이 테스트로 확인됨
- 남은 문제/차단 요소: 없음. Unity MCP는 데스크톱 앱에 미등록 상태라 이번에는 에디터를 직접 조작해 검증함
- 다음 작업: `M7.5-R2` 변형 18개 이관

### 2026-09-03 — Claude (M7.5-R1 코어 골격)

- 작업 ID: `M7.5-R1`
- 시작 상태: 증강 로직이 `YachtAugmentRuntime.cs` 한 파일의 시점별 메서드에 `if (Owns(...))` 나열로 있었고, 발동 시점을 나타내는 구조가 없었음
- 완료 내용: `Assets/Scripts/Games/Yacht/Augments/Core/`에 시점 인터페이스 9종, 컨텍스트 7종, `IAugmentState`/`AugmentStateStore`, 카탈로그, 디스패처를 추가함. `YachtAugmentPlayerState`에 증강별 상태 저장소 `States`를 추가하고 `Clone()`에 반영함. `YachtAugmentKind`를 3종(`Modification`/`Enhance`/`Quest`)으로 축소하고 `YachtAugmentHook`을 제거함
- 변경 파일: `Augments/Core/` 6개 신규, `YachtAugmentRuntime.cs`, `AugmentCardView.cs`, `AugmentIconGalleryWindow.cs`, `AugmentCardViewTests.cs`
- 실행한 검증: 리포지토리 전체에서 제거된 심볼(`YachtAugmentHook`, 구 `Kind` 값 5종) 잔존 0건 확인, 신규·수정 파일의 괄호 균형 검사, 정의 팩토리 5개의 초기화 구문 육안 확인, CRLF 줄바꿈 보존 확인
- 검증 결과: 정적 검사 통과. 컴파일과 EditMode 테스트는 이 세션에서 실행하지 못해 후속 로그에서 확인함 (결과: 83/83 통과)
- 새 결정/가정: `D-024`(시점 인터페이스를 처리기가 있는 9개로 한정), `D-025`(`YachtAugmentHook` 제거). 코어는 아직 어디에서도 호출되지 않으므로 게임 동작 변화는 `Kind` 축소에 따른 카드 종류 라벨 표시뿐임
- 남은 문제/차단 요소: Unity 컴파일·EditMode 테스트 결과 확인 대기
- 다음 작업: `M7.5-R2` 변형 18개 이관

### 2026-09-03 — Claude (M7.5-R0 작업 계획서 갱신)

- 작업 ID: `M7.5-R0`
- 시작 상태: M7-T4까지 완료 후 사용자 지시 대기 중이었고, M7.5는 "M7 완료 후 예정"으로만 적혀 있었음
- 완료 내용: 사용자 지시로 M7을 중단하고 M7.5를 현재 마일스톤으로 승격함. 진행 포인터, 마일스톤 표, `M7.5` 상세 절(현재 구조의 문제·목표 구조·디렉터리·R0~R6 작업표·완료 조건), 결정 기록 `D-019`~`D-023`을 추가함
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 활성 증강 45개의 3분류 분해(변형 18 + 강화 16 + 퀘스트 11)를 이식 매트릭스 3.1~3.3절과 대조
- 검증 결과: 45개가 정확히 나뉘어 디렉터리 분류와 `D-021`의 근거가 성립함
- 새 결정/가정: `D-019`~`D-023`. 상태 직렬화는 인터페이스만 맞춰두고 실제 구현은 M9로 연기함
- 남은 문제/차단 요소: 없음
- 다음 작업: `M7.5-R1` 코어 골격 (사용자 지시 후 시작)

### 2026-09-03 — Claude (M7-T5 증강 고유 아이콘 이식)

- 작업 ID: `M7-T5`
- 시작 상태: 카드 아이콘이 `YachtAugmentKind` 6종 글리프를 절차적으로 그려 쓰고 있었고, 증강별 구분이 없었음. `AugmentCardView`에는 `Resources.Load<Sprite>("AugmentIcons/{id}")` 조회 경로가 이미 있었으나 해당 폴더가 없었음
- 완료 내용: 이전 프로젝트 `augmented-dice`의 `src/svgIcons.js`에서 활성 증강 45개 아이콘을 추출해 앤틱 잉크(`#3B2A1D`) 64×64 투명 PNG로 렌더링하고 Sprite/Point 임포트 설정 `.meta`와 함께 배치함. 고유 아이콘 사용 시 `icon.color` 틴트를 해제하고 아이콘 받침판을 제거함
- 변경 파일: `Assets/Resources/AugmentIcons/` (PNG 45 + `.meta` 45 + 폴더 `.meta`), `Assets/Scripts/Games/AugmentedYacht/AugmentCardView.cs`
- 실행한 검증: 45개 컨택트 시트 육안 확인, 카드 컨텍스트(받침판 56px·아이콘 46px·Point 필터)에서 잉크/틴트 3안 비교, 알파 경계 상자 검사로 빈 아이콘·과소 렌더 확인
- 검증 결과: 45개 모두 정상 렌더되고 두 톤 구조가 유지됨. 기존 테스트 `CommonCard_FallbackPixelIconUses64pxPointFilter`의 조건(64×64, Point)을 새 아이콘도 만족함
- 새 결정/가정: `D-022`, `D-023`. 원본의 도달 불가능한 중복 분기 2개(`lucky-sevens`, `every-little`)는 실제 게임에 표시되던 앞쪽 분기를 채택함
- 남은 문제/차단 요소: Unity 에디터에서의 임포트·표시 확인은 사용자 확인 대기
- 다음 작업: `M7.5-R0` 작업 계획서 갱신

### 2026-08-27 — Codex (M7-T4 Scene 조정값 기본 프리팹 이관)

- 작업 ID: M7-T4 증강 카드 후속 수정
- 완료 내용: 보조 말림 레이어를 제거하고 2.5바퀴 말림을 Z 210°로 고정함. 밝은 양피지 재질, 슬롯 전체 카드 크기, 원통형 가죽 띠·왁스·인장의 공통 말림 중심을 4종 프리팹과 런타임 폴백에 적용함
- 실행한 검증: 프리팹·프리뷰 재생성, 카드 전용·전체 EditMode, 선택 창 및 트레이 Game View, Console 확인
- 검증 결과: 카드 전용 24/24, 전체 60/60 통과, Play Mode 콘솔 오류 0건
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-27 — Codex (M7-T4 증강 카드 디자인 통합 완료)

- 작업 ID: M7-T4 증강 카드 디자인 수정
- 시작 상태: 선택 창과 트레이의 카드 디자인이 달랐고, 기존 말림 외곽과 철 봉 없는 카드형 스크롤로 재정리가 필요했음
- 완료 내용: 펼쳐진 오른쪽 종이를 직사각형 카드로 재구성하고 왼쪽에 2.5바퀴 말림·가죽 띠·정육면체 인장을 결합함. 철 봉과 리본은 제외하고, 4종 손상 프리셋과 얇은 시안 내부선 및 공통 텍스트 안전 영역을 선택 창·트레이에 함께 적용함
- 변경 파일: 카드/스크롤 런타임·에셋 생성기·EditMode 테스트, `Assets/Resources/AugmentScrolls`, 검증 캡처, M7 관련 문서
- 실행한 검증: 카드 전용 및 전체 EditMode, 양쪽 드래프트 선택, 선택 프리셋의 트레이 배치, Console 확인
- 검증 결과: 카드 전용 24/24, 전체 EditMode 60/60 통과. 선택 창과 트레이에서 같은 카드형 스크롤 외형을 확인했고 콘솔 오류는 0건
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-27 — Codex (M7-T4 3D 스크롤 검증 재실행)

- 작업 ID: M7-T4 후속 카드 시각 리디자인 검증
- 시작 상태: 이전 배치 검증이 Unity 라이선스 재연결 오류로 중단된 상태
- 완료 내용: Unity MCP 연결 후 전체 에셋 재임포트·컴파일, 카드 전용 및 전체 EditMode 회귀, Play Mode 보유 카드 3장 강제 표시, 일반 모드 비생성을 재검증함. 현재 NUnit과 호환되지 않던 `IReadOnlyList Has.Count` 테스트 표현은 직접 Count 비교로 수정함
- 변경 파일: `Assets/Editor/AugmentCardViewTests.cs`, `Assets/Screenshots/m7_t4_3d_scroll_validation.png`, M7 관련 문서
- 실행한 검증: 카드·양피지 EditMode, 전체 EditMode, 프리셋 0·2·4 런타임 강제 표시, 메시·인장·오버레이·레이캐스트 상태 검사, 일반 모드 비활성 검사, Game View 캡처, Console 확인
- 검증 결과: 카드·양피지 23/23, 전체 EditMode 59/59 통과. 보유 카드 3장 각각 정적 메시 5개·중앙 인장·활성 오버레이를 확인했고 일반 모드는 활성 카드와 오버레이가 모두 0개였음
- 시각 검수: 캡처 기준 펼친 본문은 정상 배치되지만 말림이 얇은 직사각 막대처럼 읽혀 레퍼런스의 느슨한 다층 롤 기준은 미통과. 캡처 도구가 Screen Space Overlay를 합성하지 않아 텍스트 선명도는 별도 수치 상태로만 확인함
- 콘솔: 프로젝트 코드 오류는 없으며 Unity Quick Install의 한글 경로 변환 예외 1건과 Unity AI 계정 API 경고 1건이 남음
- 남은 문제/차단 요소: 기술적 차단 없음. 3D 롤과 인장 시각 보정 필요
- 다음 작업: 롤 단면 노출과 본문 연결 곡면, 밀랍 인장 크기·명암을 재조정한 뒤 동일 구도로 재검증

### 2026-08-27 — Codex (M7-T4 3D 스크롤 리디자인 구현)

- 작업 ID: M7-T4 후속 카드 시각 리디자인
- 시작 상태: 레퍼런스의 펼쳐진 종이와 느슨한 말림, 중앙 밀랍 인장을 실제 3D 오브젝트로 구현하라는 사용자 지시 접수
- 완료 내용: 펼친 양면 종이, 2중 나선 롤, 인장 밴드, 불규칙 밀랍 인장, 리본 꼬리, 오버레이 안전 앵커를 갖춘 모델을 구현함. 종이 Base Color·URP Lit 재질과 5종 정적 메시 프리팹을 생성하고 보유 카드가 프리셋 ID에 따라 로드하도록 교체함
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/AugmentScrollModel.cs`, `Assets/Editor/AugmentScrollAssetGenerator.cs`, `Assets/Resources/AugmentScrolls`, `Assets/Scripts/Games/AugmentedYacht/AugmentTrayCardView.cs`, `Assets/Editor/AugmentCardViewTests.cs`, M7 관련 문서
- 실행한 검증: 격리된 Unity 프로젝트 컴파일 및 프리팹 생성, 생성 로그와 프리팹·메시·재질 파일 확인, 카드 전용 테스트 추가, 변경 파일 공백 오류 검사
- 검증 결과: Unity 컴파일 오류 0개, 프리팹 5종과 메시 25개 생성 성공. 배치 테스트 실행은 Unity 라이선스 서비스가 반복적으로 끊겨 결과 파일 생성 전에 중단됨
- 남은 문제/차단 요소: 메인 카메라 Play Mode 시각 검수와 EditMode 재실행 필요
- 다음 작업: Unity 라이선스가 정상 연결된 편집기에서 카드 전용·전체 EditMode와 5종 강제 표시 캡처 수행

### 2026-08-27 — Codex (M7-T4 3D 스크롤 리디자인 계획 전환)

- 작업 ID: M7-T4 후속 카드 시각 리디자인
- 시작 상태: 2D 스크롤 시안 계획을 3D 오브젝트 제작 방식으로 전환한다는 사용자 결정 접수
- 완료 내용: 상단 왼쪽의 다층 롤과 펼쳐진 본체, 중앙 밀랍 인장, 카메라용 형태 과장, 3단계 종이 재질, 5종 정적 메시 프리팹, 고해상도 오버레이 앵커를 포함한 3D 제작 계획으로 재작성함
- 변경 파일: `docs/augmented_yacht_m7_scroll_redesign_plan.md`, `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 검증 결과: 기존 슬롯 비율, High-Res Overlay, 프리셋 상태 상속, 월드 콜라이더 입력과 런타임 생성 구조를 유지하도록 계획 범위를 대조함. 중단된 2D 시안은 프로젝트에서 제거함
- 남은 문제/차단 요소: 구현 시작 전 사용자 승인 필요
- 다음 작업: 프리셋 0 무재질 블록아웃 제작 및 메인 카메라 가독성 검증

### 2026-08-27 — Codex (M7-T4 양피지 말림 형태 보정)

- 작업 ID: M7-T4 후속 카드 시각 고도화
- 시작 상태: 보유 카드의 왼쪽 말림이 원통을 배치한 것처럼 보이고, 고해상도 사각 오버레이가 3D 곡률을 평면처럼 보이게 하는 피드백 접수
- 완료 내용: 왼쪽을 본체와 연속된 1.28~1.42회 나선형 종이 단면으로 재구축하고, 왼쪽 아래가 강하고 오른쪽 아래가 약한 비대칭 하단 말림을 추가함. 메시 분할 확대, 카메라용 높이 과장, 앞·뒷면 재질 분리, 중앙 콘텐츠 전용 오버레이 투영을 함께 적용함
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/AugmentParchmentVisuals.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentTrayCardView.cs`, `Assets/Editor/AugmentCardViewTests.cs`, M7 그래픽 계획 및 현재 진행 포인터
- 실행한 검증: Unity 컴파일, 카드·상태 전용 EditMode 테스트, 전체 EditMode 회귀 테스트, Play Mode 드래프트·보유 카드 캡처, Console 확인
- 검증 결과: 컴파일 오류 0개, 전용 18/18 및 전체 54/54 테스트 통과. 보유 카드에서 나선 외곽·하단 뒷면·중앙 고해상도 콘텐츠 분리를 확인함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 시각 검수 후 미세 조정 또는 다음 M7 작업 진행

### 2026-08-26 — Antigravity (M7 카드 비주얼 개선 요구사항 반영 및 백업)

- 작업 ID: M7-T4 후속 시각 고도화 사항 정리 및 백업
- 시작 상태: M7-T4(보유 카드 3D 트레이 슬롯 배치 및 포인터 상호작용) 완료 후 개선 피드백 접수
- 완료 내용: 증강 카드 비주얼 3가지 핵심 개선 요구사항을 마일스톤 및 그래픽 계획 문서에 반영하고 변경사항 백업 커밋/푸시 준비
  1. **텍스트 픽셀 필터 우회**: 3D 픽셀화 렌더 타겟으로 인한 텍스트 깨짐 방지 위해, 점수표(`ParchmentScoreSheet`) 시스템과 동일하게 3D 트레이 슬롯 좌표를 스크린 오버레이(`Pixel Presentation` Canvas)로 투영(`WorldToScreenPoint`)하는 High-Res Overlay 파이프라인 확정
  2. **호버 애니메이션 개선**: 픽셀 격자 스냅 튐 없이 부드러운 보간(`Lerp`/`SmoothDamp`)을 통해 카드 고도와 스케일(1.0 → 1.06)이 자연스럽게 늘어나는 애니메이션 확정
  3. **스크롤(두루마리) 디자인 및 3~5종 테마**: 단정한 네모 카드 형태 대신 롤 바(Rolled Spindles)와 앤틱 놋워크 보더, 왁스 인장이 적용된 **펼쳐진 양피지 스크롤 외형 세트 3~5종** 제작 및 랜덤 출력 사양 확정
- 변경 파일: `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 문서 동기화, Unity EditMode 전체 테스트 통과 상태 보존
- 검증 결과: 마일스톤 문서 갱신 완료
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-26 — Codex (M7 협업 문서 분리)

- 작업 ID: M7 문서 구조 정리
- 시작 상태: M7 상세 표가 전체 계획서에 포함되어 있었고, 완료 즉시 다음 작업을 자동으로 `DOING` 처리하는 규칙이 사용자 승인 방식과 충돌했음
- 완료 내용: M7 전용 그래픽 계획 문서를 만들고 기존 M7 내용, 확정 시각 사양, 단계별 사용자 협업 규칙을 이전함. 전체 계획서는 M7 요약과 전용 문서 참조만 유지하도록 정리함
- 변경 파일: `docs/augmented_yacht_m7_graphics_plan.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: Markdown 링크, M7-T1~M7-T12 상태, 현재 포인터, 사용자 승인 규칙, Git 변경 범위 확인
- 검증 결과: `M7-T1`은 `TODO`·사용자 지시 대기이며 `M7-T10`은 `DEFERRED`; Unity 코드·씬·에셋 변경 없음
- 새 결정/가정: M7 상세 상태는 전용 문서만 기준으로 관리하고 다음 단계는 사용자가 작업 ID를 언급한 경우에만 시작함
- 남은 문제/차단 요소: 없음
- 다음 작업: 사용자 지시 대기

### 2026-08-25 — Codex (M6 전체 이식)

- 작업 ID: `M6-T1`~`M6-T8`
- 시작 상태: M5 대표 6종만 권위 런타임에 연결되어 있었고, 나머지 활성 증강의 공통 점수 계층·개인 진행 상태·수동 행동 경로가 없었음
- 완료 내용: 활성 증강 45개 정의와 설명, 점수 교체 18종, 기본/강화/주사위 보너스 분리, 음수 점수 기입 플래그, 퀘스트·라운드 비교·저축 상태, 특수 주사위 6종, 요트 뱅크, 갬빗 4→6개, 더블 다운, 등가교환, 연금술, 랜덤 박스와 개인 충돌 필터를 구현함
- UI 연결: M7 최종 그래픽 전의 기능형 수동 행동 버튼 5종과 노즈도르무 15초 모래시계 타이머를 연결함
- 제외 처리: 3~8번 6종은 삭제, `four-by-four`·`two-households`·`strange-die`·`coin-toss` 4종은 사용자 결정에 따른 미구현 상태로 드래프트와 랜덤 박스에서 제외함
- 실행한 검증: Unity 스크립트 재컴파일, 활성 정의/점수 교체/점수 계층/음수/요트 뱅크/프로모션/수동 행동/퀘스트 조합 EditMode 테스트, 일반 요트 전체 회귀 테스트, 메인 씬 Play Mode 진입 점검
- 검증 결과: Unity 컴파일 오류 0건, 전체 EditMode 36/36 통과, Play Mode 진입 후 Console 오류 0건
- 새 결정/가정: 원본 JSON 55개는 이력·표시 근거로 유지하되 실행 정의는 구현 승인된 45개만 사용한다. M6 완료 조건의 “미구현 없음”은 이후 사용자 결정과 충돌하므로 모든 항목의 구현·삭제·의도적 미구현 상태 확정으로 변경함
- 남은 문제/차단 요소: 규칙 구현 차단 없음. 카드·스티커·특수 주사위 외형·강화 점수 글로우·요트 뱅크 킵 존 금색 표시는 M7 범위
- 다음 작업: `M7-T1` 증강별 그래픽 에셋 및 상태 표현 목록 작성

### 2026-08-25 — Codex (M5 최신 규칙 재실행)

- 작업 ID: `M5-R1`
- 시작 상태: 기존 M5 대표 수직 구현은 완료되어 있었으나, 이후 확정된 개인 변형 정책과 달리 `lucky-sevens`가 전역 보유되고 중간 획득 시 양쪽 Aces를 초기화하며 양쪽에 추가 턴을 지급했음
- 완료 내용: `lucky-sevens`를 보유자 전용 정의로 전환하고 보유자의 Aces·총점·추가 턴만 원자적으로 변경하도록 수정. 상대 플레이어가 같은 변형을 별도로 획득할 수 있고 상대 점수 미리보기에는 효과가 적용되지 않도록 회귀 테스트 보강. `GlobalAugmentIds`는 명시적 전역 카드용 예약 상태로 유지
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: Unity Console 컴파일 오류 확인, `Tessera.Editor.Tests.YachtGameRulesTests` 실행, 전체 EditMode 테스트 실행
- 검증 결과: Unity 컴파일 오류 0건, M5 규칙 테스트 22/22 통과, 전체 EditMode 28/28 통과
- 새 결정/가정: 변형 증강은 플레이어별 소유·중복·충돌·점수 계산을 사용하고, 복수 `random-box`는 P1→P2 순서만 고정하되 상대 결과를 후보 충돌로 보지 않음(`D-016`, `D-017`)
- 남은 문제/차단 요소: 없음. 전체 변형 증강과 플레이어별 족보 스티커·스냅샷 확장은 M6 범위
- 다음 작업: `M6-T1` 점수·족보 변경 증강 공통 실행 프레임 확장

### 2026-08-24 — Codex (`step-by-step` B안 적용 확정)

- 작업 ID: `M6-T2` 보상 조건 확정
- 시작 상태: `step-by-step`의 +35→+55 강화는 유지하되 상단 합계 63점 조건이 어렵다는 플레이 검토가 있었고, 55 또는 58 완화안을 비교함
- 완료 내용: B안으로 상단 보너스 기준을 63→58로 낮추고 +55 보상을 유지하도록 정적 설명, 마이그레이션 판정, 증강 런타임 판정을 확정
- 변경 파일: `Assets/StreamingAssets/WebSource/data/augments.json`, `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: `step-by-step` JSON 항목과 런타임 `stepRewarded`·상단 보너스 처리 대조, 58점 경계·순서 실패·Phase 1 후보 필터 테스트, Unity EditMode 전체 테스트 실행
- 검증 결과: JSON 증강 55개 유지, `step-by-step` 보상 문구가 기준 58·보너스 55를 명시함. Unity EditMode 테스트 27개 모두 통과
- 새 결정/가정: 기본 +35는 유지하고, 순서 완료 후 보유자 전용 threshold 58 이상이면 +55를 확정한다(`D-015`). 별도 +20은 추가하지 않는다.
- 남은 문제/차단 요소: 저장·업적 UI가 새 `stepRewarded`/`stepBonusGranted` 상태를 표시·직렬화하는지는 M6 저장 계층 점검 필요
- 다음 작업: M6 저장·UI 계층에서 `stepRewarded`, 개인 threshold 58, +55 확정 상태를 스냅샷과 표시 문구에 연결

### 2026-08-24 — Codex (`step-by-step` 보상 조건 완화안)

- 작업 ID: `M6-T2` 사전 재검토
- 시작 상태: +35→+55 강화 보상 자체는 수용 가능하나, 상단 합계 63점 조건 달성이 어렵다는 플레이 피드백이 제기됨
- 완료 내용: 보상량을 유지하고 `step-by-step` 전용 threshold를 63→55 또는 58로 낮추는 안을 비교. 즉시 +55 보장안의 밸런스 위험과 중간 점수 경쟁 영향을 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 원본 `augments.json`·`authoritativeGame.js`의 `stepRewarded` 및 상단 보너스 조건 확인, 단순 목표 눈 재굴림 모델의 기준별 확률 추정
- 검증 결과: 63 기준은 단순 모델에서 약 4%, 58 기준 약 11%, 55 기준 약 17%로 추정됨. 실제 게임 확률은 전략·조합에 따라 달라짐
- 새 결정/가정: 우선 63→55 완화, +55 보상 유지. +20 즉시 지급안은 이번 조정의 1순위에서 제외
- 남은 문제/차단 요소: 55와 58 중 최종 threshold, 보너스 확정 후 상단 칸 초기화 시 보너스 유지 정책
- 다음 작업: 사용자 결정 후 `M6-T2`의 정적 설명·threshold 적용·경계 테스트를 갱신

### 2026-08-24 — Codex (`step-by-step` 보상 변경안 검토)

- 작업 ID: `M6-T2` 사전 검토
- 시작 상태: 원본 `step-by-step`은 Aces→Sixes 완료 후 상단 보너스를 +35에서 +55로 강화하며, 실제 보너스는 상단 합계 기준 달성 시 반영됨
- 완료 내용: 퀘스트 완료 즉시 +20점을 지급하는 보상안을 검토하고 긍정·부정 관점, 기본 +35 중첩 여부, UI·상태·업적·테스트 영향을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 원본 `augments.json`, `authoritativeGame.js`, `augmentRules.js`의 `stepRewarded`·상단 보너스 처리 대조
- 검증 결과: 현재 구현은 `stepRewarded`가 상단 보너스를 55로 바꾸고 퀘스트 보상 자체는 0점으로 기록하는 구조임을 확인
- 새 결정/가정: 기본 +35를 유지하고 별도 `questBonus`로 +20을 즉시 지급하는 안을 우선 검토한다. +20은 상단 합계 판정에 포함하지 않는다.
- 남은 문제/차단 요소: +20과 +35의 중첩 여부, 상단 보너스 선지급 예외, 실패 후 재도전 정책
- 다음 작업: 사용자 결정 후 `M6-T2`의 정적 설명·런타임 보상·저장 상태·회귀 테스트를 함께 갱신

### 2026-08-24 — Codex (M5 대표 증강 수직 구현)

- 작업 ID: `M5-T1`~`M5-T9`
- 시작 상태: 증강 모드는 기본 요트 규칙만 실행했고 드래프트·증강 런타임·발동 표현이 없었음
- 완료 내용: 정적 정의/런타임 상태 분리, 라운드 1·6·9의 양 플레이어 드래프트, 공통 처리 지점, `lucky-sevens`·`8-sided`·`no-time-to-waste`·`table-flip`·`random-box`, 전역 점수 초기화/추가 턴, 임시 선택·보유·발동 UI를 연결
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Scripts/Games/Yacht/YachtAugmentRuntime.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: Unity 스크립트 재컴파일, M5 규칙 EditMode 테스트, 전체 EditMode 회귀 테스트, 공유 씬 Play Mode 드래프트/선택/혼합 주사위 굴림, Unity Console 확인
- 검증 결과: M5 규칙 18/18, 전체 EditMode 24/24 통과. Play Mode에서 드래프트 UI 중복 0건, 버튼 선택 후 혼합 프리셋·주사위 타입 일치, Unity Console 오류 0건
- 새 결정/가정: `table-flip`은 굴림으로 세지 않아 `no-time-to-waste` 성공을 유지함(`D-013`); 복수 `random-box`는 P1→P2로 순차 해결함(`D-014`)
- 남은 문제/차단 요소: 없음. 최종 카드·특수 주사위 외형·VFX·사운드는 계획대로 M7 범위
- 다음 작업: `M6-T1` 증강 공통 실행 프레임 확장

### 2026-08-24 — Codex (PC 2인 공유 변형 증강 정책)

- 작업 ID: `M5` 사전 설계 검토
- 시작 상태: 기존 구현은 증강 상태와 점수 변형을 플레이어별로 보관했으나, PC 버전에서는 변형 증강이 두 플레이어에게 함께 영향을 주도록 기획 변경
- 완료 내용: 기존 `modificationAugmentIds` 1~26번을 전역 변형 후보로 분류하고, Yacht 칸 변형 22~26번만 Phase 1(`currentRound = 1`)에 제한하도록 정책을 수정. 1~21번의 중간 획득 시 대상 점수 초기화·영향받은 플레이어 추가 턴, 전역 상태 분리, 드래프트·점수·UI·네트워크·random-box 연쇄 영향을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: 기존 증강 분류와 Phase 1 전용 필터를 이전 권위 코드 기준으로 대조
- 검증 결과: 1~26번 점수/족보 변형과 기존 Phase 1 퀘스트(`fast-straight`, `step-by-step`)를 구분했으며, 3~8번은 폐기 검토 상태를 유지
- 새 결정/가정: 1~26번 변형은 전역 효과로 두고 Yacht 칸 변형 22~26번만 Phase 1에 제한한다(`D-012`). 1~21번은 중간 획득 시 대상 칸을 초기화하고 해당 플레이어에게 추가 턴을 준다. `yacht-bank`의 은행 진행, 주사위 구성 변경의 전역화, 전역 변형 중복, random-box 복수 발동·기준 58의 범위는 별도 결정
- 남은 문제/차단 요소: 중간 변형으로 두 플레이어가 동시에 초기화될 때 추가 턴 예약 순서, `yacht-bank` 공유 범위, 전역 변형 충돌·중첩 정책
- 다음 작업: `M5-T1`에서 전역/보유자 전용 상태 타입과 후보 필터 계약을 확정한 뒤 대표 증강을 검토

### 2026-08-24 — Codex (3~8번 anti 시리즈 폐기 검토)

- 작업 ID: `M1-T6` 사전 검토
- 시작 상태: 상단 고득점용 anti 시리즈 3~8번의 유지 여부가 미정
- 완료 내용: 3~8번을 `폐기 검토`로 표시하고, 삭제 또는 대체 논의 기준을 매트릭스에 기록
- 변경 파일: `docs/augment_migration_matrix.md`
- 실행한 검증: 3.4 결정 필드에서 3~8번 상태와 ID를 확인하고 기존 데이터·규칙 보존 여부 확인
- 검증 결과: 6개 항목 모두 `논의 필요`; 원본 JSON과 이전 구현은 삭제하지 않음
- 새 결정/가정: 최종 삭제 또는 대체 전까지 M5/M6 이식 대상으로 확정하지 않음
- 남은 문제/차단 요소: 대체 콘셉트의 수와 방향 미정
- 다음 작업: 삭제안과 대체안의 재미·이름·상태 복잡도·테스트 가능성 비교

### 2026-08-24 — Codex (M3 타이머 정책 확정)

- 작업 ID: `M3-T6`
- 시작 상태: 일반 모드 턴 제한이 현재 Unity 60초와 이전 웹 45초로 달라 사용자 결정 대기
- 완료 내용: 일반 모드 턴 제한을 60초로 확정하고 공용 옵션 상수, 컨트롤러 참조, 회귀 테스트와 문서를 동기화
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, 이 문서
- 실행한 검증: Unity 스크립트 재컴파일, EditMode 전체 테스트, Unity Console 확인
- 검증 결과: 스크립트 오류 0건, Unity Console 오류 0건, EditMode 전체 15/15 통과
- 새 결정/가정: 일반 모드의 턴 제한은 60초 (`D-010`)
- 남은 문제/차단 요소: 없음
- 다음 작업: 후속 요청 시 `M5-T1` 대표 증강 5종 확정

### 2026-08-24 — Codex (M1~M4 구현)

- 작업 ID: `M1-T1`~`M4-T6` (`M3-T6` 제외)
- 시작 상태: 기존 증강 테스트 기준선 미확인, 주사위 값·킵·프리셋 난수가 Unity 컨트롤러에 결합, 모드 분리 없음
- 완료 내용: 이전 프로젝트 조사와 55개 이식표, 순수 C# 상태·명령·결과·난수·로컬 권위, 일반 요트 24턴 흐름, 일반/증강 실행 옵션과 기본 규칙 합성 구조 구현
- 변경 파일: `Assets/Scripts/Games/Yacht/YachtGameCore.cs`, `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs`, `Assets/Scripts/Games/AugmentedYacht/AugmentedYachtController.cs`, `Assets/Scripts/Games/AugmentedYacht/ParchmentScoreSheet.cs`, `Assets/Editor/YachtGameRulesTests.cs`, `docs/augment_migration_matrix.md`, `docs/yacht_state_ownership.md`, 이 문서
- 실행한 검증: 이전 프로젝트 `npm test`, `npm run validate:augments`; Unity 표준 스크립트 검증; EditMode `YachtGameRulesTests`; Play Mode 일반/증강 시작 및 일반 굴림; Unity Console 확인
- 검증 결과: 이전 테스트 36개 파일 통과, 증강 55개 유효, Unity 전체 EditMode 14/14 통과, Play Mode에서 일반 굴림 결과가 권위 상태와 함께 확정되고 증강 모드가 기본 규칙으로 시작함, 새 컴파일 오류·런타임 예외 없음
- 새 결정/가정: 공유 씬 유지; 사용자의 요청에 따라 일반 모드에서도 왼쪽 증강 트레이 그래픽은 임시 유지하고 증강 논리만 비활성화
- 남은 문제/차단 요소: 후속 `M3-T6`에서 60초로 확정하여 해결됨
- 다음 작업: 후속 `M3-T6` 완료

### 2026-08-24 — Codex (증강 이식 매트릭스 초안)

- 작업 ID: `M1-T3`
- 시작 상태: 55개 증강의 표시 데이터는 존재했지만 대화형 검토를 위한 공통 필드와 행이 없었음
- 완료 내용: 55개 증강을 ID, 판정, 설명, 유형·대상·발동, 정적 파라미터, 런타임 상태, 충돌, 근거, 테스트, 상태 필드로 초안 작성
- 변경 파일: `docs/augment_migration_matrix.md`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: JSON 항목 수와 매트릭스 행 수·augmentId 집합·열 수를 비교
- 검증 결과: JSON 55개와 매트릭스 55개가 일치하며 숫자 ID 40 공백은 별도 기록함
- 새 결정/가정: augmentId를 주 식별자로 사용하고 정적 데이터·실행 규칙·런타임 상태를 분리함
- 남은 문제/차단 요소: 모든 행은 아직 검토 전이며 일부 이전 구현과 설명의 차이를 사용자 결정으로 확정해야 함
- 다음 작업: `lucky-sevens`부터 대화형 검토를 진행하고 확정 내용을 매트릭스에 반영

### 2026-08-24 — Codex (증강 그래픽 마일스톤 추가)

- 작업 ID: 문서 계획 보완
- 시작 상태: 증강 규칙 구현과 로컬 핫시트 사이에 최종 그래픽 제작 단계가 명시되지 않았음
- 완료 내용: `M7 증강 시스템 그래픽 요소 추가`를 신설하고 이후 마일스톤을 M8~M11로 재번호화
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 마일스톤 요약, 상세 작업 ID, 결정 기록, 미결정 사항의 번호 참조 확인
- 검증 결과: 카드, 특수 주사위, 발동 VFX, 점수표/진행 상태, 수동 행동 피드백, 사운드, 폴백, 성능 검증 작업이 계획에 포함됨
- 새 결정/가정: M5에서는 검증용 임시 표현만 사용하고 M7에서 최종 게임 품질의 그래픽을 완성
- 남은 문제/차단 요소: 그래픽 에셋별 제작/재사용 여부는 `M7-T1`에서 확정
- 다음 작업: 기존 진행 포인터대로 `M1-T1` 이전 프로젝트 테스트 기준선 확인

### 2026-08-24 — Codex

- 작업 ID: `M0-T1`
- 시작 상태: 일반/증강 요트 구현 순서가 대화로만 정리돼 있었음
- 완료 내용: 현재 코드 기준선, 목표 구조, 마일스톤, 상세 작업, 완료 조건, 위험, 결정, 재개 절차를 포함한 작업 계획서 작성
- 변경 파일: `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 현재 Tessera 및 이전 `augmented-dice`의 관련 파일 목록과 핵심 구조 확인
- 검증 결과: 기본 요트 규칙·프리셋·증강 데이터는 존재하며 실제 증강 런타임과 네트워크 모듈은 아직 미구현임을 확인
- 새 결정/가정: 일반 요트를 기본 규칙으로 먼저 완성하고 공유 씬에서 증강 모드를 합성
- 남은 문제/차단 요소: 이전 프로젝트 전체 테스트 기준선은 아직 실행하지 않음
- 다음 작업: `M1-T1` 이전 프로젝트에서 `npm test` 실행
