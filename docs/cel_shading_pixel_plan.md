# 셀 셰이딩 전환과 픽셀 격자 정합 상세 (M16)

이 문서는 `docs/augmented_yacht_work_plan.md`의 `M16` 마일스톤을 뒷받침하는 기술 상세다.

## 1. 배경

`M12`에서 엣지 필터로 실루엣을 살리고, `M13`에서 포스트 색 양자화로 고유 색을 616색에서 30색까지 줄였다. 그런데도 화면은 픽셀아트가 아니라 "픽셀 필터를 씌운 3D 렌더"로 읽혔다.

M13이 세운 가설은 "픽셀아트를 픽셀아트로 만드는 것은 픽셀 크기가 아니라 적은 색 수"였다. 절반만 맞다. 정확히는 **적은 색 수가 아니라 적은 평면 영역 수**다.

포스트 양자화는 합성된 프레임에 걸린다. 그래서 색 경계가 밝기 그라데이션의 등치선 위에 생긴다. 주사위 면은 노멀이 연속적으로 휘어 있으므로 그 등치선은 면 위의 동심원 밴드가 되고, 주사위가 구르면 밴드가 표면을 흘러간다. 경계가 도형 경계가 아니라 렌더 경계다. 색을 30개로 줄여도 3D로 읽히는 이유가 이것이다.

그래서 M16은 색 감축을 **포스트 단계에서 재료 단계로 옮긴다.**

## 2. 전환 전 화면이 3D로 읽히던 요인

| 요인 | 위치 | 성격 |
|---|---|---|
| 주사위 바디가 URP Lit + 스페큘러 + 환경 반사 + 자발광 | `DicePaletteCatalog.GetBodyMaterial` | 시점 의존 하이라이트가 표면 위를 미끄러짐 |
| Metallic 최대 0.80 / Smoothness 최대 0.80 | `DicePaletteCatalog` 정의 표 | 위와 같음 |
| 키 라이트 `LightShadows.Soft`, 그림자맵 2048 | `YachtLightingRig.Configure` | 반그림자가 연속 그라데이션 |
| SSAO 렌더 피처 (Intensity 0.4) | `Assets/Settings/PC_Renderer.asset` | 크레비스에 연속 AO |
| 1920x1080 렌더 후 격자 점 샘플링 | `YachtCameraRig.CreateRenderTarget` | 셰이딩과 절차적 디테일이 픽셀보다 미세해 앨리어싱 |

픽셀화 자체가 가짜였다는 점이 중요하다. 카메라는 항상 1920x1080에 렌더하고, 업스케일 셰이더가 `floor(uv * 480x270)`으로 셀 중심만 뽑아 왔다. 격자만 굵어졌을 뿐 그림의 내용은 풀해상도 3D 렌더 그대로였다.

## 3. 셰이딩 규약

`Assets/Rendering/Shaders/CelSurface.shader`와 공유 include `CelSurfaceShading.hlsl`이 규약을 담는다. 세 패스(`UniversalForward`, `ShadowCaster`, `DepthNormals`)가 같은 상수 버퍼를 선언한다.

### 3.1 노멀 축 스냅 (`_NormalSnap`)

이번 변경의 핵심이다. 램프만 걸고 스냅하지 않으면 휜 노멀 위에 동심원 밴드가 생겨 포스트 양자화와 같은 실패를 재현한다.

```hlsl
float3 CelSnapNormalOS(float3 normalOS)  // 가장 큰 성분의 축으로 스냅
```

오브젝트 공간에서 스냅한 뒤 월드로 옮긴다. 프래그먼트에서 스냅해야 삼각형 안에서 보간이 섞이지 않는다.

효과는 두 가지다.

- 보이는 면마다 값이 하나로 떨어지고, 경계가 정확히 주사위 모서리에 놓인다.
- 메시가 실제로 어떻게 스무딩되어 있든 결과가 같다. FBX(`normal_dice.fbx`)와 절차적 폴백(`DiceMeshFactory`, 구형 노멀)의 차이가 사라진다.

주사위는 켜고, 곡면이 있는 테이블·소품은 끈다.

### 3.2 밴드 램프 (`_Bands`, `_RampValues`)

