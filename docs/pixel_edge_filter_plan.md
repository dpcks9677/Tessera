# 픽셀 필터 엣지 검출 상세 (M10.5)

이 문서는 `docs/augmented_yacht_work_plan.md`의 `M10.5` 마일스톤을 뒷받침하는 기술 상세다.
마일스톤 문서에는 배경과 태스크만 두고, 셰이더 공식·이식 함정·튜닝 기록은 여기에 남긴다.

## 1. 배경 — 기존 픽셀 필터가 못 하던 것

M10.5 이전의 픽셀 필터는 픽셀화만 했다. 월드 카메라가 `1920×1080` 렌더 타깃에 그리면
전체 화면 `RawImage`에 붙은 `DicePoC/PixelUpscale`이 UV를 `640×360`(또는 `960×540`)
가상 격자로 내림 스냅해 최근접 샘플링한다. 그게 전부였고, 뎁스·노멀 버퍼는 URP SSAO 외에
아무도 쓰지 않았다.

그래서 저해상도로 내려가면 세 가지가 무너졌다.

- 주사위·양피지 카드·프롭의 실루엣이 비슷한 명도의 배경과 뭉친다.
- 아트 가이드가 강조하는 "둥글고 두툼한 챔퍼"가 픽셀 단위에서 사라진다.
- 명암만으로 형태를 읽히게 하려고 개별 에셋의 색값을 손으로 조정해야 했다
  (`docs/augmented_yacht_augment_card_design_revision_plan.md` 참조).

## 2. 원본 기법 — KodyJKing/hello-threejs

<https://github.com/KodyJKing/hello-threejs> 는 three.js `RenderPixelatedPass`의 원본이다.
저해상도 렌더 타깃 위에서 상하좌우 네 이웃과 비교해 두 종류의 엣지를 만든다.

| 지표 | 계산 | 화면 효과 |
|---|---|---|
| `depthEdgeIndicator` | 이웃 뎁스에서 자기 뎁스를 뺀 양수만 누적한 뒤 `smoothstep` → 2단 양자화 | 실루엣 안쪽을 어둡게 = 1픽셀 외곽선 |
| `normalEdgeIndicator` | `(1 - dot(n, 이웃n))`에 뎁스 방향과 노멀 차이 부호를 곱해 4방향 합산 후 `step` | 볼록한 모서리를 밝게 = 챔퍼 하이라이트 |

두 지표는 배타적으로 적용된다. 뎁스 엣지가 서면 그쪽이 이기고, 아니면 노멀 엣지가 밝힌다.

```
coefficient = depthEdge > 0 ? (1 - depthEdgeStrength * depthEdge)
                            : (1 + normalEdgeStrength * normalEdge)
```

원본은 여기에 블룸과 최종 업스케일 패스를 더하지만, M10.5는 두 엣지 지표만 가져온다.
블룸과 팔레트 양자화·디더링은 범위 밖이다.

## 3. URP 이식에서 바꾼 것

알고리즘 자체는 파이프라인에 의존하지 않는다. 다만 세 가지를 이 프로젝트에 맞춰 바꿨다.

### 3.1 직교 카메라 뎁스

월드 카메라는 직교 투영(`orthographicSize 8.2`)이다. `LinearEyeDepth`는 원근 투영을 전제하므로
쓸 수 없다. reversed-Z를 되돌린 뒤 near~far를 선형 보간한다.

```hlsl
if (unity_OrthoParams.w > 0.5)
{
    #if UNITY_REVERSED_Z
        rawDepth = 1.0 - rawDepth;
    #endif
    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
}
return LinearEyeDepth(rawDepth, _ZBufferParams);
```

직교에서는 원근 왜곡이 없으므로 원본이 하던 뎁스 나누기 정규화도 필요 없다.

### 3.2 노멀 공간

원본은 뷰 공간 노멀을 전제한다. URP가 주는 `_CameraNormalsTexture`는 월드 공간이므로
`UNITY_MATRIX_V`로 변환한 뒤 원본 공식을 적용한다.

### 3.3 뎁스 임계값의 단위

원본의 `0.01 / 0.02`는 정규화된 뎁스 기준이다. 여기서는 월드 유닛이므로 그대로 쓰면
외곽선이 전부 뜨거나 전혀 안 뜬다. `_DepthEdgeThreshold`로 노출하고 아래 계산으로 초기값을 잡았다.

- 카메라 피치 75°, 직교 크기 8.2, 세로 360픽셀 기준
- 픽셀 한 칸의 화면 세로 크기 = `2 × 8.2 / 360 ≈ 0.0456` 월드 유닛
- 평평한 테이블 위에서 픽셀당 뎁스 변화 = `0.0456 × cos(75°) / sin(75°) ≈ 0.012`
- 테이블에 놓인 주사위 실루엣의 뎁스 단차 ≈ `0.5`

