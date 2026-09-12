# 깃펜 깃털 흰색 전환과 갈라짐 연출 계획서 (`M17-T19`)

작성일: 2026-09-09 · 개정: 2026-09-12 · 상태: `DROPPED` · 요약은 [`docs/augmented_yacht_work_plan.md`](augmented_yacht_work_plan.md) §7 `M17` 표에 있다.

> **폐기됨 (2026-09-12).** 깃펜을 절차적으로 만드는 방식 자체를 그만두고 외부 로우폴리 모델
> `Assets/Art/Reference/quill_pen_low.fbx` 로 교체했다. 이 문서가 정한 슬릿 주기, 깃가지 격자,
> 픽셀 예산, 메시 노치 상수는 모두 `InkwellAndQuill` 의 절차적 생성 코드를 전제로 한 것이라
> 함께 무효가 된다. 그 코드와 `QuillFeatherAppearanceTests`, `QuillFeatherAssetRefresh` 도
> 같은 변경에서 삭제했다.
>
> 폐기 이유는 결과물이 나빠서가 아니라 비용 때문이다. 화면이 480x270 가상 격자로 필터링되는
> 탓에 여기서 맞춘 디테일이 대부분 화면에 남지 않았고, 그런데도 상수를 바꿔 굽고 눈으로
> 확인하는 왕복이 계속 필요했다. 아래 내용은 당시 판단 근거로만 남긴다.

---

## 1. 목적

`M17-T18` 호버 깃펜 필기 연출로 깃펜이 화면 중앙에 자주 올라오면서 두 문제가 드러났다.

- 현재 깃털은 크림 `(0.90,0.82,0.70)` → 웜 토피 `(0.72,0.50,0.30)` → 마호가니 `(0.34,0.20,0.12)` 3단 웜 브라운이라 흰 깃펜으로 읽히지 않는다.
- 깃면은 넓은 쪽 가장자리에 얕은 V자 노치 6개만 있고 안쪽은 완전히 메워진 판이라 깃가지가 갈라진 느낌이 없다.

이번 작업은 (1) 깃털을 오프화이트 아이보리로 바꾸고, (2) 알베도 알파 채널로 깃가지 사이 틈을 뚫어 깃면 안쪽까지 실제로 갈라지게 하며, (3) 메시 실루엣 노치를 깊게 한다.

완료 조건: 잉크통 꽂힘 자세와 필기 자세 양쪽에서 깃털이 "흰 깃털이고 깃가지가 갈라져 있다"로 읽힌다.

---

## 2. 현재 상태에서 확인한 사실

