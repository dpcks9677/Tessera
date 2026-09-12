# 코드 리뷰 (2026-09-12)

사용자가 제시한 "Unity C# 코드 리뷰 기준 — 추가 제안 섹션" 문서의 여섯 렌즈로 진행한 리뷰다. 기존 `CodeReviewGuide.md`가 이미 덮고 있는 가독성·SOLID·캐시 효율은 다시 보지 않았고, 그 문서에서 빠져 있던 항목만 겨냥했다. **여기서 고친 것도 커밋하지 않았다.** 채택 여부는 사용자가 정한다.

지적 여덟 건 중 다섯 건을 이번 세션에서 함께 고쳤다. 나머지는 지금 고치면 손해라고 판단해 보류했고 그 이유를 아래에 적었다.

## 리뷰 범위와 한계

| 항목 | 내용 |
|---|---|
| 기준 커밋 | `635e333` (가이드 문서 추가) |
| 대상 | `Assets/Scripts` 전체 126파일 27,716줄 |
| 렌즈 | 디자인 패턴, 테스트 용이성, 보안·치팅 방지, 로깅·예외, 동시성·비동기, 리소스·에셋 로딩 |
| 제외 | 황도 12궁 별자리 연출(폐기 기능), `Assets/Editor` 하위 베이커·검증 도구, 스타일·네이밍 |
| 실행 검증 | 컴파일 오류 0·경고 0. EditMode 테스트 900개 중 889 통과, 6 실패, 5 스킵. 실패 6건은 모두 이번 변경과 무관하다(아래 참조). `TEST-1`의 신규 테스트 2개는 통과했고, 각각이 겨냥한 가드를 실제로 지나는지도 코드로 확인했다 |
| 남은 검증 | `ASYNC-1`은 정적 검증만 끝났다. 연기 가림 0.6초 동안 입력이 실제로 막히는지는 Play 모드 확인이 필요하며, 이는 `M17-T9-1-5`와 같은 대상이다 |

### 이번 변경과 무관한 기존 테스트 실패 6건

EditMode 전체 실행에서 실패한 6건은 모두 이번 리뷰가 건드린 7파일 밖에 있다.

- `Tessera.Editor.Tests.FontFallbackTests` 2건 — `AlagardFontFallbackContainsMulmaru`, `M6x11FontFallbackContainsMulmaru`. 둘 다 `fallbackFontReferences 속성을 찾을 수 없습니다`로 실패한다. 폰트 에셋 쪽 문제이며 이번 변경은 폰트 파일을 하나도 건드리지 않았다. 별도 조사가 필요하다.
- `UnitySkills.Tests.Core` 4건 — 문서 바이트 예산, 마이그레이션 멱등성, csproj 파일 잠금. `unity-skills` 패키지 자체의 테스트로 이 프로젝트 코드와 무관하다.

여섯 렌즈로 좁힌 리뷰이므로 "지적 사항 없음"이 "문제 없음"을 뜻하지 않는다. 이 렌즈들이 보지 않는 결함은 그대로 남아 있다.

이미 `docs/solid_refactoring_work_plan.md`에 `TODO`로 등록된 항목(`SOLID-T05`~`T12`)은 중복이므로 지적에서 제외했다. 리뷰 중 그와 겹치는 후보가 여러 건 나왔으나 모두 계획서가 이미 인지하고 있는 사안이었다.

## 총평

여섯 렌즈 중 다섯에서 코드베이스는 양호하거나 이례적으로 좋다.

**테스트 용이성이 특히 그렇다.** 규칙 계층(`Games/Yacht`, `Games/AugmentedYacht/Logic`) 전체에 `UnityEngine` 참조, `MonoBehaviour`, `Time.*`, `DateTime.Now`, 직접 난수 호출이 하나도 없다. 난수는 `YachtGameCore.cs`의 `IRandomSource` 한 경로뿐이고 시드를 주입할 수 있다. 리뷰 기준 문서가 겨냥한 대표 문제("핵심 로직이 MonoBehaviour와 강하게 결합", "비결정성이 로직 안에 직접 섞임")가 구조적으로 이미 해소돼 있다.

**디자인 패턴도 마찬가지다.** 정적 팩토리 메서드 `Create`가 `Tabletop` 소품 14개의 전역 컨벤션이고, 상태 패턴은 `QuillHoverAnimator`와 `PresentationPhase`에 이미 적용돼 있으며, 증강 수동 행동은 ID 기반 디스패치라 `switch` 사다리가 없다. `MonoBehaviour` 싱글톤은 한 건도 없다.

