# 코인 메시 반입과 앞뒷면 구분 계획서 (`M17-T23`)

> **문서 종류**: 작업 지시서 (AI 대상)
> **코드 대조 기준**: 2026-09-14 · 커밋 `de671a9`
> 이 문서는 위 시점의 코드에서 확인된 것만 기술합니다. 계획 항목은 상태 표기로 구분합니다.
> 작성 원칙은 [`docs/README.md`](../README.md)를 보십시오.
>
> `M17-T23` 코인 메시 반입과 앞뒷면 구분 계획입니다.

작성일: 2026-09-14 · 개정: 2026-09-14 (1차 시각 확인 피드백 반영: 굵기 보정 1텍셀·스케일 0.8·필드/문양 음각 §4.4·§4.5·§4.6·§6) · 2026-09-15 (2차 재확인 통과, `M17-T23-8` 사용자 결정 현행 유지) · 2026-09-20 (`M17-T23-7` 3차 재확인 통과, 최종 완료) · 상태: `DONE` (`M17-T23-1`~`M17-T23-8` 완료) · 요약은 [`docs/agent/work_plan.md`](work_plan.md) §7 `M17` 표에 있다.

---

## 1. 배경 (Context)

`coin-toss` 증강(HOLD)을 구현하기 전에 동전 3D 모델을 먼저 프로젝트에 들인다. 소스는
`C:\Users\dpcks\Downloads\source\coin.glb`(Blender glTF 2.0, 45KB, `gold-coin.zip`에서 풀린 파일).

문제 두 가지:
1. **Unity가 `.glb`를 임포트하지 못한다.** `Packages/manifest.json`에 glTFast/UniGLTF 없음. 프로젝트에 glb 선례도 없음.
2. **앞뒷면이 구분되지 않는다.** 텍스처 없이 단색 머티리얼 2개뿐이고, 앞면·뒷면이 같은 UV 사각형(`u 0.03~0.47, v 0.53~0.97`)을 공유해 텍스처를 입혀도 양면에 같은 그림이 찍힌다.

사용자 결정: UV 문제는 Unity 에디터 베이커로 해결(Blender 왕복 없음) / 범위는 반입 + 앞뒤 구분까지(증강 로직·물리·굴림 제외) / 문양은 사용자 제공 이미지 사용.

현재 코인 관련 코드·에셋은 프로젝트에 전혀 없다. `coin.glb`, `head.png`, `tail.png`는 사용자 로컬 다운로드 폴더에만 있고, 이 문서의 경로 표기는 반입 전 출처 기록일 뿐이다.

---

## 2. 사용자 제공 문양 (실측)

| 면 | 원본 파일 | 도안 | 크기 | 형식 | 잉크 영역(bbox) |
|---|---|---|---|---|---|
| head | `Downloads/head.png` | 고전 흉상(곱슬머리 청년) | 2738×1536 | RGBA, 실제 투명 배경 + 검은 잉크 | 1137×1448, 세로로 긴 사각 |
| tail | `Downloads/tail.png` | 월계관을 두른 리라 | 1369×768 | RGBA, 실제 투명 배경 + 검은 잉크 | 668×658, 거의 원형 |

- 대화창 미리보기의 체커보드는 렌더링 표시일 뿐이다. 불투명 회색 픽셀은 0개로, 배경은 진짜 알파다. 정리 작업 불필요.
- 둘 다 **흑백 선화**다. 색은 변환 단계에서 입힌다.

**획 두께 측정** (잉크 bbox 긴 변을 1000px로 정규화한 뒤 침식으로 남는 잉크 비율):

| 커널 | 3 | 5 | 9 | 13 | 17 | 25 | 33 |
|---|---|---|---|---|---|---|---|
| head | 0.89 | 0.78 | 0.56 | 0.37 | 0.25 | 0.14 | 0.08 |
| tail | 0.78 | 0.55 | 0.16 | 0.02 | 0 | 0 | 0 |

- tail(리라) 획은 대부분 도안 폭의 **0.5~0.9%**. 리라 줄 간격은 약 4%.
- head(흉상) 획은 약 **1~1.3%**, 목 그림자·받침대 같은 채움 덩어리만 3% 이상.

