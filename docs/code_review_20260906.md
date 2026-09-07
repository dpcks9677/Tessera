# 코드 리뷰 (2026-09-06)

`CodeReviewGuide.md` 기준으로 진행한 리뷰다. **여기서 고친 것도 커밋하지 않았다.** 채택 여부는 사용자가 정한다.

지적 사항 중 새로 들어온 코드(`M16`)의 세 건은 이번 세션에서 함께 고쳤고, 각 항목에 무엇을 어떻게 고쳤는지 적었다. 나머지는 손대지 않았다.

## 리뷰 범위와 한계

| 항목 | 내용 |
|---|---|
| 기준 커밋 | `26953db` (M16 셀 셰이딩 전환) |
| 중점 대상 | `M16`에서 새로 쓴 코드(자기 리뷰), 매 프레임 도는 입력·프레젠테이션 경로, 주사위 판정 로직, `M15` 프리셋 베이커 |
| 코드 규모 | `Assets/Scripts` 약 24,000줄. 전수 리뷰가 아니다 |
| 실행 검증 | 리뷰 시점에는 없었다. 이후 Unity에서 컴파일 오류 0, Tessera EditMode 121/121 통과를 확인했다. 지적 사항 자체는 정적 읽기로 찾은 것이다 |

전수 리뷰가 아니므로 "지적 사항 없음"이 "문제 없음"을 뜻하지 않는다. 아래에 적지 않은 파일은 대부분 읽지 않았다.

## 총평

프레젠테이션 계층 분해(`M11`)와 상태 소유권 정리가 실제로 자리를 잡았다. `public` 가변 필드가 하나도 없고, 이벤트 구독은 `Ensure*` 메서드의 null 가드 안에 들어 있어 중복 등록이 생기지 않는다. 매직 넘버는 `DiceBoardMetrics`·`TesseraPixelPalette` 같은 곳으로 모여 있고, 왜 그 값인지가 주석에 남아 있다. 아래 지적은 대부분 새로 들어온 코드에 몰려 있다.

가장 시급한 것은 `M16-1`이다. 런타임에 렌더러 피처를 껐다가 플레이 모드를 나가면 그 상태가 에셋에 남는다.

## 발견된 문제

### M16-1. SSAO 토글이 렌더러 에셋에 영구히 남는다 (수정함)