두 값이 40배 넘게 벌어지므로 임계값 `(0.05, 0.12)`면 테이블 표면 기울기는 통과시키고
실루엣만 잡는다. 노멀 엣지의 뎁스 편향도 같은 단위로 `_NormalEdgeDepthBias = 0.01`을 쓴다
(원본의 `depthDiff * 0.25 + 0.0025`와 같은 경계다).

### 3.4 격자 정합

엣지 패스는 컬러·뎁스·노멀을 모두 **가상 격자 중심 UV**에서 읽고, 이웃 오프셋도 가상 픽셀
한 칸(`1 / virtualResolution`)으로 잡는다. 업스케일 셰이더와 같은 격자를 써야 두 격자가
어긋나 생기는 떨림이 없다. 해상도는 `YachtCameraRig.ApplyRenderSettings()`가
`Shader.SetGlobalVector("_PixelEdgeVirtualResolution", ...)`로 전역에 밀어 넣으므로
F1/F2 해상도 전환이 자동으로 반영된다.

## 4. 패스 배치 — 코스믹 큐브 문제의 해답

`DicePoC/CosmicCube`는 `Queue=Transparent`의 자발광 유리 큐브다. 자체적으로 12개 모서리에
네온 시안 와이어프레임(`EvaluateCubeEdges`)을 그리고, 프레넬 림이 실루엣을 밝힌다.
여기에 검은 뎁스 외곽선이 둘리면 발광하는 유리라는 인상이 정면으로 깨지고,
노멀 하이라이트는 이미 있는 네온 모서리와 중복된다.

**엣지 패스를 `RenderPassEvent.AfterRenderingSkybox`에 두어 해결한다.**

- 코스믹 큐브(`Transparent`)와 호버 아웃라인(`Transparent+1`)은 엣지 합성이 끝난 뒤에 그려진다.
  검은 외곽선이 아예 닿지 않으므로 큐브의 시각 계약은 그대로다.
- 이것은 아트 취향이 아니라 기술적으로도 옳은 위치다. URP의 뎁스 텍스처는 불투명 패스까지만
  담으므로, 반투명 이후에 합성하면 큐브 뒤 배경의 뎁스로 계산한 외곽선을 큐브 픽셀 위에
  덧칠하게 된다.
- 큐브 뒤 불투명 배경에는 외곽선이 정상으로 그려지고 큐브가 알파 블렌딩으로 겹치므로,
  "외곽선이 유리 너머로 비친다"는 읽기가 된다. 유리 큐브 콘셉트와 맞는다.
- 큐브 실루엣이 배경에서 덜 떨어져 보이면 코드 수정 없이 `_EdgeIntensity`(기본 2.60)와
  `_RimIntensity`(기본 0.85)를 올려 자체 발광 외곽을 강화한다.

이 배치는 `PixelEdgeFilterTests.엣지_피처는_불투명_직후에_실행된다`로 고정해 두었다.
뒤로 옮기면 테스트가 깨진다.

밝은 발광체를 선택적으로 빼야 할 경우를 대비해 `_EdgeLuminanceSuppression`을 노출했다.
픽셀 휘도가 높을수록 뎁스 엣지 계수를 감쇠시킨다. 기본값 `0`이면 아무 영향이 없다.

## 5. A/B 전환

기존 필터(픽셀화만)와 새 필터(픽셀화 + 엣지)를 런타임에 바꿔 볼 수 있다.

| 조작 | 동작 |
|---|---|
| `F3` | 픽셀 엣지 필터 켜기/끄기 |
| 화면 좌상단 `Edge: ON` / `Edge: OFF` 버튼 | 같은 동작. 현재 상태를 문구로 표시 |
| `F1` / `F2` | 내부 해상도 `960×540` / `640×360` (기존 그대로) |

끄면 `PixelEdgeRendererFeature.AddRenderPasses`가 패스를 등록하지 않는다. 블릿 비용까지
사라지므로 엣지 도입 이전과 완전히 같은 경로가 되고, 그래야 비교가 정직하다.

## 6. 구성 요소

| 역할 | 파일 |
|---|---|
| 엣지 셰이더 | `Assets/Rendering/Shaders/DicePixelEdge.shader` (`DicePoC/PixelEdge`) |
| 렌더러 피처와 패스 | `Assets/Scripts/Rendering/PixelEdgeRendererFeature.cs` |
| 대상 카메라 표시 | `Assets/Scripts/Rendering/PixelEdgeCamera.cs` |
| 표시 부착, 해상도 전달, 켜고 끄기 | `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtCameraRig.cs` |
| `F3` 입력 | `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtInputRouter.cs` |
| 토글 버튼과 문구 | `Assets/Scripts/Games/AugmentedYacht/Presentation/YachtSceneAssembler.cs` |
| 렌더러 에셋 등록 (일회성 메뉴) | `Assets/Editor/RegisterPixelEdgeFeature.cs` |
| 계약 테스트 | `Assets/Editor/PixelEdgeFilterTests.cs` |
| 등록 대상 | `Assets/Settings/PC_Renderer.asset`, `Assets/Settings/Mobile_Renderer.asset` |

