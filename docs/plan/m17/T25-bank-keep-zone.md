# 요트 뱅크 킵 존 하이라이트·반짝이 연출 계획서 (`M17-T25`)

> **문서 종류**: 작업 지시서 (AI 대상)
> **코드 대조 기준**: 2026-09-15 · 커밋 `ab8300d`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.
> 작성 원칙은 [`docs/README.md`](../../README.md)를 보십시오.
>
> `M17-T25` 25 `yacht-bank` 증강의 킵 존 하이라이트·반짝이 연출과 슬롯 0 로직 정합화 계획입니다.

작성일: 2026-09-15 · 상태는 [마일스톤 README](README.md) 태스크 표에 있다.

---

## 1. 배경 (Context)

25 `yacht-bank`는 로직만 `DONE`이고(`docs/reference/augments_specification_and_status.md:785`) 화면에는 효과가 드러나지 않는다. 보유자는 3턴 동안 어느 칸의 주사위가 저축되는지 알 수 없다.

연출 방향은 사양서가 이미 확정했다.
- [`m17_vfx_spec.md`](T09-vfx.md) §3 25번 행: "첫 번째 주사위 킵 칸이 금색으로 빛남(골드 파티클 및 네온 글로우)". 구현 사양 `P3`+`P5` / `DICE(킵 0번 칸)` / `#e5a93c` / `M` / `누적`. 대상은 가장 왼쪽 킵 칸 하나로 고정.
- 같은 문서 §3.4 핵심 설계 3: 지속형(`C`)은 이벤트 큐가 아니라 **매 상태 갱신 시 `State`를 보고 on/off를 맞추는 별도 경로**로 처리한다. 25번 칸 하이라이트가 명시적으로 이 부류다.
- 같은 문서 §3.5 신규 에셋 `I3` 킵 슬롯 하이라이트 쿼드: "킵 칸은 좌표(`DiceBoardMetrics.GetKeepPosition`)일 뿐 오브젝트가 아님. 슬롯에 무언가를 붙이려면 실체가 필요".
- 같은 문서 §7 결정 로그(2026-09-08) 두 건과 §8 미결 질문(해결): 강조 대상은 슬롯 0 고정. 로직도 `lowestSlot` 탐색 대신 `KeepSlotIndex == 0` 주사위를 읽도록 정합화 확정.
- 구 명세 [`docs/archive/augment_migration_matrix.md`](../../archive/augment_migration_matrix.md) §4.4 "킵 존 연출"은 오른쪽 은행 구역·빛 이동·`+눈금` 표기 등을 적었으나, 현행 사양서(슬롯 0 고정)가 이를 대체한다. 참고용.

---

## 2. 사용자 결정 (2026-09-15)

| 항목 | 결정 |
|---|---|
| 표시 구간 | **현재 턴이 보유자이고 저축 턴이 남아 있을 때만** 표시. 상대 턴·지급 대기(`PayoutPending`)·지급 완료(`Paid`)·드래프트 중에는 숨김. 핫시트 공유 보드에서 상대 효과로 오인하지 않게 하려는 목적 |
| 연출 범위 | **칸 하이라이트 + 반짝이만**. 사양서 25번 행 후반부 "첫 번째 칸에 주사위가 들어가 있으면 주사위가 금색으로 변하면서 서서히 투명해지는 연출"은 이번 범위에서 제외하고 후속 태스크로 분리 |
| 로직 정합화 | **이번 태스크에 포함**. `YachtBank.cs`를 `KeepSlotIndex == 0` 기준으로 수정해 하이라이트 칸과 실제 저축 주사위를 일치시킴 |
| 문서화 | 별도 계획서(이 문서) 작성 + `work_plan.md`에 `M17-T25` `TODO` 등록. 구현은 추후 세션 |

---

## 3. 현재 코드 조사 결과

### 3.1 로직 `YachtBank`