| 사실 | 근거 |
| --- | --- |
| 깃털 색은 메시가 아니라 절차적 알베도 텍스처가 낸다. 머티리얼 `_BaseColor`는 순백이고 팔레트는 코드에 하드코딩돼 있다 | `Assets/Scripts/Tabletop/InkwellAndQuill.cs` L639~642 |
| 버텍스 컬러는 안 쓴다. 깃털 메시 채널은 position/normal/uv0뿐이고, 프로젝트 전체에서 버텍스 컬러 사용은 `RollCosmicCube` 한 곳뿐이다 | 코드 조사 |
| 베이커가 머티리얼 키워드를 보존한다. 구워진 머티리얼에 `m_ValidKeywords: [_NORMALMAP]`이 있고, 이 값은 런타임 `EnableKeyword`에서만 올 수 있다. 이번 세션에 파일을 열어 직접 확인했다 | `Assets/Art/Generated/Tabletop/Materials/3D_Inkwell_and_Quill_Decoration_Quill_Feather_Material.mat` L27~28 |
| PNG 저장 경로가 알파를 보존한다. `TryWritePng`가 Blit → ARGB32 렌더 텍스처 → RGBA32 ReadPixels → EncodeToPNG를 거치고 전 구간에 알파 채널이 있다. 임포터 `alphaSource` 기본값은 `FromInput` | `Assets/Editor/TabletopPrefabBaker.cs` L230~263 |
| **밉맵이 알파 클립 슬릿을 닫는다. 실제 결함이다.** `mipmapEnabled = true`이고, 박스필터가 듀티 40 % 슬릿의 알파를 0.6으로 평균 내 `_Cutoff = 0.5`를 전부 통과시킨다 | `ConfigureTextureImporter` (`TabletopPrefabBaker.cs` L276) |
| **전체 재베이크가 프리팹 GUID를 재발급한다.** `DeleteAsset(prefabPath)` 후 `SaveAsPrefabAsset`을 한다. `.meta`가 지워져 씬 인스턴스 링크 15개와 `AugmentedYachtController`의 `quillHoverAnimator` 직렬화 참조가 끊긴다 | `BakeProp` (`TabletopPrefabBaker.cs` L122) |
| `AssetDatabase.Contains(mesh)`인 메시를 건너뛴다. 그냥 재베이크하면 깃털은 아무것도 안 바뀐다 | `ExtractMeshes` (`TabletopPrefabBaker.cs` L139) |
| 덮어쓰지 않아 재베이크 시 `..._01.asset`이 쌓인다 | `UniquePath` (`TabletopPrefabBaker.cs` L305) |
| 프로젝트에 `.asmdef`가 없다. `Assets/Editor`의 테스트가 `internal`을 못 보므로 검증 대상 함수는 `public`이어야 한다 | 코드 조사 |
| 카메라는 직교이고 `size`는 8.2다. 세로 시야 16.4 월드 유닛 | `Assets/Scripts/Games/AugmentedYacht/YachtSceneAssembler.cs` L162~163 |
| 픽셀 필터 기본 해상도는 480×270이다. 깃펜은 `Decoration` 레이어(11)라 필터를 그대로 통과한다 | `PixelFilterSettings.StartResolution = ResolutionB` |
| 셀 모드는 알파 클립을 못 나른다. `CelVariantOf`가 `_BaseColor`·`_BaseMap`·`_Metallic`만 전달하고 `frag`는 `.rgb`만 샘플하며 `clip`이 없다. `M16`은 `DEFERRED`이고 기본값이 `Baseline`이라(`D-037`) 이번 범위 밖이다 | `CelStyleSwitcher.CelVariantOf`, `CelSurface.shader` L105 |

---

## 3. 픽셀 예산 — 모든 상수의 근거

이 절이 계획서의 핵심이다. 숫자만 남기면 다음 사람이 근거 없이 만진다. 유도 과정을 전부 남긴다.

```
직교 카메라 size 8.2         → 세로 시야 16.4 월드 유닛
픽셀 필터 480×270            → 16.46 필터픽셀 / 월드 유닛
깃털 날 로컬 3.22 × 스케일 2.5 → 월드 8.05
깃펜은 기울어져 단축된다. 보수적으로 0.5배로 잡는다.

깃털 날 화면 길이  ≈ 66 px   (정면일 때 최대 132 px)
넓은 깃면 반폭     ≈ 7.4 px  (최대 14.8 px)
```

깃면이 화면에서 7~15픽셀뿐이라는 것이 모든 판단의 출발점이다. 현재 텍스처의 barb 주파수 90은 화면 0.7 px이라 완전히 뭉갠다.

기존 사선 계수 `0.35f`가 함정인 이유: "40도 사선"이라는 주석은 512×1024 이미지 안의 각도이고, 이 텍스처가 가늘고 긴 깃털에 늘어붙으면 월드에서 깃가지는 깃대에서 겨우 17.7도 벌어진다(`atan(0.36 / (0.35 × 3.22))`). 실제 조류 깃가지는 30~45도다. 게다가 그 값이면 7.4 px 깃면을 가로지르며 슬릿을 4.2개 만나 간격 1.8 px, 구멍 0.7 px로 서브픽셀이 된다.

채택값 `BarbSlant = 1/6`, `SlitCycles = 12`의 유도:

```
깃가지 각도 = atan(0.36 / (1/6 × 3.22)) = 33.8도
길이 방향: 66/12 = 5.5 px 간격, 듀티 40 % → 구멍 2.2 px
가로 방향: 12 × 1/6 = 2회 만남 → 7.4/2 = 3.7 px 간격, 구멍 1.5 px
```

최악 조건에서 구멍 1.5~2.2 필터픽셀이고, 4×4 블록 업스케일이므로 디스플레이 6~9 px다.