`PixelEdgeCamera`가 붙은 카메라에서만 패스가 돈다. 렌더러 피처는 렌더러 에셋 단위라
같은 에셋을 쓰는 Crisp UI 카메라와 Display 1 카메라에서도 후보가 되기 때문이다.
`Camera.CopyFrom`은 컴포넌트를 복사하지 않으므로 Crisp UI 카메라로 번지지 않는다.

렌더러 에셋은 YAML이라 손으로 고치면 서브에셋 로컬 ID와 피처 목록이 어긋난다.
`Tools/Tessera/Register Pixel Edge Renderer Feature` 메뉴가 인스펙터와 같은 순서로
서브에셋을 만들고 `m_RendererFeatures`·`m_RendererFeatureMap`을 함께 갱신한다.
한 번 실행하면 에셋에 저장되므로 이후에는 다시 부를 필요가 없다.

## 7. 실행 기록

### 2026-09-05 — 이식과 등록

- Unity `6000.3.21f1`, URP `17.3.0`, Deferred, Render Graph 비활성(호환 모드).
- 패스는 호환 모드 API(`Execute` + `CommandBuffer` + `Blitter.BlitCameraTexture`)로 작성했다.
  임시 타깃은 `RenderingUtils.ReAllocateHandleIfNeeded`로 잡는다
  (URP 17에서 `ReAllocateIfNeeded`는 폐기됨).
- 처음 `RenderPassEvent.AfterRenderingOpaque`로 적었다가 컴파일 오류로 잡혔다.
  실제 열거자 이름은 `AfterRenderingOpaques`이며, 배경 클리어 방식이 바뀌어도 안전하도록
  최종적으로 `AfterRenderingSkybox`를 택했다. 반투명보다 앞이라는 성질은 그대로다.
- `Tools/Tessera/Register Pixel Edge Renderer Feature`로 `PC_Renderer`와 `Mobile_Renderer`
  양쪽에 피처를 등록했다. 콘솔 오류 0개.

### 튜닝 기록

| 날짜 | 값 | 근거 |
|---|---|---|
| 2026-09-05 | `_DepthEdgeThreshold = (0.05, 0.12)` | §3.3의 계산값. 실측 조정은 미완료 |
| 2026-09-05 | `_DepthEdgeStrength = 0.4`, `_NormalEdgeStrength = 0.3` | 원본 기본값 |

## 8. 알려진 문제 (2026-09-05 미해결)

**HUD 디버그 버튼 두 개가 눌러도 반응하지 않는다.**

- `960 / 640` 해상도 전환 버튼 (`Debug`) — M10.5 이전부터 있던 버튼이다.
- `Edge: ON/OFF` 픽셀 엣지 전환 버튼 (`PixelEdgeToggle`) — M10.5에서 추가했다.

두 버튼이 함께 죽어 있으므로 원인은 엣지 기능 자체가 아니라 디버그 버튼의 클릭 경로일 가능성이
높다. 확인할 후보는 다음과 같다.

- `Pixel Presentation` 캔버스가 `ScreenSpaceOverlay`인데 그 위에 전체 화면 `RawImage` 두 장
  (`Point Upscale`, `Crisp UI Overlay`)이 형제로 놓여 있다. 둘 다 `raycastTarget = false`로
  두고 있지만, 씬에 저장된 값이 코드와 어긋나면 버튼이 가려진다.
- `EnsureEventSystem`이 만드는 `EventSystem` / `InputSystemUIInputModule`이 실제로 살아 있는지.
- `BindPresentationActions`가 씬에 이미 있는 버튼을 이름으로 찾아 리스너를 다시 거는데,
  `GameObject.Find`가 비활성 오브젝트를 못 찾으므로 씬에 저장된 버튼이 비활성이면 조용히 실패한다.
- `AugmentedYachtController`가 `debugButtons`를 두 번 덮어쓰는 초기화 순서
  (`BuildPresentation` 직후 `BindPresentationActions`) 때문에 리스너가 유실될 가능성.

키보드 경로(`F1` / `F2` / `F3`)는 버튼과 독립적인 `YachtInputRouter`를 타므로 별도로 확인해야 한다.
이 문제가 남아 있는 동안 A/B 비교는 키보드로만 가능하다.

## 8. 남은 확인

- `640×360`·`960×540` 양쪽에서 실루엣과 챔퍼가 읽히는지, 물리 회전 중 외곽선이
  반짝이지(pixel crawl) 않는지 육안 확인. `Assets/Docs/Decision.md`에 열린 리스크로 적혀 있다.
- 주사위·양피지 카드·족보 종이의 머티리얼이 `Transparent` 큐가 아닌지 확인. `Transparent`라면
  이 패스 배치에서 외곽선을 받지 못한다.
