# M17 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 2026-10-22 이전 항목은 옛 `docs/agent/session_log.md`에서 옮겼습니다. 양식은 [`process.md`](../process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-10-23 — Claude (`M17-T25` yacht-bank 킵 존 레퍼런스 기반 재설계, 문서 갱신)

- 작업 ID: `M17-T25`. T25-3 상태 `DOING` 유지. T25-6 `DONE`. T25-5 `TODO`
- 변경 ①(재설계): 빌보드 빛기둥은 모래시계 하단을 가려 폐기. 뒤집힌 사각뿔대 옆면 4장(Frustum, 50°, 빗변 0.4·s) + 바닥 쿼드(Floor, 알파 0.25) 구조로 교체. 텍스처는 4×32 연속 그라데이션, 펄스는 코사인 0.6~1.0 주기 2.7초
- 변경 ②(제거): 금색 테두리 제거
- 결정 ①(레퍼런스 기반 VFX): 레퍼런스 이미지를 받아 요소별 대응표(§4.5)로 설계
- 결정 ②(계단식 금지): 픽셀 필터가 있어도 알파를 계단식으로 만들지 않음. 필터 색 양자화는 기본 Off이며 "계단으로 둔다"는 근거는 삭제
- 결정 ③(펄스 주기): 기존 6단 0.18초(주기 1.08초)를 2.5배 늘리고 연속으로 바꿈
- 문서: `T09-vfx.md` 25번 행에 "주사위 금색 변환·투명화는 후속 분리" 표기. `augments_specification_and_status.md` 603·786행의 저축 기준을 "첫 번째 킵 칸(슬롯 0)"으로 정정
- 검증: 이 세션은 문서 작업만 수행. 최종 코드 기준 EditMode 전체 재실행 미확인
- 다음 작업: ① EditMode 전체 재실행 ② 사용자 최종 통과(T25-5) ③ DONE 게이트 판정 후 T25-3 `DONE` 전이 ④ `PYTHONHASHSEED=0 graphify update .`

### 2026-10-22 — Claude Opus (`M17-T25` yacht-bank 킵 존 하이라이트·반짝이 연출 구현, 중단)

- 작업 ID: `M17-T25`. 상태는 `DOING` 유지. 사용자 지시로 중단. DONE 처리는 하지 않음
- 변경 ①(구현): `YachtBank.cs` 저축 대상을 "킵 칸 슬롯 0 주사위"로 정합화. `FindLowestKeptIndex`를 `FindSlotZeroKeptIndex`로 바꾸고 `AfterScoreCommit`의 중복 루프를 제거. 음수 슬롯 폴백을 없애고 설명 문구를 "첫 번째 킵 칸의 주사위"로 변경
- 변경 ②(구현): `YachtTurnFlowPresenter`에 `ShouldShowYachtBankHighlight(IReadOnlyYachtGameState, bool isDrafting)` public static 판정 함수를 추가하고, `RefreshAugmentPresentation` 끝에 `SyncYachtBankHighlight()`를 붙임
- 변경 ③(구현): 신규 뷰 `YachtBankKeepZoneVfx`. 금색(`#e5a93c`) 테두리 쿼드 4장, 6단계 계단식 펄스, 루프 파티클(`maxParticles` 16), 레이어 Dice
- 변경 ④(테스트): 신규 `YachtBankHighlightTests` 7건. `YachtEnhanceAugmentTests`에 `YachtBank_WithOnlySlotOneKept_DoesNotBankAndStillConsumesTurn`, `YachtBank_WithSlotZeroAndOneKept_BanksOnlySlotZeroDieRegardlessOfArrayOrder` 2건 추가
- 설계와의 차이 ①: 판정 함수 인자를 계획의 `YachtGameState` 대신 `IReadOnlyYachtGameState`로 함. 사유는 프레젠터가 받는 상태가 읽기 전용 인터페이스이고 통제 매니페스트가 이를 요구하기 때문. 은행 상태는 `FindState(YachtBankId) as YachtBankState`로 읽어 `GetOrCreate` 기본값 3 함정을 피함
- 설계와의 차이 ②: `SetVisible(bool visible, float centerX = 0f)`. 주사위 루트가 `centerSectionX`(-0.39)만큼 이동돼 있어 `GetKeepPosition`에 더해야 월드 위치가 맞음(`DiceVisualPool.EnsureDiceRoot`)
- 설계와의 차이 ③: 테두리 높이를 계획의 소켓 바닥에서 앞쪽 턱 윗면(약 1.784)으로 변경. 크기는 안쪽 17·바깥 21 소스 단위. 반짝이는 킵 주사위 윗면(2.557)에서 생성. Z는 `KeepParallaxZ` 없이 `KeepCenterZ`(3.18). 사유: 트레이 메시 실측에서 소켓 바닥 1.597, 앞쪽 턱 1.774, 뒤쪽 벽 2.257이었고, 바닥 테두리와 칸 안 파티클이 킵 주사위(16/17 단위)와 벽에 가려 보이지 않음. 계획서 §4.2가 높이를 화면 확인으로 판정하도록 열어 둔 범위 안
- 설계와의 차이 ④(버그): 첫 Play Mode에서 `Particle Velocity curves must all be in the same mode` 에러. 원인은 `velocityOverLifetime`의 축별 모드 불일치. 세 축을 TwoConstants로 통일해 해소
- 검증 ①: Unity 재컴파일 오류 0. EditMode 380/380 통과(기준선 371 + 신규 9, 실패 0·스킵 0). `M6_YachtBankSavesLeftmostKeptForThreeTurnsThenPaysNextTurn`·`YachtBank_HoldsLeftmostKeptDieForThreeTurnsThenReturnsIt` 기대값 불변 통과
- 검증 ②(한계): 380/380은 뷰 파티클·위치 수정 전 시점. 이후 수정은 `YachtBankKeepZoneVfx.cs` 값 변경뿐이며 컴파일 오류 0만 확인. 최종 코드 기준 EditMode 재실행은 미수행
- 검증 ③(Play Mode, 최종): 드래프트 중 미생성. 플레이어 0 턴 루트 (-3.030, 1.787, 3.180) active, `particleCount` 6. 킵 전·후 모두 금색 앞쪽 테두리와 반짝이 화면 확인. 플레이어 1 턴 `active=false`. 콘솔 Error 0·Warning 0, 씬 `isDirty=false`
- 시각 강도: 약함(앞쪽 변 한 줄, 반짝이 점 몇 개). 강화 여부는 사용자 판단 대기
- 중단 사유: 사용자 지시로 중단
- 다음 작업: ① DONE 게이트(DONE-CRITERIA) 판정 ② `T25-6` 문서 갱신(T09-vfx.md 25번 행 "주사위 금색 변환·투명화는 후속 분리" 표기, `augments_specification_and_status.md`:602·:785 "왼쪽/가장 왼쪽" → "첫 번째 킵 칸(슬롯 0)" 정정, 태스크 표 DONE 처리) ③ 최종 코드 기준 EditMode 재실행 ④ `PYTHONHASHSEED=0 graphify update .` ⑤ graphify 재라벨링. 미커밋 `graphify-out/.graphify_labels.json`이 있으므로 커밋 직전 마지막 단계로 다시 해야 함
- 시각 확인 방식: `tessera-unity-operator` 의 Play Mode 화면 확인. 사용자 확인은 아직 없음. 스크린샷 파일은 내부 조회용이며 문서에 경로를 남기지 않음

### 2026-09-20 — Claude (`M17-T7` 무작위 전체 게임 완주 테스트, 문서 정정, 증강 밸런스 분석)

