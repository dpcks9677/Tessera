# PIXEL 트랙 세션 로그

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

### 2026-10-23 — Claude (PIXEL-T6 공용 텍셀 밀도 규칙)

- 작업 ID: `PIXEL-T6` (`TODO` → `DONE`)
- 변경:
  - `Rendering/PixelTexelDensity.cs` 신규: `PixelSurfaceFacing`, `PixelTexelDensity`(`TexelWorldSize`, `TexelCountFor` 내림·최소 1, `CanvasUnitsPerWorldUnit = 100`). Games 참조 없음
  - 카메라 피치 단일화: `PixelFilterSettings.WorldCameraPitch = 75` 추가, `YachtSceneAssembler.CameraPitchAngle`은 이 값을 가리킴. `Dice/DiceFaceOrientation.cs:40` `DefaultCameraPitch`는 범위 밖으로 남김
  - `AugmentStickerTexture.TexelCountFor`는 새 API를 호출하고 최소 8텍셀 클램프만 유지. `ParchmentScoreSheet`·`AugmentTrayCardView`의 중복 상수 삭제
  - 임포트 규칙 검사를 전체 스캔 + 대기 예외 목록 축소 방식으로 구현(설계는 검사 목록 증가 방식). 검사 루트 `Assets/04.Art/Generated`, `Assets/04.Art/Source`, `Assets/Resources`. 영구 제외 3개, 대기 예외 20개(T7 4, T8 12, T9 4)
  - 설계 절 갱신, T7·T8·T9 설계 절에 대기 예외 제거 항목 추가
- 검증: 에셋 리프레시 후 컴파일 오류 0. EditMode 420/420, 실패·스킵 0. `PixelTexelDensityTests` 전부, `PixelTextureImportRuleTests` 3개, `AugmentStickerTextureTests` 11개 통과. 플레이 모드 확인 해당 없음(값·계산식 동일)
- 다음 작업: `PIXEL-T7` 탁자·트레이 표면(착수 전 미결 질문 7 펠트 골 주기 결정 필요). `PIXEL-T9`는 병렬 가능

### 2026-10-23 — Claude (PIXEL 텍셀 밀도 정합 설계, PIXEL-T6~T10 등록)

- 작업 ID: `PIXEL-T6`~`PIXEL-T10` 신규 등록(`TODO`). 결정 `D-050`, `D-051`
- 변경:
  - 사용자 요청: 트레이 픽셀 기준으로 폰트를 제외한 모든 요소의 PPU를 일치시키는 설계
  - 코드·에셋 조사: 텍셀 밀도가 칸당 약 0.42(판자)~14(별자리 큐브)로 흩어져 있음. Crisp UI 경로의 비폰트 그래픽은 화면 1px 단위
  - 사용자 결정 4건: Crisp UI 비폰트 그래픽 필터 이관, 엄격 1:1, 정지 자세만 보장, PIXEL 트랙 확장
  - `README.md`: 목표·범위·확정 결정 갱신, 「텍셀 밀도 정합 공통」과 `PIXEL-T6`~`PIXEL-T10` 설계 절 추가, 태스크 표·순서·완료 게이트·미결 질문 7·8·관련 결정 갱신
  - `decisions.md`: `D-050`, `D-051` 추가
  - `docs/STATUS.md`: PIXEL 남은 8개(전체 10)
- 검증: 문서 작업이므로 코드·테스트 검증 없음. 칸당 텍셀 수치는 메시를 단위 크기로 가정한 추정이며 화면에서는 미확인
- 다음 작업: `PIXEL-T6` 공용 텍셀 밀도 규칙. `PIXEL-T3`는 독립 진행 가능. 미결 질문 7은 `PIXEL-T7` 전, 8은 `PIXEL-T8` 전에 정함

---

### 2026-10-23 — Claude (PIXEL 결정 D-048·D-049 기록, PIXEL-T5 등록)

- 작업 ID: `PIXEL-T5` 신규 등록(`TODO`). `PIXEL-T2` 설계 갱신(상태 `DEFERRED` 유지)
- 변경:
  - `decisions.md`: `D-048` 배율 반올림 `floor`, `D-049` 시야 상한 없음·테이블 바깥 배경 채움 추가
  - `README.md`: 「미결 질문」 3·4번 해소 표기. 「PIXEL-T2」 설계에 두 결정 반영. 「완료 게이트」의 배율 테스트와 2560×1440 플레이 확인을 `PIXEL-T2` 재개 시 적용으로 표시. 태스크 표에 `PIXEL-T5` 추가와 순서 문장 보강. 「완료 기록」 T4 후속 문장을 `PIXEL-T5` 등록으로 바꿈
  - `docs/STATUS.md`: PIXEL 남은 3개(전체 5)