램버트 값을 `dot(n, L) * 0.5 + 0.5`로 감싸 뒷면이 순흑으로 죽지 않게 한 뒤 밴드 인덱스로 자른다. 밴드 값은 `TesseraPixelPalette.ValueScales`를 그대로 쓴다.

```
{ 0.35, 0.65, 1.0, 1.45 }
```

아트 가이드 §4의 명도 램프이고, M13의 포스트 팔레트도 같은 배열로 만들어진다. 재료 단계 밴드와 포스트 팔레트가 같은 값을 쓰므로 둘을 겹쳐도 색이 어긋나지 않는다. `TesseraPixelPalette.RampVector`가 이 계약을 한 곳에 고정한다.

밴드 수는 확산 재질 3, 금속으로 읽히길 원하는 재질 4다. 금속 구분은 새 필드를 두지 않고 기존 `Metallic > 0.5f`에서 파생시킨다. Baseline 경로의 데이터를 건드리지 않기 위한 것이다.

### 3.3 하드 그림자 계단

그림자 감쇠도 `step(0.5, shadowAttenuation)`으로 계단화한다. 반그림자가 남으면 밴드 경계가 흐려져 3D로 읽힌다.

### 3.4 하드 림

프레넬 감쇠 대신 `step(_RimThreshold, 1 - NdotV)` 한 밴드다. 아트 가이드 §2.1의 웜 키 + 쿨 림 균형을 픽셀아트 규칙 안에서 유지하기 위한 것이며, 색은 `color-light-cool-rim` `#364b6e`다.

### 3.5 DepthNormals 패스

엣지 필터가 노멀을 여기서 읽는다. Forward 경로에는 GBuffer가 없으므로 이 패스가 유일한 노멀 공급원이다. **스냅한 노멀을 그대로 내보낸다.** 그래야 노멀 엣지가 색 경계와 같은 자리, 즉 주사위 모서리에 걸린다.

URP는 비-OCT 경로에서 `half4(normalWS, 0.0)`을 쓴다(`DepthNormalsPass.hlsl:76`). 이 패스도 같은 규약을 따른다.

## 4. Deferred에서 Forward로

`Assets/Settings/PC_Renderer.asset`의 `m_RenderingMode`를 `2`(Deferred)에서 `0`(Forward)으로 바꿨다.

셀 셰이더는 커스텀 라이팅이라 GBuffer를 채우지 않는다. Deferred에서는 forward-only 목록으로 밀려나고, 그러면 엣지 피처가 읽는 GBuffer 노멀이 비어 주사위에서 노멀 엣지가 사라진다. 셀 셰이딩 후에는 광원이 사실상 하나라 Deferred 이점도 없다.

`DicePixelEdge.shader`의 `#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT`가 두 경로를 모두 처리하므로 엣지 셰이더는 손대지 않았다.

이 변경은 Baseline과 Cel 양쪽에 함께 걸린다. 광원이 적어 Baseline 외형은 사실상 변하지 않아야 하는데, 그것이 실제로 그런지가 판정 기준이다.

## 5. 저해상도 실렌더

`YachtCameraRig`가 만드는 렌더 타깃 크기를 연출 방식에 따라 정한다.

| 연출 방식 | 렌더 타깃 |
|---|---|
| Baseline | 1920x1080 (기존) |
| Cel | 내부 해상도 (480x270 또는 640x360) |

효과는 세 가지다.

- 셰이딩과 절차적 디테일이 픽셀 크기로 자동 필터링된다(`ddx`/`ddy`가 셀 크기가 됨). 나무결·네뷸라 노이즈가 격자에서 지글거리던 원인이 사라진다.
- 엣지 필터가 내부 해상도에서 네이티브로 돌아 윤곽선이 정확히 1셀이 된다.
- 화면상 크기는 그대로다. 격자 자체는 이미 480x270이었고 렌더 지점만 옮긴 것이다.

`crispUiTarget`(스크린 해상도, Bilinear)은 분리돼 있어 UI는 영향받지 않는다.

**한계.** 이 변경은 셰이딩·텍스처 디테일의 지글거림을 없애지만, 구르는 주사위 **실루엣 계단**이 프레임마다 재배열되는 것은 3D 회전에 내재한다. §7의 동적 지표로 정량화하고, 수치가 나쁘면 굴림 중 회전을 단계로 스냅하는 방안을 다음 마일스톤에서 검토한다.