- 작업 ID: `M17-T7`. 문서 정정 3건과 밸런스 분석·설계 백로그는 태스크 ID 없이 같은 세션에서 수행
- 변경 ①(`M17-T7`): `Assets/Editor/Tests/Yacht/YachtRandomFullGamePlaythroughTests.cs` 신규. 시드 고정 난수원으로 증강 요트 한 판을 끝까지 자동 진행하는 하네스를 만들고 테스트 2개를 둔다. 하나는 시드 0~59로 60판을 완주시켜 예외 없이 끝나는지 보는 것이고, 다른 하나는 시드 0~19로 20판을 돌려 서로 다른 증강 ID가 10종 이상 발동하고 수동 행동이 최소 한 번 발동하는지 보는 것이다. 무한 루프 방지로 2000회 반복 상한을 둔다
- 검증(`M17-T7`): EditMode 전체 369/369 통과(기존 367 + 신규 2), 실패·스킵 0, 컴파일 에러·경고 0건. 신규 2건 단독 실행 시 합산 7초, 전체 실행 시간은 기존 대비 약 +10초
- 문서 정정 ①: `docs/agent/m17_vfx_spec.md` §9.6 파일 표의 절차적 에셋 `Assets/Resources/Vfx/DiceSmokePuff.png`를 실제 사용 중인 `Assets/Resources/Vfx/DiceSmokeBurst.prefab`·`.mat`·`DiceSmokeBurstTexture.png`로 교체. 근거는 `DiceSmokePuffVfx.cs:15`의 `PrefabResourcePath = "Vfx/DiceSmokeBurst"`이며, 커밋 `2497bfc`에서 외부 에셋팩 기반 프리팹으로 확정된 것
- 문서 정정 ②: `docs/reference/augments_specification_and_status.md` 19번 `duplex-house`의 대상 칸을 `FullHouse`에서 실제 코드값인 `LargeStraight`(`DuplexHouse.cs:10`)로 정정. `mountain`·`high-dice`·`prime-collection`과 경합하는 구조가 문서에도 드러남
- 문서 정정 ③: 같은 문서 2장 5절의 핸들러 개수 "45개"·수동 행동 "5종"을 실제값 46개·6종으로 정정. 오기의 출처였던 `YachtAugmentCatalog.cs`의 주석 `// 수동 행동 5종`도 `// 수동 행동 6종`으로 고쳤다. 같은 문서 5장의 "46종 DONE" 집계는 원래 옳았고 2장 서술만 어긋나 있었다
- 신규 문서 ①: `docs/reference/augment_balance_analysis.md`. GEV(한 판 전체 기대 추가 총점)를 기준으로 삼고, 상단 칸 증강은 `ΔE[슬롯 점수] + 35 × ΔP(상단 합 ≥ 63)`으로 계산해 F~S 6등급을 정의. 정확 DP(정렬 상태 252가지, 리롤 2회 역방향 최적화)로 기본 족보 기대값과 상단 보너스 확률을 구했고 검산값은 `Choice` 23.33, `P(상단 합 ≥ 63)` = 4.36%. 족보 변형 18종 전부 측정한 결과 목표 대역(+6~+12)에 든 것은 4종뿐이고 절반이 D 이하. `gambler`(−5.72)와 `double-large-straight`(−0.26)는 얻으면 손해, `mountain`(+0.04)은 효과가 사실상 없음. 최대값은 `reverse-choice`(+16.03). 변형 이외 37종은 모델로 풀리지 않아 정성 추정으로 다루고, 발동 횟수 차이(변형 1회 대 주사위 강화 12회)가 보정되지 않는 구조적 문제와 `random-box`가 기댓값상 항상 이득이라는 문제를 지적
- 신규 문서 ②: `docs/agent/augment_design_backlog.md`. 전부 미검토 기획안이며 구현된 것은 없다. 삭제된 안티 계열 6종의 빈자리를 메울 상단 칸(`Aces`~`Sixes`) 변형 후보 52종을 정확 DP로 달성 확률을 구하고 GEV +7.0에 맞춰 점수를 이분법으로 역산했다. `Fours` 이상 칸은 가치 대부분이 상단 보너스 기여에서 나오고, `Sixes` 대상 후보는 슬롯 기대값이 0 이하여도 GEV가 +6.5 이상임을 확인. 콰르텟 계열 6종(달성률 10.4%)은 제외 권고, 하우스 계열 6종은 축소 권고, 우선 채택 12종을 칸별로 제시. 신규 증강 34종(주사위 변형 7·강화 7·퀘스트 9·수동 행동 7·하단 족보 변형 4)도 함께 기획
- 지식 그래프: `PYTHONHASHSEED=0 graphify update .` 실행. 4589 노드, 9481 엣지, 268 커뮤니티. 커뮤니티 수가 저장된 라벨 266개와 달라져 81개가 허브 이름으로 자동 개명됨. LLM 재라벨링(`graphify label`)은 API 비용이 들어 실행하지 않았고, 이름 복구는 별도 실행 필요
- 코드 감사에서 나온 지적(고치지 않음): `StepByStep`의 보너스 값 55가 `StepByStep.cs:38`, `YachtGameCore.cs:134`, `StepByStep.cs:95` 세 곳에 중복. `CoinToss`의 값도 중복 정의 있음. `StepByStep.cs:87`에 상단 칸 개수 6이 리터럴로 박혀 있음
- 보강(사용자 지적): 최초 GEV 모델은 변형 증강을 "처음부터 그 칸의 규칙이 바뀐 상태"로만 계산했고, 이미 점수가 기입된 칸을 변형해 그 칸을 초기화하고 추가 턴을 얻는 경로를 평가하지 않았다. 해당 규칙은 `Assets/Scripts/Games/AugmentedYacht/Logic/YachtAugmentRuntime.cs`의 `ResetFilledTarget`(1020-1048행)에 있고 변형 분류 전체에 적용됨(995-998행). 소비는 `Assets/Scripts/Games/Yacht/LocalGameAuthority.cs` 399-453행의 `IsExtraTurnPhase`. 추가 턴은 공짜 득점 기회가 아니라 재도전이며, 12칸을 13굴림으로 채우고 한 칸을 두 번 굴리는 구조다. 이득은 `E[변형 점수] − 지운 점수 + 35 × Δ상단보너스`로 계산되고, 스크래치(0점) 칸을 되살리는 경우 `gambler`는 빈 칸 기준 −5.72에서 +17.61로 뒤집힌다. `evens`/`odds` +1.03 → +10.26, `duplex-house` +0.93 → +8.77, `mountain` +0.04 → +7.87, `double-large-straight` −0.26 → +7.83. 즉 §5의 F·D 등급이 이 경로에서는 뒤집히며, 원 족보를 이기지 못하도록 설계한 변형일수록 이미 버린 칸에는 제 점수를 통째로 준다. `PhaseOneOnly`인 변형 4종(`reverse-choice`, `2nd-choice`, `blackjack-21`, `fibonacci-numbers`)은 라운드 1에만 얻을 수 있어 이 경로를 쓸 수 없다. 분석 결과는 `docs/reference/augment_balance_analysis.md` §8 「이미 채워진 칸을 변형할 때 — 재도전 옵션」으로 추가했고, `docs/agent/augment_design_backlog.md` §1.1·§1.8에도 "52종 가격은 빈 칸 기준" 경고를 넣었다
- 남은 일: 밸런스 문서 §7.1의 재설계 후보(`gambler`, `mountain`, `double-large-straight`, D 등급 4종)를 실제로 손댈지, 설계 백로그의 상단 변형 12종과 신규 증강 34종 중 무엇을 살릴지는 사용자 판단 대기. 측정값은 전부 단일 슬롯 가정의 상한이라 실측 GEV를 얻으려면 `M17-T7`의 하네스를 전체 게임 시뮬레이터로 확장해야 함. 라운드 6·9 시점에 대상 칸이 이미 차 있을 확률도 아직 측정하지 못했다. 플레이어의 칸 배분 정책에 의존하므로 전체 게임 시뮬레이터가 필요함
- 시각 확인 생략, 사유: 코드·문서·수치 검증만 있고 시각 결과물 없음
- 커밋하지 않았다. 사용자 허가 대기 중
- 다음 작업: 미지정

### 2026-09-20 — Claude (`M17-T10` 점수표 증강 표시)

- 작업 ID: `M17-T10`. 사용자가 확정한 범위는 "문서 확정분만"
- 변경 ①: 보너스 라벨 버그 수정. 화면 표시가 항상 `+35`로 고정돼 있었으나 `step-by-step` 증강 보유 시 실제 지급은 55점이었다. `PlayerScoreData.BonusAward`(`stepBonusGranted ? 55 : 35`)를 추가해 `RecalculateTotal()`과 `ParchmentScoreSheet` 표시가 같은 값을 쓰게 하고 `IReadOnlyPlayerScoreData`에도 노출
- 변경 ②: bounty-hunter 타깃 칸 상시 표시. `ParchmentScoreSheet`에 `SetTargetMark`/`ClearTargetMark`/`HasTargetSlot`을 추가해 타깃 족보 칸에 골드 테두리+증강 아이콘+남은 횟수를 그린다. 스티커와 달리 원래 족보 아이콘·이름은 가리지 않는다. `YachtTurnFlowPresenter.RefreshAugmentPresentation()`의 기존 동기화 지점에 `SyncBountyHunterTargetMarks()`를 붙여 새 타이밍을 만들지 않았다
- 완료 조건 수정: 기존 완료 조건의 "잠금" 문구를 삭제했다. 조사 결과 점수표 칸 잠금 기능은 코드에도 기획에도 실존하지 않았다. 사용자가 "문구 삭제 + 근거 기록"으로 결정
- 조사 시점에 이미 구현돼 있던 항목: "교체된 족보 표시"와 "보너스 기준 표시"는 `M17-T10` 착수 이전부터 동작 중이었다
- 신규 테스트: `AugmentBountyHunterTargetMarkTests.cs`(3개), `YachtGameRulesTests.PlayerScoreData_BonusAward_MatchesStepBonusGrantedAndFeedsTotal`
- 검증: EditMode `filter=Tessera.Editor.Tests` 367/367 통과(기준선 363 + 신규 4). 실패·스킵 0
- 시각 확인 생략, 사유: 점수표 UI는 Play Mode에 코드로 생성되므로 bounty-hunter 타깃 표시의 실제 렌더 결과는 EditMode 테스트로 판정 불가. 해당 상태 재현에 실제 플레이가 필요
- 다음 작업: 미지정

### 2026-09-20 — Claude (`M17-T4` 증강 수동 행동 UI, `M17-T12` 수동 증강 행동 피드백)