**가독성 예측**: 480×270 픽셀 필터에서 문양 평면부는 약 16칸이다. tail 획은 0.1칸, head 획은 0.2칸 수준이라 **원본 획 그대로는 둘 다 사라진다.** 리라 줄 7가닥은 한 덩어리로 합쳐진다. 굵기를 보정해도 픽셀 필터 경로에서 기대할 수 있는 것은 "흉상 덩어리 vs 링+U자" 실루엣 구분까지다. 흉상 얼굴·머리칼, 월계수 잎은 CrispUI 승격(원본 해상도 합성, 면 약 70px)에서만 읽힌다. 이번 완료 조건인 "앞뒤 구분"은 실루엣으로 충족 가능성이 있고, 세부 판독은 `M17-T23-8`의 사용자 판단 뒤 결정한다.

---

## 3. 소스 메시 실측 (삼각형 기하 법선 기준, 검증 완료)

| prim | 머티리얼 | 윗면 tri | 아랫면 tri | 옆면 tri | 윗/아랫 정점 공유 |
|---|---|---|---|---|---|
| 0 | `Coin` (면+챔퍼) | 428 | 414 | 0 | 0 |
| 1 | `CoinRim` (테두리) | 186 | 188 | 472 | — |

- prim 0은 앞면·뒷면 원반뿐이다. `faceNormal.y` 부호로 경계 삼각형 없이 둘로 갈리고(최소 `|ny|` 0.745), 정점 집합도 겹치지 않는다.
- 캡 UV는 XZ 평면 아핀 투영(`u ≈ 0.246x + 0.25`, `v ≈ 0.246z + 0.75`)이고 앞뒤 부호가 같다.
- 치수: 지름 1.987, 두께 0.200. 노드 변환 없음. 인덱스 ushort, 비인터리브.

---

## 4. 설계

### 4.1 반입 경로: 에디터 전용 C# 최소 glb 파서

- Blender CLI → FBX 변환은 서브메시 분할 베이커를 없애 주지 못하면서 외부 의존과 `Use File Scale` 함정(깃펜 전례)만 더한다.
- 파서 범위는 이 파일에 필요한 것만: GLB 헤더·JSON 청크(`Newtonsoft.Json`, 설치됨)·BIN 청크, float32 VEC3/VEC2, ushort/uint SCALAR. 그 외 형식이면 명시적 예외.
- **축 변환은 파서 한 곳에서**: position·normal의 X 반전, `uv.v → 1 - v`, 인덱스 와인딩 `(i0, i2, i1)` 반전. Y는 보존되므로 `y > 0`이 그대로 앞면이다.

### 4.2 분할과 UV

- prim 0 → `faceNormal.y > 0` 이면 Head, 아니면 Tail. `|ny| < 0.3` 삼각형이 나오면 오류 로그 후 중단(소스 교체 회귀 감지).
- prim 1 → 통째로 Edge.
- 버킷마다 정점 재색인 패스를 거쳐 독립 메시로 만든다.
- **캡 UV는 베이커에서 XZ 재투영**: `R` = 캡 최대 XZ 반지름(실측), Head `u = x/2R + 0.5, v = z/2R + 0.5`, Tail은 `v = -z/2R + 0.5`. 텍스처 전체(0~1)를 쓰게 되고(원본 UV는 44%만 사용), Tail은 X축으로 뒤집었을 때 정립으로 읽힌다. Edge UV는 원본 유지.

### 4.3 구조: 서브메시가 아니라 자식 GameObject 3개

```
Coin.prefab (root, layer Decoration=11, localScale = WorldDiameter / SourceDiameter)
├── Body       Coin_Edge.mesh  ← CoinEdge.mat
├── Face_Head  Coin_Head.mesh  ← CoinHead.mat
└── Face_Tail  Coin_Tail.mesh  ← CoinTail.mat
```

레이어는 GameObject 단위라, 선화 세부를 살리려고 CrispUI로 올릴 때 면만 따로 승격할 수 있어야 한다. 위 가독성 예측상 승격 가능성이 높으므로 이 구조가 필수다. `DiceShapeBaker`의 `Body` + `Pip_*` 구조와 같은 관례.

### 4.4 크기

- **갱신(사용자 1차 시각 확인 피드백, 2026-09-14)**: `ModelScale = 0.8f`(구 `WorldDiameter = 1.20f` 방식을 대체). 지름 약 1.59(주사위 한 변 `DiceBoardMetrics.ArrangedDieSize = 1.014`의 약 1.57배). 프리팹 루트 `localScale` 0.6039 → 0.8. 메시는 소스 단위 유지.
- ~~`SourceDiameter = 1.98719f`, `WorldDiameter = 1.20f`~~ (초판 값, 시각 확인에서 너무 작다고 판정돼 폐기)
- 480×270에서 동전 크기는 위 스케일 갱신에 따라 재실측 필요(초판 "약 20칸" 수치는 구 스케일 기준).