## 6. 비교와 롤백 구조

채택 여부가 정해지지 않았으므로 구조 자체에 A/B와 롤백을 넣었다. 세 층이다.

### 6.1 런타임 토글

`RenderStyle { Baseline, Cel }` 한 번의 전환으로 아래가 동시에 바뀐다. `V` 키와 HUD의 `Style:` 버튼이 왕복시킨다. 기존 quantize 3단 순환(`Q`)과 직교하므로 조합을 모두 볼 수 있다.

| 대상 | Baseline | Cel | 담당 |
|---|---|---|---|
| 주사위 바디 재료 | URP Lit | `Tessera/CelSurface` | `DiceVisualPool.SetRenderStyle` |
| 테이블·러너·소품 재료 | URP Lit | `Tessera/CelSurface` | `CelStyleSwitcher.Apply` |
| 키 라이트 그림자 | `Soft` | `Hard` | `YachtLightingRig.SetRenderStyle` |
| SSAO 렌더 피처 | 활성 | 비활성 | 같음 |
| 월드 카메라 렌더 타깃 | 1920x1080 | 내부 해상도 | `YachtCameraRig.SetRenderStyle` |
| 엣지 임계값 | 에셋 값 | 런타임 덮어쓰기 | `PixelEdgeRendererFeature.CelOverrideEnabled` |

**기본값은 Baseline이다.** 아무것도 채택하지 않은 상태가 M14까지의 화면과 같다.

### 6.2 코드 경로 보존

기존 PBR 경로를 삭제하지 않았다.

- `DicePaletteCatalog.GetBodyMaterial(DieType)`은 그대로 두고 `(DieType, RenderStyle)` 오버로드를 더했다. 캐시도 두 벌이다.
- `DiePaletteDefinition`의 `Metallic`/`Smoothness`는 남는다. 밴드 수는 `Metallic`에서 파생시킨다.
- 엣지 피처의 직렬화된 임계값은 Baseline 기준 그대로다. Cel은 런타임 덮어쓰기만 쓴다.

죽은 코드를 남기는 것이 원칙에 어긋나지만, 여기서는 "채택 여부 미결정"이 실제 상태다. 채택이 확정되면 별도 정리 마일스톤에서 Baseline 경로를 제거한다.

### 6.3 테이블·소품은 빌더를 고치지 않았다

계획 초안은 재질 생성 지점마다 분기를 넣는 것이었다. 실제로는 `CelStyleSwitcher`가 이미 만들어진 렌더러를 훑어 URP Lit 재질을 셀 재질로 바꾸고 원본을 들고 있다가 되돌리는 방식으로 했다.

이유는 두 가지다.

- 재질 생성 지점이 `Assets/Scripts/Tabletop/` 아래에 열 곳 넘게 흩어져 있어, 분기를 넣으면 롤백 지점이 그만큼 늘어난다.
- 빌더 코드를 건드리지 않으면 Baseline 경로가 문자 그대로 무손상이다.

이미 Unlit인 재질(촛대, 룬 슬레이트 채널 등)은 두 모드 공용이라 그대로 둔다. 주사위와 Crisp UI 레이어는 각자 담당이 있어 제외한다.

### 6.4 되돌릴 수 없는 에셋 변경과 복구값

런타임 토글로 덮이지 않는 변경은 원래 값을 여기 적는다. 한 줄 수정으로 복구할 수 있다.

| 파일 | 필드 | 원래 값 | 현재 값 |
|---|---|---|---|
| `Assets/Settings/PC_Renderer.asset` | `m_RenderingMode` | `2` (Deferred) | `0` (Forward) |

`m_SoftShadowsSupported`와 `m_MainLightShadowmapResolution`은 결국 건드리지 않았다. 그림자 하드화를 라이트 컴포넌트 쪽(`keyLight.shadows`)에서만 처리해 런타임 토글로 완전히 덮이기 때문이다.

## 7. 지표