- 파일: `Assets/02.Scripts/Games/Yacht/Augments/Enhance/YachtBank.cs`, 네임스페이스 `Tessera.Games.Yacht`.
- `YachtBankState`(L7): `RemainingTurns = 3`(**기본값 3**), `Balance`, `PayoutPending`, `Paid`.
- `YachtBank : EnhanceAugment, IOnAugmentSelected, IOnTurnStarted, IScoringDiceFilter, IAfterScoreCommit`(L24). `MaxBalance = 15`.
- `OnSelected`(L34): 상태와 `context.Player.YachtBank*` 미러 필드를 3/0/false/false로 초기화.
- `GetOrSync`(L48): 증강 상태와 플레이어 미러 필드 동기화.
- `OnTurnStarted`(L61): `PayoutPending && !Paid`면 잔액 지급, `Paid = true`.
- `FilterScoringDice`(L75): `RemainingTurns > 0`이면 `FindLowestKeptIndex` 결과 1개를 점수 계산에서 제외.
- `AfterScoreCommit`(L91): 킵 주사위 중 `KeepSlotIndex` 최소(음수면 인덱스로 대체, L102)를 저축. 잔여 턴 감소, 0이면 `PayoutPending = true`.
- `FindLowestKeptIndex`(L124): 킵 && 슬롯 최소 인덱스.
- `Description`(L32): "3턴 동안 가장 왼쪽 킵 주사위를 점수에서 제외해 최대 15까지 저축하고 다음 내 턴에 받습니다."
- **사양서와 불일치**: 슬롯 0이 비고 슬롯 1만 차 있으면 현재 코드는 슬롯 1 주사위를 저축한다. 하이라이트(슬롯 0 고정)와 어긋난다.

### 3.2 상태 접근자

- `Assets/02.Scripts/Games/Yacht/Augments/Framework/YachtAugmentRuntime.cs:221-240`: `YachtAugmentPlayerState.YachtBankRemainingTurns/Balance/PayoutPending/Paid`. 전부 `States.GetOrCreate<YachtBankState>(YachtBankId)` 경유.
- **함정**: `GetOrCreate`가 없으면 새로 만들고 기본값이 `RemainingTurns = 3`이라, **요트 뱅크를 갖지 않은 플레이어도 잔여 턴 3을 읽는다.** 표시 판정은 보유 여부(`OwnedIds`)를 반드시 먼저 검사해야 한다.
- 현재 플레이어: `YachtGameState.CurrentPlayerIndex`(`Assets/02.Scripts/Games/Yacht/Rules/YachtGameCore.cs:233`), 세션 쪽 `LocalGameAuthority.cs:723`.
- 보유 목록: `state.AugmentPlayers[i].OwnedIds`(테스트 `YachtGameRulesTests.cs:643` 사용례).
- 드래프트 여부: `gameSession.IsDrafting`(`YachtTurnFlowPresenter.cs:814` 사용례).

### 3.3 킵 존 좌표

- 킵 존은 씬 오브젝트가 아니다. `Assets/01.Scenes/Augmented Dice.unity`에 Keep 이름 오브젝트 0건. 트레이 STL에 파인 홈이고 좌표는 상수로만 존재.
- `Assets/02.Scripts/Core/DiceBoardMetrics.cs`
  - L57-68: `KeepSurfaceY`, `KeepStartX`(-2.64), `KeepSpacingX`(1.32), `KeepCenterZ`(+3.18). 12시 방향 킵 홈.
  - L95-104: 소켓은 17×17 소스 단위 정사각형. 칸 중심 소스 x -44/-22/0/22/44, 깊이 z 49.5~66.5. `KeepDieScale`, `KeptDieSize`, `KeptDieHalfSize`.
  - L106-112: 카메라 피치 75도 시차 보정 `KeepParallaxZ`.
  - L147 `GetKeepPosition(int slot)`: 킵 주사위 정렬 위치(주사위 중심 높이 + 시차 보정 포함). 킵 칸 좌표의 단일 진실원.
- 화면 사본 킵 상태: `YachtDiceRoundPresenter.cs` `keptDice`(L21), `keptSlotIndices`(L23), `ClearKeepMarks`(L191), `ApplyKeep`(L207). 이번 작업은 칸 위치 고정이라 이 상태를 읽을 필요 없음.

### 3.4 연결 지점 후보