### 4.5 머티리얼 (URP/Lit, `Shader.Find(...) ?? "Standard"` 관례, 텍스처는 `mainTexture`/`_BaseMap`/`_MainTex` 셋 다)

| 머티리얼 | Base Color | Metallic | Smoothness | 텍스처 |
|---|---|---|---|---|
| `CoinEdge` | `#E5A93C` (runner-gold) | 0.88 | 0.75 | 없음 |
| `CoinHead` / `CoinTail` | 흰색 | 0.0 | 0.12 | 면 앨비도 |

- 평평한 금속 캡은 키라이트(60°)·카메라 피치(75°) 조합에서 스페큘러 정점에 걸려 면 전체가 흰 덩어리가 된다. 그래서 금속감은 곡면 테두리에만 두고 캡은 무광으로 둔다. 원본 오렌지(`#FF8200`)는 팔레트 밖이라 버린다.
- `globalIlluminationFlags = None` (`DicePaletteCatalog.cs:211` 선례).
- 캡이 그래도 뭉개지면 캡 셰이더를 URP/Unlit으로 교체(프로퍼티 이름 동일, 한 줄).

**갱신(사용자 1차 시각 확인 피드백, 2026-09-14)**: "동전 내부원이 너무 평평하다, 음각 느낌 필요"는 지적에 필드(테두리 안쪽 원판)와 문양 획 둘 다에 반영.
- **메시**: 캡 평판 높이 `|y|` 0.0696 → 0.03으로 파고(테두리 안턱 0.0996 대비 깊이 0.03 → 0.07). 필렛은 선형 재매핑으로 이음매 유지. 법선은 높이축 확대에 맞춰 해석적으로 보정(`n = (nx·k, ny, nz·k).normalized`, `k = 0.0696/0.03`), 평판 법선은 수직 유지.
- **텍스처**: 단색 2색(잉크/바탕) 대신 3색 음각 음영. 바탕 `#E5A93C`(×1.0), 홈 안쪽 `#956E27`(×0.65), 홈 그림자 `#503B15`(×0.35, 구판의 잉크색 재사용). 빛이 들어오는 쪽 홈 벽이 그림자. 광원 방향 텍셀 `(+1, -1)`은 키라이트 `Euler(60,-35,0)`(`YachtSceneAssembler.cs:139`)와 카메라 yaw 0(`:159`)에서 도출. 뒷면도 `v = -z` 투영이라 뒤집힌 상태에서 같은 방향을 쓴다.

### 4.6 선화 → 동전 면 앨비도 변환 (에디터, 순수 함수)

원본 선화는 `Assets/Art/Source/Coin/coin_head_source.png`(흉상), `coin_tail_source.png`(리라)로 복사해 보존한다. 베이커가 이를 읽어 면 앨비도를 `Assets/Art/Generated/Coin/Textures/coin_{head,tail}_albedo.png`로 굽는다. 원본은 덮어쓰지 않는다.

변환 단계(`CoinFaceTextureConverter`, 입력 `Color32[]`·크기 → 출력 `Color32[]`):
1. **잉크 마스크**: `alpha ≥ 128 && 휘도 < 128` 이면 잉크.
2. **크롭·원 안에 맞춤**: 잉크 bbox로 자르고, bbox 반대각선이 도안 원 반지름(텍스처 반지름의 90%, 바깥 10%는 챔퍼에 감기므로 비움) 안에 들어가게 균일 축소 후 중앙 배치. 흉상은 세로로 긴 사각이라 리라보다 작게 들어간다(허용).
3. **굵기 보정**: 출력 해상도에서 최소 획이 `MinStrokeTexels`(기본 2) 이상이 되도록, 축소 전 원본 해상도에서 잉크를 팽창(dilate)한다. 팽창 반경 = `MinStrokeTexels × (원본 px / 출력 텍셀) / 2 − 측정 반두께`. 가는 선이 사라지는 대신 굵어져 붙는 쪽을 택한다.
4. **축소**: 텍셀별 잉크 커버리지가 `CoverageThreshold`(기본 0.35) 이상이면 잉크. 보간 없이 두 값만 남긴다.
5. **색 입히기**: 잉크 → `#503B15`(runner-gold × 0.35), 바탕 → `#E5A93C`(runner-gold × 1.0). 명도 램프 칸에 정확히 얹어 색 양자화에서 튀지 않게 한다. 순백·밝은 금(`× 1.45`) 사용 안 함.