- **중요도**: High
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtLightingRig.cs` — `SetAmbientOcclusionEnabled`

```csharp
foreach (ScriptableRendererFeature feature in Resources.FindObjectsOfTypeAll<ScriptableRendererFeature>())
{
    if (feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
    feature.SetActive(enabled);
}
```

- **문제**: `ScriptableRendererFeature.m_Active`는 `[SerializeField]`다(URP 17.3 `ScriptableRendererFeature.cs:14`). 씬 오브젝트와 달리 **에셋의 런타임 변경은 플레이 모드를 나가도 되돌아오지 않는다.** 게다가 `Resources.FindObjectsOfTypeAll`은 로드된 모든 피처를 준다. `PC_Renderer`뿐 아니라 `Mobile_Renderer`의 SSAO까지 함께 꺼진다.
- **결과**: Cel 모드로 둔 채 플레이를 멈추면 SSAO가 꺼진 상태로 `PC_Renderer.asset`·`Mobile_Renderer.asset`에 저장된다. 이후 Baseline으로 돌려도 SSAO가 돌아오지 않는다. 즉 **M16의 핵심 계약인 "토글을 되돌리면 기준선 화면이 그대로 돌아온다"가 깨진다.** 실수로 커밋되면 원인을 찾기 어려운 조용한 회귀가 된다.
- **수정 제안**: 에셋을 건드리지 않는 방향으로 바꾼다. 세 가지 중 하나.
  1. SSAO 피처 자체에 `PixelEdgeRendererFeature.CelOverrideEnabled`와 같은 런타임 덮어쓰기 경로를 두고, `AddRenderPasses`에서 Cel일 때 패스를 건너뛴다. `PixelEdgeRendererFeature`가 이미 쓰는 방식이라 새 개념이 아니다.
  2. `SetActive` 호출 전 원래 값을 기억했다가 `OnDestroy`/`OnApplicationQuit`에서 반드시 되돌린다. 플레이 모드 강제 종료에는 취약하다.
  3. Cel 전용 URP 에셋을 하나 더 만들고 `QualitySettings.renderPipeline`을 갈아 끼운다. 가장 확실하지만 에셋이 두 벌이 된다.

  최소한 대상 필터를 `PC_Renderer`의 피처로 좁혀야 한다. 지금은 `GetType().Name` 문자열 비교라 어느 렌더러 소속인지 구분하지 않는다.

- **적용한 수정**: 2번을 택했다. 1번이 더 깨끗하지만 SSAO는 URP 내장 피처라 우리 쪽에서 덮어쓰기 경로를 넣을 수 없다.
  - `ambientOcclusionOriginalStates` 사전에 끄기 전 `isActive`를 기억하고, Baseline 복귀와 `OnDisable`에서 되돌린다.
  - `OnDisable`은 플레이 모드를 나갈 때도 불리므로 정상 종료 경로는 모두 덮인다.
  - **남는 위험**: 에디터가 강제 종료되거나 스크립트 리로드 중 예외로 죽으면 되돌리지 못한다. `Mobile_Renderer`의 SSAO까지 함께 끄는 것도 그대로다. 완전히 없애려면 3번(Cel 전용 URP 에셋)이 필요하다.

### M16-2. 검증 도구가 매 에디터 틱마다 1920x1080 텍스처를 새로 만든다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Editor/RunPixelReadabilityValidation.cs` — `Capture`

```csharp
Texture2D full = new(target.width, target.height, TextureFormat.RGBA32, false);
...
full.ReadPixels(...);
Color32[] source = full.GetPixels32();
Object.DestroyImmediate(full);
```

- **문제**: 굴림 중 프레임 변화율을 재는 단계(phase 3·4)에서 이 함수가 `EditorApplication.update`마다 불린다. Baseline은 렌더 타깃이 1920x1080이므로 호출마다 약 8MB 텍스처 생성 + `ReadPixels`(GPU→CPU 동기 스톨) + `GetPixels32`(또 한 벌 복사)가 일어난다.
- **결과**: 측정 중 프레임 레이트가 크게 떨어진다. 그런데 이 도구가 재는 지표가 바로 "프레임 간 변화 셀 비율"이라, **측정 행위가 측정 대상을 흔든다.** 프레임이 느려지면 프레임 사이 주사위 이동량이 커져 변화율이 실제보다 높게 나온다. Baseline 쪽이 캡처 비용이 훨씬 크므로 편향도 한쪽으로만 생긴다.
- **수정 제안**: `Texture2D`를 렌더 타깃 크기별로 한 번만 만들어 재사용하고, 캡처 주기를 고정 간격(예: 0.1초)으로 제한한다. 두 연출 방식이 같은 간격으로 캡처해야 비교가 성립한다. 또는 `AsyncGPUReadback`으로 스톨을 없앤다.
- **적용한 수정**: `captureBuffer`를 크기별로 재사용하고 `CaptureThrottled`가 `CaptureIntervalSeconds = 0.1`마다만 캡처하도록 했다. 종료·실패 경로에서 버퍼를 놓아 준다. `AsyncGPUReadback`은 넣지 않았다. 간격 제한만으로 스톨 빈도가 충분히 낮아지고, 비동기 경로는 프레임 짝맞춤을 새로 관리해야 한다.

### M16-3. 셀 재질이 정리되지 않는다 (수정함)

- **중요도**: Low
- **위치**: `Assets/Scripts/Rendering/CelStyleSwitcher.cs`, `Assets/Scripts/Rendering/CelMaterialFactory.cs`

- **문제**: `CelMaterialFactory.Create`가 만든 `Material`을 아무도 파괴하지 않는다. `CelStyleSwitcher`에는 정리 진입점 자체가 없고, `AugmentedYachtController.OnDestroy`도 `cameraRig`·`dicePool`만 정리한다. `DicePaletteCatalog.ClearCache`는 셀 캐시를 지우지만 이 스위처는 별도 캐시를 쓴다.
- **결과**: 플레이 모드를 반복하면 도메인 리로드 전까지 재질이 쌓인다. 개수는 씬의 Lit 재질 수만큼으로 유한해서 실사용에 지장은 없지만, 에디터가 누수 경고를 낼 수 있다.
- **수정 제안**: `CelStyleSwitcher`에 `Dispose()`를 두어 `celByOriginal`의 재질을 파괴하고 `AugmentedYachtController.OnDestroy`에서 부른다. `cameraRig?.Dispose()` 옆에 한 줄이면 된다.
- **적용한 수정**: 제안대로 했다. `Dispose()`는 원본 재질 복구까지 함께 하고 `currentStyle`을 `Baseline`으로 되돌린다.

### M16-4. 셀 전환 이후에 만들어진 렌더러는 변환되지 않는다

- **중요도**: Low
- **위치**: `Assets/Scripts/Rendering/CelStyleSwitcher.cs` — `ConvertToCel`

- **문제**: 전환 시점에 존재하는 렌더러만 훑는다. `AugmentedYachtController.RegenerateTableSurfaces`(에디터 메뉴)나 증강 카드 생성처럼 이후에 만들어지는 오브젝트는 Cel 모드인데도 Lit 재질을 그대로 쓴다.
- **결과**: 한 화면에 두 셰이딩이 섞인다. 스크린샷 비교의 신뢰도가 떨어진다.
- **수정 제안**: 지금 범위에서는 문서에 한계로 적어 두는 것으로 충분하다고 본다. 고친다면 `Apply`를 다시 부르는 진입점을 카드 생성 후에 두는 편이 렌더러 생성 지점마다 분기를 넣는 것보다 낫다.

### INPUT-1. 증강 카드 포인터가 매 프레임 배열을 할당한다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtInputRouter.cs:102`

```csharp
RaycastHit[] hits = Physics.RaycastAll(ray, PointerRayDistance);
```

- **문제**: `Physics.RaycastAll`은 호출마다 새 배열을 반환한다. `Update` → `PollAugmentCardPointer` 경로라 증강 모드에서 드래프트 중이 아닐 때 매 프레임 돈다.
- **결과**: 매 프레임 GC 할당이 쌓여 주기적인 GC 스파이크를 만든다. 픽셀 필터가 프레임 시간에 민감한 화면이라 체감될 수 있다.
- **수정 제안**: 같은 파일 `PollDicePointer`가 이미 비할당 방식(`Physics.Raycast(ray, out RaycastHit hit, ...)`)을 쓴다. 카드도 최근접 하나만 필요하므로 같은 형태로 바꾸거나, 버퍼를 필드에 두고 `Physics.RaycastNonAlloc`을 쓴다.

  ```csharp
  private readonly RaycastHit[] pointerHits = new RaycastHit[8];
  ...
  int count = Physics.RaycastNonAlloc(ray, pointerHits, PointerRayDistance);
  ```

  다만 `RaycastAll`은 정렬을 보장하지 않으므로 지금 코드도 "가장 앞" 카드를 고르지 않는다. 비할당으로 바꾸는 김에 거리 비교를 넣는 편이 낫다. 지금은 겹친 카드에서 뒤쪽이 잡힐 수 있다.

- **적용한 수정**: 제안대로 했다.
  - `pointerHits` 고정 버퍼(16칸)와 `Physics.RaycastNonAlloc`으로 바꿔 매 프레임 할당을 없앴다.
  - 첫 히트에서 `break` 하던 것을 `distance` 최소값 선택으로 바꿔 겹친 카드에서 앞쪽이 잡히게 했다. 함께 지적한 두 번째 문제도 여기서 사라진다.
  - `RaycastNonAlloc`은 버퍼가 넘치면 잘리고 잘리는 순서도 거리순이 아니다. 카드가 16장 넘게 한 광선에 겹칠 일은 없지만, 그 전제가 깨지면 조용히 틀린 카드를 고르게 된다. 버퍼 크기 주석에 그 전제를 적어 두었다.

### BAKE-1. 프리셋 초기 회전이 SO(3) 균등 분포가 아니다

- **중요도**: Low
- **위치**: `Assets/Editor/DicePresetBakeRig.cs` — `RandomRotation`

```csharp
return Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f));
```

- **문제**: 오일러 각 세 개를 균등 추출하면 회전 자체는 균등하지 않다. 극 근처 자세가 과대 표집된다.
- **결과**: 게임 공정성에는 영향이 없다. 눈은 게임 난수가 정하고 `DiceMaterialFactory.ApplyPredictedTopValue`가 착지 후 재질을 재매핑하므로, 이 회전은 굴러가는 모습의 다양성에만 관여한다. 다만 클립 20종의 궤적이 실제보다 덜 다양해질 수 있다.
- **수정 제안**: 지금 문제로 삼을 근거가 약하다. 클립 다양성이 부족하다고 판단될 때만 `UnityEngine.Random.rotationUniform`에 해당하는 균등 사원수 추출로 바꾼다. 주석에 "원본 웹 베이커와 같은 범위"라고 적혀 있으므로 원본과의 동등성을 깨는 변경이 되는 점도 함께 고려해야 한다.

### SCENE-1. `[ExecuteAlways]` 프롭이 씬을 계속 더럽혀 저장이 깨끗하지 않다

- **중요도**: Medium
- **위치**: `Assets/Scripts/Tabletop/CozyCandleStand.cs:15`, `Assets/Scripts/Tabletop/RollCosmicCube.cs:21`

- **문제**: 두 프롭이 `[ExecuteAlways]`라 편집 모드에서도 `Update`가 돌며 불꽃 흔들림과 큐브 회전을 트랜스폼에 쓴다. 그 값이 프리팹 인스턴스 오버라이드로 잡혀 씬이 늘 더러운 상태가 된다.
- **결과**: 세 가지가 따라온다.
  1. 씬을 저장할 때마다 관련 없는 트랜스폼 잡음이 diff에 섞인다. 이번 세션에서 `PixelEdgeCamera` 표시 하나를 저장하려 했더니 촛대와 코스믹 큐브의 회전·스케일 오버라이드 20여 줄이 함께 딸려 왔다.
  2. `test_run`이 "dirty scene"을 이유로 거부한다. 실제로 이번에 테스트 실행이 한 번 막혔다.
  3. 진짜 씬 변경이 잡음에 묻혀 리뷰가 어려워진다.
- **수정 제안**: 세 가지 중 하나.
  1. 편집 모드에서는 애니메이션을 돌리지 않는다. `Update` 앞에 `if (!Application.isPlaying) return;`을 두면 된다. `[ExecuteAlways]`를 둔 이유가 편집 모드 미리보기라면 이 방법은 그 목적을 없앤다.
  2. 애니메이션 결과를 트랜스폼이 아니라 `MaterialPropertyBlock`이나 셰이더 시간 항으로 옮긴다. 직렬화되지 않으므로 씬이 더러워지지 않는다.
  3. 애니메이션 대상 트랜스폼을 프리팹 인스턴스 바깥의 런타임 오브젝트로 분리한다.

  2번이 연출을 유지하면서 원인을 없앤다. 다만 불꽃 흔들림이 스케일 애니메이션이라 셰이더로 옮기려면 메시 쪽 작업이 필요하다.
- 이번 리뷰에서는 고치지 않았다. 연출 코드라 변경 범위 판단이 필요하다.

### DOC-1. 마일스톤 요약 표의 완료 상태가 실제와 다르다

- **중요도**: Low
- **위치**: `docs/augmented_yacht_work_plan.md` §6

- **문제**: `M12`(픽셀 필터 엣지 검출)와 `M13`(픽셀 필터 색 양자화)이 `TODO`인데, 두 마일스톤의 하위 작업은 `M13-T4`를 빼고 전부 `DONE`이고 커밋도 끝났다(`ad1aabf`, `7adea73`, `4183e1b`, `56ae273`).
- **결과**: §2 현재 진행 포인터와 §6 요약 표가 어긋나 다음 담당자가 상태를 잘못 읽는다.
- **수정 제안**: `M12`는 `DONE`, `M13`은 `M13-T4`가 `DEFERRED`이므로 `DONE`으로 올리거나 `DOING`으로 명시한다. 이번 리뷰에서는 고치지 않았다. 마일스톤 상태는 사용자 판단 영역이다.

## 설계 구조 관찰

`CodeReviewGuide.md`가 요구하는 수준의 구조적 문제는 찾지 못했다. 대신 관찰 두 가지만 남긴다.

### 관찰 1. `Assets/Scripts/Tabletop/`의 프롭 클래스가 크다

`RollCosmicCube`(1,282줄), `RunicSlateMatrix`(1,199줄), `RollOrb`(1,084줄)이 각각 메시 생성, 재질 생성, 애니메이션, 상태 표시를 한 클래스에서 다룬다. 단일 책임 관점에서는 분리 대상으로 보인다.

다만 지금 분리를 권하지는 않는다. 이유는 셋이다.

- 세 클래스 모두 "절차적으로 만들고 스스로 애니메이션하는 하나의 프롭"이라는 한 가지 개념이다. 메시 생성과 애니메이션이 같은 매개변수를 공유해서, 나누면 그 매개변수를 넘기는 통로가 새로 생긴다.
- `M9`에서 이미 프리팹으로 구워 두어, 런타임에는 대부분 코드가 돌지 않는다.
- `M16-T7`이 이 셰이더들을 손볼 예정이다. 그 결과에 따라 어떤 코드가 남을지가 달라진다.

`M16-T7` 이후 다시 판단하는 것이 낫다.

### 관찰 2. 두 경로 유지 비용이 예정대로 발생하고 있다

`M16`이 Baseline과 Cel 두 경로를 살려 둔 것은 의도된 결정이다(`D-034`). 그 대가로 `DicePaletteCatalog`에 캐시가 두 벌 생겼고, `CelStyleSwitcher`가 원본 재질을 들고 있어야 하며, `M16-3`의 정리 누락도 여기서 나왔다.

채택이 확정되면 미루지 말고 정리 마일스톤을 잡는 편이 좋다. 문서에도 그렇게 적혀 있다.

## 잘 되어 있는 점

- `public` 가변 필드가 없다. 프로퍼티도 대부분 읽기 전용이다.
- 이벤트 구독이 `Ensure*`의 null 가드 안에 있어 중복 등록이 구조적으로 막혀 있다. `turnFlow`처럼 외부에서 다시 만들어질 수 있는 것만 `OnDestroy`에서 해지한다.
- 매직 넘버가 파생 상수로 정리돼 있고 근거가 주석에 남아 있다. `DiceBoardMetrics.TrayVisualY`가 배율과 바닥 높이의 관계를 식으로 고정한 것이 좋은 예다.
- `YachtDiceFacts.From`이 눈 범위를 검사하고 한국어 메시지로 던진다. 계산 전에 전제를 확인하는 형태다.
- `DicePresetBaker`가 `try`/`finally`로 프리뷰 씬과 임시 오브젝트를 정리한다.

## 캐시 효율 항목

`CodeReviewGuide.md`의 우선 조건(대량 데이터를 매 프레임 처리, 프로파일링으로 병목 확인)에 해당하는 코드를 찾지 못했다. 프로파일링 근거가 없으므로 추측으로 캐시 미스를 지적하지 않는다.

`DicePresetBakeRig`가 대량 물리 시뮬레이션을 돌지만 에디터 전용 일괄 작업이고, 실행 시간이 문제로 보고된 적이 없다.