- 작업 ID: `M17-T4`, `M17-T12`. 계획서가 "함께"로 묶은 쌍이라 한 단위로 진행
- 변경 ①: 버튼 활성 판정을 실제 규칙으로 통일. 기존 `AugmentTrayPresenter.CanUseManualAugment`는 `TableFlip`만 검사하고 나머지 5종은 무조건 `true`를 반환해, 이미 사용한 증강도 버튼이 활성으로 보이다가 클릭해야 거부됐다. `LocalGameAuthority`의 판정부를 `TryResolveManualAction`으로 추출해 `UseAugmentAction`과 공유하고, 상태를 바꾸지 않는 조회 메서드 `CanUseAugmentAction(playerIndex, augmentId, out YachtCommandErrorCode code, out string message)`를 추가했다. `YachtGameSession`에 래퍼를 두고 트레이가 이 경로를 쓴다
- 변경 ②: 남은 횟수·사용 조건을 카드 푸터에 표시. 수동 행동 6종 중 `CoinTossState`만 있던 `IAugmentProgressText` 구현을 나머지 5종 상태(`TableFlipState`·`DiceAlchemyState`·`EquivalentExchangeState`·`GambitState`·`DoubleDownState`)에 추가. 문구: 판 뒤집기·주사위 연금술은 "사용 가능"/"사용함", 등가교환은 "남은 사용 {N}/3"/"모두 사용함", 더블 다운은 "9턴부터 사용 가능"/"사용 가능"/"사용함 · 배율 적용 중"/"사용함", 갬빗은 단계별 "사용 가능"/"사용함 · 이번 턴 주사위 4개"/"사용함 · 다음 턴 주사위 6개"/"사용함"
- 변경 ③: 발동 성공 메시지의 표시명 정정. `table-flip`·`equivalent-exchange`·`gambit`·`double-down` 4종이 원시 augmentId 슬러그를 찍던 것(`"table-flip 사용"`)을 `LocalGameAuthority`의 메시지 생성부가 `DisplayName`을 쓰도록 고쳐 `"판 뒤집기 발동"` 형태로 정정
- 고아 심볼 제거: `YachtGameSession.CanUseTableFlip`, `LocalGameAuthority.ContainsOwnedAugment`
- 범위 밖으로 남긴 것: 월드 발동 VFX·카메라 셰이크는 `M17-T9`, 증강 효과음은 `M17-T13`(`DEFERRED`)
- 검증: EditMode `filter=Tessera.Editor.Tests` 363/363 통과(직전 356 + 신규 7). 신규 테스트 파일 2개: `AugmentManualActionQueryTests.cs`(조회가 상태를 바꾸지 않음 + 사용 후 거부 2건), `AugmentManualActionProgressTextTests.cs`(5종 푸터 문구, 등가교환은 0/1/3회 포함)
- 다음 작업: 미지정

### 2026-09-20 — Claude (코인 토스 앞면 1개 효과 변경 및 스핀별 효과음)

- 작업 ID: 없음. 직전 "코인 토스 푸터 정정 및 동전 효과음" 항목의 연장선. 사용자 요청에 따른 추가 조정이며 마일스톤·태스크로 기록하지 않는다
- 변경 ①: 코인 토스 앞면 1개 효과를 "최저 주사위 6 고정"에서 **보너스 +3점 획득**으로 변경. `CoinToss.BonusScore = 3` 추가, 고아가 된 `ApplyLowestDieToSix` 삭제. 최종 효과표: 0개 -5점 / 1개 보너스 +3점 / 2개 리롤 +1 / 3개 상단 보너스 기준 min(현재,57)
- 변경 ②: 카드 푸터의 취소선(`<s>`)과 "퀘스트: " 접두를 제거. 퀘스트 증강 카드만 기존 표기를 유지하고, 비퀘스트 카드(코인 토스 포함)는 얻은 효과 문구만 그대로 출력. `AugmentCardView.BindRow`에 `showStatusHeader` 인자 추가
- 변경 ③: 코인 효과음을 시퀀스 시작 1회 재생에서 동전 3개 각각의 스핀 시작 시점에 1회씩(총 3회) 재생으로 변경. `CoinTossVfx.Play`에 `onCoinSpinStarted` 콜백 인자 추가, `YachtTurnFlowPresenter.RunCoinTossSequence`가 이 콜백에 `YachtAudioService.PlayCoinToss`를 연결. 클립은 직전 항목에서 확인한 `coin_flip.mp3` 그대로 사용
- 검증: EditMode 필터 실행(`filter=Tessera.Editor.Tests`) 356개 전부 통과, 실패 0. 기존 테스트 1개 교체, 신규 테스트 1개 추가
- 변경 파일: `Assets/Editor/Tests/Yacht/AugmentCardViewTests.cs`, `Assets/Editor/Tests/Yacht/YachtManualActionAugmentTests.cs`, `Assets/Scenes/Augmented Dice.unity`, `Assets/Scripts/Dice/CoinTossVfx.cs`, `Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/CoinToss.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtAudioService.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtTurnFlowPresenter.cs`, `docs/agent/m17_coin_mesh_plan.md`, `docs/agent/work_plan.md`, `docs/reference/augments_specification_and_status.md`, `docs/agent/m17_vfx_spec.md`, 이 문서
- 다음 작업: 미지정

### 2026-09-20 — Claude (코인 토스 푸터 정정 및 동전 효과음)