- `YachtTurnFlowPresenter.RefreshAugmentPresentation`(`Assets/02.Scripts/Games/Yacht/Presentation/Flow/YachtTurnFlowPresenter.cs:793`): 트레이 재바인딩 → `augmentTray.Refresh` → `SyncAugmentStickers`. 같은 파일 안 호출처 15곳(L235, 272, 278, 319, 359, 381, 439, 470, 502, 557, 596, 607, 639, 677, 722) + `YachtDebugPanel.cs:88`. 턴 전환·점수 확정·증강 선택·드래프트·수동 발동이 모두 지난다.
- 선례 `RefreshRollBudgetState`(L785): "굴림 수를 건드리거나 턴을 넘기는 모든 경로가 이미 여기를 지나므로 한 곳에 두면 상태가 고착될 경로가 남지 않는다". 같은 원칙을 따른다.
- 판정 순수 함수 선례 `ResolveRollBudgetState`(L774, `public static`).
- `AugmentVfxPlanner`(`AugmentVfxPlanner.cs:47`)/`AugmentVfxCue` 파이프라인은 단발 이벤트 큐라 지속형에 맞지 않음. 사용하지 않는다.

### 3.5 재사용 가능한 시각 구현

- 지연 생성: `coinTossVfx ??= new CoinTossVfx();`(`YachtTurnFlowPresenter.cs:700`).
- 주사위 카메라 레이어: `DiceSmokePuffVfx.cs` `ResolveDiceCamera`(L125), `SetLayerRecursive`(L137). 연기 쿼드 깊이 테스트 함정 주석(L18-29)도 참고.
- 루프 파티클 절차 생성: `Assets/02.Scripts/Tabletop/RollCosmicCube.cs:1305-1349` `Cosmic_Internal_Stars`. loop, maxParticles 44, Box 셰이프, 저속·소형, velocityOverLifetime orbital, colorOverLifetime, sizeOverLifetime 커브, 그림자 끔.
- 언릿 셰이더 폴백 체인: `Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")`(`Assets/02.Scripts/Dice/DicePaletteCatalog.cs:230`, `CozyCandleStand.cs:186`, `RunicSlateMatrix.cs:761`).
- 트리거형 글로우 참고: `TabletopTrinketManaCrystal.cs:344-390` `TriggerGlow`/`GlowAnimationRoutine`(emission + `Light.intensity` 페이드). 지속형이 아니라 참고만.
- 기존 `Sparkle/Glint/Highlight/Glow` 이름 클래스는 `Assets/02.Scripts`에 없음.

### 3.6 테스트

- `Assets/Editor/Tests/Games/Yacht/YachtGameRulesTests.cs:639` `M6_YachtBankSavesLeftmostKeptForThreeTurnsThenPaysNextTurn`: dice[1]이 슬롯 0(값 2), dice[0]이 슬롯 1(값 6). Choice 18(2 제외), 3회 저축 잔액 6, 다음 턴 지급 6. **슬롯 0 기준으로 바꿔도 기대값 불변.**
- `Assets/Editor/Tests/Games/Yacht/YachtEnhanceAugmentTests.cs:197` `YachtBank_HoldsLeftmostKeptDieForThreeTurnsThenReturnsIt`: 매 턴 dice[0] 슬롯 0에 6·5·5, 15점 상한 지급. **기대값 불변.**
- EditMode 기준선(T24 시점): 1204건 중 1200 통과·실패 0·스킵 4(unity-skills 패키지 소속).

### 3.7 아트·모듈 규칙

- `AGENTS.md`: 식별자는 영문, 한국어는 주석·문서만. 로직 `Tessera.Games.Yacht`는 프레젠테이션을 참조하지 않는다. 뷰는 `Tessera.Games.Yacht.Presentation` 프레젠테이션 쪽.
- 색: 러너 골드 `#e5a93c`(사양서 공통 강조색). 팔레트 원천 [`docs/reference/art_style_guide.md`](../../reference/art_style_guide.md).
- 사양서 25번 행 주의: 알파 페이드가 픽셀 필터를 통과하면 디더링으로 깨지므로 4~5스텝 계단식 처리.