- 검증: 문서 작업이므로 코드·테스트 검증 없음. `PIXEL-T5`의 화면 가시성은 미확인
- 다음 작업: `PIXEL-T3` 정지 주사위 눈 선명도(설계안 A). `PIXEL-T5`는 `PIXEL-T4` 뒤. `PIXEL-T2` 재개 시 게이트 두 항목 적용

---

### 2026-10-23 — Claude (PIXEL-T1·PIXEL-T4 DONE 전환, 플레이 모드 확인)

- 작업 ID: `PIXEL-T1`, `PIXEL-T4` (DOING → DONE. 5단계 종료 게이트 PASS. 플레이 모드 사용자 확인 2026-10-23)
- 변경:
  - `README.md` 태스크 표: `PIXEL-T1`, `PIXEL-T4` 상태 `DONE`. 「완료 기록」 절에 두 항목 추가
  - `docs/STATUS.md`: PIXEL 남은 개수, 마지막 완료 갱신
- 검증:
  - 플레이 모드 확인(사용자, 2026-10-23). T1: 가장자리 여백과 UI·연출 위치 이상 없음. T4: 점수표 모든 행의 스티커 금테·그림자 두께가 같음
  - 5단계 판정: T1 PASS, T4 PASS
  - 0단계 READY 판정은 FAIL이었습니다(선행 `PIXEL-T1`이 DOING). 사용자 승인으로 진행했고, T1과 함께 플레이 모드를 확인해 해소했습니다
- 다음 작업: `PIXEL-T3` 정지 주사위 눈 선명도(설계안 A) 착수. `PIXEL-T2`는 DEFERRED(1920×1080 고정 작업)

---

### 2026-10-23 — Claude (PIXEL-T4 스티커 텍셀 밀도 정합 구현, DOING)

- 작업 ID: `PIXEL-T4` (DOING. 구현·EditMode 검증 완료, 플레이 모드 확인 대기. `PIXEL-T1`과 함께 받음. 판정: NOT ASSESSED(플레이 모드 미확인))
- 변경:
  - `AugmentStickerTexture.TexelCountFor(Vector2 worldSize)` 추가. 가로 = `floor(폭 / CellWorldSize)`, 세로 = `floor(깊이 × sin(CameraPitchAngle) / CellWorldSize)`, 하한 8
  - `ParchmentScoreSheet`: `StickerPixelsPerUnit`(25) 제거. `topLayerObject.lossyScale`을 곱한 월드 크기로 계산
  - `YachtSceneAssembler.CameraPitchAngle`: private → public const
  - 테스트 3개 추가: `TexelIsAtLeastOneFilterCellAlongX`, `TexelIsAtLeastOneFilterCellAlongDepth`, `TinyWorldSizeClampsToMinimumTexels`
  - `README.md` T4 설계 절 수치 정정(1.5 스케일 반영): 칸 대비 텍셀 가로 96%·세로 93%, 스티커 텍스처 59×15 → 약 56×13
- 검증:
  - 컴파일 오류 0
  - EditMode 387/387 통과, 스킵 0
  - 실수 1: 설계에서 `CameraPitchAngle`을 internal로 정했으나 asmdef가 없는 구조라 `Assembly-CSharp-Editor` 테스트에서 보이지 않았습니다. public으로 정정했습니다.
  - 실수 2: 테스트 입력 0.55가 하한 8에 걸려 실패했습니다. 0.6으로 교체했습니다.
  - 위험: 점수표 루트 스케일을 바꾸는 애니메이션은 코드에서 찾지 못했습니다. 프리팹·Animator 쪽은 미확인입니다.
- 다음 작업:
  - 플레이 모드 확인(`PIXEL-T1`과 함께). 점수표 모든 행의 금테·그림자 두께 확인
  - 범위 밖 발견(후속 후보, 사용자 결정 대기): 이름 열이 접히면 천 조각 가로 스케일만 줄고 텍스처는 펴진 크기로 고정됩니다. 그래서 세로 금테가 빠질 수 있습니다