- 작업 ID: 없음. 사용자 요청에 따른 코인 토스 증강 수정이며 마일스톤·태스크로 기록하지 않는다. 같은 요청에서 `M17-T23-7`은 사용자 육안 확인으로 완료 판정
- 문제 ①: 코인 토스 카드 푸터에 퀘스트 증강용 상태 배지("퀘스트 진행 중" / "퀘스트 성공")가 떴다. 코인 토스는 `YachtAugmentKind.Enhance`이고 퀘스트가 아니다. 원인은 푸터가 켜지는 조건이 상태 객체의 `IAugmentProgressText` 구현 여부뿐이고, `AugmentCardView.StatusLabelText()`가 outcome 3값을 전부 "퀘스트 ○○"로 하드코딩한 것. 이 인터페이스 구현체 12개 중 비퀘스트는 `CoinTossState` 하나
- 문제 ②: 동전을 굴릴 때 효과음이 없었다. 사용자가 지목한 `coin_toss.mp3`는 존재하지 않고 실제 파일은 `Assets/Audio/Sfx/Coin/coin_flip.mp3`였다
- 구현 ①: `AugmentCardView.SetProgressBlock`이 `showStatusHeader` 인자를 받아 상태 라벨과 점선 줄의 GameObject를 켜고 끈다. `Bind()`가 `definition.Kind == YachtAugmentKind.Quest`를 넘긴다. 헤더가 꺼지면 행 시작 위치가 `RowGapAboveDash`만 남는다. 카드 인스턴스는 재사용되므로 양방향 모두 처리한다
- 구현 ②: `CoinTossState.DescribeProgress`가 미사용 시 줄 없는 진행 정보를 반환해 푸터 자체를 끈다(기존 "사용 가능" 문구 제거). 사용 후에는 `앞면 N개 · 효과` 한 줄만 남기고, outcome을 앞면 0개(-5점)일 때 `Failed`, 그 외 `Succeeded`로 바꿔 불이익이 성공 색으로 표시되던 것을 고쳤다
- 구현 ③: `YachtAudioService`에 `coinTossClip` 직렬화 필드와 `PlayCoinToss()`를 추가했다. 클립이 비어도 조용히 반환한다는 이 클래스의 기존 방침을 따른다. `ClipsReady` 이벤트 시그니처는 주사위 클립 전달 경로이므로 건드리지 않았다. `YachtTurnFlowPresenter.RunCoinTossSequence()`가 `coinTossVfx.Play(...)` 직전에 `GetComponent`로 서비스를 지연 확보해 재생한다. 오디오는 이 프로젝트에서 `Resources`나 `RuntimeAssetLibrary`가 아니라 인스펙터 직렬화 방식이라 그 패턴을 따랐다
- 씬 작업: `Assets/Scenes/Augmented Dice.unity`의 씬 루트 `Dice Graphics PoC`(`AugmentedYachtController`와 `YachtAudioService`가 같은 오브젝트에 있음)에서 `coinTossClip`에 `coin_flip.mp3`를 할당하고 저장했다. 기존 `rollClips` 4개와 `impactClips` 4개는 손대지 않았다
- 검증: 컴파일 에러 0. EditMode 필터 실행(`filter=Tessera.Editor.Tests`) 355개 전부 통과, 실패 0, 스킵 0. 신규 테스트 3개는 `CoinTossState_Unused_HasNoProgressLines`, `CoinTossState_Used_ReportsOutcomeByHeadCount`(앞면 0~3개 4케이스), `NonQuestCard_HidesStatusLabelButQuestCard_ShowsIt`. 기대값 352와의 차이 3은 파라미터 테스트가 4개로 세어진 것
- 남은 것: 비퀘스트 푸터의 실제 레이아웃은 육안 확인을 하지 않았다. 겹침 회귀 테스트(`QuestCard_ReservesBlockHeightWithoutOverlappingBody`)는 퀘스트 경로만 덮는다
- 시각 확인 생략, 사유: 표시 조건과 활성 상태는 테스트로 판정했고, 푸터 레이아웃 육안 확인은 사용자 요청 시에만 한다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/CoinToss.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtAudioService.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtTurnFlowPresenter.cs`, `Assets/Editor/Tests/Yacht/YachtManualActionAugmentTests.cs`, `Assets/Editor/Tests/Yacht/AugmentCardViewTests.cs`, `Assets/Scenes/Augmented Dice.unity`, `docs/agent/work_plan.md`, 이 문서
- 다음 작업: 미지정

### 2026-09-15 — Claude (`M17-T24` EditMode·Play Mode 검증 통과, `DONE`)

- 작업 ID: `M17-T24` (시작 상태 `DOING`, 현재 `DONE`)
- 완료 내용: 새 연출 사양(주사위 숨김·게임 트레이 내부 투척·2초 대기·재표시)을 반영한 코드로 EditMode 전체 재실행. 1204건 중 1200 통과·실패 0·스킵 4(unity-skills 패키지 소속). 사용자가 Play Mode 시각 확인을 통과함(2026-09-15). `M17-T24` 상태를 `DONE`으로 전이
- 변경 파일: `docs/agent/work_plan.md`(§2 진행 포인터, `M17-T24`·`M17-T13` 행), `docs/agent/m17_vfx_spec.md`(50번 행 효과음 미구현 명시), `docs/reference/augments_specification_and_status.md`(구현 현황 종합·50번 행 `DONE`), `docs/agent/session_log.md`
- 실행한 검증: EditMode 전체 실행, 사용자 Play Mode 시각 확인
- 검증 결과: EditMode 1204건 중 1200 통과·실패 0·스킵 4(unity-skills 패키지). Play Mode 시각 확인 통과
- 새 결정/가정: 효과음(동전 튕김·회전·착지)은 `M17-T24` 완료 조건에서 제외하고 `M17-T13`(증강 사운드·촉각 피드백, `DEFERRED`) 범위로 이관
- 남은 문제/차단 요소: 없음(효과음은 `M17-T13` 재개 시점으로 이관)
- 다음 작업: `M17` 남은 `TODO`(`M17-T4`~`M17-T7`, `M17-T9`, `M17-T10`, `M17-T12` 등) 중 사용자 지목 대기

### 2026-09-15 — Claude (`M17-T24` coin-toss 연출 사양 변경: 트레이 내부 배치·주사위 숨김)

- 작업 ID: `M17-T24` (기존 `DOING` 유지)
- 사용자 요청: coin-toss 연출 사양 변경. 기존 배치(트레이 오른쪽 러너 위, `centerSectionX+5.8/7.6/9.4`, Y `-0.30`, 착지 후 0.8초 유지 뒤 결과 반영, 주사위는 그대로 노출)를 다음으로 교체 — 발동 시 주사위(킵 줄 포함 전체)를 숨기고, 게임 트레이 안(`centerSectionX -1.8/0/+1.8`, Y `= RollSurfaceY(0.2)+0.08`, Z `= TrayCenterZ(-0.3)`)에 동전 3개를 던짐. 착지 후 2초 대기 → 주사위 값 반영 후 재표시 → 동전 숨김(파괴) → 점수표·리롤 바·문구 반영 → 입력 해제. 새 게임 시작으로 연출이 중단되는 경로에서도 주사위 표시를 복구
- 완료 내용: 문서 사양 갱신(`docs/agent/m17_vfx_spec.md` §3.2 50번 행). 코드 구현도 병행 반영됨 — 주사위 숨김은 기존 `YachtDiceRoundPresenter.SetVisible` 재사용, 신규 테스트 `ResolveAnchorStaysWithinTrayPlayBounds` 추가, `dotnet build` 오류 0. EditMode 검증은 아직 진행 중이며 이 항목은 검증 완료를 의미하지 않음
- 변경 파일: `docs/agent/m17_vfx_spec.md`, `docs/agent/session_log.md`
- 실행한 검증: 없음(문서 변경만)
- 검증 결과: 해당 없음
- 새 결정/가정: 없음(기존 결정 미변경, 연출 사양 값만 교체)
- 남은 문제/차단 요소: EditMode 테스트 실행 및 Play Mode 시각 확인 대기
- 다음 작업: EditMode 테스트 실행 후 Play Mode 시각 확인

### 2026-09-15 — Claude (`M17-T24` coin-toss 증강 로직·동전 스핀 연출 구현)

- 작업 ID: `M17-T24` (신규, `TODO`에서 시작, 현재 `DOING`)
- 완료 내용: HOLD였던 `coin-toss`(원본 id 50) 증강을 구현. 동전 3개 앞면 수로 분기(0개 −5점, 1개 최저 주사위 6 고정, 2개 이번 턴 리롤 +1, 3개 상단 보너스 기준 min(현재,57)). `CoinToss`/`CoinTossState`(`Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/CoinToss.cs`)가 `EnhanceAugment`·`IManualActionAugment` 구현. `YachtGameState.BonusRolls`를 신설해 보너스 굴림을 일반 굴림과 분리 기록하고 `Commit`의 `normalRollCount`를 `MaxRolls + BonusRolls − RollsRemaining`으로 재계산해 `NoTimeToWaste` 판정을 보존. `ParchmentScoreSheet`의 상단 보너스 표시 63 하드코딩을 플레이어 `upperBonusThreshold` 참조로 교체(부수적으로 `random-box`(58) 등 표시도 교정됨). 연출은 동전 3개를 해석적으로 굽는 스핀 프리셋 5종(`Tessera/Bake/Coin Spin Presets`, 체공 0.9초·높이 3.0·X축 반바퀴 4/6/8/10/12회·착지 후 0.45초 감쇠 흔들림·30fps, `Assets/Resources/Vfx/CoinSpin/coin_spin_0..4.json`)과 재생기 `Assets/Scripts/Dice/CoinTossVfx.cs`로 구현. `YachtTurnFlowPresenter.RunCoinTossSequence`가 착지 후 주사위·점수·리롤 바·카드 문구를 반영하며 연출 중 입력·턴 타이머 정지. 테스트 신규: `YachtManualActionAugmentTests`(코인 7종), `CoinSpinPresetTests`, `AugmentVfxPlannerTests`. 정의 수 45→46, 드래프트 제외 목록에서 `coin-toss` 제거
- 변경 파일: 프로덕션·테스트 코드 다수(오케스트레이터 세션 별도 커밋 예정), `docs/agent/work_plan.md`, `docs/agent/m17_vfx_spec.md`, `docs/agent/m7_graphics_spec.md`, `docs/reference/augments_specification_and_status.md`, `docs/archive/augment_migration_matrix.md`, `docs/agent/session_log.md`
- 실행한 검증: 미실행(EditMode 테스트와 Play Mode 시각 확인 대기)
- 검증 결과: 대기
- 새 결정/가정: (1) 동전 스핀 연출은 물리 시뮬레이션 대신 해석적 궤적 베이크 방식 채택(재현성·성능). (2) 보너스 굴림은 `RollsRemaining` 소모와 별도로 `BonusRolls`에 기록해 기존 리롤 카운트 기반 판정(`NoTimeToWaste` 등)을 보존. (3) 점수표 상단 보너스 기준 표시를 하드코딩 63에서 플레이어 실제 임계값 참조로 교정. (4) 코인 프리셋 선택 난수를 판정(코인 결과) 난수와 분리해 시드 기반 결정성 테스트를 보호
- 남은 문제/차단 요소: EditMode 테스트 미실행, Play Mode 시각 확인(동전 배치 Y/Z 임시값 포함) 대기
- 다음 작업: `M17-T24` EditMode 테스트 실행 후 Play Mode 시각 확인

### 2026-09-15 — Claude (`M17-T23-7` 2차 재확인 통과, `M17-T23-8` 현행 유지, `M17-T23` 완료)

- 작업 ID: `M17-T23-7`·`M17-T23-8` (시작 상태 각각 `TODO`/재확인 대기·`미착수`, 현재 `DONE`). `M17-T23` 전체 시작 상태 `DOING`, 현재 `DONE`
- 완료 내용: `tessera-unity-operator`가 `Assets/Scenes/Augmented Dice.unity`에 확인용 코인 인스턴스 2개(`Coin_CheckHead` `(4.4, 2.814, -0.3)` 무회전, `Coin_CheckTail` `(5.6, 2.814, -0.3)` 뒤집음, 둘 다 스케일 0.8·레이어 Decoration)를 배치해 사용자 2차 시각 확인 진행. 씬에 주사위가 런타임 스폰이라 배치 위치는 `DiceBoardMetrics` 상수 역산으로 산출. 사용자 판정: 통과. 코멘트: 리라(뒷면) 실루엣은 잘 안 보이지만 그대로 사용. 확인 후 인스턴스 제거·씬 저장. 이어서 `M17-T23-8` 가독성 후속 판단을 사용자에게 확인한 결과 현행 유지 결정(`MinStrokeTexels`·`CoverageThreshold` 조정, 크기 확대, CrispUI 승격 모두 보류). 코드·에셋 변경 없음(문서만)
- 변경 파일: `docs/agent/m17_coin_mesh_plan.md`, `docs/agent/work_plan.md`, `docs/agent/session_log.md`
- 실행한 검증: 없음(코드 변경 없음). 시각 확인은 사용자 직접 수행
- 검증 결과: `Coin_CheckHead`/`Coin_CheckTail` 시각 확인 통과
- 새 결정/가정: `M17-T23-8` 가독성 후속 조정(획 굵기·`CoverageThreshold`·크기 확대·CrispUI 256 승격) 전부 보류, 현행 유지. `CrispPathSize = 256` 경로는 미사용으로 남음
- 남은 문제/차단 요소: `scene_screenshot`이 Game View 미렌더 상태에서 파일을 쓰지 않는 문제가 재발해 `camera_screenshot`(오프스크린 카메라)으로 우회함(도구 자체는 미해결). §8의 `coin.glb`·문양 소스 라이선스는 여전히 미확인
- 다음 작업: `M17` 남은 `TODO`(`M17-T4`~`M17-T7`, `M17-T9`, `M17-T10`, `M17-T12` 등) 중 사용자 지목 대기

### 2026-09-14 — Claude (`M17-T23` 코인 메시 반입 계획 수립)

- 작업 ID: `M17-T23` (신규, `TODO`. 하위 `M17-T23-1`만 `DONE`)
- 사용자 요청: `coin-toss` 증강(HOLD) 구현 전에 동전 3D 모델을 프로젝트에 반입하고 앞뒤(흉상/리라) 문양을 구분
- 승인된 계획: 사용자 제공 문양(`head.png`, `tail.png`) 실측, 소스 메시(`coin.glb`) 실측, Unity 에디터 전용 최소 glb 파서로 반입, prim 0을 `faceNormal.y` 부호로 Head/Tail 분할, 캡 UV를 XZ 재투영, 서브메시가 아닌 자식 GameObject 3개(Body/Face_Head/Face_Tail) 구조, 선화→면 앨비도 변환 파이프라인(잉크 마스크→크롭→굵기 보정→축소→색 입히기)
- 완료 내용: 계획서 [`docs/agent/m17_coin_mesh_plan.md`](T23-coin-mesh.md) 작성. 구현은 아직 없다. `docs/agent/work_plan.md` M17 표에 `M17-T23` 행 추가, `docs/reference/art_style_guide.md` §6 출처표에 `coin.glb`·`coin_head_source.png`·`coin_tail_source.png` 반입 예정 행 3개 추가
- 변경 파일: `docs/agent/m17_coin_mesh_plan.md`(신규), `docs/agent/work_plan.md`, `docs/reference/art_style_guide.md`, `docs/agent/session_log.md`
- 다음 작업: `M17-T23-2`(`CoinGlbReader` + 테스트)

**같은 날 후속(구현 진행)**: `M17-T23-2`~`M17-T23-6` 완료. `CoinGlbReaderTests` 5/5, `CoinFaceTextureConverterTests` 8/8, `CoinMeshBakerTests` 8/8 통과. 베이킹 산출물 9개 존재, 삼각형 Head 428/Tail 414/Edge 846, 텍스처 64×64 Point·밉맵 off·무압축·Clamp, 프리팹 `localScale` 0.6039·레이어 11·콜라이더 없음 확인. 구현 중 수정 2건: (a) 변환기 풀링 영역을 소스 전체가 아니라 크롭 bbox로 제한(가장자리 클램프로 bbox 경계 잉크가 격자 모서리까지 번지던 버그 수정), (b) 캡 UV 중심 테스트를 "축에 가장 가까운 정점" 대신 "캡 정점 평균 UV"로 교체(데시메이트 원반이라 캡 안쪽 정점 없음). 텍스처 잉크 비율 head 26.3% / tail 34.1%. 64×64 결과 관찰: head(흉상)는 구멍 있는 실루엣으로 남고, tail(리라)은 굵기 보정으로 거의 꽉 찬 원반이 되어 리라 형태가 소실됨. 가독성 판단은 `M17-T23-7`·`M17-T23-8`에서. `M17-T23-7`·`M17-T23-8`은 `TODO` 유지(사용자 시각 확인 대기)

**같은 날 후속(`M17-T23-7` 1차 시각 확인 피드백 반영)**: 사용자 1차 확인 피드백 3건 — (1) 굵기 보정 2텍셀 → 1텍셀(양면), (2) 프리팹 루트 `localScale` 0.6039 → 0.8(지름 약 1.59, 주사위 한 변 1.014의 약 1.57배), (3) 동전 내부원이 너무 평평함 → 필드(테두리 안쪽 원판)와 문양 획 둘 다에 음각 느낌 필요. 반영: 메시는 캡 평판 높이 `|y|` 0.0696 → 0.03으로 파고, 법선을 높이축 확대에 맞춰 해석적으로 보정. 텍스처는 3색 음각 음영(바탕 `#E5A93C`, 홈 안쪽 `#956E27`, 홈 그림자 `#503B15`)으로 전환, 광원 방향 `(+1, -1)`은 씬 키라이트·카메라 세팅에서 도출. `CoinFaceTextureConverter.Convert`에 `lightFrom` 인자 추가, `DefaultMinStrokeTexels` 1, 신규 `InkShadowColor`. 베이커 상수는 `ModelScale = 0.8f`·`FaceLightFrom`·`SourcePlateauHeight`/`RimLipHeight`/`FieldPlateauHeight`로 재구성. 검증: `CoinGlbReaderTests` 5/5, `CoinFaceTextureConverterTests` 11/11(신규 6종), `CoinMeshBakerTests` 11/11 통과. 재굽기 후 픽셀 분포 head 바탕 3170/안쪽 752/그림자 174, tail 2863/1057/176. 관찰: head는 머리칼·얼굴·목·받침대 구분됨, tail은 리라 U자 몸통·줄·월계관 테두리가 드러남(이전 원반화 개선). 재굽기 중 메시 이름 미설정 경고 3건(`Main Object Name '' does not match filename`) 발견, `SaveMesh`에서 `mesh.name` 지정 후 재굽기로 소멸 확인. 커밋 직전 EditMode 전체 재실행은 하지 않음(피드백 반영 전 전체 실행에서 Tessera 테스트는 전부 통과, 실패 8건은 `UnitySkills.Tests.Core` 소속). 상태: `M17-T23-7`은 `TODO` 유지(재확인 대기), `M17-T23` 전체는 `DOING` 유지
- 변경 파일: `docs/agent/m17_coin_mesh_plan.md`, `docs/agent/work_plan.md`, `docs/agent/session_log.md` 및 관련 프로덕션·테스트 코드(오케스트레이터 세션 별도 커밋 예정)
- 다음 작업: `M17-T23-7` 재확인(1텍셀·스케일 0.8·음각 반영 결과 시각 확인)