`SlitCycles × BarbSlant = 2`가 정수라서 위상 보정 상수가 0이 된다. 외곽(`dist = 1`)에서 `phase(v = n/6) = frac(2n - 2) = 0`이라 슬릿 중심이 메시 노치 위치와 정확히 맞는다. `SlitCycles`가 `BarbCount`의 배수라 두 슬릿 중 하나는 실루엣 노치 바닥에, 하나는 깃가지 한가운데 떨어진다.

기각한 대안: `SlitCycles = 18`(BarbSlant 1/9, 각도 45도)은 길이 간격 3.7 px로 마진이 없다. `SlitCycles = 6`(BarbSlant 1/3, 각도 18.6도)은 빗살로 보이고 각도가 비현실적이다. 24 이상은 서브픽셀이다.

---

## 4. 공유 상수 격자

알베도 알파 슬릿·알베도 결 음영·노멀맵 결·메시 노치가 전부 이 격자를 공유한다.

```
BarbCount 6, SlitCycles 12, BarbSlant 1/6, SlitDuty 0.40
RachisHold 0.22, VaneOpen 0.92
SlitRootStart 0.06, SlitRootFull 0.16, SlitTipStart 0.72, SlitTipEnd 0.88
FeatherCutoff 0.5, MaxNotchDepth 0.40, NotchSharpness 1.8
```

`SlitCycles × BarbSlant ∈ ℤ`와 `SlitCycles % BarbCount == 0` 두 조건이 정렬의 전부다. 셋 중 하나라도 바꾸면 재확인해야 한다.

팔레트:

```
VaneIvory       (0.95, 0.94, 0.91)  깃면 바탕
VaneShadowBeige (0.86, 0.83, 0.76)  넓은 깃면 외곽 음영
BarbGrooveGray  (0.78, 0.75, 0.70)  좁은 깃면 외곽 음영
RachisHighlight (0.99, 0.98, 0.96)  중심 깃대 하이라이트
```

팔레트 교체의 파급 효과: 값 범위가 0.56(0.90 → 0.34)에서 0.17로 줄어 알베도가 형태를 거의 못 그리게 된다. 그래서 알파 슬릿·노멀맵 결·깊어진 노치 셋이 대체 부담을 지며, 노멀맵 진폭 상향과 `_BumpScale` 0.75 → 1.0은 폴리시가 아니라 필수다. `barbPattern` 진폭을 0.08에서 0.18로 올리는 것도 같은 이유다(실제 명도 변화 = 진폭 × 값범위이므로 0.08 × 0.17 = 0.014로는 안 보이고, 0.18 × 0.17 ≈ 0.031이 종전 0.08 × 0.56 = 0.045에 근접한다).

---

## 5. 알파 슬릿 설계

```
dist     = |u - 0.5| * 2
openness = SmoothStep(InverseLerp(RachisHold, VaneOpen, dist))
rootFade = SmoothStep(InverseLerp(SlitRootStart, SlitRootFull, v))
tipFade  = 1 - SmoothStep(InverseLerp(SlitTipStart, SlitTipEnd, v))
amount   = openness * rootFade * tipFade
phase    = Repeat((v - dist * BarbSlant) * SlitCycles, 1)
toCenter = Min(phase, 1 - phase) * 2
alpha    = toCenter < SlitDuty * amount ? 0 : 1
```

설계 판단:

1. 슬릿 안에서도 RGB는 그대로 둔다. 0으로 밀지 않는다. 바이리니어·밉 보간이 경계 텍셀을 섞을 때 어두운 값이 번져 남은 깃면 가장자리를 더럽히는 것을 막는다. 임포터 `alphaIsTransparency`를 켤 필요도 없어진다.
2. 이진 알파를 쓴다. 알파 클립은 어차피 하드 컷이고 반값은 밉 커버리지 보존 계산을 흐린다. `M17-T16`의 "결 경계가 계단으로 끊긴다" 아트 방향과도 맞는다.
3. `u`에 대해 대칭이다. 베이커 임포터가 코드의 `Clamp`를 `Repeat`으로 덮어써도(`TabletopPrefabBaker.cs` L274) 좌우 경계 텍셀이 같은 값을 샘플하므로 랩 이음매가 원리적으로 안 생긴다.
4. 뿌리·팁 창(0.06~0.16, 0.72~0.88)은 메시 `notchFade`(0.55~0.95) 안쪽에 든다. 메시 노치가 살아 있는 구간에서 텍스처 슬릿이 먼저 닫히므로 "틈 → 얕은 결 → 매끈한 팁"으로 수렴한다. `notchFade`는 건드리지 않는다.

