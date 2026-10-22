# M17. 증강 요트 로컬 핫시트 완성

> **문서 종류**: 마일스톤 계획 (AI 대상)
> **코드 대조 기준**: 2026-10-22 · 커밋 `6527034` (내용은 2026-09-20 기준 `work_plan.md`에서 옮김)

## 목표

전체 증강 게임 완주 + 완주에 필요한 그래픽(특수 주사위·VFX·상태 표시). 구 `M7-T5~T12` 흡수 (`D-038`). 일부 태스크 진행 중

## 완료 게이트

[공통 완료 기준](../process.md#6-공통-완료-기준)에 더해 다음을 만족합니다.

- 로컬 핫시트만으로 일반/증강 게임을 모두 선택해 완주할 수 있다.
- 씬 재시작 없이 새 게임을 시작할 수 있다.
- 잘못된 입력에서 상태가 손상되지 않는다.
- `M17-T8`~`M17-T12`: 특수 주사위 논리·외형 일치, 증강 발동 주체·대상·결과 전달, 점수표·진행형·수동 증강 상태를 화면에서 확인 가능하다.
- `M17-T13`~`M17-T15`(사운드·폴백·해상도/성능)는 `DEFERRED`이며 핵심 완주 완료 조건에서 제외한다 (`D-038`).

## 태스크

| ID | 작업 | 상태 | 수락 기준 | 문서 |
|---|---|---|---|---|
| `M17-T1` | 플레이어 교대 가림 화면 | `DROPPED` | 상대의 비공개 선택지를 바로 볼 수 없음 | — |
| `M17-T2` | 플레이어별 드래프트 UI | `DONE` | 드래프트 차례 플레이어만 증강을 선택할 수 있음 | — |
| `M17-T3` | 보유 증강 표시 | `DONE` | 대상 족보와 진행 상태를 확인 가능함 | — |
| `M17-T4` | 증강 수동 행동 UI | `DONE` | 사용 가능 조건과 남은 횟수가 표시됨 | — |
| `M17-T5` | 시간 관련 증강과 타이머 | `TODO` | 로컬 규칙 기준으로 일관되게 처리됨 | — |
| `M17-T6` | 게임 종료 요약 | `TODO` | 점수와 증강 결과를 확인 가능함 | — |
| `M17-T7` | 반복/무작위 전체 게임 테스트 | `DONE` | 여러 증강 조합으로 완주함 | — |
| `M17-T8` | 특수 주사위 외형 구현 (구 `M7-T5`) | `DONE` | 8종 전부 화면에서 구분 가능함 | — |
| `M17-T9` | 증강 발동 VFX와 전환 연출 (구 `M7-T6`) | `TODO` | 발동 주체·대상·효과 결과가 과도한 텍스트 없이 전달됨 | [T09-vfx.md](T09-vfx.md) |
| `M17-T10` | 점수표 증강 표시 (구 `M7-T7`) | `DONE` | 교체된 족보·보너스·진행 조건을 점수표에서 확인 가능 | — |
| `M17-T11` | 진행형 증강 상태 표시 (구 `M7-T8`) | `DROPPED` | 횟수·남은 턴·누적치·성공/실패 확인 | — |
| `M17-T12` | 수동 증강 행동 피드백 (구 `M7-T9`) | `DONE` | 사용 가능 여부·입력 확인·성공/실패 이유 표현 | — |
| `M17-T13` | 증강 사운드·촉각 피드백 (구 `M7-T10`) | `DEFERRED` | — | — |
| `M17-T14` | 그래픽 폴백과 누락 검증 (구 `M7-T11`) | `DEFERRED` | 원본·픽셀 보정본 누락 시 안전 표시 | — |
| `M17-T15` | 해상도·가독성·성능 검증 (구 `M7-T12`) | `DEFERRED` | 32·48·64px·목표 해상도, 프레임·메모리 | — |
| `M17-T16` | 테이블 나뭇결 픽셀화 | `DONE` | 결 경계가 계단으로 끊기고, 판자 4장이 서로 다른 결·옹이·균열을 가짐 | — |
| `M17-T17` | 드래프트 제시 구성과 선공 순서 규칙 | `DONE` | 같은 대상 족보를 교체하는 변형 증강이 한 제시에 둘 이상 나오지 않음 | — |
| `M17-T18` | 점수 칸 호버 깃펜 필기 연출 | `DONE` | 기입 가능한 칸을 가리키면 깃펜이 잉크통에서 나와 그 칸에 닙을 댐 | [T18-quill-hover.md](T18-quill-hover.md) |
| `M17-T19` | 깃펜 깃털 흰색 전환과 갈라짐 연출 | `DROPPED` | — | — |
| `M17-T22` | 깃펜 외부 모델 교체 | `DONE` | 절차적 깃펜 8개를 FBX 인스턴스 하나로 교체 | — |
| `M17-T23` | 코인 메시 반입과 앞뒷면 구분 | `DONE` | 앞면·뒷면 문양이 구분되는 `Coin.prefab`을 만듦 | [T23-coin-mesh.md](T23-coin-mesh.md) |
| `M17-T24` | coin-toss 증강 로직·동전 스핀 연출 | `DONE` | 동전 앞뒤 개수에 따라 효과가 갈리는 수동 발동 증강 | — |
| `M17-T25` | yacht-bank 킵 존 하이라이트·반짝이 연출 | `TODO` | 보유자 턴이면서 저축 턴이 남은 동안에만 킵 0번 칸 글로우 표시 | [T25-bank-keep-zone.md](T25-bank-keep-zone.md) |

결번: `M17-T20`, `M17-T21`

`M17-T9-1`(56 `dice-alchemy` 연기 가림 연출)은 `M17-T9`의 하위 작업이라 이 표에 두지 않는다. 상태는 [`T09-vfx.md`](T09-vfx.md) §9에 있다.

`M17-T8`~`M17-T12`의 시각 사양은 [`docs/archive/milestones/m7_graphics_spec.md`](../../archive/milestones/m7_graphics_spec.md)를 기준으로 한다 (구 `M7-T5`~`M7-T9`). `M17-T8` 특수 주사위는 `augmented-dice` 프로젝트 구현을 재사용한다.

## 착수 메모

> 착수 전 사양 메모를 옛 태스크 표에서 옮긴 것입니다.

### `M17-T9` 증강 발동 VFX와 전환 연출

발동 주체·대상·효과 결과가 과도한 텍스트 없이 전달됨. 대상은 활성 증강 46개이며 구현 완료된 50 `coin-toss`도 포함한다. 사양과 하위 구현 계획은 [`T09-vfx.md`](T09-vfx.md)

### `M17-T25` yacht-bank 킵 존 하이라이트·반짝이 연출

보유자 턴이면서 저축 턴이 남은 동안에만 킵 0번 칸에 골드(`#e5a93c`) 프레임 글로우와 루프 파티클 표시. 상대 턴·지급 대기·지급 완료·드래프트 중 숨김. `RefreshAugmentPresentation`에서 상태 재평가로 on/off. 로직 `YachtBank.cs`를 `KeepSlotIndex == 0` 기준으로 정합화(사양서 §7·§8 결정). 슬롯 0 주사위 금색 변환·투명화는 범위 밖. 상세는 [`T25-bank-keep-zone.md`](T25-bank-keep-zone.md)(2026-09-15 계획 작성)

## 완료 기록

> 2026-10-22 이전 기록은 옛 태스크 표의 완료 조건 칸을 그대로 옮긴 것입니다.

### `M17-T1` 플레이어 교대 가림 화면

상대의 비공개 선택지를 바로 볼 수 없음. 사용자 판단으로 범위에서 제외(2026-09-13). 상세 사유 미확인

### `M17-T2` 플레이어별 드래프트 UI

게이팅은 이미 이중으로 존재. `LocalGameAuthority.TrySelectAugment`는 `playerIndex` 인자를 받지 않고 내부에서 `Draft.PlayerIndex`를 직접 읽어 UI 경로 사칭이 불가능하고, `YachtAugmentRuntime.TrySelectAugment`는 `Draft.PlayerIndex != playerIndex`이면 `YachtCommandErrorCode.NotDrafting`으로 거부. 차례 표시도 이미 있음: `AugmentTrayPresenter`가 `P{n} 증강 선택 · {라운드}라운드` 타이틀을 그리고, 점수표 활성 열이 드래프트 차례를 따라감. 이번 산출물은 그 게이팅을 고정하는 회귀 테스트 `OnlyCurrentDraftPlayerCanSelectAugment`(`Assets/Editor/Tests/Yacht/YachtDraftOrderTests.cs`) 추가. 프로덕션 코드 미수정. EditMode 전체 242/242 통과(2026-09-13)

### `M17-T3` 보유 증강 표시

대상 족보와 진행 상태를 확인 가능함. 사용자 결정으로 `M17-T11`(진행형 증강 상태 표시)을 흡수함. 화면 확인에서 퀘스트 진행 배지가 획득 직후 비는 문제 발견. 원인은 `CautiousStraight`·`Copycat`·`Doubling`·`EveryLittleCounts`·`Holdout` 5종이 `IOnAugmentSelected`를 구현하지 않아 첫 점수 기입 전까지 상태 객체가 없었던 것. 표시층 폴백 대신 5종에 `IOnAugmentSelected`를 붙여 획득 시점에 상태를 생성하도록 수정(기본값이 곧 시작 진행도라 값 대입 없음). 이후 사용자 화면 확인에서 진행도를 원본 웹 프로젝트(`augmented-dice` `gameRuntime.js` `getQuestProgressText`) 형식으로 출력해야 한다는 지적을 받아, 짧은 배지 한 줄 방식을 폐기하고 카드 자체에 상태 라벨(퀘스트 진행 중/성공/실패) + 점선 + "퀘스트: …" 하위 목표 줄 목록을 출력하도록 재구현. 달성 줄은 취소선 + 투명도 0.7, 실패 시 미달성 줄도 취소선 + 0.6. 진행 줄만 `TextMeshProUGUI`로 전환하고 취소선은 리치 텍스트 `<s>` 태그로 그림(사용자 결정, 2026-09-14). Mulmaru.ttf로 런타임 동적 TMP 폰트 에셋을 생성하며, 픽셀 폰트에 취소선 메트릭이 없어 한글 '가' 글리프 세로 중앙으로 `strikethroughOffset`을 보정. "퀘스트" 밑줄은 역할이 없어 제거(사용자 결정). 진행 블록은 고정 3줄 예약 대신 실측 높이의 가변 블록으로 카드 하단(푸터)에 붙이고, 콘텐츠 세이프 영역 아래 여백을 `FooterBleed`(10px)만큼 사용. `Assets/TextMesh Pro/`(TMP Essential Resources) 추가. 로직: `AugmentProgress`를 `Outcome` + 줄 목록(`AugmentProgressLine`) 구조로 변경, `DescribeProgress(in AugmentProgressQuery)`로 턴 수·점수표를 받아 판정. `IReadOnlyPlayerScoreData.IsFilled` 추가. 족보 표시명을 `ScoreCategoryNames`로 올려 점수표와 공유. 퀘스트 11종 문구는 웹 원문을 따르되 Copycat·EveryLittleCounts 두 개는 사용자 결정으로 Tessera 규칙에 맞춰 수정. 호버 상세의 "진행:" 줄 제거. 검증: EditMode 267/267 통과(2026-09-13); `AugmentCardViewTests` 56/56, `AugmentProgressTextTests` 14/14, `YachtQuestAugmentTests` 14/14 통과(2026-09-14, EditMode 전체 재실행은 이번엔 하지 않음). 취소선 오프셋이 한글 글리프 중앙 범위인지와 줄 수에 따른 블록 높이 가변을 단언. 화면 확인 완료(2026-09-14)

### `M17-T4` 증강 수동 행동 UI

사용 가능 조건과 남은 횟수가 표시됨. 버튼 활성 판정을 `LocalGameAuthority.TryResolveManualAction` 추출로 `UseAugmentAction`과 공유하고, 상태를 바꾸지 않는 조회 `CanUseAugmentAction(playerIndex, augmentId, out YachtCommandErrorCode, out string)`을 추가해 `YachtGameSession` 래퍼를 거쳐 `AugmentTrayPresenter.CanUseManualAugment`가 이 경로를 쓰도록 통일(기존엔 `TableFlip`만 검사하고 나머지 5종은 무조건 활성). `M17-T12`와 함께 완료(2026-09-20)

### `M17-T7` 반복/무작위 전체 게임 테스트

여러 증강 조합으로 완주함. `Assets/Editor/Tests/Yacht/YachtRandomFullGamePlaythroughTests.cs` 신규. 시드 고정 난수원으로 증강 요트 한 판을 끝까지 자동 진행하는 하네스를 만들고 테스트 2개를 둠: 시드 0~59로 60판을 완주시켜 예외 없이 끝나는지 보는 것, 시드 0~19로 20판을 돌려 서로 다른 증강 ID가 10종 이상 발동하고 수동 행동이 최소 한 번 발동하는지 보는 것. 무한 루프 방지로 2000회 반복 상한. 검증: EditMode 전체 369/369 통과(기존 367 + 신규 2), 컴파일 에러·경고 0건(2026-09-20)

### `M17-T8` 특수 주사위 외형 구현 (구 `M7-T5`)

8종 전부 화면에서 구분 확인(2026-09-07). 8면체 면의 숫자는 `CrispUI` 레이어로 올려 픽셀 필터를 우회

### `M17-T9-1` 56 `dice-alchemy` 연기 가림 연출

연기가 킵 안 된 주사위를 가린 사이에 눈이 바뀌고, 점수표 후보 점수가 먼저 결과를 알리지 않음. `M17-T9-1-1`~`M17-T9-1-5` 전부 완료, Play 모드 화면 확인 완료(2026-09-13). 연기는 절차적 스프라이트가 아니라 외부 에셋팩 프리팹으로 교체돼 룩이 확정됨(커밋 `2497bfc`, `6f62c96`). 상세는 VFX 사양서 §9

### `M17-T10` 점수표 증강 표시 (구 `M7-T7`)

교체된 족보·보너스·진행 조건을 점수표에서 확인 가능. `M17-T3`과 함께. 변형(`Modification`) 계열은 이미 구현돼 동작 중임: `AugmentStickerCatalog.CollectFor`가 배치를 계산하고 `YachtTurnFlowPresenter`가 `ParchmentScoreSheet.SetSticker`/`ClearSticker`로 점수표에 반영하며, 스티커에 색상 코드와 축약 표기(`MarkLabel`)가 찍힘. 강화(`Enhance`)·퀘스트(`Quest`) 계열은 스티커 대상이 아니라 미표시(`HasSticker`가 `Modification`만 허용). 2026-09-13 확인. 남은 두 항목을 2026-09-20에 완료: ① 보너스 라벨이 항상 `+35`로 고정 표시되던 버그 수정. `step-by-step` 증강 보유 시 실제 지급은 55점인데 화면은 35로 남아 `PlayerScoreData.BonusAward`(`stepBonusGranted ? 55 : 35`)를 추가하고 `RecalculateTotal()`과 `ParchmentScoreSheet` 표시, `IReadOnlyPlayerScoreData`가 같은 값을 쓰도록 정정. ② bounty-hunter 타깃 칸 상시 표시. `ParchmentScoreSheet.SetTargetMark`/`ClearTargetMark`/`HasTargetSlot`을 추가해 타깃 족보 칸에 골드 테두리+증강 아이콘+남은 횟수를 그리며 원래 족보 아이콘·이름은 가리지 않음. `YachtTurnFlowPresenter.RefreshAugmentPresentation()`의 기존 동기화 지점에 `SyncBountyHunterTargetMarks()`를 추가(새 타이밍 없음). 신규 테스트: `AugmentBountyHunterTargetMarkTests.cs`(3개), `YachtGameRulesTests.PlayerScoreData_BonusAward_MatchesStepBonusGrantedAndFeedsTotal`. 완료 조건의 "잠금" 문구는 삭제함: 조사 결과 점수표 칸 잠금 기능은 코드·기획 어디에도 존재하지 않았다(2026-09-20). 검증: EditMode `filter=Tessera.Editor.Tests` 367/367 통과. 시각 확인 미실시(점수표는 Play Mode 런타임 생성이라 EditMode로 렌더 결과 판정 불가)

### `M17-T11` 진행형 증강 상태 표시 (구 `M7-T8`)

횟수·남은 턴·누적치·성공/실패 확인. `M17-T3`으로 흡수됨(2026-09-13). 별도 작업으로 진행하지 않음

### `M17-T12` 수동 증강 행동 피드백 (구 `M7-T9`)

사용 가능 여부·입력 확인·성공/실패 이유 표현. `M17-T4`와 함께. 카드 푸터 `IAugmentProgressText`를 `CoinTossState` 외 5종(`TableFlipState`·`DiceAlchemyState`·`EquivalentExchangeState`·`GambitState`·`DoubleDownState`)에 구현해 사용 가능/사용함/남은 횟수/발동 조건 문구 표시. 발동 성공 메시지의 `table-flip`·`equivalent-exchange`·`gambit`·`double-down` 4종이 원시 augmentId 슬러그를 찍던 것을 `DisplayName` 사용으로 정정. 고아 심볼 `YachtGameSession.CanUseTableFlip`·`LocalGameAuthority.ContainsOwnedAugment` 제거. 신규 테스트 `AugmentManualActionQueryTests.cs`·`AugmentManualActionProgressTextTests.cs`. EditMode `filter=Tessera.Editor.Tests` 363/363 통과(2026-09-20). 월드 발동 VFX·카메라 셰이크는 `M17-T9`, 증강 효과음은 `M17-T13`(`DEFERRED`) 범위 유지

### `M17-T13` 증강 사운드·촉각 피드백 (구 `M7-T10`)

후속 오디오·플랫폼 작업으로 이관 유지. `coin-toss`(50) 동전 튕김·회전·착지 효과음 포함(2026-09-15, `M17-T24`에서 이관)

### `M17-T14` 그래픽 폴백과 누락 검증 (구 `M7-T11`)

`M17` 핵심 완주 뒤. 원본·픽셀 보정본 누락 시 안전 표시

### `M17-T15` 해상도·가독성·성능 검증 (구 `M7-T12`)

`M17` 핵심 완주 뒤. 32·48·64px·목표 해상도, 프레임·메모리

### `M17-T16` 테이블 나뭇결 픽셀화

결 경계가 계단으로 끊기고, 판자 4장이 서로 다른 결·옹이·균열을 가짐(2026-09-08). `D-037`이 남긴 "나무결 텍스처 표면" 항목 해소

### `M17-T17` 드래프트 제시 구성과 선공 순서 규칙

같은 대상 족보를 교체하는 변형 증강이 한 제시에 둘 이상 나오지 않고, 1번 증강은 무작위·2·3번 증강은 총점이 낮은 쪽이 먼저 선택(2026-09-08). `D-039`

### `M17-T18` 점수 칸 호버 깃펜 필기 연출

기입 가능한 칸을 가리키면 깃펜이 잉크통에서 나와 그 칸에 닙을 대고, 칸을 옮기면 따라붙고, 포인터를 빼면 잉크통으로 복귀. 그동안에도 점수표 전체가 읽힘. `M17-T18-1`~`M17-T18-5` 전부 완료, 화면 확인 완료(2026-09-13). 상세는 [`T18-quill-hover.md`](T18-quill-hover.md)

### `M17-T19` 깃펜 깃털 흰색 전환과 갈라짐 연출

절차적 깃펜 자체를 폐기하고 외부 로우폴리 모델 `Assets/Art/Reference/quill_pen_low.fbx`로 교체(2026-09-12). 화면이 480x270 격자로 필터링돼 여기서 맞춘 디테일이 남지 않는데도 굽고 눈으로 확인하는 왕복이 계속 필요해 비용이 맞지 않았다. 계획서는 [`docs/archive/plans/quill_feather_appearance_plan.md`](../../archive/plans/quill_feather_appearance_plan.md)에 폐기 표시와 함께 남김

### `M17-T22` 깃펜 외부 모델 교체

절차적 깃펜(닙·칼라·깃대·깃판) 8개를 FBX 인스턴스 하나로 교체. `InkwellAndQuill`은 잉크통만 만들고 깃펜은 직렬화된 모델 참조를 붙인다. 텍스처는 256 알베도 한 장(BaseColor+Opacity 합성). 코드·에셋 작업에 이어 화면 확인 완료(2026-09-13)

### `M17-T23` 코인 메시 반입과 앞뒷면 구분

외부 `coin.glb`를 에디터 베이커로 반입해 앞면(흉상)·뒷면(리라) 문양이 구분되는 `Coin.prefab`을 만듦. HOLD 증강 `coin-toss`의 선행 에셋이며 증강 로직·굴림은 범위 밖. 상세는 [`T23-coin-mesh.md`](T23-coin-mesh.md). 2차 재확인 통과(2026-09-15): 리라(뒷면) 실루엣은 약하지만 사용자가 그대로 수용. `M17-T23-8` 가독성 후속 조정은 사용자 결정으로 현행 유지. `M17-T23-7` 3차 재확인 통과(2026-09-20): 사용자 육안 확인 후 완료 판정

### `M17-T24` coin-toss 증강 로직·동전 스핀 연출

동전 3개 앞뒤에 따라 0개(−5점)·1개(보너스 +3점 획득)·2개(이번 턴 리롤 +1)·3개(상단 보너스 기준 min(현재,57))로 갈리는 수동 발동 증강. `CoinToss`/`CoinTossState`(`Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Enhance/CoinToss.cs`), 보너스 굴림 기록(`YachtGameState.BonusRolls`), 해석적 코인 스핀 프리셋 5종 베이크와 재생기 `CoinTossVfx.cs` 구현 완료. 연출은 발동 시 주사위(킵 줄 포함) 숨김 → 게임 트레이 내부 투척 → 착지 후 2초 대기 → 주사위 값 반영 후 재표시 → 동전 숨김 → 점수표·리롤 바·문구 반영 순으로 재구성(2026-09-15). EditMode 1204건 중 1200 통과·실패 0·스킵 4(unity-skills 패키지 소속), 사용자 Play Mode 시각 확인 통과(2026-09-15). 효과음은 동전 3개 각각의 스핀 시작 시점에 1회씩(총 3회) 재생하도록 구현 완료(`CoinTossVfx.Play`의 `onCoinSpinStarted` 콜백 → `YachtAudioService.PlayCoinToss`, `coin_flip.mp3`, 2026-09-20). 촉각 피드백 등 나머지는 `M17-T13`(증강 사운드·촉각 피드백, `DEFERRED`) 범위로 이관