### 2026-09-14 — Claude (`M17-T3` 퀘스트 진행 표시 TMP 전환 및 화면 확인 완료)

- 작업 ID: `M17-T3` (시작 상태 `DOING`, 현재 `DONE`)
- 완료 내용: 사용자 화면 확인에서 지적된 문제를 반영해 퀘스트 진행 줄 표시 방식을 재작업. 진행 줄만 `TextMeshProUGUI`로 전환하고 취소선은 리치 텍스트 `<s>` 태그로 처리. Mulmaru.ttf로 런타임 동적 TMP 폰트 에셋을 생성했고, 픽셀 폰트에 취소선 메트릭이 없어 한글 '가' 글리프 세로 중앙 기준으로 `strikethroughOffset`을 보정. "퀘스트" 밑줄은 역할이 없어 제거(사용자 결정). 진행 블록은 고정 3줄 예약을 버리고 실측 높이의 가변 블록으로 카드 하단(푸터)에 붙였고, 콘텐츠 세이프 영역 아래 여백을 `FooterBleed`(10px)만큼 사용
- 환경 조치: TMP Essential Resources(`Assets/TextMesh Pro/`)를 REST에 `ImportPackage` 엔드포인트가 없어 `.unitypackage`를 tar로 직접 풀어 GUID를 보존한 채 수동 배치
- 검증: `AugmentCardViewTests` 56/56, `AugmentProgressTextTests` 14/14(신규), `YachtQuestAugmentTests` 14/14 통과(2026-09-14). EditMode 전체 재실행은 이번엔 하지 않음(267/267은 2026-09-13 기록)
- 화면 확인: 사용자가 직접 완료(2026-09-14)
- 변경 파일: `docs/agent/work_plan.md`, `docs/agent/session_log.md` 및 관련 프로덕션·테스트 코드(오케스트레이터 세션 별도 커밋 예정)
- 다음 작업: `M17` 남은 `TODO` 중 다음 태스크 선정 대기

### 2026-09-13 — Claude (`M17-T2` 플레이어별 드래프트 UI)

- 작업 ID: `M17-T2` (시작 상태 `DOING`, 현재 `DONE`)
- 조사 결과: 게이팅·차례 표시가 이미 구현돼 있어 프로덕션 코드 변경 없이 회귀 테스트만 추가했다
- 추가한 테스트: `OnlyCurrentDraftPlayerCanSelectAugment`. 차례가 아닌 플레이어의 `TrySelectAugment` 호출이 `NotDrafting`으로 거부되고 `Draft.PlayerIndex`와 양쪽 `OwnedIds`가 불변이며, 같은 상태에서 올바른 플레이어는 성공하는 대조를 포함
- 검증: EditMode 242/242 통과
- 부수 작업: `R-006` 대응책을 정보 은닉 수용으로 교체. `M17-T1` `DROPPED`. `M17-T9-1`·`M17-T18`·`M17-T22` 화면 확인 완료 처리. §6 요약표 `M17` 상태 `DOING` 정정
- 환경 조치: EditMode 테스트가 미저장 씬 변경 때문에 Unity에서 거부되어, 사용자 승인 후 `Assets/Scenes/Augmented Dice.unity`를 내용 변경 없이 저장한 뒤 실행했다
- 변경 파일: `Assets/Editor/YachtDraftOrderTests.cs`, `docs/agent/work_plan.md`, `docs/agent/session_log.md`, `Assets/Scenes/Augmented Dice.unity`
- 다음 작업: 미지정

### 2026-09-13 — Claude (`M17-T9-1`·`M17-T18`·`M17-T22` 화면 확인 완료)

- 작업 ID: `M17-T9-1-5`·`M17-T18-5`·`M17-T22` (시작 상태 각각 `DOING`, 현재 `DONE`)
- 시작 상태: 세 태스크 모두 코드·에셋 작업은 끝났고 Play 모드 화면 확인만 열려 있었다
- 완료 내용: 사용자가 세 화면 확인을 직접 수행. 코드 변경 없이 계획서 상태만 `DOING` → `DONE`으로 갱신
- 변경 파일: 없음 (`docs/agent/work_plan.md`, `docs/agent/session_log.md` 상태 갱신만)
- 다음 작업: 미지정. M17 미착수 태스크(`M17-T1`~`M17-T7`, `M17-T9`, `M17-T10`~`M17-T12`) 중 사용자 지목 대기
- 추가 교정: §6 마일스톤 요약표의 `M17` 행 상태를 `TODO` → `DOING`으로 정정. `M17-T8`·`M17-T16`·`M17-T17`·`M17-T9-1`·`M17-T18`·`M17-T22`가 이미 `DONE`이라 미착수가 아니었음
- 추가 교정: 사용자 지시로 `M17-T1`을 `DROPPED` 처리하고 `M17-T2`를 `DOING`으로 전환

