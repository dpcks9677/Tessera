# 개선 트랙 세션 로그

> **문서 종류**: 시점 기록
> 최신 항목이 위에 옵니다. 각 항목은 **작성 당시의 사실**이며 이후 코드 변경을 반영하지 않습니다.
> 2026-10-22 이전 항목은 옛 `docs/agent/session_log.md`에서 옮겼습니다. 양식은 [`process.md`](../../process.md) §5.3을 따릅니다.

<!-- 양식
### YYYY-MM-DD — <작업자> (<작업 ID> <요약>)

- 작업 ID:
- 변경:
- 검증:
- 다음 작업:
-->

---

### 2026-09-20 — Claude (LOAD-03 정적 TMP 아틀라스 캐시 가드 보강)

- 작업 ID: `LOAD-03`
- 경위: LOAD-01 문서 갱신 뒤 `origin/feature/m17-augmented-hotseat`를 머지했다. 머지로 `Tessera.Editor.Tests` 네임스페이스 테스트가 333개에서 349개로 늘고(코인 토스 `M17-T24` 관련 신규 테스트, `CoinSpinPresetTests` 8개 포함) 실행 순서가 바뀌면서, 아침에 등재만 하고 「긴급도 낮음」으로 미뤄 뒀던 LOAD-03 결함이 필터 실행에서도 상시 재현되기 시작했다. `AugmentCardViewTests.QuestCard_*` 5건이 `MissingReferenceException: The object of type 'UnityEngine.Texture2D' has been destroyed`로 실패했고 2회 반복에서 동일하게 재현됐다. 기본 검증 게이트를 빨간불로 만드는 문제라 사용자 판단으로 푸시 전에 먼저 고쳤다
- 수정: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`의 `LoadProgressFont()` 캐시 가드만 변경. `if (progressFont != null) return progressFont;`를 `if (progressFont != null && progressFont.atlasTexture != null) return progressFont; progressFont = null;`로 바꿔, 폰트 에셋은 살아 있지만 동적 아틀라스 `Texture2D`만 파괴된 상태를 걸러내고 새로 굽게 했다. 다른 파일은 손대지 않았고 새 테스트도 추가하지 않았다. 기존 `QuestCard_*` 5건이 재현 테스트 역할을 했다
- 검증: 컴파일 에러 0. 필터 실행(`testMode=EditMode`, `filter=Tessera.Editor.Tests`) 2회 모두 총 349개, 통과 349, 실패 0, 스킵 0. 직전에 실패하던 5건이 전부 통과로 바뀌었다. 패키지 테스트 혼입 없음
- 같은 머지에서 함께 처리한 것: `Assets/Editor/CoinSpinPresetTests.cs`가 origin에서 네임스페이스 없이 들어와 `Tessera.Editor.Tests` 필터에서 통째로 빠지는 상태였다. 커밋 `dfe6b8a`의 네임스페이스 통일 대상에서 누락된 것으로 보고 `namespace Tessera.Editor.Tests`로 감쌌다. 이 래핑 덕분에 해당 8개 테스트가 디스커버리·실행 목록에 들어온 것을 확인했다
- 머지 충돌 4건 해소: `session_log.md`(양쪽 항목 모두 보존, 역순 정렬 유지), `docs/reference/augments_specification_and_status.md`(origin의 개수 표기와 로컬의 `augments.json` 삭제 사실을 결합), graphify 라벨 2개(`PYTHONHASHSEED=0 graphify update .` 재생성본 채택)
- 머지가 `.claude/hooks/korean-guard.js`를 삭제하고 `korean-guard.py`·`main-session-guard.py`를 되살리며 `settings.json`의 훅 커맨드를 `node`에서 `python`으로 되돌렸다. origin 쪽이 나중 변경이라 그대로 받았다
- 검증 중 Unity Editor 메인 스레드가 다시 멈춰 사용자가 직접 해제해야 했다(머지로 C# 55개가 한꺼번에 바뀐 직후). `mainThreadIdleMs`가 190초→212초로 단조 증가하고 `queuedRequests`가 5건에서 정체됐다
- 남은 것: `Assets/Editor/CoinSpinPresetBaker.cs`와 `Assets/Editor/CoinSpinPresetTests.cs`가 `Assets/Editor/` 루트에 있다. 폴더 분리 규칙대로면 각각 `Assets/Editor/Dice/`, `Assets/Editor/Tests/Dice/`다. Unity가 켜진 상태에서 파일 이동이 위험해 미뤘다
- 시각 확인 생략, 사유: 폰트 캐시 가드 수정이라 테스트로 판정 가능
- 변경 파일: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `Assets/Editor/CoinSpinPresetTests.cs`, `docs/agent/session_log.md`, `docs/reference/augments_specification_and_status.md`, `graphify-out/.graphify_labels.json` 및 대응 `.sig`, `.claude/hooks/main-session-guard.py`, `.claude/hooks/korean-guard.py`, `.claude/settings.json`, `docs/agent/improvement_tasks_spec.md`
- 다음 작업: `M17-T23-7` 동전 시각 재점검

### 2026-09-20 — Claude (LOAD-01 런타임 에셋 로딩 카탈로그화)

- 작업 ID: `LOAD-01`
- 문제: `AugmentCardView.cs`, `AugmentedYachtController.cs`, `InkwellAndQuill.cs`, `ParchmentScoreSheet.cs`, `YachtHudFactory.cs` 5개 런타임 스크립트가 `#if UNITY_EDITOR` + `AssetDatabase.LoadAssetAtPath`로만 에셋을 로드해 플레이어 빌드에서 null이 되는 결함. `TabletopSurfaceBuilder.cs`는 조사 결과 에디터 전용 베이크 도구(`[ContextMenu]` + `if (Application.isPlaying) return;`)로 밝혀져 범위에서 제외
- 구현: 신규 `Assets/Scripts/Core/RuntimeAssetLibrary.cs`(`Tessera.Core`)가 `Resources/RuntimeAssetLibrary.asset` 하나를 `Resources.Load`로 캐시해 폰트 3종(Mulmaru 한글, alagard·m6x11 라틴), 양피지 텍스처 3종, 점수 아이콘 스프라이트 배열을 정적 프로퍼티로 노출하고 `FindScoreIcon(string)`을 제공. 신규 `Assets/Editor/Tools/RuntimeAssetLibraryBaker.cs`(`Tools/Tessera/Bake Runtime Asset Library`)로 카탈로그를 굽는다. 신규 `Assets/Editor/Tests/RuntimeAssetLibraryTests.cs` 5개 추가. 호출부 3곳(`AugmentCardView.LoadFont`, `YachtHudFactory`, `ParchmentScoreSheet`의 텍스처·폰트·아이콘) 교체, 폰트 폴백 순서와 마지막 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 보존
- 검증: 최종 판정은 필터 실행(jobId c043c411, `testMode=EditMode`, `filter=Tessera.Editor.Tests`)으로 총 333개, 통과 333, 실패 0, 스킵 0, 패키지 테스트 혼입 0건. `AugmentCardViewTests` 63개, `RuntimeAssetLibraryTests` 5개, `ScoreSheetColumnLayoutTests` 전부 통과
- 여기 도달하기까지 두 라운드가 무효였다. ① 첫 라운드는 실행 창과 겹친 09:46:45에 GitHub Desktop이 작업 트리를 통째로 스태시해 무효. LOAD-01 파일이 빠진 채 측정됐을 가능성이 있었음. ② 둘째 라운드는 필터 없이 전체 1193개를 돌려 `AugmentCardViewTests` 63개 중 36개가 `MissingReferenceException: The object of type 'UnityEngine.Texture2D' has been destroyed`로 실패했으나 필터 실행에서는 재현되지 않음. ③ 원인 판정: 필터 없는 실행에서 `com.besty.unity-skills` 패키지 테스트 약 860개가 사이사이 돌며 에셋 생성·삭제·undo/redo를 수행하고, 이때 `AugmentCardView.cs:613`의 `static TMP_FontAsset progressFont`가 물고 있는 동적 TMP 아틀라스 `Texture2D`가 파괴됨. 가드가 `if (progressFont != null)` 하나뿐이라 "폰트 에셋은 살아 있고 아틀라스만 파괴된" 상태를 못 걸러냄. LOAD-01의 회귀가 아니라 기존 취약점이며 `LOAD-03`으로 별도 등재
- 저장소 사고: 작업 도중 GitHub Desktop이 `origin/feature/m17-augmented-hotseat`를 끌어오는 머지를 시작해 충돌 4개로 멈춘 상태가 발견됐고, 같은 시점에 작업 트리가 통째로 스태시돼 LOAD-01 파일이 트리에서 사라져 있었다. 사용자 판단으로 `git merge --abort` 후 `git stash pop`으로 복구
- 알게 된 도구 함정 2건, 둘 다 `.claude/agents/tessera-verifier.md` §3.1·§3.2에 기록. ① git 조작으로 디스크의 씬 파일이 바뀐 뒤에는 `asset_refresh`만으로 이미 열려 있는 씬이 갱신되지 않는다. `isDirty:false`라 겉보기엔 정상이지만 `octahedronDieModel`이 `null`로 조회됐다. `scene_load`로 명시적 재로드가 필요. ② Unity가 켜진 채로 `git stash` 등으로 로드된 에셋을 트리에서 빼면 Editor 메인 스레드가 멈춘다. `isCompiling:false`인데 `mainThreadIdleMs`가 254초→553초로 단조 증가하고 `queuedRequests`가 17→38로 쌓이기만 했다. REST로는 해소할 수 없어 사용자가 Editor 창에서 풀어야 했다
- 검증 방침 교정: `AugmentCardViewTests`를 포함한 판정은 필터 실행(`Tessera.Editor.Tests`, 333개, 약 3분) 기준으로 한다. 필터 없는 전체 실행(1193개, 약 8분)은 패키지 테스트와의 상호작용으로 거짓 실패를 낸다
- 후속 등재: `LOAD-03`(`AugmentCardView.LoadProgressFont`의 정적 TMP 아틀라스 캐시 가드 보강)
- 시각 확인 생략, 사유: 폰트·텍스처 로드 경로 교체라 테스트로 판정 가능하고 렌더 결과 자체는 변경 대상이 아님
- 변경 파일: `Assets/Scripts/Core/RuntimeAssetLibrary.cs`(신규), `Assets/Editor/Tools/RuntimeAssetLibraryBaker.cs`(신규), `Assets/Editor/Tests/RuntimeAssetLibraryTests.cs`(신규), `Assets/Resources/RuntimeAssetLibrary.asset`(신규), `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentCardView.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/ParchmentScoreSheet.cs`, `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtHudFactory.cs`, `Assets/Scenes/Augmented Dice.unity`, `docs/agent/improvement_tasks_spec.md`
- 다음 작업: `M17-T23-7` 동전 시각 재점검