**보안 렌즈는 해당 사항이 없다.** `Assets/Scripts/Network`는 파일이 없는 빈 디렉터리이고, 세이브 데이터·서버 통신·하드코딩된 시크릿이 전부 0건이다. 리뷰 기준 문서 자신이 "순수 싱글플레이 프로토타입 단계에서는 우선순위를 낮춘다"고 적은 조건에 정확히 해당한다. M18(네트워크 준비 검증) 착수 시점에 다시 켜야 할 렌즈다.

문제가 몰린 곳은 **비동기 렌즈 한 곳**이다. 코루틴 관리 관례("새로 시작하기 전에 이전 핸들을 정지")는 `RollCosmicCube`, `RollOrb`, `RunicSlateMatrix`, `TurnBalanceIndicator`, `HourglassTimer`가 모두 지키는데, 가장 최근에 들어온 연기 가림 연출만 그 관례 밖에 있었다. 가장 시급한 것은 `ASYNC-1`이다.

## 발견된 문제

### ASYNC-1. 연기 가림 연출 중에 굴림·킵 입력이 열려 있다 (수정함)

- **중요도**: High
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtTurnFlowPresenter.cs` — `RunDiceSmokeSwapSequence`, `RollDice`, `UseAugmentAction`, `SetDieKept`

네 가지가 맞물려 생긴 구멍이다.

1. `RunDiceSmokeSwapSequence`는 `Phase`를 한 번도 대입하지 않는다. 0.6초 동안 `Phase`가 `Settled`로 남는다.
2. `PresentationPhaseExtensions.IsInteractive()`는 `Settled`에 `true`를 돌려준다.
3. 연기 분기는 `SetRollInteraction(false)`를 타지 않고 곧바로 `return`한다. 코스믹 큐브와 수정구가 클릭 가능한 상태로 남는다.
4. `RollDice()`와 `UseAugmentAction()`의 재굴림 분기는 `smokeRoutine`을 확인하지도 정지시키지도 않고 새 `rollRoutine`을 시작한다. `StartNewGame()`만 유일하게 두 코루틴을 모두 정지시킨다.

- **재현 경로**: `ScoreSelection` 단계에서 굴림 횟수가 남은 채 56 `dice-alchemy`를 사용하면 `smokeRoutine`이 0.6초 타임라인으로 돈다. 그 창 안에 굴림 트리거를 클릭하면 `RollDice()`가 연기를 멈추지 않고 새 굴림을 시작한다. 굴림 애니메이션이 매 프레임 주사위 `Transform`을 갱신하는 동안 연기 시퀀스가 `SyncFromAuthority` + `ApplyValuesToVisuals`로 같은 주사위를 최종값으로 스냅시킨다. 굴러가는 도중에 눈이 확정된다.
- **추가 노출**: `SetDieKept`도 `Phase != Settled`만 검사하므로, 연기가 눈을 가린 0.15~0.28초 구간에서 킵 토글이 가능하다. 플레이어가 보이지 않는 눈을 킵하게 된다. 이는 `M17-T9-1`이 지키려던 정보 은닉을 반대 방향에서 깨는 문제다.
- **수정**: `PresentationPhase`에 값을 추가하거나 `Arranging`을 재사용하는 대신, `StartNewGame`이 이미 쓰는 관용구인 `smokeRoutine` 핸들 검사를 입력 게이트에 맞췄다. `RefreshGameInteraction`의 `canRoll`에 `smokeRoutine == null`을 더하고, `CanInitiateRoll()`·`UseAugmentAction()`·`SetDieKept()`에 가드를 추가했다. 연기 시작 시 `SetRollInteraction(false)`를 부르고, 종료 시 `RefreshRollBudgetState()`를 `SetRollInteraction(CanInitiateRoll())`로 바꿨다(`SetRollInteraction`이 내부에서 `RefreshRollBudgetState`를 부르므로 기존 동작은 보존된다).
- **위상 값을 쓰지 않은 이유**: `Arranging`을 재사용하면 `HasCompletedRoll()`이 `true`가 되어 상태 표시줄의 `valuesSummary`가 주사위 값을 그려낸다. 연기로 가리는 동안 값을 숨기려는 연출의 목적과 정면으로 충돌한다. 새 열거형 값을 추가하는 쪽은 올바르지만 `Phase` 복원 시점을 타임라인 중간에 맞춰야 해 변경이 커진다.

### LOG-1. 프리셋 로딩 실패가 완전히 조용하다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Scripts/Dice/DicePresetCatalog.cs` — `AppendError`, 두 개의 `catch` 블록