### 2026-09-12 — Claude (`M17-T22` 깃펜 외부 모델 교체, `M17-T19` 폐기)

- 작업 ID: `M17-T22` (신규, 현재 `DOING`) · `M17-T19`를 `DROPPED`로 전환
- 시작 상태: 깃펜의 닙·칼라 5개·깃대·깃판을 `InkwellAndQuill`이 전부 절차적으로 만들고 있었다. 깃털 실루엣과 깃가지 틈을 코드 상수로 맞추는 왕복이 계속됐는데, 화면이 480x270 가상 격자로 필터링되는 탓에 그렇게 맞춘 디테일이 대부분 화면에 남지 않았다. 사용자가 외부 로우폴리 모델을 제공하며 방향 전환을 지시했고, 외형에 대한 시각 확인은 사용자가 직접 하기로 정했다
- 완료 내용: 절차적 깃펜을 `Assets/Art/Reference/quill_pen_low.fbx`(메시 셋 합계 285 버텍스, 단일 머티리얼) 인스턴스 하나로 교체. `InkwellAndQuill`은 잉크통 원통 5개만 만들고, 깃펜은 직렬화된 모델·머티리얼 참조를 `Quill Pen Root` 아래에 붙인다. 텍스처는 원본 2048² 6장(11.2 MB) 대신 128² 한 장(24 KB)만 쓴다. 알파는 실루엣 전용으로 이진화하고, 깃가지 질감은 `Mixed_AO`·`Roughness`의 결을 알베도에 섞어 색으로 낸다. FBX 임포터의 `Use File Scale`은 꺼야 한다(켜면 0.01이 적용돼 깃펜이 점 하나로 줄어든다)
- 확인한 사실 1 — **닙 정렬**: Unity는 FBX를 읽으면서 메시마다 원점을 자기 바운드 중심으로 옮기고 그 이동량을 노드 Transform에 넣는다. FBX 파일의 원본 정점 좌표와 Unity 노드 위치를 섞어 계산하면 값이 어긋나며, 실제로 두 번 틀렸다(월드에서 각각 2.78, 1.41 어긋남). 올바른 기준은 각 노드의 `localPosition`과 `MeshRenderer.localBounds`이고, 가장 아래 정점은 닙 장식이 아니라 **깃대 메시**의 `(0.117, -5.817, -0.230)`이다. 깃대가 펜촉부터 깃털 끝까지 관통해 위아래 끝이 모두 그 메시에 있다. 배율 `0.3412`(= 3.97 / 11.634)와 오프셋 `(-0.040, 1.985, 0.079)`로 확정했다
- 확인한 사실 2 — **씬이 프리팹 인스턴스가 아니다**: 씬의 깃펜은 프리팹 인스턴스가 아니라 씬이 직접 소유하는 오브젝트라, 프리팹만 고치면 씬에 전달되지 않는다. 게다가 Play 모드 중 씬에 가한 변경은 Play를 끄면 사라진다. 그래서 `EnsureGeometry`가 `RealignQuillModel`로 배치와 머티리얼을 코드 상수에서 다시 강제하도록 만들어 씬 저장 여부와 무관하게 같은 자세가 나오게 했다
- 확인한 사실 3 — **픽셀 격자**: 화면의 깃펜은 `DicePixelUpscale.shader`가 1920x1080 렌더에서 480x270 격자의 셀 중심 한 점만 뽑은 결과다. 안티앨리어싱이 없어 알파 경계가 프레임마다 켜졌다 꺼졌다 한다. 텍스처 필터로는 고칠 수 없어 알파를 이진화하고(중간 알파 46.5% → 0%) 깃펜의 화면 위치를 같은 격자에 스냅했다. 같은 이유로 `CrispUiDepthMask`도 풀해상도로 깊이를 쓰면 실루엣이 한 셀만큼 어긋나므로, 알파를 읽을 자리를 같은 격자의 셀 중심으로 옮기게 했다
- 변경 파일: `Assets/Scripts/Tabletop/InkwellAndQuill.cs`(834줄 → 277줄), `Assets/Prefabs/Tabletop/3D Inkwell and Quill Decoration.prefab`(1,293줄 → 682줄), `Assets/Art/Reference/quill_pen_low.fbx`·`quill_pen_albedo.png`·`QuillPen.mat`(신규), `Assets/Docs/README.md`, `docs/quill_feather_appearance_plan.md`(폐기 표시)
- 삭제한 것: `Assets/Editor/QuillFeatherAppearanceTests.cs`(테스트 9개), `Assets/Editor/QuillFeatherAssetRefresh.cs`, 구운 깃펜 에셋 8종(메시 3, 텍스처 2, 머티리얼 3) 약 720 KB. `QuillWritingPoseTests`와 `QuillCrispUiMask`는 절차적 기하에 의존하지 않아 그대로 둠
- 실행한 검증: 컴파일 통과, 콘솔 에러 0건. EditMode 241/241 통과. 프리팹은 `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`으로 제자리 수정해 `.prefab.meta`가 변경 목록에 없음을 확인(GUID 보존). 닙 정렬은 씬의 실제 Transform 값으로 검산해 닙 끝이 `Quill Pen Root` 원점에서 0.0012 유닛 안에 들어옴을 확인했다. 월드 전장은 9.92로, `QuillHoverAnimator`가 전제하는 9.93과 같다
- 남은 문제/차단 요소: 화면 확인 미실시. 깃펜 자세·픽셀 안정성·족보 가림 범위·깃털 표현 네 가지를 함께 본다
- 다음 작업: 화면 확인 세 건(`M17-T22`·`M17-T18-5`·`M17-T9-1-5`)을 한 세션에서 처리

### 2026-09-09 — Claude (`M17-T9-1` 56 `dice-alchemy` 연기 가림 연출)

- 작업 ID: `M17-T9-1` (시작 상태 `TODO`, 현재 `DOING`)
- 시작 상태: 증강 56 `dice-alchemy`는 연출이 하나도 없어, 굴림 없이 값만 바뀌면 주사위 눈이 예고 없이 툭 바뀌고 점수표 후보 점수가 먼저 결과를 알렸다. 계획은 `docs/agent/m17_vfx_spec.md` §9에 이미 작성돼 있었다
- 완료 내용: 연기 파티클이 킵하지 않은 주사위를 가리는 0.25초 동안 주사위 눈 표시와 점수표 후보 점수 갱신을 보류하고, 가려진 순간 스냅 교체한 뒤 연기가 걷히도록 구현. 로직은 그대로 두고 표시만 미룸. 하위 작업 `M17-T9-1-1`(연기 스프라이트)·`M17-T9-1-2`(표시 갱신 지연 큐 `I5`)·`M17-T9-1-3`(`AugmentVfxPlanner` 확장)·`M17-T9-1-4`(연기 컴포넌트)까지 완료. `M17-T9-1-5`(Play 모드 화면 확인)는 남음
- 확인한 사실: 계획서 §9.5는 `AugmentedYachtController`가 연기 컴포넌트를 만들어 주입한다고 적었으나, 실제로는 `YachtDiceRoundPresenter`가 직접 지연 생성하도록 구현했다. `YachtTurnFlowPresenter.BindProps`가 이미 인자 11개이고 연기는 주사위 비주얼 소관이라 컨트롤러를 경유할 이유가 없었다. `AugmentedYachtController.cs`는 이번 작업으로 변경되지 않았다
- 변경 파일: `Assets/Scripts/Dice/BakedDiceController.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceSmokePuffVfx.cs`(신규), `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtDiceRoundPresenter.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentVfxPlanner.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtTurnFlowPresenter.cs`, `Assets/Editor/DiceSmokeSpriteBaker.cs`(신규), `Assets/Resources/Vfx/DiceSmokePuff.png`(신규 에셋), `Assets/Editor/AugmentVfxPlannerTests.cs`
- 실행한 검증: Unity 컴파일 통과, 신규 경고 없음. `AugmentVfxPlannerTests` 격리 실행 13/13 통과(기존 10 + 신규 3). 전체 EditMode 887개 중 Tessera 실패 3건(`FontFallbackTests` 2건, `YachtGameRulesTests.LuckySevens_MidGameAcquisitionResetsOnlyOwnerAcesAndGrantsExtraTurn` 1건) — 셋 다 이번 변경과 무관한 선행 실패로 손대지 않음. `DiceSmokePuff.png` 픽셀 검사에서 유니크 알파 `{0,64,128,192,255}` 5종만 확인, 안티에일리어싱 중간값 0건, 알파>0 픽셀 RGB 전부 (255,255,255)
- 남은 문제/차단 요소: `M17-T9-1-5` Play 모드 화면 확인 미실시. `M17-T18-5`·`M17-T19-6` 화면 확인도 아직 열려 있어 세 화면 확인이 동시에 대기 중. §2 「한 번에 하나만 `DOING`」 규칙과 달리 `M17-T18`·`M17-T19`·`M17-T9-1` 셋 다 `DOING`으로 남아 있다. 임의로 상태를 바꾸지 않고 그대로 둠
- 다음 작업: `M17-T9-1-5` Play 모드 화면 확인. 이후 `M17-T19-6`·`M17-T18-5`와 합쳐 진행