---

## 4. 설계

### 4.1 표시 판정 (순수 함수)

`YachtTurnFlowPresenter`에 `ResolveRollBudgetState`와 같은 `public static`으로 둔다.

```
ShouldShowYachtBankHighlight(YachtGameState state, bool isDrafting)
  isDrafting                                        → false
  current = state.AugmentPlayers[state.CurrentPlayerIndex]
  current.OwnedIds 에 YachtAugmentRuntime.YachtBankId 없음 → false   // 기본값 3 함정 차단. 반드시 먼저
  current.YachtBankRemainingTurns <= 0              → false
  current.YachtBankPayoutPending || current.YachtBankPaid → false
  그 외                                              → true
```

인덱스 범위 밖·null 방어는 기존 호출부 수준에 맞춘다.

### 4.2 뷰 `YachtBankKeepZoneVfx` (신규)

- 경로: `Assets/02.Scripts/Games/Yacht/Presentation/AugmentViews/YachtBankKeepZoneVfx.cs`, 네임스페이스 `Tessera.Games.Yacht.Presentation`.
- `MonoBehaviour`. 공개 API는 `SetVisible(bool visible)` 하나. 첫 `SetVisible(true)` 때 자식 오브젝트를 지연 생성.
- **위치**: `DiceBoardMetrics.GetKeepPosition(0)`의 X·Z. Y는 소켓 바닥(`KeepSurfaceY - KeptDieHalfSize`)에서 z-fighting 피할 만큼만 띄움. 시차 보정 `KeepParallaxZ`는 주사위 중심 높이 기준이라 바닥 쿼드에는 그대로 맞지 않을 수 있음. 구현 시 화면 확인으로 판정.
- **크기**: 소켓 17 소스 단위(`17f * DiceBoardMetrics.TrayScale`) 기준.
- **하이라이트**: 소켓 테두리 프레임 쿼드 1장(가운데가 빈 사각 테두리. 텍스처 절차 생성 또는 얇은 쿼드 4장 중 단순한 쪽). URP Unlit 폴백 체인, 색 `#e5a93c`. 발광 펄스는 **4~5단계 계단식** 밝기 변화.
- **반짝이**: 루프 `ParticleSystem` 1개. `RollCosmicCube.cs:1305-1349` 구성 방식을 따르되 색은 골드 계열(`#e5a93c` ~ 흰색), 셰이프 Box를 소켓 크기로, `maxParticles` 16 안팎, 방출 소량. 그림자 끔.
- **레이어**: 주사위 카메라에 찍히도록 `DiceSmokePuffVfx.cs`의 `ResolveDiceCamera`/`SetLayerRecursive` 방식을 확인 후 따름.
- **끄기**: `ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` 후 루트 `SetActive(false)`. 같은 값 반복 호출은 즉시 반환.
- 신규 에셋 없음(절차 생성).

### 4.3 연결

`YachtTurnFlowPresenter`
- 필드 `private YachtBankKeepZoneVfx yachtBankKeepZoneVfx;` 추가.
- `RefreshAugmentPresentation`(L793)에 `SyncYachtBankHighlight();` 한 줄 추가.
- `SyncYachtBankHighlight()`: `gameSession == null`이면 끔. 판정 결과가 true일 때만 지연 생성, false이고 아직 없으면 생성하지 않음.
- 동전 토스 연출이 주사위를 숨겨도 하이라이트는 칸 표시라 유지.

### 4.4 로직 정합화 `YachtBank.cs`

- `FindLowestKeptIndex`(L124) → `FindSlotZeroKeptIndex`: `IsKept && KeepSlotIndex == 0`인 인덱스, 없으면 -1.
- `AfterScoreCommit`(L96-106): 같은 기준으로 저축 대상 선택. 슬롯 0이 비면 0점 저축·턴 1회 소모(기존 규칙 유지).
- `KeepSlotIndex` 음수 시 배열 인덱스로 대체하던 폴백(L102, L131)은 슬롯 0 기준에서 의미가 없으므로 제거.
- `Description`(L32)과 요약 주석(L23): "가장 왼쪽 킵 주사위" → "첫 번째 킵 칸의 주사위".
- `augments_specification_and_status.md:602`·`:785`의 "왼쪽/가장 왼쪽" 표현도 "첫 번째 킵 칸(슬롯 0)"으로 정정.