M13이 쓴 "고유 색 수"는 616 → 30까지 줄고도 3D로 읽혔으므로 판단 근거로 부적합했다. `PixelReadabilityMetrics`가 대체 지표 세 가지를 순수 함수로 제공하고, 합성 입력으로 검증한다(`PixelReadabilityMetricsTests`).

| 지표 | 의미 | 기대 |
|---|---|---|
| 밝기 밴드 수 | 계조가 몇 단계로 끊겼는가 | 주사위 크롭에서 3 이하(금속 4 이하) |
| 최대 동일색 연결 영역 비율 | 평면이 실제로 넓은가 | Baseline 대비 크게 증가 |
| 굴림 중 최대 변화 셀 비율 | 움직일 때 화면이 얼마나 들끓는가 | Baseline 대비 감소 |

연결 영역은 4방향으로 센다. 대각까지 세면 디더 체커보드가 하나의 큰 영역으로 잡혀 지표가 무의미해진다.

`Tools/Tessera/Run Pixel Readability Validation`이 한 실행 안에서 Baseline과 Cel을 모두 재고 표로 출력한다. 두 방식은 렌더 타깃 크기가 다르므로 캡처를 모두 가상 격자로 리샘플한 뒤 비교한다. 업스케일 셰이더가 화면에 하는 것과 같은 셀 중심 점 샘플링이다.

## 8. 구성 요소

| 파일 | 역할 |
|---|---|
| `Assets/Rendering/Shaders/CelSurface.shader` | 셀 셰이더 세 패스 |
| `Assets/Rendering/Shaders/CelSurfaceShading.hlsl` | 노멀 스냅과 밴드 규약, 상수 버퍼 |
| `Assets/Scripts/Rendering/RenderStyle.cs` | 연출 방식 열거형 |
| `Assets/Scripts/Rendering/CelMaterialFactory.cs` | 셀 재질을 만드는 유일한 지점 |
| `Assets/Scripts/Rendering/CelStyleSwitcher.cs` | 씬 재질 교체와 복구 |
| `Assets/Scripts/Rendering/PixelReadabilityMetrics.cs` | 지표 순수 함수 |
| `Assets/Editor/RunPixelReadabilityValidation.cs` | Baseline/Cel 동시 측정 도구 |
| `Assets/Editor/CelSurfaceTests.cs` | 셰이더·재질·Forward 계약 |
| `Assets/Editor/PixelReadabilityMetricsTests.cs` | 지표 검증 |

## 9. 조작법

| 입력 | 동작 |
|---|---|
| `V` / HUD `Style:` | Baseline ↔ Cel |
| `Q` / HUD `Quant:` | 포스트 양자화 Off → Steps → Palette |
| `F1` / `F2` | 내부 해상도 640x360 / 480x270 |
| `F3` / HUD `Edge:` | 픽셀 엣지 필터 |

## 10. 남은 확인

- **엣지 임계값 재조정.** `PixelEdgeRendererFeature`의 Cel 덮어쓰기 값(`0.70` / `0.40` / `(0.35, 0.80)`)은 손으로 맞춘 시작값이다. 화면 확인 후 재조정이 필요하다. 미완으로 남아 있던 `M13-T4`를 여기서 흡수했다.
- **특수 연출 셰이더 축소.** `DiceCosmicCube`·`DiceCosmicCore`·`DiceCosmicCrystalShell`·`DiceOrb*` 계열은 프레넬 림, 시차 내부 깊이, 별밭, 유리 알파 블렌드로 되어 있어 픽셀아트와 정면 충돌한다. 연출 자산을 가장 많이 소모하는 작업이라 착수 전 별도 확인이 필요하다. `_CelMode` 프로퍼티로 연속 항과 계단 항을 나란히 두어 토글에 편입시키는 것이 계획이다.
- **SSAO 제거 후 접지감.** 먼저 끄고 판단한다. 부족하면 주사위 아래 1셀 하드 그림자 프록시로 대체한다.
- **실루엣 픽셀 크롤.** §5의 한계 참조.
- **아트 가이드 팔레트의 중성색·시안 부재.** 셀 램프는 재료 색을 그대로 쓰므로 이번 범위에서는 문제되지 않지만, Palette 모드를 다시 켜면 §9.7(`pixel_edge_filter_plan.md`)의 한계가 재현된다.