### 2026-09-09 — Claude (`M17-T19` 깃펜 깃털 흰색 전환과 갈라짐 연출)

- 작업 ID: `M17-T19` (시작 상태 `DOING`)
- 시작 상태: `M17-T18` 호버 필기 연출로 깃펜이 화면 중앙에 자주 올라오면서, 깃털이 웜 브라운 3단 그라데이션이라 흰 깃펜으로 안 읽히고 얕은 실루엣 노치뿐이라 깃가지가 갈라진 느낌이 없다는 두 문제가 드러남
- 사용자 결정: 흰색 바탕은 오프화이트 아이보리 + 웜 그레이 결로, 갈라짐은 알베도 알파 클립 슬릿 + 메시 노치 강화를 병행하는 것으로 확정
- 한 일:
  - 픽셀 예산 재계산: 480×270 필터에서 깃면이 화면 7~15 px뿐임을 확인하고, 이를 근거로 `BarbSlant = 1/6`·`SlitCycles = 12`(각도 33.8도, 최악 구멍 1.5~2.2 px)를 채택
  - 공유 상수 격자(`BarbCount`·`SlitCycles`·`BarbSlant`·`SlitDuty` 등)와 아이보리 4색 팔레트로 교체
  - 알베도 알파 슬릿 함수와 메시 노치(`MaxNotchDepth 0.40`, `NotchSharpness 1.8`) 강화 구현
  - 머티리얼에 알파 클립(`_Cutoff`·`_ALPHATEST_ON`·`_AlphaClip`·큐 2450) 적용
  - 베이커에 밉 커버리지 보존 2줄과 `isNormalMap` 가드 추가, 헬퍼 `internal` 승격, 전용 갱신 도구 `QuillFeatherAssetRefresh` 신설(전체 재베이크 대신 4개 경로만 같은 GUID로 덮어써 프리팹 GUID 재발급을 피함)
  - `QuillCrispUiMask` 깊이 마스크와 알파 슬릿의 불일치를 수용하고 탈출구를 사전 명세
  - 상세 계획서 [`docs/archive/plans/quill_feather_appearance_plan.md`](../../archive/plans/quill_feather_appearance_plan.md) 신규 작성
- 실행한 검증: `M17-T19-1`~`M17-T19-5` 범위의 EditMode 테스트 9개(`QuillFeatherAppearanceTests`)와 컴파일 확인. `M17-T19-6` 에셋 갱신·화면 확인은 미실시
- 결정 기록: `D-042`
- 남은 문제/차단 요소: `M17-T19-6`(에셋 4종 갱신과 화면 확인) 미실시. `M17-T18-5`(호버 필기 화면 확인)도 아직 열려 있어 두 화면 확인을 한 세션에 합쳐 진행하기로 함
- 다음 작업: `M17-T19-6`을 `M17-T18-5`와 합쳐 진행. 이후 `M17-T9` 증강 발동 VFX 복귀

### 2026-09-09 — Claude (`M17-T18` 깃펜 호버 필기 연출 계획 수립)

- 작업 ID: `M17-T18` (신규, `TODO`)
- 시작 상태: 깃펜 재모델링(`7cebe26`)으로 잉크통·깃펜 소품은 끝났으나 정지 장식이었다. 점수 칸을 고를 때의 피드백은 `Button`의 색 하이라이트뿐이었다
- 사용자 요청: 점수 칸에 호버하면 깃펜이 잉크통에서 나와 그 칸으로 따라붙고, 호버가 풀리면 잉크통으로 돌아가는 필기 연출
- 완료 내용: 계획서 [`docs/agent/m17_quill_hover_animation.md`](T18-quill-hover.md) 작성. 구현은 아직 없다
- 확인한 사실:
  - `Quill Pen Root`의 원점이 곧 닙 끝이라 목표 좌표를 그대로 넣으면 닙이 그 지점에 닿는다. 오프셋 계산이 필요 없다
  - 점수 칸의 기입 가능 조건은 이미 `button.interactable`에 정리돼 있어 깃펜 반응 조건으로 그대로 쓸 수 있다
  - 점수표 오버레이는 `CrispUI` 전용 카메라가 월드 화면 위에 합성하므로 깊이 비교가 없다. 깃펜은 언제나 오버레이 뒤에 그려진다. 칸 인셋 알파는 22 %라 비쳐 보이지만 호버 하이라이트 알파가 70 %라 가리키는 칸에서 닙이 묻힌다
  - 깃털 날이 3.22 단위인데 시트 세로가 8.80 단위라, 자세를 잘못 잡으면 깃털 하나가 여러 행을 덮는다. 깃털 방향(yaw)을 항상 `칸 → 잉크통`으로 두면 시트 바깥으로 빠진다
- 변경 파일: `docs/quill_hover_writing_animation_plan.md`(신규), `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 없음(문서 작업)
- 다음 작업: `M17-T18-1` 점수표 호버 이벤트 노출. 또는 진행 중인 `M17-T9` 복귀

### 2026-09-08 — Claude (`M17-T17` 드래프트 제시 구성과 선공 순서)

- 작업 ID: `M17-T17` (신규, `DONE`)
- 시작 상태: `M17-T9` 증강 발동 VFX 사양을 기입하던 중 사용자가 드래프트 규칙 두 건을 지시. 제시 구성은 `CreateDraftOptions`가 후보를 섞어 앞에서 3개를 자르기만 했고, 선공은 `FindNextDraftPlayer`가 항상 인덱스 0부터 훑어 `P1`이 언제나 먼저 골랐다
- 사용자 결정: 같은 대상 족보를 교체하는 변형 증강이 한 제시에 겹치지 않게 하고, 1번 증강은 무작위·2·3번 증강은 총점이 낮은 쪽이 먼저 고르게 한다. 이 변경은 신규 태스크로 분리한다 (`D-039`)
- 완료 내용:
  - `YachtDraftState`에 `FirstPlayerIndex` 추가. 드래프트 라운드가 시작될 때(`Draft.IsActive`가 꺼져 있을 때) 한 번만 정하고 라운드 내내 고정
  - `DetermineFirstDraftPlayer` 추가. 라운드 1은 무작위, 이후는 `PlayerScoreData.totalScore`가 낮은 쪽. 동점이면 무작위
  - `FindNextDraftPlayer`가 0부터가 아니라 선공부터 순환하도록 변경
  - `CreateDraftOptions`가 이미 뽑힌 변형 증강의 `Target`과 겹치는 후보를 건너뛰도록 변경
  - `Assets/Editor/YachtDraftOrderTests.cs` 신규. 제시 중복·선공 결정·차례 이동을 EditMode 테스트 5개로 고정
- 확인한 사실:
  - 보유자 기준 중복 차단은 이미 있었다. `CanAcquire`가 보유한 변형 증강의 `Target`과 같은 후보를 거른다. 빠져 있던 것은 **한 번의 제시 안에서의** 중복이었다
  - 변형 증강에는 쓸 만한 트리거 점이 없다. 현재 `AugmentTriggered`를 발행하지 않지만(`ModificationAugment.ModifyScores`에 `Emit`이 없고 `AugmentScoreContext`가 이벤트 수집기를 `null`로 받음) 이는 고치면 되는 문제이고, 진짜 이유는 `ModifyScores`가 굴림 직후와 킵 토글마다 호출되는 `UpdateCandidates`(`LocalGameAuthority.cs:170`·`237`)에서 매번 돌면서 조건 불성립 시에도 `0`을 쓴다는 점이다. 이벤트를 내도 "실행됐다"와 "뭔가 일어났다"가 구분되지 않는다. 퀘스트 계열이 `AfterScoreCommit`에서 한 턴에 한 번 발행하는 것과 대비된다. `M17-T9` 변형 계열을 상태 기반으로 설계한 근거이며 사양 문서 §3.1.1에 기록했다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Logic/YachtAugmentRuntime.cs`, `Assets/Editor/YachtDraftOrderTests.cs`, `docs/augmented_yacht_work_plan.md`, `docs/augmented_yacht_m17_vfx_spec.md`
- 실행한 검증:
  - C# 컴파일 오류 0 (`Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`)
  - 빌드된 어셈블리를 직접 실행해 11개 항목 전부 통과. 제시 200회에서 대상 족보 중복 0회, 변형 증강이 2개 이상 함께 제시된 표본 94회로 규칙이 실제로 작동할 여지 확인
  - 변경 전 어셈블리(`Library/ScriptAssemblies`)로 같은 검사를 돌려 5개 실패 확인. 제시 200회 중 12회 중복(FullHouse 5, LargeStraight 3, SmallStraight 2, Yacht·Aces 각 1)
- 남은 문제/차단 요소: 없음
- 다음 작업: `M17-T9` 변형 계열 구현

### 2026-09-07 — Claude (`M17-T8` 후속: 8면 면 숫자 폰트 전환과 렌더링 결함 세 건 수정)