---

## 6. 메시 노치 강화

`MaxNotchDepth = 0.40`이 원칙적 상한인 이유: 컬럼 최외곽에서 좌측 반폭이 `baseWidth × 1.20 × 0.60 = baseWidth × 0.72`로 우측 반폭과 정확히 같아진다. 더 깊이 가면 노치 바닥에서 좌우 비대칭이 뒤집혀 Primary Feather 실루엣이 깨진다.

실제 도달값은 0.644다. `t = s/96`이고 96/6 = 16이라 `k`는 0, 1/16, …, 15/16만 취한다. 최대 `k = 0.9375` → `0.9375^1.8 = 0.890` → `1 - 0.40 × 0.890 = 0.644`.

화면 깊이 비교:

```
개선 후: 0.36 × (1 - 0.644) = 0.128 로컬 → 0.32 월드 → 5.3 px (단축 시 2.6 px)
현재:    0.36 × (1 - 0.794) = 0.074 로컬 → 0.19 월드 → 3.0 px (단축 시 1.5 px, 사실상 안 보임)
```

약 2배지만 최악 2.6 px는 필터픽셀 1~2개짜리 계단이라 **슬릿이 주역이고 노치는 보조**라는 한계를 정직하게 남긴다.

`NotchSharpness = 1.8`의 이유: 선형 램프는 깃가지 전체에서 폭이 고르게 줄어 매끄러운 파도로 읽힌다. 1.8 지수면 각 깃가지의 앞 70 %가 최대폭의 90 % 이상을 유지하고 마지막 30 %에서 급격히 파여 이산적인 V로 읽힌다.

바꾸지 않는 것: `barbCount` 6 유지(96 = 6 × 16이라 노치 경계가 슬라이스 경계에 정확히 떨어진다), `slices` 96, `cols` 7, `BladeStartY`, `BladeLength`, 로컬 원점, 정점 수 1358, 좁은 깃면 노치 미도입(화면 폭 최대 9 px에 30 % 노치는 에일리어싱만 만든다).

---

## 7. 머티리얼 알파 클립

`_Cutoff` + `EnableKeyword("_ALPHATEST_ON")` + `_AlphaClip` + `RenderType` 오버라이드 태그 + `renderQueue = AlphaTest`(2450)를 함께 세팅해야 한다. URP 머티리얼 검증기가 `_AlphaClip` 프로퍼티를 읽어 키워드와 큐, 태그를 재유도하므로 프로퍼티를 빼면 에셋으로 구울 때 키워드가 도로 꺼진다.

건드리지 않는 것: `_Surface`는 0(Opaque) 유지, `_Blend`·`_ZWrite` 미변경. URP의 알파 클리핑은 "불투명 표면 + 클립"이지 투명 표면이 아니다. `_Surface`를 1로 바꾸면 깊이 쓰기가 꺼져 그림자와 `QuillCrispUiMask` 깊이 마스크가 동시에 깨진다.

`CreateMaterial` 헬퍼는 고치지 않고 호출부에만 붙인다. 그 헬퍼는 같은 파일의 머티리얼 7개가 공유하고, 컷아웃 파라미터를 시그니처에 넣으면 쓰지도 않는 개념을 6개 호출부에 강요한다.

---

## 8. 베이크 파이프라인

밉 커버리지 보존 2줄(`mipMapsPreserveCoverage`, `alphaTestReferenceValue = 0.5`)을 넣고, `isNormalMap` 가드를 둔다. `alphaTestReferenceValue`는 머티리얼 `_Cutoff`와 같아야 한다.

