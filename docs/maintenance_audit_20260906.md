# 유지보수·설계·성능 점검 (2026-09-06, 2차)

사용자 지시로 진행한 프로젝트 전반 점검이다. 같은 날 이미 있던
[`docs/code_review_20260906.md`](code_review_20260906.md)는 `M16` 신규 코드와 매 프레임 경로에
범위를 좁힌 리뷰였고, 그 문서 스스로 "아래에 적지 않은 파일은 대부분 읽지 않았다"라고 밝혀 두었다.
이번 점검은 그 바깥, 특히 게임 코어·에셋 적재·프롭 생명주기를 본다.

**여기서 고친 것도 커밋하지 않았다.** 채택 여부는 사용자가 정한다.

## 1. 범위와 방법

| 항목 | 내용 |
|---|---|
| 기준 커밋 | `6dd46a0` (셀 셰이더 그림자·시선 벡터 버그 수정) |
| 읽은 범위 | `Assets/Scripts` 전체 목록 훑기 + `Core`·`Dice`·`Games/Yacht`·`Rendering` 정독, `Tabletop`·`Presentation`은 진입점과 `Update` 경로 위주 |
| 관점 | 유지보수성, 설계 패턴 적용점, 취약점, 성능 |
| 실행 검증 | Unity 컴파일 오류 0, Tessera EditMode 121/121 통과 (1차·2차 조치 후 각각 재실행). 자세한 결과는 10.8절 |
| 안 본 곳 | 셰이더 15종의 HLSL 본문, `Assets/Editor`의 베이커 구현 세부, 씬·프리팹 에셋 내용 |

`Assets/Scripts` 약 24,000줄, `Assets/Editor` 약 8,500줄이다. 전수 리뷰가 아니므로
"지적 사항 없음"이 "문제 없음"을 뜻하지 않는다.

## 2. 총평

구조는 건강하다. 권위 계층(`LocalGameAuthority`)과 프레젠테이션이 명령·이벤트로만 만나고,
증강 로직은 전략 패턴(시점 인터페이스 9종 + 카탈로그 + 디스패처)으로 분리되어 있으며,
증강별 회귀 테스트가 121개 있다. `public` 가변 필드가 없고 매직 넘버는
`DiceBoardMetrics`·`TesseraPixelPalette`로 모여 있다.

이번에 찾은 것은 대부분 **경계에서 새는 상태**다. 전역 난수를 덮어쓰고 되돌리지 않는 곳,
편집 모드에서 직렬화 대상에 계속 쓰는 곳, 씨앗 충돌로 두 난수열이 완전히 상관되는 곳,
그리고 로그 문자열의 부수 효과에 기대고 있는 워밍업이다. 넷 다 고쳤다.

가장 중요한 것은 `RNG-1`이다. 판정용 난수와 연출용 난수가 실제로 같은 수열을 쓰고 있었다.

## 3. 고친 항목

### RNG-1. 판정 난수와 연출 난수가 같은 씨앗을 받는다 (수정함)

- **중요도**: High
- **위치**: `Assets/Scripts/Games/Yacht/YachtGameCore.cs` — `SystemRandomSource()`

```csharp
public SystemRandomSource() : this(Environment.TickCount) { }
```

- **문제**: `Environment.TickCount`는 Windows에서 해상도가 약 15ms다. `LocalGameAuthority`
  생성자는 판정용 `random`과 연출용 `visualRandom`을 **연달아** 인자 없이 만든다.

  ```csharp
  this.random = random ?? new SystemRandomSource();
  this.visualRandom = visualRandom ?? new SystemRandomSource();
  ```

  두 줄 사이의 경과 시간은 마이크로초 단위이므로 `TickCount`가 거의 항상 같은 값을 준다.
  즉 두 `System.Random`이 **같은 씨앗으로 같은 수열**을 낸다.

- **결과**: 두 난수원을 나눈 설계 의도가 무효가 된다. 연출용 난수가 쓰이는
  드래프트 카드 프리셋 배정(`CreateCardPresetIds`)이 판정용 난수 호출 횟수의 결정적 함수가 된다.
  지금 당장 눈에 띄는 오동작은 아니지만, "판정과 연출은 독립"이라는 전제로 쓴 코드가
  실제로는 성립하지 않는 상태다. 이런 상관은 재현 시나리오를 만들 때 조용히 결과를 왜곡한다.

  보안 관점도 있다. 씨앗이 `TickCount`면 게임 시작 시각을 아는 쪽이 전체 주사위 수열을
  재현할 수 있다. 로컬 핫시트에서는 문제가 아니지만 `M18` 이후 권위 서버에서는 그대로 취약점이다.

- **적용한 수정**: 기본 씨앗을 `Guid.NewGuid().GetHashCode()`로 바꿨다. Guid는 같은 프로세스에서
  연달아 만들어도 서로 다르므로 두 난수원의 충돌이 사라진다.
- **남는 일**: Guid 해시도 암호학적 난수는 아니다. 권위 서버로 옮길 때는
  `System.Security.Cryptography.RandomNumberGenerator`로 씨앗을 뽑아야 한다. 주석에 남겨 두었다.
- **테스트 영향 없음**: 테스트는 모두 `new SystemRandomSource(seed)`로 씨앗을 명시한다.

### ZODIAC-1. 별자리 텍스처 베이킹이 Unity 전역 난수를 덮어쓴다 (수정함)

- **중요도**: Medium
- **위치**: `Assets/Scripts/Tabletop/ZodiacConstellationData.cs` — `BakeConstellationTexture`

```csharp
int seed = (int)def.type * 100 + 42;
UnityEngine.Random.InitState(seed);
```

- **문제**: `UnityEngine.Random`은 전역 정적 상태다. `InitState`는 그 상태를 통째로 덮어쓴다.
  이 함수는 별가루 80개 배치를 결정적으로 만들려고 씨앗을 고정하지만, 끝나고 되돌리지 않는다.