**갱신(사용자 1차 시각 확인 피드백, 2026-09-14)**: 굵기 보정 2텍셀 → 1텍셀(양면 모두), §4.5의 3색 음각 음영 반영.
- `CoinFaceTextureConverter.Convert`에 `Vector2Int lightFrom` 인자 추가. `DefaultMinStrokeTexels` 2 → 1. 색 상수 `InkColor` → `#956E27`(홈 안쪽), 신규 `InkShadowColor` `#503B15`(홈 그림자, 구판 잉크색 재사용). 잉크 마스크·크롭·굵기 보정 단계는 동일하되, "5. 색 입히기" 단계가 잉크/바탕 2값이 아니라 광원 방향(`lightFrom`)에 따라 잉크 픽셀을 안쪽 색과 그림자 색으로 나눈다.
- 베이커는 `WorldDiameter`/`SourceDiameter` 대신 `ModelScale = 0.8f`, `FaceLightFrom = (1, -1)`, 필드 상수 `SourcePlateauHeight`/`RimLipHeight`/`FieldPlateauHeight`(§4.4 깊이 값)를 쓴다.
- 초판(2텍셀·2색) 실측(64×64 픽셀 분포 head 바탕 3170 / 잉크 나머지, tail 유사)은 §6 `M17-T23-5` 행에 이력으로 남아 있다. 1텍셀·3색 갱신 후 실측은 §6 `M17-T23-7` 행 참조.

해상도는 두 벌을 상수로 둔다.
- `PixelPathSize = 64`: 픽셀 필터 경로 기본값. 텍셀 ≈ 격자 한두 칸.
- `CrispPathSize = 256`: CrispUI 승격 시 사용(면 약 70 화면 px의 3.6배, 굵기 보정 `MinStrokeTexels = 3`).

이번 범위에서는 64만 머티리얼에 연결하고, 256은 `M17-T23-8` 판단 뒤 필요하면 쓴다.

임포터 강제(`WoodPlankTextureGenerator.cs:327-336` 패턴): Point, 밉맵 off, 무압축, Clamp, `npotScale None`. 원본 선화 `*_source.png`는 `isReadable = true`로 두고 게임에서 참조하지 않는다.

---

## 5. 파일

| 경로 | 역할 |
|---|---|
| `Assets/Art/Source/Coin/coin.glb` | 메시 소스 복사본. Unity는 DefaultAsset으로 두고 베이커가 `File.ReadAllBytes`로 읽음 |
| `Assets/Art/Source/Coin/coin_head_source.png`, `coin_tail_source.png` | 사용자 선화 원본(`Downloads/head.png`, `tail.png`) |
| `Assets/Editor/Dice/CoinGlbReader.cs` | glb → 축 변환된 프리미티브 배열. 순수 함수 |
| `Assets/Editor/Dice/CoinFaceTextureConverter.cs` | 선화 → 면 앨비도 픽셀. 순수 함수 |
| `Assets/Editor/Dice/CoinMeshBaker.cs` | 메뉴 `Tessera/Bake/Coin Model`. 분할·재투영(순수 static) + mesh/mat/png/prefab 저장. 자동 호출 안 함 |
| `Assets/Art/Generated/Coin/{Meshes,Materials,Textures}/` | 산출물 |
| `Assets/Prefabs/Coin/Coin.prefab` | 산출물 |
| `Assets/Editor/Tests/Dice/CoinGlbReaderTests.cs`, `CoinFaceTextureConverterTests.cs`, `CoinMeshBakerTests.cs` | EditMode 테스트 (메서드명 영문) |

위 파일은 `M17-T23-1` 작성 시점에는 전부 계획 항목이었다. `M17-T23-2`~`M17-T23-6` 완료(2026-09-14)로 전부 실재하며, §6에 검증 결과가 있다.