`Refresh Quill Feather Assets` 전용 도구를 만든 이유는 §2의 세 가지 사실(메시 스킵, `UniquePath` 중복, GUID 재발급)이다. 갱신 대상은 아래 4개 경로다.

- `Assets/Art/Generated/Tabletop/Meshes/`의 깃털 메시 에셋
- `Assets/Art/Generated/Tabletop/Textures/`의 깃털 알베도·노멀 PNG
- `Assets/Art/Generated/Tabletop/Materials/3D_Inkwell_and_Quill_Decoration_Quill_Feather_Material.mat`
- 프리팹 내부의 메시·머티리얼 참조(같은 GUID 유지)

같은 경로 덮어쓰기로 GUID를 보존하므로, 문제가 생겨도 `git checkout`으로 에셋만 되돌리면 씬·프리팹이 복구된다.

---

## 9. `QuillCrispUiMask` 불일치 — 수용

`CrispUiDepthMask.shader`는 UV도 텍스처도 `clip`도 없는 순수 실루엣이다. `AddMaskPart`(L69)가 `source.sharedMesh`를 그대로 재사용하므로 깊어진 메시 노치는 저절로 반영되고, 알파 슬릿만 어긋나 깃펜 아래 족보 글자에서 폭 1.5~2.2 필터픽셀짜리 조각이 사라진다.

수용 근거:

1. 오차 방향이 안전하다. 마스크의 존재 이유가 깃펜 아래 글자를 가리는 것이므로 "조금 더 가림"이지 "깃펜을 뚫고 글자가 새어 나옴"이 아니다.
2. 비용이 설계를 깬다. 셰이더에 텍스처 샘플을 넣으면 모든 깃펜 파츠가 공유하는 static 머티리얼 하나(`QuillCrispUiMask.cs` L28, L84) 구조가 파츠별 상태를 요구하게 되고, `ColorMask 0`의 저렴함이 존재 목적인 셰이더에 텍스처 페치를 붙이는 것도 역행이다.
3. 되돌릴 수 있다.

**탈출구 사전 명세.** 화면 확인에서 글자 획이 눈에 띄게 끊기면 `CrispUiDepthMask.shader`에 `_BaseMap`·`_Cutoff`와 `TEXCOORD0`을 추가하고 `frag`에서 `clip(SAMPLE_TEXTURE2D(...).a - _Cutoff)`를 하며, `AddMaskPart`에서 원본 `sharedMaterial`의 두 값을 `MaterialPropertyBlock`으로 넘긴다. 이 셰이더는 CBUFFER가 없어 이미 SRP 배처 비호환이라 MPB 비용이 0이고 static 공유 머티리얼 구조도 유지된다.

---

## 10. 작업 분할표

| ID | 작업 | 상태 | 검증 |
| --- | --- | --- | --- |
| `M17-T19-0` | 계획서 작성과 마일스톤 문서 갱신 | `DONE` | 기존 계획서 형식 일치 |
| `M17-T19-1` | 공유 상수 격자, 팔레트 교체, 알파 슬릿 | `DONE` | EditMode 테스트 1·2·3·4·5·7·9 |
| `M17-T19-2` | 노멀맵을 같은 격자에 정렬, `_BumpScale` 0.75 → 1.0 | `DONE` | 컴파일과 상수 참조 확인 |
| `M17-T19-3` | 메시 노치 강화 | `DONE` | EditMode 테스트 6·8 |
| `M17-T19-4` | 머티리얼 알파 클립 | `DONE` | 씬 깃펜 `Rebuild Geometry` 후 인스펙터에서 Alpha Clipping과 큐 2450 확인 |
| `M17-T19-5` | 베이커 밉 커버리지 보존, 헬퍼 `internal` 승격, `QuillFeatherAssetRefresh` 신설 | `DONE` | 컴파일과 메뉴 항목 노출 |
| `M17-T19-6` | 에셋 4종 갱신과 화면 확인 | `TODO` | §11 |

`M17-T19-6`은 `M17-T18-5` 화면 확인과 한 세션에 합쳐 진행한다.

---

## 11. 검증

EditMode 테스트 9개(`Assets/Editor/QuillFeatherAppearanceTests.cs`)가 각각 고정하는 내용:

| # | 고정 내용 |
| --- | --- |
| 1 | 깃대 부근은 뚫리지 않는다 (`dist < RachisHold`에서 알파 1) |
| 2 | 뿌리와 팁은 막혀 있다 |
| 3 | 외곽은 실제로 갈라진다 (알파 0과 1이 모두 나옴) |
| 4 | 좌우 대칭이라 Repeat 랩 이음매가 없다 |
| 5 | 슬릿 수가 픽셀 예산 안에 있다 |
| 6 | 슬릿이 메시 노치와 정렬된다 (`SlitCycles × BarbSlant = 2` 직접 검증) |
| 7 | 팔레트에 갈색이 없다 (마호가니 회귀 차단) |
| 8 | 노치가 깊어졌지만 비대칭을 뒤집지 않는다 (최솟값 0.60~0.68) |
| 9 | IsDeterministic |

노멀맵 정렬은 테스트하지 않는다. 같은 상수를 참조하는 것 자체가 구조적 보장이라 테스트가 상수를 두 번 적는 동어반복이 된다.

화면 캡처로만 확인되는 것:

| # | 확인 항목 | 실패 시 레버 |
| --- | --- | --- |
| 1 | 슬릿이 480×270에서 실제로 갈라짐으로 읽히는가. 픽셀 예산은 전부 예측이므로 **이번 작업의 핵심 리스크**다 | `SlitDuty` 0.40 → 0.50 (간격은 유지한 채 구멍만 굵힌다) |
| 2 | 밉 커버리지 보존이 실제로 슬릿을 열어 뒀는가 | 없음(재확인 필요 시 §8 재점검) |
| 3 | 아이보리 깃털이 양피지 점수표와 분리되는가. 둘 다 밝아 **최대 아트 리스크**다. 분리 수단은 슬릿·노치의 실루엣 파괴와 `TwoSided` 그림자다 | 음영색을 `(0.72, 0.68, 0.62)` 쪽으로 낮춘다. **갈색 재도입은 금지** |
| 4 | 웜 앰버 키라이트 `#ff9e3b`가 아이보리를 순백으로 날리는가 | `_Smoothness` 0.16은 두고 `VaneIvory`를 `(0.92, 0.91, 0.88)`로 반 단계 내린다 |
| 5 | 노멀 결이 골로 보이는가 능선으로 보이는가 | `barbSlope` 부호 반전이 유일한 레버 |
| 6 | 깃펜 아래 글자 획이 슬릿에 끊겨 보이는가 | §9 탈출구의 발동 조건 |
| 7 | 베이크 산출물 상태. `.mat`에 `_ALPHATEST_ON`·`_AlphaClip: 1`·`_Cutoff: 0.5`·`_Surface: 0`·`_ZWrite: 1`과 큐 2450(또는 -1에 `RenderType: TransparentCutout`)이 있는지 grep하고, PNG 임포터 포맷이 BC7 계열인지와 `mipMapsPreserveCoverage`가 켜졌는지 인스펙터에서 확인한다 | §8 베이크 파이프라인 재점검 |

절차: Play Mode 진입 → 기입 가능한 점수 칸에 호버해 깃펜을 시트 위로 올린다(깃털이 가장 크고 글자에 가장 가까운 자세) → 480×270과 640×360 양쪽에서 캡처 → 잉크통 꽂힘 자세에서도 캡처.

---

## 12. 미결 질문

| ID | 질문 | 필요 시점 | 잠정 결론 |
| --- | --- | --- | --- |
| `Q-018` | 슬릿 듀티를 0.40으로 둘지 0.50으로 올릴지? | `M17-T19-6` | 화면 확인 후 결정. 기본값 0.40 |
| `Q-019` | 노멀맵 결 진폭 0.22/0.18과 `_BumpScale` 1.0이 적절한지, `barbSlope` 부호를 뒤집어야 하는지? | `M17-T19-6` | 화면 확인 후 결정 |
| `Q-020` | 아이보리 바탕이 양피지와 분리되는지, 안 되면 음영색을 얼마나 낮출지? | `M17-T19-6` | 화면 확인 후 결정. 갈색 재도입은 금지 |