- **결과**: 이 함수를 부른 뒤 프로세스 전체의 `UnityEngine.Random` 호출이 이 씨앗에서 이어지는
  수열을 받는다. `AugmentedYachtController.WarmUpRollAssets`가 시작 시 12개 텍스처를 모두 굽고
  마지막 별자리의 씨앗이 남으므로, 이후 `BakedDiceController`의 굴림·충돌 사운드 선택과
  `TabletopTrinketBrooch`·`TabletopTrinketRing`의 흔들림 주파수가 매 실행 같은 값이 된다.

  게임 판정에는 영향이 없다. 판정은 `System.Random` 기반 `IRandomSource`를 쓰고 Unity 전역 난수를
  쓰지 않는다. 하지만 "전역 난수를 쓰면 조용히 결정적이 된다"는 함정이 코드에 남아 있는 상태다.

- **적용한 수정**: `UnityEngine.Random.state`를 `InitState` 전에 기억했다가 별가루 루프가 끝나면
  되돌린다. 생성되는 텍스처는 완전히 동일하다(같은 씨앗, 같은 호출 순서).

### SCENE-1. `[ExecuteAlways]` 프롭이 편집 모드에서 씬을 계속 더럽힌다 (수정함)

- **중요도**: Medium
- **위치**: `CozyCandleStand`, `RollCosmicCube`, `RollOrb`, `RerollCounterBar`, `HourglassTimer`의 `Update`

- **문제**: 이전 리뷰가 `CozyCandleStand`·`RollCosmicCube` 두 건으로 지적했던 것인데,
  실제로는 다섯 개다. 다섯 클래스 모두 `[ExecuteAlways]`이고 `Update`가 직렬화 대상에 쓴다.

  | 클래스 | 편집 모드에서 매 틱 쓰는 값 |
  |---|---|
  | `CozyCandleStand` | 불꽃 3개의 `localScale`·`localRotation`, `Light.intensity` |
  | `RollCosmicCube` | `localPosition`·`localRotation` 2개, 테서랙트 메시 정점 |
  | `RollOrb` | `Renderer.enabled` |
  | `RerollCounterBar` | 보석 3개의 `Light.intensity`·`Light.enabled` |
  | `HourglassTimer` | `isRunning`이 참일 때 모래 비주얼과 발광 색, 그리고 `OnTimerTick` 이벤트 발행 |

- **결과**: 이전 리뷰가 관측한 그대로다. 씬 저장마다 관련 없는 오버라이드 20여 줄이 diff에 섞이고,
  `test_run`이 "dirty scene"으로 막히며, 진짜 씬 변경이 잡음에 묻힌다.

  `HourglassTimer`는 추가 위험이 있다. `isRunning`이 `[SerializeField]`라 참인 채로 저장된 씬을
  열면 편집 모드에서 타이머가 실제로 흘러가고 `OnTimerExpired`까지 발행한다.

- **적용한 수정**: 다섯 `Update` 앞에 `if (!Application.isPlaying) return;`을 넣었다.
  `[ExecuteAlways]`는 그대로 둔다. 그 속성이 실제로 필요한 것은 `BuildGeometry` 컨텍스트 메뉴와
  `OnValidate`·`OnEnable` 미리보기지, `Update` 애니메이션이 아니다.
- **바뀌는 것**: 편집 모드에서 불꽃이 흔들리지 않고 큐브가 돌지 않는다. 프롭은 `M9`에서
  프리팹으로 구웠으므로 편집 모드 애니메이션 미리보기의 실질 가치는 낮다고 판단했다.
  되돌리려면 다섯 파일에서 한 줄씩 지우면 된다.

### CATALOG-1. 프리셋 워밍업이 로그 문자열의 부수 효과에 걸려 있다 (수정함)

- **중요도**: Medium (유지보수)
- **위치**: `Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs` — `InitializePresetCatalog`

```csharp
presetCatalog = DicePresetCatalog.LoadAll();          // 인덱스만 읽는다
if (!presetCatalog.IsLoaded) presetCatalog = DicePresetCatalog.LoadNormalFiveDice();
Debug.Log($"Preset Catalog loaded: {presetCatalog.NormalFiveDiceClipCount} clips available.");
```

- **문제**: `LoadAll()`은 `index.json`만 읽고 실제 프리셋 파일은 최초 사용 시 적재한다(주석에 명시).
  기본 5개 프리셋 파일은 약 230KB이고 `JArray.Parse`가 동기다. 첫 굴림에서 읽으면 그 프레임이 멈춘다.

  실제로는 멈추지 않는데, 그 이유가 **로그 문자열 보간이 `NormalFiveDiceClipCount`를 읽어
  `LoadFile`을 유발하기 때문**이다. 워밍업이 로그의 부수 효과로만 존재한다.

- **결과**: 릴리스에서 `Debug.Log`를 걷어내거나 로그 문구를 손보는 평범한 정리 작업이
  첫 굴림 프레임 스톨을 되살린다. 원인과 결과가 멀어서 추적하기 어려운 종류의 회귀다.

  바로 위 `WarmUpRollAssets`가 별자리 텍스처를 같은 이유로 미리 굽고 그 근거를 주석에 남겨 둔 것과
  대비된다. 같은 문제에 대한 처리가 한쪽은 명시적이고 한쪽은 암묵적이다.

- **적용한 수정**: 적재를 `int warmedClipCount = presetCatalog.NormalFiveDiceClipCount;`로 끌어내
  독립된 문장으로 만들고, 왜 여기서 읽는지를 주석에 적었다. 동작은 동일하다.
- **범위**: 기본 5개 파일만 미리 읽는다. `StreamingAssets` 전체가 34MB이고 나머지 파일은
  증강 구성에 따라 쓰일지 알 수 없으므로 전부 미리 읽는 것은 옳지 않다.

## 4. 1차에서 문서화만 한 항목