---

### 2026-10-23 — Claude (PIXEL-T4 등록, 미결 질문 6 종료)

- 작업 ID: `PIXEL-T4` 등록(TODO, 선행 `PIXEL-T1`). 미결 질문 6 종료, 미결 질문 5는 `PIXEL-T4`로 이관
- 변경:
  - 사용자 결정(2026-10-23): 미결 질문 5는 새 태스크 `PIXEL-T4`로 분리하고 추천안으로 진행. 미결 질문 6은 해당 없음으로 닫음
  - `README.md`: 태스크 표에 `PIXEL-T4` 행 추가, 「PIXEL-T4 스티커 텍셀 밀도 정합」 설계 절 추가, 미결 질문 5·6 해소 표기
- 검증: 코드 변경 없음. 문서만 갱신
- 다음 작업: `PIXEL-T1` 플레이 모드 확인 후 `PIXEL-T4` 구현 전 점수표 루트·천 조각 스케일과 기울기 확인

---

### 2026-10-23 — Claude (PIXEL-T1 픽셀 격자 기준 단위 구현, DOING)

- 작업 ID: `PIXEL-T1` (DOING. 5단계 종료 게이트 중 플레이 모드 확인 미실시로 DONE 보류)
- 변경:
  - `PixelFilterSettings.cs`: `CellWorldSize = 1f/16f` 상수, `WorldOrthographicSize => ResolutionB.y * CellWorldSize * 0.5f`(1920×1080에서 8.4375) 추가
  - `YachtSceneAssembler.cs` `ApplyWorldCameraFraming`: 하드코딩 `8.2f`를 `PixelFilterSettings.WorldOrthographicSize`로 교체, `using Tessera.Rendering` 추가
  - `Assets/Editor/Tests/Rendering/PixelGridUnitTests.cs` 신규. 테스트 4개(`CellWorldSize` 정확값, `WorldOrthographicSize` 정확값, 세로 1칸 = `CellWorldSize`, `ApplyWorldCameraFraming` 적용)
  - `Assets/01.Scenes/Augmented Dice.unity`: Full Field World Camera(:4066)와 Crisp UI Camera(:8668)의 orthographic size 8.2 → 8.4375. 텍스트 치환 2줄로 처리
  - 함정: Unity `scene_save`로 씬을 저장하면 재직렬화 churn이 생겼습니다(내장 Mesh·Material 문서 4개 소실, 978줄 diff). 되돌린 뒤 텍스트 치환으로 처리했습니다. 씬의 카메라 값 변경은 `scene_save`를 쓰지 않습니다.
- 검증:
  - 컴파일 오류 0
  - EditMode `Tessera.Editor.Tests` 384/384 통과(`PixelGridUnitTests` 4, `PixelEdgeFilterTests` 7 포함)
  - 에디터 리로드 후 두 카메라 8.4375, isDirty false
  - 종료 게이트 DONE-CRITERIA: NOT ASSESSED 2026-10-23. 플레이 모드에서 화면 좌표 의존 UI·연출(입력 레이캐스트, 타이머, 깃펜 스냅, 증강 카드·점수판, 화면 가장자리 여백) 확인 미실시. 코드 조사상 화면 좌표 경로는 모두 카메라를 매번 읽어 따라가며, 시야 상수 가정 코드는 없음
  - 640×360 디버그 프리셋에서 1칸은 3/64 월드(의도된 동작, 기록만)
- 다음 작업: 플레이 모드 확인 후 DONE 게이트 판정. 후속 항목은 `README.md` 미결 질문 5·6 참조

---

### 2026-10-23 — Claude (PIXEL 트랙 신설)

- 작업 ID: 트랙 등록(`PIXEL-T1`~`PIXEL-T3` 신설)
- 변경:
  - 트랙 문서 `plan/tracks/pixel/README.md` 작성
  - `BuildDiceGraphicsPoC.cs:152` 펠트 판정 수정(`Mathf.Abs(normal.y)` → `normal.y`)
  - `yacht-tray.asset` 재생성(GUID 유지, 펠트 x 범위 ±51.62로 대칭화)
- 검증:
  - 컴파일 오류 0
  - 메시 재생성 수치 확인(펠트 x 범위 ±51.62)
- 다음 작업: `PIXEL-T1` 착수