재사용: `DiceShapeBaker.SaveMesh`/`SavePrefab` 패턴(`Assets/Editor/Dice/DiceShapeBaker.cs:468,484`), `AugmentScrollAssetGenerator.CreateOrReplaceLitMaterial`(`Assets/Editor/Yacht/AugmentScrollAssetGenerator.cs:101-122`), 임포터 설정(`Assets/Editor/Tabletop/WoodPlankTextureGenerator.cs:327-336`), 순수 픽셀 함수 + 테스트 관례(`AugmentStickerTexture.CreatePixels` ↔ `AugmentStickerTextureTests`).

---

## 6. 작업 분해

| ID | 작업 | 검증 |
|---|---|---|
| `M17-T23-1` | 계획서·work_plan 요약 작성 (`tessera-scribe`) | 문서 링크 확인. **완료(2026-09-14)** |
| `M17-T23-2` | glb·선화 복사, `CoinGlbReader` + 테스트 (`tessera-implementer`) | EditMode: 프리미티브 2, 정점 470/586, 인덱스 2526/2538, 바운드 실측 일치, 변환 후 전 삼각형 `dot(faceNormal, 정점법선합) > 0`. **완료(2026-09-14)**: `CoinGlbReaderTests` 5/5 통과 |
| `M17-T23-3` | `CoinFaceTextureConverter` + 테스트 | EditMode(합성 입력): 1px 가는 선이 64 출력에서 사라지지 않고 최소 2텍셀 굵기, 출력 색이 정확히 두 값, 바깥 10% 링에 잉크 0, 정사각·세로 직사각 입력 모두 원 안에 중앙 배치, 좌우 비대칭 입력의 방향 보존. **완료(2026-09-14)**: `CoinFaceTextureConverterTests` 8/8 통과. 구현 중 수정: 풀링 영역을 소스 전체가 아니라 크롭 bbox로 제한(가장자리 클램프로 bbox 경계 잉크가 격자 모서리까지 번지던 버그 수정) |
| `M17-T23-4` | `CoinMeshBaker` 분할·재투영 + 테스트 | EditMode: Head 428 / Tail 414 / Edge 846 tri, Head·Tail 정점 교집합 0, Head 전 정점 `y > 0`, 캡 UV ∈ [0,1], 중심 ≈ (0.5, 0.5), Head max-z 정점 v≈1 · Tail v≈0. **완료(2026-09-14)**: `CoinMeshBakerTests` 8/8 통과, 삼각형 수 실측대로 Head 428 / Tail 414 / Edge 846 확인. 구현 중 수정: 캡 UV 중심 테스트를 "축에 가장 가까운 정점" 대신 "캡 정점 평균 UV"로 교체(데시메이트 원반이라 캡 안쪽 정점이 없음. 최소 반지름 0.817 / R 0.897) |
| `M17-T23-5` | 메뉴 실행으로 에셋·프리팹 산출 (`tessera-unity-operator`) | 프리팹 자식 3개 이름, 메시가 에셋 참조, `localScale ≈ 0.6039`, 레이어 11, 앨비도 임포터 Point/밉맵 off/무압축 — REST 조회로 확인. **완료(2026-09-14)**: 베이킹 산출물 9개 존재, 텍스처 64×64 Point·밉맵 off·무압축·Clamp 확인, 프리팹 `localScale` 0.6039·레이어 11·콜라이더 없음 확인. 텍스처 잉크 비율 head 26.3% / tail 34.1%. 64×64 결과 관찰: head(흉상)는 머리 덩어리·얼굴 윤곽·받침대가 구멍 있는 실루엣으로 남음, tail(리라)는 월계관과 리라 획이 굵기 보정으로 합쳐져 거의 꽉 찬 원반이 됨(리라 형태 소실). 가독성 판단은 `M17-T23-7`·`M17-T23-8`에서 |
| `M17-T23-6` | 컴파일 + EditMode 전체 (`tessera-verifier`) | 전체 통과, 기존 테스트 회귀 없음. **완료(2026-09-14)** |
| `M17-T23-7` | 씬에 주사위 옆 배치 후 시각 확인 | **사용자 확인**(3D 외형 시각 확인은 사용자 담당). 판정: 앞(흉상)·뒤(리라)가 다르게 보임 / 캡 번짐 없음 / 테두리 금속 띠 / 크기 자연스러움 / 뒷면 반전 없음. **1차 확인(2026-09-14, 피드백 반영 완료)** — 피드백 3건: (1) 굵기 보정 2텍셀 → 1텍셀(양면), (2) 프리팹 루트 `localScale` 0.6039 → 0.8(지름 약 1.59, 주사위 한 변의 약 1.57배), (3) 동전 내부원이 너무 평평함 → 음각 느낌 필요(필드+문양 획 둘 다, 사용자 선택). 반영: 캡 평판 높이 `\|y\|` 0.0696 → 0.03(§4.4), 3색 음각 음영 텍스처(§4.5·§4.6). 검증: `CoinGlbReaderTests` 5/5, `CoinFaceTextureConverterTests` 11/11(신규 `OutputContainsOnlyThreeKnownColors`·`CenterSquareShadesEdgesOnTheLightSide`·`ZeroLightProducesNoShadow`·`FieldPlateauIsDeepenedBelowRimLip`·`FieldPlateauNormalsStayVertical`·`CapWindingMatchesNormalsAfterDeepening`), `CoinMeshBakerTests` 11/11 통과. 재굽기: `localScale` 0.8, Head y 범위 약 0.03~0.0996(Tail 반대), 삼각형 428/414/846 유지, 64×64 Point·밉맵 off 유지. 픽셀 분포 head 바탕 3170 / 안쪽 752 / 그림자 174, tail 2863 / 1057 / 176, 그 외 색 0. 1텍셀 결과 관찰(문자 미리보기): head는 머리칼 곱슬 윤곽·얼굴·목·받침대가 구분됨, tail은 이전의 꽉 찬 원반에서 리라 U자 몸통·줄·월계관 테두리가 드러나는 형태로 개선. 재굽기 중 경고 3건(`Main Object Name '' does not match filename 'Coin_Head'` 등) 발견 → 메시 이름 미설정이 원인(`EditorUtility.CopySerialized`가 이름 없는 새 메시의 빈 이름까지 복사). `SaveMesh`에서 `mesh.name = assetName` 지정 후 재굽기로 경고 소멸 확인. 검증 중 씬 미저장 상태로 EditMode 테스트가 차단된 일이 있었고 사용자가 확인용 코인을 씬에서 지우고 저장함. **2차 재확인(2026-09-15, 통과, `DONE`)**: `tessera-unity-operator`가 `Augmented Dice.unity`에 확인용 인스턴스 2개(`Coin_CheckHead` `(4.4, 2.814, -0.3)` 무회전, `Coin_CheckTail` `(5.6, 2.814, -0.3)` 뒤집음, 둘 다 스케일 0.8·레이어 Decoration) 배치. 씬에 주사위가 없어(런타임 스폰) 위치는 `DiceBoardMetrics` 상수 역산. `scene_screenshot`이 Game View 미렌더 상태에서 파일을 쓰지 않아 `camera_screenshot`(`Full Field World Camera` 오프스크린, 960x540)으로 대체해 확인. 사용자 판정: 통과. 코멘트: 리라(뒷면) 실루엣은 잘 안 보이지만 그대로 사용. 확인 후 인스턴스 제거·씬 저장. **3차 확인(2026-09-20, 통과, 최종 완료)**: 사용자가 육안 확인 후 완료 판정 |
| `M17-T23-8` | 가독성 후속 판단 (사용자 결정) | 실루엣만으로 부족하면 순서대로: `MinStrokeTexels`·`CoverageThreshold` 조정 → `WorldDiameter` 확대 → CrispUI 승격(256 앨비도 + `CrispUiDepthMask`, 별도 태스크로 분리). **사용자 결정(2026-09-15, `DONE`)**: 현행 유지. 세 후속 조정 모두 하지 않음. `CrispPathSize = 256` 경로는 미사용으로 남음 |

`M17-T23-2`~`M17-T23-4`는 Unity 없이 판정되고, Unity 접근 에이전트(`M17-T23-5`·`M17-T23-6`)는 직렬 실행한다. `graphify update .`는 코드 변경 후 실행. 커밋은 M17 마일스톤 단위 규칙대로 별도 허가.

---

## 7. 범위 밖

짓지 않음: 런타임 로드 코드, 콜라이더, CrispUI 승격 컴포넌트, 굴림·뒤집기, `coin-toss` 로직.

---

## 8. 확인이 필요한 열린 항목

1. `coin.glb`(`gold-coin.zip`)와 head/tail 선화의 출처·라이선스
2. 면 배정: head = 흉상(`head.png`), tail = 리라(`tail.png`)로 파일명 그대로 따름
3. 태스크 ID를 `M17-T23`으로 붙여도 되는지 (HOLD 증강의 선행 에셋이라 M17 범위와 성격이 조금 다름)