- 작업 ID: `M17-T8` (후속 수정)
- 시작 상태: 사용자가 실제 화면에서 세 가지 문제를 지적. 주사위 몸체에 픽셀 필터가 걸리지 않음, 면 숫자가 작고 타이머 폰트를 쓰기로 한 결정이 미구현, 몸체 색이 의도한 딥블루보다 밝음
- 완료 내용:
  - 몸체 재질의 자발광 제거. 바탕색과 같은 색을 자발광으로 더해 조명 계조가 덮이고 어두운 팔레트가 화면에서 밝게 뜨던 문제 해소
  - 면 숫자를 직접 만든 7세그먼트 메시에서 `Assets/Fonts/alagard.ttf` 기반 `TextMesh`로 교체. 타이머·점수표와 같은 서체. 글리프 높이 0.22 → 0.43
  - 깊이를 검사하는 글리프 셰이더 `DicePoC/CrispUiText` 추가. 뒷면 글자가 몸체를 뚫고 보이던 문제 해소
  - 깊이 전용 판이 몸체 재질로 덮여 Crisp 카메라가 몸체를 원본 해상도로 다시 그리던 결함 수정
- 수정한 버그:
  - 내장 폰트 재질(`GUI/Text Shader`)은 `ZTest Always`에 `Cull Off`라 깊이 버퍼를 무시한다. `CrispUiDepthMask`가 깊이를 남겨도 글자가 검사를 받지 않아 8면 전부의 숫자가 겹쳐 보였다. 이전 메시 숫자는 Unlit이라 우연히 가려지고 있었다
  - `ApplyDiceMaterialsToFbx`가 자식 렌더러를 전부 훑으면서 `Crisp Depth Mask`까지 몸체 재질로 덮었다. 색을 쓰지 않아야 할 판이 조명 받는 몸체 사본이 되어 `CrispUI` 레이어에서 풀 해상도로 렌더됐고, 그래서 주사위에만 픽셀 필터가 걸리지 않은 것처럼 보였다. 깊이 마스크를 도입한 시점부터 있던 결함이다
  - `TextMesh`는 글자가 바로 읽히는 쪽이 `-Z`다. 면 법선을 `+Z`에 맞췄더니 좌우가 뒤집혀 `LookRotation(-n, localUp)`으로 고쳤다. 같은 이유로 `Cull Back`에서는 글자가 통째로 사라져 컬링 대신 깊이 검사로 뒷면을 가린다
- 변경 파일: `Assets/Editor/DiceShapeBaker.cs`, `Assets/Editor/DiceShapeAssetTests.cs`, `Assets/Rendering/Shaders/CrispUiText.shader`, `Assets/Scripts/Dice/DicePaletteCatalog.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceVisualPool.cs`, `Assets/Prefabs/Dice/Die_Octahedron.prefab`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: 프로젝트 EditMode 테스트 121/121 통과. Play 모드 실렌더로 확인 — 8면체 실루엣에 배경과 같은 크기의 픽셀 계단이 생기고, 면 숫자는 원본 해상도로 또렷하며, 보이는 면에만 숫자가 하나씩 나온다. 실제 `8면 주사위` 증강으로 굴린 주사위가 값 면을 정면으로 세우고 숫자를 똑바로 표시하는 것도 확인
- 진단 방법 기록: 카메라 컬링 마스크를 하나씩 꺼서 어느 카메라가 몸체를 그리는지 좁혔다. 월드 카메라에서 `Dice` 레이어를 빼도 몸체가 남고 Crisp 카메라까지 꺼야 사라지는 것으로 원인을 특정했다
- 새 결정/가정: 8면체 면 숫자는 UI와 같은 폰트로 찍는다. 동적 폰트 아틀라스는 다시 구워질 수 있으므로 `Font.textureRebuilt`를 받아 재질의 텍스처를 다시 물린다. 같은 폰트로 증강 카드 한글도 찍는 씬이라 실제로 일어난다
- 남은 문제/차단 요소: 없음
- 다음 작업: 주사위 눈 렌더링 방식 논의

### 2026-09-07 — Claude (`M17-T8` 특수 주사위 외형 검수 완료)

- 작업 ID: `M17-T8` (`DONE`)
- 시작 상태: 2026-09-05 중단 기록상 "8면 도중"이었으나, 실제로는 `M12` 진행 중 매핑·프리셋·테스트가 채워져 있어 신규 구현이 아닌 검수 성격이었음
- 완료 내용:
  - Unity Editor Play 모드에서 8종 주사위 외형을 실렌더로 검수. 논리 종류→팔레트 적용을 런타임 머티리얼 `_BaseColor`로 8종 전부 확인
  - 실제 증강(묵직한 주사위) 보유 상태에서 화면 주사위 5개 중 1개만 빨강으로 표시되는 것을 확인
  - 주사위 몸체 누적 버그 수정 (`DiceVisualPool.BuildVisual`)
  - 8면체 정규화 크기를 1.0 → 1.2로 확대 (`DiceVisualPool.NormalizedSizeFor`)
  - 8면체 면 숫자 가독성 실측 결과를 `DiceShapeBaker` 주석에 기록
  - 일반 모드에서 드래프트 오버레이·보유 증강 카드·특수 주사위가 모두 비활성인 것을 확인
- 수정한 버그: 재생 중 `Destroy`가 프레임 끝에 처리되는데 `Find("Visual")`이 파괴 예약된 몸체를 다시 잡아, 같은 프레임에 종류가 두 번 바뀌면 몸체가 겹쳐 쌓였다. 실측에서 주사위당 `Visual` 자식이 5~7개까지 누적됐다. 이름이 `Visual`인 자식을 전부 버리고, `Destroy` 전에 이름 변경과 비활성화를 먼저 해 같은 프레임의 다음 조회가 다시 잡지 못하게 했다
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Presentation/DiceVisualPool.cs`, `Assets/Scripts/Dice/DiceFaceOrientation.cs`, `Assets/Editor/DiceShapeBaker.cs`, `docs/augmented_yacht_work_plan.md`
- 실행한 검증: EditMode 전체 807/812 통과(실패 1건은 `UnitySkills` 패키지 자체 문서 용량 테스트). 재베이킹 후 프로젝트 테스트 121/121 통과. 종류 9회 연속 교체 후 주사위당 `Visual` 1개 유지 확인
- 8면체 면 숫자 해결: 픽셀 패스에서는 어떤 크기로도 읽을 수 없음을 실측으로 확인한 뒤(면 내접원 약 7.6px, 획 0.5px는 얼룩·2px는 흰 덩어리·점 표기는 값 6이 5×8px로 초과), 크기를 키우는 대신 숫자만 픽셀 필터 밖으로 뺐다. 런타임이 8면체의 `Pip_*` 를 `CrispUI` 레이어로 올려 점수표·증강 카드 글자와 같은 경로로 원본 해상도에 합성한다. Crisp 카메라는 월드 물체를 찍지 않아 깊이 버퍼가 비어 있으므로 몸체 사본을 깊이 전용으로 같은 레이어에 두어 뒤쪽 면 숫자가 뚫고 나오지 않게 했다
- `GetOctaCameraFacingRotation`이 면 법선만 맞추고 롤을 정하지 않아 면 숫자가 누워 보이던 것도 고쳤다. 6면과 같이 면 법선과 글자 윗방향을 함께 맞춘다
- 새 결정/가정: 8면체 값 표시는 면에 새긴 숫자를 `CrispUI`로 올리는 방식으로 확정한다. 몸체는 픽셀 패스에 남아 질감이 유지된다
- 남은 문제/차단 요소: 일반 모드에서 빈 증강 카드 트레이(석재 프롭)가 그대로 보인다. 카드 인스턴스는 모두 비활성이라 리소스 누수는 아니며, 프롭을 감출지는 미결
- 다음 작업: 사용자 지시 대기

### 2026-09-07 — Claude (M17 착수 준비, `M17-T8` 시작)

- 작업 ID: `M17-T8` (`DOING` 지정)
- 시작 상태: `feature/yacht-dice-migration` → `main` 병합이 이미 완료(`fbe93df`, `origin/main` 동기)됐으나 §2 포인터가 "병합 준비"로 남아 있었음
- 사용자 결정: `M17` 첫 작업은 `M17-T8` 특수 주사위 검수. 새 브랜치 `feature/m17-augmented-hotseat`에서 진행. graphify 라벨 갱신분은 선행 `chore` 커밋으로 정리
- 완료 내용:
  - `main` 기준 새 브랜치 `feature/m17-augmented-hotseat` 생성
  - graphify 커뮤니티 이름 갱신분 커밋(`44fef09`)
  - §2 포인터를 `M17` / `M17-T8` `DOING` 기준으로 갱신, §M17 표의 `M17-T8` 상태 변경
  - `M17` 15개 작업의 코드 기준 현행 격차 조사
- 조사 결과 (문서 기록과 코드의 차이):
  - `M17-T8`은 2026-09-05 중단 기록보다 진척됨. `YachtDieType` 8종 전부 `YachtDieVisuals.Resolve`로 팔레트에 매핑되고 8면 형상·프리셋·회전 테스트가 이미 존재. 남은 범위는 게임 내 최종 검수와 프로모션 레벨 등 잔여 표현
  - `M17-T2`(드래프트 오버레이)·`M17-T4`(수동 발동 버튼)·`M17-T5`(턴 시간 증강 배선)는 상당 부분 구현되어 있음
  - `M17-T1`(교대 가림)·`M17-T9`(발동 VFX)·`M17-T11`·`M17-T12`는 미착수, `M17-T6`은 점수만 표시, `M17-T7` 무작위 완주 테스트는 없음
- 변경 파일: `docs/augmented_yacht_work_plan.md`, `graphify-out/.graphify_labels.json(.sig)`
- 실행한 검증: `git log`로 병합·푸시 상태 확인. 코드·씬 변경 없음
- 새 결정/가정: 없음
- 남은 문제/차단 요소: 없음
- 다음 작업: `M17-T8` 게임 내 특수 주사위 외형 검수