### 4.5 레퍼런스 기반 재설계 (2026-10-23)

1차 구현(§4.2, 테두리 + 반짝이)은 Play Mode에서 "앞쪽 변 한 줄, 반짝이 점 몇 개"로 약했다. 사용자가 보물상자 레퍼런스(상자 안 금화의 광원, 위로 뻗는 빛기둥, 떠오르는 반짝이)를 주고 이를 기준으로 다시 만들도록 지시했다. §4.2의 하이라이트·반짝이 항목을 아래로 대체한다. 표시 판정(§4.1)·연결(§4.3)·`SetVisible(bool, float centerX)` API는 그대로다.

| 레퍼런스 요소 | 슬롯 0 대응 | 구현 |
|---|---|---|
| 금화가 내는 따뜻한 광원 | 칸에서 새어 나와 턱 위로 번지는 금빛 웅덩이. 킵 주사위가 있으면 가장자리로만 보임 | 바닥 쿼드(Floor) 1장, 균일 `#e5a93c`, 알파 0.25, 가산 |
| 위로 뻗는 빛기둥 | 칸에서 화면 위쪽으로 솟는 빛기둥 4줄. 위로 갈수록 가늘고 흐려지고, 위쪽 끝이 바깥으로 살짝 벌어짐 | 뒤집힌 사각뿔대(Frustum) 옆면 4장, 연속 그라데이션 텍스처, 가산, 코사인 펄스 |
| 빛 속에 떠오르는 반짝이 | 빛기둥을 따라 천천히 떠오르는 금색 점 | Frustum 자식 루프 파티클, 칸 위에서 짧게 떠오름, 가산 |

변경 이력
- 빌보드 빛기둥은 모래시계 하단을 가려 폐기했다. 기둥 높이 목표(주사위 1.5~2개)도 아래 사각뿔대 경사로 대체한다.
- 사용자 요청에 따라 순서대로 진행했다. "글로우는 칸 근처에서만", "뾰족하지 않은 평평한 직사각형 면", 사각뿔대 레퍼런스 이미지와 "뒤집어서 위아래 면을 제외한 옆면에 적용"이다.

사용자 결정(2026-10-23)
- 기존 금색 테두리는 **제거**한다.
- 슬롯 0 주사위 금색 변환은 기존대로 범위 밖(§9)이다.
- 픽셀 필터가 있어도 **계단식으로 만들지 않는다.**

최종 설계
- **형태:** 뒤집힌 사각뿔대(frustum) 옆면 4장이다. 아래 테두리는 슬롯 0 칸 입구이고 한 변 `s = 17·TrayScale`이다. 옆면은 수평 대비 `FrustumSlopeDegrees = 50°`로 바깥을 향해 올라간다. 빗변은 `FrustumSlantRatio 0.4·s`다. 윗면과 아랫면은 없다.
- **텍스처:** 4×32, 필터 Bilinear, 연속 그라데이션이다. 알파는 `(1−v)^1.5`(`FrustumAlphaFalloffPower`)이고, 색은 `#fff1c4`에서 `v = 0.3`(`FrustumPaleFadeEnd`)까지 `#e5a93c`로 보간한다.
- **깊이:** 가산 블렌딩, 컬링 Off다. Frustum은 시선 반대로 `FrustumCameraPull 0.4`만큼 당겨 벽·칸막이 깊이 잘림을 피한다. 직교 카메라라 화면 위치는 변하지 않는다.
- **바닥면 Floor:** 소켓 바닥 0.005 위에 깐 쿼드다. 한 변은 `s × 0.96`, 색은 균일 `#e5a93c`, 알파 0.25다. 카메라 당김을 받지 않아 킵된 주사위가 깊이로 가린다.
- **펄스:** 코사인 곡선 0.6~1.0, 주기 2.7초(`PulsePeriodSeconds`)다. Frustum과 Floor는 같은 박자로 움직이고, 표시를 시작할 때 0.6부터 시작한다. 기존 6단 0.18초(주기 1.08초)를 사용자 요청으로 2.5배 늘리고 연속으로 바꿨다.
- **반짝이:** Frustum 자식으로, 칸 위에서 짧게 떠오른다.