- **문제**: 두 `catch`가 `AppendError()`로 메시지를 `LastError`에 누적하지만 **`LastError`를 읽는 코드가 코드베이스 전체에 없다.** 유일한 소비자는 `AugmentedYachtController`의 `IsLoaded` 검사뿐이다.
- **결과**: 프리셋 JSON이 깨지면 `IsLoaded`가 `false`가 되고 컨트롤러는 조용히 폴백 카탈로그를 로드한다. 그것도 실패하면 클립 0개로 그냥 진행한다. 굴림 연출이 빠졌는데 콘솔에 아무 흔적이 없어 원인 추적이 불가능하다. 리뷰 기준 문서의 "catch 블록에서 예외를 로그도 남기지 않고 무시"에 형태만 다를 뿐 실질적으로 해당한다.
- **수정**: `AppendError`에서 `Debug.LogWarning`을 함께 호출한다.

### LOG-2. 굴림마다 무조건 로그와 문자열 할당이 발생한다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtDiceRoundPresenter.cs` — `PlayRoll`

- **문제**: `Debug.Log`는 조건부 컴파일이 아니라 **인자가 항상 먼저 평가된다.** 굴림마다 `List<int>` 두 개와 그 루프, `string.Join` 세 번, 색 마크업 보간 문자열이 릴리스 빌드에서도 할당된다. 12라운드 × 3굴림 × 2인이면 한 게임에 72회다.
- **수정**: 로그와 로그 전용 지역변수를 `[System.Diagnostics.Conditional("UNITY_EDITOR")]`가 붙은 `LogRollTrace`로 옮겼다. 이러면 빌드에서 호출 자체가 사라져 인자 평가도 함께 없어진다. `Debug.isDebugBuild` 검사로는 인자 평가를 막지 못하므로 부족하다. `using System.Diagnostics;`를 넣지 않고 속성을 완전한 이름으로 쓴 이유는 그 using이 `Debug`를 `UnityEngine.Debug`와 모호하게 만들기 때문이다.

### TEST-1. 진행형 증강의 가드 두 개가 테스트에 닿지 않는다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Logic/Augments/Quest/EveryLittleCounts.cs`, `Assets/Editor/YachtQuestAugmentTests.cs`

- **문제**: `AfterScoreCommit`에는 가드가 둘 있다. 보상 후 카운트를 멈추는 `if (!state.Rewarded)` 래치와, 스크래치 턴의 눈금을 세지 않는 `if (baseScore == 0) return 0;`이다. 기존 테스트는 4개+3개로 7개에 도달해 15점을 받는 정상 경로 하나만 검증한다. 두 가드 중 어느 쪽도 지나지 않는다.
- **결과**: 리팩토링 중 래치가 사라지면 문턱을 넘은 뒤에도 계속 15점씩 받고, 스크래치 가드가 사라지면 스크래치한 턴의 눈금까지 세어 조기 도달한다. **두 경우 모두 기존 테스트는 통과한다.** SOLID 리팩토링이 Phase 2~4의 여덟 태스크를 남겨 둔 상황이라 가상의 위험이 아니다.
- **수정**: `EveryLittleCounts_StopsCountingAfterRewardIsGranted`와 `EveryLittleCounts_DoesNotCountOnesFromScratchedTurn` 두 테스트를 추가했다.

### PAT-1. 구독자가 없는 이벤트 세 건 (수정함)

- **중요도**: Low
- **위치**: `Assets/Scripts/Tabletop/RunicSlateMatrix.cs`(`StateChanged`), `RollOrb.cs`·`RollCosmicCube.cs`(`OnClicked`)