아래 일곱 건 중 다섯 건은 사용자 지시로 2차에서 조치했다. 각 항목 제목에 결과를 표시했고,
무엇을 어떻게 고쳤는지는 [10절](#10-2차-조치-사용자-지시)에 모아 두었다.

### ENCAP-1. 권위 상태가 살아 있는 참조로 새어 나간다 (2차에서 수정함)

- **중요도**: Medium
- **위치**: `LocalGameAuthority.CurrentState`, `YachtGameSession.State`, `YachtGameSession.GetPlayer`

- **문제**: `Execute`는 결과에 `state.Clone()`을 담아 스냅샷을 준다. 그런데 `CurrentState`는
  내부 `state` 객체를 그대로 돌려주고, `YachtGameSession.State`가 이를 그대로 노출한다.
  `YachtDieState`·`PlayerScoreData`는 필드가 전부 `public`이므로 프레젠테이션이
  `session.State.Dice[0].Value = 6`을 쓸 수 있다.
- **결과**: 명령·revision·중복 방지로 세운 권위 구조를 아무 데서나 우회할 수 있다.
  `M18` 네트워크 준비 검증에서 재동기화를 넣을 때 "누가 상태를 바꿨는지" 추적이 불가능해진다.
  `docs/yacht_state_ownership.md`가 세운 소유권 표와 실제 접근 가능성이 어긋난다.
- **수정 제안**: 세 가지.
  1. `CurrentState`를 `Clone()`으로 바꾼다. 가장 확실하지만 프레젠테이션이 매 프레임 읽는 곳이
     있으면 할당이 늘어난다. 먼저 호출 빈도를 세어 봐야 한다.
  2. 읽기 전용 뷰 인터페이스(`IReadOnlyYachtGameState`)를 두고 `CurrentState`가 그것을 돌려준다.
     할당이 없고 컴파일 시점에 막힌다. 인터페이스와 어댑터가 늘어난다.
  3. 지금 구조를 유지하고 "프레젠테이션은 상태를 읽기만 한다"를 규약으로만 둔다. 현 상태다.
- **판단**: `M18` 착수 전에 2번을 권한다. 지금 고치면 프레젠테이션 계층 전반을 건드려야 하므로
  `M17` 완료 후가 적기다.

### CMD-1. 처리한 명령 ID 집합이 무한히 자란다 (2차에서 수정함)

- **중요도**: Low (로컬) / Medium (`M18` 이후)
- **위치**: `LocalGameAuthority.acceptedCommandIds`

- **문제**: 수락한 모든 명령 ID를 `HashSet`에 영구 보관한다. 비우는 경로가 없다.
  `ResetGameState`도 이 집합은 건드리지 않는다.
- **결과**: 로컬 2인 게임 한 판은 수백 건이라 문제가 없다. 다만 게임을 여러 번 새로 시작해도
  누적되고, 원격 클라이언트가 명령 ID를 정하는 `M18` 이후에는 메모리가 클라이언트 입력에 따라
  무한히 자라는 구조가 된다.
- **수정 제안**: 게임 시작 시 비우거나, revision 기준 슬라이딩 윈도로 바꾼다.
  후자가 재전송 중복 방지의 표준 방식이다. `M18`에서 함께 다루면 된다.

### IO-1. `StreamingAssets`를 `File` API로 직접 읽는다 (해당 없음으로 확정)

- **중요도**: Medium (Android/WebGL 대상일 때) / 해당 없음 (스탠드얼론만일 때)
- **위치**: `DicePresetCatalog.LoadIndex`·`LoadFile`, `YachtAudioService`

- **문제**: `File.Exists` / `File.ReadAllText`로 `Application.streamingAssetsPath`를 읽는다.
  Android에서 이 경로는 APK 안의 압축 엔트리라 `File` API가 통하지 않고, WebGL은 파일 시스템 자체가 없다.
  두 플랫폼에서는 `UnityWebRequest`로 읽어야 한다.
- **현재 상태**: `YachtAudioService`는 이미 `UnityWebRequestMultimedia`를 쓰지만
  그 앞에서 `Directory.Exists`·`File.Exists`로 존재를 확인하므로 같은 문제에 걸린다.
- **결과**: 스탠드얼론과 에디터에서는 정상이다. 모바일 빌드를 만들면 프리셋과 사운드가
  통째로 로드되지 않는다. 오류가 아니라 조용한 빈 카탈로그로 나타나므로 원인 파악이 오래 걸린다.
- **수정 제안**: 대상 플랫폼이 확정되기 전에는 고치지 않는 편이 낫다. 스탠드얼론 전용이면
  현 코드가 더 간단하고 빠르다. 모바일을 넣기로 하면 `StreamingAssets` 접근을 어댑터 하나로 모으고
  `UnityWebRequest` 경로를 붙인다.
- **확인 필요**: `ProjectSettings`에 iPhone·Android·tvOS 아이콘 슬롯이 있으나 실제 빌드 대상 설정은
  스탠드얼론뿐이다. 모바일이 계획에 있는지 사용자 확인이 필요하다.

### GUARD-1. `RuntimeAssetGuard`가 에디터와 빌드에서 다르게 동작한다 (2차에서 수정함)

- **중요도**: Low (현재) / Medium (프롭을 복수 배치하면)
- **위치**: `Assets/Scripts/Core/RuntimeAssetGuard.cs` — `GetWritableMesh`, `IsAsset`

- **문제**: `IsAsset`은 `UNITY_EDITOR`가 아니면 항상 `false`를 돌려준다. 따라서 빌드에서는
  `GetWritableMesh`가 사본을 만들지 않고 `filter.sharedMesh`를 그대로 돌려준다.
- **결과**: 에디터에서는 프롭마다 자기 메시 사본을 갖고, 빌드에서는 같은 프리팹의 모든 인스턴스가
  하나의 메시를 공유한다. 현재 씬은 코스믹 큐브와 모래시계가 각각 하나뿐이라 드러나지 않는다.
  같은 프롭을 둘 이상 놓는 순간 두 인스턴스가 같은 메시 정점을 서로 덮어쓴다.
  에디터에서는 재현되지 않으므로 빌드에서만 나타나는 버그가 된다.
- **수정 제안**: 빌드에서도 "이 `MeshFilter`에 대해 이미 사본을 만들었는가"를 기준으로 한 번만
  복제하도록 바꾼다. `IsAsset` 대신 `HashSet<MeshFilter>` 또는 `Mesh` 인스턴스 ID 비교를 쓴다.
  지금 고치지 않는 이유는 인스턴스가 하나뿐이라 검증할 방법이 없어서다.
  프롭을 복수로 배치하는 작업이 생기면 그때 함께 다룬다.

### ZODIAC-2. 별자리 텍스처 캐시를 비울 때 텍스처를 파괴하지 않는다 (2차에서 수정함)

- **중요도**: Low (에디터 전용)
- **위치**: `ZodiacConstellationData.ClearCache`

```csharp
public static void ClearCache() { cachedTextures = null; }
```

- **문제**: 256x256 RGBA32 텍스처 12장(약 3MB)의 참조만 버리고 `Destroy`하지 않는다.
  `RollOrb.RebuildOrbGeometry`와 `RollCosmicCube.RebuildGeometry`가 이를 부르므로
  지오메트리를 다시 만들 때마다 3MB가 도메인 리로드 전까지 남는다.
- **왜 지금 고치지 않았나**: 단순히 `Destroy`를 넣으면 회귀가 생긴다. 캐시는 `RollOrb`와
  `RollCosmicCube`가 **공유**하고, `ClearCache`는 자식 파괴보다 **먼저** 불린다. 따라서
  구슬을 다시 만들면 코스믹 큐브가 아직 쓰고 있는 텍스처까지 파괴되어, 큐브가 자기 차례에
  다시 만들어질 때까지 별자리 면이 비게 된다.
- **수정 제안**: 두 갈래.
  1. 캐시를 참조 계수로 관리하고 마지막 사용자가 놓을 때만 파괴한다. 정석이지만 프롭 두 개에는 과하다.
  2. `ClearCache`를 없애고, 다시 굽는 쪽이 텍스처 내용만 덮어쓰도록(`SetPixels32` 재사용) 바꾼다.
     텍스처 객체가 유지되므로 다른 프롭의 참조도 깨지지 않고 할당도 사라진다. 이쪽을 권한다.

### PERF-1. 매 프레임 셰이더 프로퍼티를 문자열로 조회한다 (2차에서 수정함)

- **중요도**: Low (프로파일링 근거 없음)
- **위치**: `RollCosmicCube.Update`(약 12회), `RollOrb.Update`(약 10회),
  `RerollCounterBar.Update`(보석·리지 수에 비례, 약 60회), `HourglassTimer.Update`(2회)

- **내용**: `SetFloat("_Intensity", ...)` 형태가 매 프레임 돈다. 저장소 전체에서
  문자열 리터럴 셰이더 프로퍼티 접근이 348곳이고 `Shader.PropertyToID`는 4곳에서만 쓴다.
- **왜 고치지 않았나**: `CodeReviewGuide.md`와 이전 리뷰의 기준을 따른다. 호출당 비용이
  해시 + 사전 조회 수준이라 프레임당 수 마이크로초로 추정되고, 프로파일링으로 병목이 확인된 적이 없다.
  추측으로 82곳을 고치는 것은 `CLAUDE.md`의 "요청받지 않은 개선 금지"에 어긋난다.
- **다시 볼 조건**: 프로파일러에서 이 `Update`들이 실제로 잡히면, 그때 정적 `readonly int`
  프로퍼티 ID로 한 번에 바꾼다. `SCENE-1` 수정으로 편집 모드 호출이 사라져 부담은 이미 줄었다.

### PERF-2. 시작 시 별자리 텍스처 12장을 메인 스레드에서 굽는다 (사용자가 별도로 해소)

- **중요도**: Low (의도된 설계)
- **위치**: `AugmentedYachtController.WarmUpRollAssets`

- **내용**: 256x256 픽셀 12장, 픽셀마다 별가루 80개와 별자리 선에 대해 `Mathf.Exp`/`Mathf.Pow`를
  돈다. 약 786,000 픽셀이므로 시작 시 눈에 띄는 정지가 생길 수 있다.
- **판단**: 결함이 아니다. 주석에 "첫 `RollDice` 호출이 프레임을 점유하지 않도록"이라고
  근거가 적혀 있고, 비용을 시작 시점으로 옮긴 것은 옳은 선택이다. 다만 정지 시간을 실제로 잰 기록이 없다.
- **다시 볼 조건**: 시작 정지가 문제로 보고되면 `Texture2D.SetPixelData` + Job/Burst로 옮기거나,
  런타임 베이킹 대신 구운 텍스처 에셋으로 대체한다. 후자가 더 간단하다.

## 5. 이미 백로그에 있어 다루지 않은 항목

중복 기록을 피하려고 위치만 남긴다.

| 항목 | 기록 위치 |
|---|---|
| `ApplyAugment`의 증강 ID `if-else` 체인 11개 (`IOnAugmentSelected`로 이관 가능) | `augmented_yacht_work_plan.md` M8 잔여 항목 |
| `UseAugmentAction`의 수동 행동 ID 리터럴 5분기 | 같은 표 |
| `YachtAugmentRuntime.Definitions` 죽은 정의 테이블 27개와 팩토리 헬퍼 | 같은 표 |
| `YachtAugmentPlayerState`의 호환용 위임 프로퍼티 약 50개 | 같은 표 |
| `YachtGameRules.cs` 2줄 스텁 | 같은 표 |
| `Tabletop` 프롭 클래스 3개가 1,000줄 이상 | `code_review_20260906.md` 관찰 1 |
| Baseline/Cel 두 경로 유지 비용 | `code_review_20260906.md` 관찰 2, `D-034` |
| 셀 전환 이후 생성된 렌더러 미변환 (`M16-4`) | `code_review_20260906.md` |
| 프리셋 초기 회전이 SO(3) 균등이 아님 (`BAKE-1`) | `code_review_20260906.md` |

M8 잔여 항목 다섯 건은 이번 점검에서도 그대로 확인했다. 특히 `ApplyAugment`는
전략 패턴을 도입해 놓고 초기화만 옛 분기에 남긴 형태라, 증강을 추가할 때 두 곳을 봐야 한다.
`M17` 착수 전에 정리하는 편이 좋다.

## 6. 확인했지만 문제를 찾지 못한 곳

- **증강 계층 구조**: 시점 인터페이스 9종 + 카탈로그 + 디스패처 + 증강별 상태(`AugmentStateStore`)로
  나뉘어 있고, 새 증강 추가가 파일 1개와 카탈로그 1줄로 끝나는 형태다. 설계 패턴 적용점을
  따로 제안할 것이 없다.
- **예외 처리**: `catch` 3곳뿐이고 모두 메시지를 남긴다. 삼킨 예외가 없다.
- **`async void` 없음**, **`Camera.main` 없음**, **`Update` 안의 LINQ 없음**,
  **`using System.Linq`를 쓰는 런타임 파일 없음**.
- **`GameObject.Find`/`FindFirstObjectByType`**: 21곳이 있으나 전부 `Ensure*`·`Bind*` 같은
  1회성 초기화 경로다. `Update`에서 부르는 곳이 없다.
- **점수 계산**: `YachtScoreCalculator.Calculate`가 주사위 5개와 눈 1~6 범위를 먼저 검사하고
  한국어 메시지로 던진다. 족보 판정 로직 자체에서 오류를 찾지 못했다.
- **직렬화·역직렬화 취약점**: 외부 입력을 역직렬화하는 경로가 없다. `Newtonsoft`는
  `StreamingAssets`의 자기 소유 파일만 읽고, `TypeNameHandling` 같은 위험 설정을 쓰지 않는다.
- **`PlayerPrefs`·네트워크 요청 없음**: `Assets/Scripts/Network`는 빈 폴더다.

## 7. 검증

| 항목 | 결과 |
|---|---|
| Unity 컴파일 | 오류 0 (`asset_refresh` 후 `debug_check_compilation`) |
| Tessera EditMode 테스트 | 121/121 통과 |
| 전체 EditMode 테스트 | 798/809 통과. 실패 5건은 전부 `UnitySkills.Tests.Core` 패키지 테스트이며 Tessera와 무관하다 (`Assembly-CSharp.csproj` 공유 위반 3건, 패키지 문서 바이트 예산 1건, 패키지 설정 마이그레이션 1건) |
| 플레이 모드 육안 확인 | 하지 않음. `SCENE-1`이 바꾼 것은 편집 모드 동작뿐이라 플레이 모드 연출은 그대로다 |

## 8. 변경 파일

```
Assets/Scripts/Games/Yacht/YachtGameCore.cs                                  RNG-1
Assets/Scripts/Tabletop/ZodiacConstellationData.cs                           ZODIAC-1
Assets/Scripts/Tabletop/CozyCandleStand.cs                                   SCENE-1
Assets/Scripts/Tabletop/RollCosmicCube.cs                                    SCENE-1
Assets/Scripts/Tabletop/RollOrb.cs                                           SCENE-1
Assets/Scripts/Tabletop/RerollCounterBar.cs                                  SCENE-1
Assets/Scripts/Tabletop/HourglassTimer.cs                                    SCENE-1
Assets/Scripts/Games/AugmentedYacht/Presentation/AugmentedYachtController.cs  CATALOG-1
docs/maintenance_audit_20260906.md                                           이 문서
```

## 9. 다음 행동 제안

우선순위 순이다.

1차 작성 시점의 제안이다. 1·3·4번은 [10절](#10-2차-조치-사용자-지시)에서 처리했다.

1. ~~**`ENCAP-1`을 `M17` 완료 후 `M18` 착수 전에 처리한다.**~~ 2차에서 처리함.
2. **M8 잔여 항목 다섯 건을 `M17` 착수 전에 정리한다.** 증강 요트 완성이 목표인 마일스톤에서
   증강 추가 비용을 낮춰 두는 것이 순서상 맞다. 이 항목만 남았다.
3. ~~**모바일 대상 여부를 확정한다.**~~ 스탠드얼론 전용으로 확정함.
4. ~~**`ZODIAC-2`는 `M16` 채택 판단이 끝난 뒤에 본다.**~~ 텍스처 객체를 유지하는 방식이라
   셀 셰이딩 채택 여부와 무관해져 2차에서 함께 처리함.

2차 조치 뒤 남은 후속 작업은 둘이다.

- `GetPlayer`가 돌려주는 `PlayerScoreData`의 쓰기 가능성(10.1절 "남는 구멍").
  점수표를 `YachtGameState` 스냅샷 기준으로 다시 그리도록 바꿀 때 함께 정리한다.
- 권위 서버로 옮길 때 `SystemRandomSource`의 씨앗을 암호학적 난수로 교체(`RNG-1`의 "남는 일").

---

## 10. 2차 조치 (사용자 지시)

1차에서 문서화만 해 둔 항목을 사용자 지시로 처리했다. 결정이 필요한 세 건은 먼저 물어 결론을 받았다.

| 항목 | 사용자 결정 | 결과 |
|---|---|---|
| `ENCAP-1` | 읽기 전용 인터페이스 도입 | 수정함 |
| `IO-1` | 스탠드얼론 전용 | 코드 변경 없음. 아래 근거 기록 |
| `PERF-1` | `Update` 경로만 수정 | 수정함 |
| `CMD-1` | (판단 불필요) | 수정함 |
| `GUARD-1` | (판단 불필요) | 수정함 |
| `ZODIAC-2` | (판단 불필요) | 수정함 |
| `PERF-2` | (판단 불필요) | 사용자가 별도로 해소 |

### 10.1 ENCAP-1. 읽기 전용 뷰를 경계에 세웠다

`Assets/Scripts/Games/Yacht/YachtGameStateView.cs`를 새로 만들고 인터페이스 넷을 정의했다.
`IReadOnlyYachtGameState`, `IReadOnlyYachtDieState`, `IReadOnlyYachtDraftState`,
`IReadOnlyYachtAugmentPlayerState`다. 기존 상태 클래스가 이들을 **명시적으로** 구현한다.
필드 이름과 프로퍼티 이름이 겹치므로 명시적 구현이 아니면 컴파일되지 않는다.

배열은 그대로 넘긴다. `IReadOnlyList<T>`가 공변이라 `YachtDieState[]`를
`IReadOnlyList<IReadOnlyYachtDieState>`로 직접 줄 수 있다. 어댑터 객체도, 복사도 없다.
읽기 비용은 예전과 같고 달라지는 것은 쓰기가 컴파일되지 않는다는 점뿐이다.

**경계를 어디에 두었는가.** 1차 문서는 `LocalGameAuthority.CurrentState`까지 바꾸는 것을 상정했으나,
실제 사용처를 세어 보고 경계를 `YachtGameSession`에 두기로 했다.

| 접근자 | 타입 | 근거 |
|---|---|---|
| `LocalGameAuthority.CurrentState` | `YachtGameState` (그대로) | 권위 계층 자신과 그 테스트 하네스가 쓰는 내부 핸들이다. `YachtGameRulesTests`가 시나리오를 조립하려고 `state.Phase`·`state.Dice`·`state.AugmentPlayers[i].OwnedIds` 등을 직접 세우는 곳이 37군데다. 여기는 상태의 안쪽이므로 막을 대상이 아니다 |
| `YachtGameSession.State` | `IReadOnlyYachtGameState` (변경) | 화면이 권위와 만나는 유일한 지점이다. 프레젠테이션 코드에서 `LocalGameAuthority`를 직접 참조하는 곳은 하나도 없다 |
| `YachtGameSession.GetPlayer` | `PlayerScoreData` (그대로) | 이 인스턴스는 `ParchmentScoreSheet`가 만들어 권위에 **주입**한 것이다(`docs/yacht_state_ownership.md`). 화면이 이미 같은 객체를 직접 들고 있으므로 여기에 인터페이스를 씌워도 실제로 막히는 것이 없다 |

세션 내부 계산은 새로 둔 `private YachtGameState AuthorityState`를 쓴다. 읽기 전용 뷰에 없는
`Candidates`·`Players`·`TableFlipUsed` 같은 값을 세션이 직접 봐야 하기 때문이다.
치환 대상은 18곳이었다.

**인터페이스에 담은 것.** 화면이 실제로 읽는 것만 담았다. 새 값이 필요해지면 인터페이스에 한 줄
추가하게 되고, 그 한 줄이 "이 값을 화면에 넘긴다"는 명시적 결정이 된다.
`Candidates`와 `Players`는 화면이 `State`를 통해 읽지 않으므로 넣지 않았다.

**바뀐 호출부**는 넷이다. `AugmentTrayPresenter`(배열 타입 6곳과 `Array.IndexOf` 1곳),
`YachtTurnFlowPresenter`(`.Length` → `.Count` 2곳), `YachtDiceRoundPresenter`
(`SyncFromAuthority`·`ResetForTurn`의 매개변수 타입), `AugmentCardValidationRunner`(에디터 검증 도구).
`Array.IndexOf`는 읽기 전용 목록에 쓸 수 없어 같은 일을 하는 루프로 바꿨다.

**남는 구멍**: `GetPlayer`가 돌려주는 `PlayerScoreData`는 여전히 쓰기가 가능하다. 위 표의 근거대로
지금은 막아도 실익이 없다. 점수표를 `YachtGameState` 스냅샷 기준으로 다시 그리도록 바꾸는 시점에
함께 정리하는 것이 맞다.

### 10.2 CMD-1. 새 게임을 시작할 때 명령 ID 집합을 비운다

`ResetGameState`에서 `acceptedCommandIds.Clear()`를 호출한다. `StartGame` 처리 중에 비우고
그 명령의 ID는 `Execute`가 나중에 넣으므로, 같은 ID를 두 번 보내면 여전히 `DuplicateCommand`로 거부된다
(`Authority_중복명령과_오래된Revision을_거부한다`가 이 순서를 고정한다).
revision은 계속 증가하므로 옛 명령이 다시 들어와도 `RevisionMismatch`에서 걸린다.

### 10.3 GUARD-1. 메시 소유권 규칙을 에디터와 빌드에서 같게 했다

`GetWritableMesh`의 판정 순서를 바꿨다.

1. 이 헬퍼가 이미 갈라 준 메시(`writableMeshes`)면 그대로 돌려준다. 에디터·빌드 공통이다.
2. 에디터에서는 추가로 `IsAsset`이 거짓이면 복제하지 않는다. 프롭이 절차적으로 방금 만든 메시는
   그 프롭만 쓰므로 복제할 이유가 없다.
3. 그 외에는 복제하고 사본을 소유권 집합에 등록한다.

이전에는 빌드에서 `IsAsset`이 항상 거짓이라 3번에 아예 도달하지 못했고, 프리팹 메시가 인스턴스끼리
공유된 채 정점이 덮어써졌다. 이제 빌드는 1번과 3번으로 판정한다.

보호 동작(프리팹 메시를 복제한다)이 두 환경에서 같아졌으므로 에디터 테스트가 빌드와 같은 경로를
검증한다. `RuntimeAssetGuardTests`에 테스트 둘을 추가했다.
같은 내장 메시를 쓰는 프리미티브 둘이 서로 다른 사본을 받는지, 같은 필터가 두 번 물어보면 같은
사본이 돌아오는지를 본다.

### 10.4 ZODIAC-2. 캐시를 비울 때 텍스처 객체를 유지한다

`ClearCache`가 `cachedTextures = null` 대신 `cacheStale = true`만 세운다.
`GetAllZodiacTextures`는 크기가 맞는 기존 배열이 있으면 그 텍스처 객체에 픽셀만 다시 굽는다.
`BakeConstellationTexture`에 `reuse` 매개변수를 추가해 그 경로를 만들었다.

이렇게 하면 두 가지가 함께 해결된다. 참조만 버려 12장(약 3MB)이 도메인 리로드까지 남던 누수가
사라지고, `RollOrb`와 `RollCosmicCube`가 캐시를 공유하는 탓에 한쪽이 다시 만들 때 다른 쪽이
옛 텍스처를 들고 있던 불일치도 사라진다. 1차 문서에서 "단순히 `Destroy`를 넣으면 회귀"라고 적은
이유가 이 공유였는데, 객체를 유지하면 그 회귀 자체가 생기지 않는다.

### 10.5 PERF-1. Update 경로의 셰이더 프로퍼티만 ID로 바꿨다

`RollCosmicCube`(프로퍼티 12종·호출 21곳), `RollOrb`(3종·10곳), `RerollCounterBar`(3종·6곳),
`HourglassTimer`(1종·2곳). 합계 39곳이다. 각 클래스에 `private static readonly int ...Id` 선언을 두고
`Update` 안에서만 쓴다.

1회성 생성 경로의 문자열 접근은 그대로 뒀다. 저장소 전체 348곳 중 39곳만 바뀐 이유가 이것이다.
거기서는 조회 비용이 의미가 없고, 전부 바꾸면 요청 범위를 넘는 변경이 된다.

`SCENE-1`이 편집 모드 `Update`를 막았으므로 이 경로는 이제 플레이 모드에서만 돈다.

### 10.6 IO-1. 스탠드얼론 전용으로 확정했다

사용자가 빌드 대상을 스탠드얼론 전용으로 확정했다. `File.Exists`/`File.ReadAllText`로
`StreamingAssets`를 읽는 현재 방식이 그 대상에서 정확히 동작하고 `UnityWebRequest` 경로보다 간단하며
동기라 다루기 쉽다. 코드를 바꾸지 않는다.

Android나 WebGL을 나중에 추가하기로 하면 그때 `StreamingAssets` 접근을 어댑터 하나로 모으고
비동기 경로를 붙여야 한다. `DicePresetCatalog`가 동기 API라 호출부까지 함께 바뀐다.

### 10.7 PERF-2는 사용자가 별도로 해소했다

이 세션 도중 사용자가 `ZodiacConstellationData.EnabledInGame` 스위치를 추가해 별자리 연출을 껐다.
`AugmentedYachtController.WarmUpRollAssets`가 먼저 반환하므로 시작 시 12장 베이킹이 사라졌고,
`RollOrb`·`RollCosmicCube`도 텍스처를 물리지 않는다. `PERF-2`가 지적한 비용이 그대로 없어졌다.

`ZODIAC-1`과 `ZODIAC-2` 수정은 그대로 유효하다. 스위치를 다시 켜거나 에디터에서 프롭을
다시 만들 때 이 경로가 살아난다.

### 10.8 2차 검증

모든 항목을 적용한 뒤 실행한 결과다.

| 항목 | 결과 |
|---|---|
| `Assembly-CSharp` 컴파일 | 오류 0 / 경고 3 (`dotnet build`). 경고 셋은 `PixelEdgeRendererFeature`의 기존 URP deprecation 경고로 이번 변경과 무관하다 |
| `Assembly-CSharp-Editor` 컴파일 | 오류 0 / 경고 3. `Assembly-CSharp`를 `ProjectReference`로 참조하므로 바뀐 런타임 코드를 대상으로 빌드했다 |
| Unity 컴파일 | 오류 0 (`debug_check_compilation`, Editor 로그 `error CS` 0건) |
| Tessera EditMode 121개 | **121/121 통과.** `ENCAP-1`까지 전부 적용한 상태에서 실행했다 |
| `RuntimeAssetGuardTests` | **4/4 통과.** `GUARD-1` 수정과 새로 추가한 테스트 둘을 포함하며, 역시 `ENCAP-1` 적용 후 실행이다 |
| 플레이 모드 육안 확인 | 하지 않음 |

`ENCAP-1` 적용 직후 Unity Editor가 약 15분간 응답하지 않아 한때 검증이 막혔다. Editor 로그에는
컴파일 오류가 없었고(`error CS` 0건) 도메인 리로드까지 진행된 기록만 있었다. 그동안은
`dotnet build`로 두 어셈블리를 독립 컴파일해 오류 0을 확인했다. Editor가 재시작된 뒤
위 표의 테스트를 모두 실행했다.

재시작 뒤 UnitySkills REST 브리지 포트가 **8092에서 8090으로 바뀌었다.** 8092는 이전 인스턴스의
리스너가 남아 점유하고 있어 응답하지 않는다. 다음 세션에서 브리지가 응답하지 않으면
8090~8100 범위를 훑어 실제 포트를 찾아야 한다.

### 10.9 전체 EditMode 실행에서 남은 실패 7건

전체 실행은 812개 중 799개 통과, 7개 실패다. 일곱 건 모두 이번 조치와 무관하다.

| 실패 | 분류 |
|---|---|
| `UnitySkills.Tests.Core` 2건 (문서 바이트 예산, 워크플로 스냅샷) | unity-skills 패키지 자체 테스트. Tessera 코드와 무관 |
| `AugmentCardViewTests` 5건 (`Cyan Inner Border`가 `null`이어야 하는데 존재함) | 사용자가 이 세션과 **동시에 진행 중인 작업**의 중간 상태 |

`AugmentCardViewTests` 실패는 사용자가 하늘색 내부 테두리를 걷어내는 작업을 진행하면서
`AugmentCardViewTests.cs`·`AugmentScrollAssetGenerator.cs`·`AugmentCardView.cs`·
`AugmentParchmentVisuals.cs`를 고치고 시안 테두리 메시·머티리얼 에셋을 지운 상태에서 발생했다.
테스트 실행 시점에는 `Resources/AugmentScrolls`의 프리팹이 아직 옛 구조였고, 그 두 분 뒤
생성기가 프리팹 넷을 다시 구웠다. 이 조치 범위의 파일과는 겹치는 것이 없으므로
이번 변경이 원인이 아니다. 재실행은 그 작업이 끝난 뒤에 하는 것이 맞다.

---

## 11. 후속 작업 (사용자 지시)

10절에서 남긴 후속 작업 둘을 처리했다. 하나는 조치했고 하나는 지금 하지 않는 근거를 정리했다.

### 11.1 GetPlayer의 쓰기 가능성 — 점수표 소유권을 권위로 넘겼다

10.1절이 "남는 구멍"으로 적어 둔 항목이다. 조사해 보니 구멍이 `GetPlayer` 하나가 아니었다.

**실제 구조.** `ParchmentScoreSheet`가 `PlayerScoreData` 둘을 `[SerializeField]`로 들고 있었고,
`YachtTurnFlowPresenter`가 그것을 `new YachtGameSession(scoreSheet.Player1, scoreSheet.Player2, ...)`로
권위에 넘겼다. 즉 **UI 컴포넌트가 권위 데이터의 저장소를 겸했다.** 점수는 씬 파일에 직렬화되고,
권위는 화면이 소유한 객체를 고쳤다.

**죽은 쓰기 경로.** 그 위에 화면 쪽 쓰기 API가 넷 있었는데, 호출자를 세어 보니 전부 도달 불가였다.

| 메서드 | 외부 호출자 |
|---|---|
| `ParchmentScoreSheet.SetPlayerScore` | 없음 (아래 `OverwriteScoreFromAugment`에서만 호출) |
| `ParchmentScoreSheet.ResetScores` | 없음 |
| `ParchmentScoreSheet.OverwriteScoreFromAugment` | `YachtRunicPresenter.ApplyScoreOverwrite` 하나 |
| `YachtRunicPresenter.ApplyScoreOverwrite` | 없음 |

넷이 서로만 부르는 닫힌 사슬이고 바깥 진입점이 없다. `M2`에서 권위를 `LocalGameAuthority`로 옮길 때
남은 잔재다. 씬·프리팹의 `UnityEvent` 배선도 확인했고 참조가 없었다.

**조치.** 사슬 넷을 지우고 소유권을 권위로 옮겼다.

- `ParchmentScoreSheet`의 `[SerializeField] player1Data/player2Data`와 `Player1`/`Player2`를 없앴다.
  대신 `IReadOnlyList<IReadOnlyPlayerScoreData> players`를 두고 `BindPlayers`로 권위 뷰를 받는다.
  초깃값은 빈 점수표 둘이라 편집 모드와 게임 시작 전에도 표가 그려진다.
- `YachtGameSession`에 점수표를 주입받지 않는 생성자를 추가했다. 기존 생성자는 남긴다.
  테스트가 결과를 직접 들여다보는 경로가 그쪽이다.
- `YachtTurnFlowPresenter.CreateGameSession`이 `new YachtGameSession(options)`로 세션을 만들고
  `scoreSheet.BindPlayers(session.State.Players)`로 뷰를 연결한다.
- `IReadOnlyPlayerScoreData`를 정의하고 `IReadOnlyYachtGameState.Players`를 추가했다.
  `YachtGameSession.GetPlayer`의 반환 타입도 이 인터페이스로 바꿨다.

이제 `PlayerScoreData`를 쓸 수 있는 곳은 권위 계층 안쪽뿐이다. 화면은 읽기 전용 뷰만 본다.
`docs/yacht_state_ownership.md`가 "씬 호환을 위해 권위 상태에 주입한다"라고 적어 둔 예외가 없어졌다.

**프리팹에 남는 흔적**: `Assets/Prefabs/Tabletop/3D Layered Parchment Score Sheet.prefab`에
`player1Data`/`player2Data` 키가 아직 있다. 필드가 사라졌으므로 Unity가 그 프리팹을 다음에 저장할 때
자동으로 걷어낸다. 남아 있어도 무시되므로 동작에는 영향이 없다.

### 11.2 난수 씨앗의 암호학적 교체 — 지금 하지 않는다

`RNG-1`에 "권위 서버로 옮길 때는 암호학적 난수로 씨앗을 뽑아야 한다"라고 적었는데,
그 문장만 따라 씨앗만 바꾸는 것은 목표를 달성하지 못한다.

`SystemRandomSource`는 `System.Random`을 쓴다. 이 생성기는 Knuth 뺄셈식이라 **씨앗을 몰라도**
출력 56개 정도를 관찰하면 내부 상태를 복원해 이후 전부를 예측할 수 있다. 즉 원격 클라이언트가
주사위 눈을 예측하지 못하게 하려면 씨앗이 아니라 **생성기 자체**를 바꿔야 한다.

지금 `CryptoRandomSource`를 넣으면 쓰는 곳이 없는 코드가 된다. `CLAUDE.md`의 "요청받지 않은
추상화·유연성 금지"에 어긋난다. 그래서 다음과 같이 정리한다.

- `M18`(네트워크 준비 검증)에서 `IRandomSource`의 암호학적 구현을 추가하고 권위 쪽에서 주입한다.
  인터페이스가 이미 있으므로 구현 하나와 주입 한 줄이면 된다.
- 그때까지 `Guid.NewGuid().GetHashCode()` 씨앗을 유지한다. 이 수정의 실제 목적은 예측 방지가 아니라
  **판정용과 연출용 난수가 같은 씨앗을 받던 충돌을 없애는 것**이었고, 그 목적은 이미 달성됐다.
- `YachtGameCore.cs`의 주석을 이 결론에 맞춰 둔다.

### 11.3 검증

| 항목 | 결과 |
|---|---|
| `Assembly-CSharp` + `Assembly-CSharp-Editor` 컴파일 | 오류 0 / 경고 3 (`dotnet build`). 경고 셋은 기존 URP deprecation |
| Unity 컴파일 | 오류 0 |
| Tessera EditMode 121개 | **121/121 통과** |
| 전체 EditMode 812개 | **800 통과 / 6 실패 / 6 건너뜀.** 실패 여섯은 전부 `UnitySkills.Tests.Core` 패키지 자체 테스트다. Tessera 코드에서 실패한 테스트는 없다 |
| 씬 상태 | `isDirty: false` (`SCENE-1` 수정 이후 편집 모드가 씬을 더럽히지 않는다) |
| 플레이 모드 육안 확인 | 하지 않음 |

이 변경으로 시그니처가 바뀐 테스트는 `YachtGameRulesTests` 두 줄
(`session.GetPlayer(0).upperScores[0]` → `.UpperScores[0]`)뿐이고, 나머지는 타입만 좁혔다.

**10.9절에서 적은 `AugmentCardViewTests` 5건은 해소됐다.** 그때는 사용자의 시안 테두리 제거 작업이
중간 상태였고, 작업이 끝난 뒤 실행한 이번 결과에서는 전부 통과한다. 예상대로 이번 조치와 무관했다.

한때 Unity가 Play Mode라 `test_run`이 `InvalidOperationException: This cannot be used during play mode`로
실패했다. 사용자가 게임을 실행 중이었으므로 Play Mode를 임의로 끄지 않고 기다렸다가 실행했다.

**브리지 포트 주의**: 이 세션에서만 8092 → 8090 → 8091로 두 번 바뀌었다. Unity가 재시작될 때마다
이전 리스너가 포트를 물고 있어 다음 번호로 밀린다. 브리지가 응답하지 않으면 8090~8100을 훑어야 한다.