제약
- 전역 후처리(블룸)는 씬 전체 룩을 바꾸므로 쓰지 않고 가산 블렌딩으로 대신한다.
- 필터의 색 양자화는 기본 Off다(`PixelFilterSettings.StartQuantizeMode = 0`). 양자화를 켜면 필터가 Bayer 디더로 띠를 만든다. 5차 확인에서 모드 1이 자연스러웠다. 「픽셀 필터 때문에 알파를 계단으로 둔다」는 근거는 틀려서 삭제한다.

시각 확인 이력
- 1차 (2026-10-22, §4.2 구현): 테두리 + 반짝이를 Play Mode에서 확인. 앞쪽 변 한 줄로 약했다.
- 2차 (2026-10-23, 레퍼런스 1차 구현): 방사형 바닥 글로우 + 카메라 빌보드 빛기둥 4줄. 기둥 높이를 칸의 2.2배로 늘렸으나 모래시계 하단을 80~100px 가려 폐기했다. 이후 사용자 요청 순서(글로우 칸 근처만 → 평평한 직사각형 면 → 사각뿔대 옆면)로 형태를 바꿨다. 스크린샷 `t25_5_*_v2.png`.
- 3차: 사각뿔대 옆면, 기울기 30°, 4단 계단 알파. 네 면 모두 보이고 잘림 없음. 높이 0.20으로 평평한 액자처럼 읽혔다. `t25_5_*_v3.png`.
- 4차: 기울기 50°, 바닥면(Floor) 추가. 퍼짐 폭이 줄어 슬롯 1과 여유가 생겼다. 상대 턴에서도 켜져 있었는데, P1도 드래프트로 yacht-bank를 받았기 때문이며 정상 동작이다(판정은 현재 플레이어 보유 기준). `t25_5_*_v4.png`.
- 5차: 연속 그라데이션 텍스처. 계단 층과 테두리 선이 사라졌고 양자화 모드 1에서도 자연스러웠다. 사용자 "훨씬 좋다". P1 미보유 판에서 상대 턴 숨김을 확인했다. `t25_5_*_v5.png`, `t25_5_kept_v5_q1.png`.
- 스크린샷 위치: `Assets/Screenshots/`.

---

## 5. 수정 파일

| 파일 | 변경 | 상태 |
|---|---|---|
| `Assets/02.Scripts/Games/Yacht/Augments/Enhance/YachtBank.cs` | 슬롯 0 기준 정합화, 설명 문구 | `DONE` |
| `Assets/02.Scripts/Games/Yacht/Presentation/AugmentViews/YachtBankKeepZoneVfx.cs` | 신규 뷰 | `DONE` |
| `Assets/02.Scripts/Games/Yacht/Presentation/Flow/YachtTurnFlowPresenter.cs` | 판정 static, `SyncYachtBankHighlight`, 필드 1개 | `DONE` |
| `Assets/Editor/Tests/Games/Yacht/YachtEnhanceAugmentTests.cs` | 슬롯 0 규칙 테스트 추가 | `DONE` |
| `Assets/Editor/Tests/Games/Yacht/YachtBankHighlightTests.cs` | 신규. 판정 함수 테스트 | `DONE` |
| `docs/plan/m17/README.md` | `M17-T25` 상태 갱신 | `TODO` 등록 완료 |
| `docs/plan/m17/T09-vfx.md` | 25번 행에 "주사위 금색 변환·투명화는 후속 분리" 표기 | `TODO` |
| `docs/reference/augments_specification_and_status.md` | 25번 규칙 문구 정정 | `TODO` |
| `docs/plan/m17/log.md` | 세션 기록 | `TODO` |

---

## 6. 세부 작업