- **문제**: 세 클래스가 `public event Action`을 선언하고 내부에서 `?.Invoke()`까지 하지만 구독(`+=`)하는 코드가 `Assets/Scripts`·`Assets/Editor` 어디에도 없다. 실제 통신은 다른 경로로 이뤄진다. 상태 조회는 `AugmentedYachtController`가 프로퍼티를 `Func<string>`으로 감싸 디버그 패널에 넘기고, 클릭은 `YachtInputRouter`의 별도 레이캐스트가 처리한다.
- **결과**: 동작은 멀쩡하다. 다만 나중에 이 이벤트를 "이미 있는 통신 채널"로 오인해 구독하면 아무도 발화시키지 않는 조용한 버그가 된다.
- **수정**: 세 이벤트의 선언과 `Invoke` 호출을 삭제했다. 그 결과 `RunicSlateMatrix.cs`와 `RollOrb.cs`에서 쓰이지 않게 된 `using System;`도 함께 제거했다(`RollCosmicCube.cs`는 `StringComparison`이 남아 유지).

## 보류한 항목

### RES-1. `Assets/Resources` 4.5MB / 95파일이 전량 상주한다

- **중요도**: Medium
- **위치**: `Assets/Resources/`(`AugmentIcons`, `AugmentScrolls/{Materials,Meshes,Previews}`, `Vfx`), `Resources.Load` 호출 7건

- **문제**: `Resources` 폴더는 참조 여부와 무관하게 **전량 빌드에 포함**되고 앱 시작 시 인덱스가 메모리에 올라간다. `Resources.UnloadUnusedAssets`·`Resources.UnloadAsset`·Addressables 호출은 0건이다. 일반 모드에서 증강 아이콘 45개가 전혀 필요 없는데도 빌드에 따라온다.
- **계획서와의 관계**: M7 완료 조건 중 "일반 모드의 증강 전용 리소스 미생성"은 **코드 수준에서만 성립하고 빌드 수준에서는 성립하지 않는다.** 런타임이 생성하지 않을 뿐 에셋은 포함된다.
- **지금 고치지 않는 이유**: 현재는 단일 씬이라 실질 피해가 없다. 4.5MB 규모에서 Addressables 전환은 비용이 이득을 넘는다. 씬 전환이 생기는 M18 시점에 로딩 방식을 한 번에 정하는 편이 맞다. 그때 `RES-2`도 함께 처리한다.
- **다시 볼 시점**: M18 착수 시. 보안 렌즈 재적용 시점과 같다.

### RES-2. 같은 아이콘을 한 곳은 캐시하고 한 곳은 매번 로드한다

- **중요도**: Low
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `YachtTurnFlowPresenter.cs`

- **문제**: 둘 다 `AugmentIcons/{id}`를 로드한다. 프레젠터는 `stickerIcons` 딕셔너리로 캐시하는데 카드 뷰는 `Bind()` 호출마다 경로 문자열을 보간해 다시 로드한다. `Resources.Load`는 이미 로드된 에셋을 반환하므로 디스크 재접근은 없지만 경로 보간이 매번 GC 할당을 만든다.
- **더 큰 문제는 일관성이다**: 같은 리소스의 로딩 정책이 두 벌이라 나중에 로딩 방식을 바꿀 때 두 곳을 따로 고쳐야 한다.
- **지금 고치지 않는 이유**: 증강 아이콘 조회를 한 곳으로 모으는 것이 옳지만 그 위치는 `AugmentPixelIconFactory` 주변이 자연스럽고, 이는 이번 리뷰 범위 밖의 구조 변경이다. `RES-1`과 함께 로딩 방식을 정할 때 한 번에 처리하는 편이 낫다.

### LOG-3. 시작 정보 로그에 가드가 없다

- **중요도**: Low
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs`

`Debug.Log($"Preset Catalog loaded: ...")`가 빌드에서도 남는다. 1회성이라 실질 피해가 없어 손대지 않았다.

## 렌즈별 결과 요약

| 렌즈 | 지적 | 상태 |
|---|---|---|
| 동시성·비동기 | `ASYNC-1` | High 1건. 수정함 |
| 로깅·예외 | `LOG-1`, `LOG-2`, `LOG-3` | Medium 2건 수정, Low 1건 보류 |
| 테스트 용이성 | `TEST-1` | Medium 1건. 수정함 |
| 리소스·에셋 로딩 | `RES-1`, `RES-2` | Medium 1건·Low 1건. 둘 다 M18로 보류 |
| 디자인 패턴 | `PAT-1` | Low 1건. 수정함 |
| 보안·치팅 방지 | 없음 | 해당 사항 없음. M18에 재적용 |