| ID | 내용 | 완료 조건 | 상태 |
|---|---|---|---|
| `M17-T25-1` | `YachtBank.cs` 슬롯 0 정합화 + 로직 테스트 추가 | 신규·기존 요트 뱅크 테스트 통과 | `DONE` |
| `M17-T25-2` | 판정 함수 + `YachtBankHighlightTests` | §7.1 케이스 전부 통과 | `DONE` |
| `M17-T25-3` | `YachtBankKeepZoneVfx` 뷰 구현. 2026-10-23 §4.5 최종 설계로 재구현(뒤집힌 사각뿔대 옆면 4장 + 바닥 Floor, 코사인 펄스 2.7초, Frustum 자식 반짝이) | 컴파일, 켜고 끄기 반복 시 오브젝트 누수 없음 | `DOING` |
| `M17-T25-4` | `RefreshAugmentPresentation` 연결 | EditMode 전체 실패 0 | `DONE` |
| `M17-T25-5` | Play Mode 시각 확인. §4.5 재구현 후 레퍼런스 대조로 재확인 | 보유자 턴 표시·상대 턴 숨김·3턴 후 소등 확인, 레퍼런스 대조, 사용자 통과 | `TODO` |
| `M17-T25-6` | 문서 갱신 | §5 문서 항목 반영 | `DONE` |

---

## 7. 테스트 케이스

### 7.1 표시 판정 (`YachtBankHighlightTests`)

| 케이스 | 기대 |
|---|---|
| 현재 플레이어 미보유 (잔여 턴 기본값 3을 읽는 상황) | false |
| 보유, 현재 턴, 잔여 3 | true |
| 보유, 잔여 1 | true |
| 상대가 보유, 현재 턴은 미보유자 | false |
| 보유, 잔여 0, `PayoutPending` | false |
| 보유, `Paid` | false |
| 보유, 잔여 3, 드래프트 중 | false |

### 7.2 로직 (`YachtEnhanceAugmentTests`)

| 케이스 | 기대 |
|---|---|
| 슬롯 1만 킵, 슬롯 0 비어 있음 | `FilterScoringDice`가 원본 그대로 반환, 잔액 불변, 잔여 턴 1 감소 |
| 슬롯 0·1 모두 킵 | 슬롯 0 주사위만 제외·저축 |
| 기존 2건 (`YachtGameRulesTests.cs:639`, `YachtEnhanceAugmentTests.cs:197`) | 기대값 변경 없이 통과 |

---

## 8. 실행 순서 (오케스트레이션)

1. `tessera-implementer`: `M17-T25-1` → `-2` → `-3` → `-4` 순. 위 설계와 파일:줄 근거를 프롬프트에 그대로 싣는다.
2. `tessera-verifier`: 컴파일 + EditMode 전체. 판정 기준은 T24와 동일(unity-skills 패키지 소속 스킵 4건은 잡음). → 실패 0.
3. 시각 확인(`M17-T25-5`): 글로우·파티클은 수치로 판정할 수 없어 스크린샷이 정당하다. `tessera-unity-operator`가 Play Mode에서 디버그 패널(`YachtDebugPanel`)로 요트 뱅크를 부여하고 960x540으로 보유자 턴 1장, 턴을 넘긴 뒤 상대 턴 1장(숨김 확인)을 찍는다. 또는 사용자 Play Mode 확인으로 대체.
4. `tessera-scribe`: §5 문서 항목 갱신. 이후 `PYTHONHASHSEED=0 graphify update .`
5. 커밋은 마일스톤 규칙상 보류. 필요 시 사용자 허가 후 `tessera-committer`.

---

## 9. 범위 밖 · 후속

- 슬롯 0 주사위 금색 변환(`DiceVisualPool.ApplyDieType` → `DieType.Golden`, `DiceVisualPool.cs:88`) 후 4~5스텝 계단식 투명화. 사양서 25번 행 후반부. 후속 태스크로 분리.
- 저축·지급 순간 연출(빛 이동, `+눈금`, 지급 펄스). 구 명세 §4.4에만 있고 현행 사양서에 없음.
- 효과음은 `M17-T13`(`DEFERRED`) 범위.
