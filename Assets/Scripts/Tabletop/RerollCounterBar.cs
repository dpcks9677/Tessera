using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 수정구(RollOrb)의 스톤 베이스와 결합/연장되는 100도 순수 스톤 부채꼴 3D 리롤 카운터 플랫폼
    /// - 3시 방향(+X)을 대칭 중심축으로 하는 100도 중심각(-50도 ~ +50도) 순수 스톤 2단 플레이트
    /// - RollOrb와 완벽히 동일한 높이(Y=0~0.060m, Y=0.060~0.118m) 및 스톤 머티리얼로 일체화
    /// - 상단판 내부에 여유 있는 스톤 여백(R = 1.460m, 각도 -28도, 0도, +28도)을 두고 3D 입체 패싯 보석 안착
    /// - 수정구 내부 오로라 리본 색상(맑은 아쿠아 사파이어)과 100% 톤 매칭 및 0.4초 부드러운 페이드 아웃
    /// </summary>
    /// <summary>
    /// 수정구(RollOrb)의 스톤 베이스와 결합/연장되는 100도 순수 스톤 부채꼴 3D 리롤 카운터 플랫폼
    /// - 3시 방향(+X)을 대칭 중심축으로 하는 100도 중심각(-50도 ~ +50도) 순수 스톤 2단 플레이트
    /// - RollOrb와 완벽히 동일한 높이(Y=0~0.060m, Y=0.060~0.118m) 및 스톤 머티리얼로 일체화
    /// - 상단판 내부에 여유 있는 스톤 여백(R = 1.460m, 각도 -28도, 0도, +28도)을 두고 3D 입체 패싯 보석 안착
    /// - 수정구 내부 오로라 리본 색상(맑은 아쿠아 사파이어)과 100% 톤 매칭 및 0.4초 부드러운 페이드 아웃
    /// </summary>
    [ExecuteAlways]
    public sealed class RerollCounterBar : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("State")]
        [SerializeField] private int rollsRemaining = 3;
        [SerializeField] private int maxRolls = 3;

        private readonly List<MeshRenderer> gemRenderers = new();
        private readonly List<List<MeshRenderer>> gemRidgeRenderers = new();
        private readonly float[] gemFadeProgress = new float[3] { 1f, 1f, 1f };

        private Material baseGemMat;
        private Material baseRidgeMat;
        private MaterialPropertyBlock propBlock;

        // 보석은 Unlit 평면색이라 이 값이 곧 최종 픽셀 색이다. 카메라가 HDR을 끄고 씬에 블룸 볼륨도
        // 없으므로 점등/소등은 오직 LDR 명도 차로만 읽힌다. 본체 휘도비를 약 21:1로 벌려 둔다.
        // 알파는 반드시 1이다. _Surface 0인 Unlit 블렌드는 One/Zero라 알파를 버린다.
        private readonly Color activeBodyColor = new(0.28f, 0.72f, 0.90f, 1f);       // 수정구 오로라 리본 톤
        private readonly Color inactiveBodyColor = new(0.012f, 0.028f, 0.055f, 1f);  // 소등 딥 미드나잇
        private readonly Color activeRidgeColor = new(0.62f, 0.95f, 1.00f, 1f);      // 패싯 능선 하이라이트
        private readonly Color inactiveRidgeColor = new(0.030f, 0.055f, 0.090f, 1f); // 소등 딥 림

        public int RollsRemaining => rollsRemaining;

        public static RerollCounterBar Create(Transform parent, Vector3 worldPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("3D Reroll Counter Bar");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition;
            root.transform.rotation = rotation ?? Quaternion.identity;
            root.transform.localScale = scale ?? Vector3.one;

            RerollCounterBar comp = root.AddComponent<RerollCounterBar>();
            comp.BuildGeometry();
            return comp;
        }

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            EnsureGeometry();
        }

        private void OnEnable()
        {
            propBlock ??= new MaterialPropertyBlock();
            EnsureGeometry();
        }

        public void EnsureGeometry()
        {
            if (transform.childCount == 0)
            {
                BuildGeometry();
                return;
            }

            BindExistingGeometry();
        }

        private void BindExistingGeometry()
        {
            gemRenderers.Clear();
            gemRidgeRenderers.Clear();

            Transform platform = transform.Find("Sector_100_Stone_Platform");
            if (platform == null) return;

            for (int i = 0; i < 3; i++)
            {
                Transform gemRoot = platform.Find($"Faceted_Sapphire_Gem_{i}");
                gemRenderers.Add(gemRoot?.Find("Faceted_Gem_Mesh")?.GetComponent<MeshRenderer>());

                List<MeshRenderer> ridges = new();
                Transform ridgeRoot = gemRoot?.Find("Facet_Ridge_Lines");
                if (ridgeRoot != null)
                {
                    for (int r = 0; r < ridgeRoot.childCount; r++)
                    {
                        MeshRenderer renderer = ridgeRoot.GetChild(r).GetComponent<MeshRenderer>();
                        if (renderer != null) ridges.Add(renderer);
                    }
                }
                gemRidgeRenderers.Add(ridges);

                // 구운 프리팹에는 Gem_Light_{i}가 남아 있다. 연출에서 라이트를 걷어냈고 컬링 마스크가
                // 장식 레이어를 제외해 보석을 비추지도 못하므로 꺼 둔다. 편집 모드에서 끄면 프리팹
                // 인스턴스 오버라이드가 생겨 씬이 더러워지므로 플레이 모드에서만 만진다.
                if (Application.isPlaying)
                {
                    Transform legacyLight = gemRoot?.Find($"Gem_Light_{i}");
                    if (legacyLight != null) legacyLight.gameObject.SetActive(false);
                }
            }

            baseGemMat = gemRenderers.Count > 0 ? gemRenderers[0]?.sharedMaterial : null;
            baseRidgeMat = gemRidgeRenderers.Count > 0 && gemRidgeRenderers[0].Count > 0
                ? gemRidgeRenderers[0][0].sharedMaterial
                : null;

            SeedFadeProgress();
        }

        /// <summary>
        /// 페이드 진행도를 현재 리롤 수에 맞춘다. 이걸 빼먹으면 지오메트리를 다시 물어올 때마다
        /// 3개가 모두 켜진 상태에서 시작해 잘못된 페이드 아웃이 한 번 재생된다.
        /// </summary>
        private void SeedFadeProgress()
        {
            for (int i = 0; i < gemFadeProgress.Length; i++)
            {
                gemFadeProgress[i] = (i < rollsRemaining) ? 1.0f : 0.0f;
            }
        }

        public void SetRollsRemaining(int count, int max = 3)
        {
            rollsRemaining = Mathf.Clamp(count, 0, max);
            maxRolls = max;
        }

        // 매 프레임 도는 경로라 셰이더 프로퍼티 이름을 미리 ID로 바꿔 둔다.
        // 1회성 생성 경로의 문자열 접근은 그대로 둔다. 거기서는 조회 비용이 의미가 없다.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Update()
        {
            // 편집 모드에서는 연출을 돌리지 않는다. 이 애니메이션은 트랜스폼과 컴포넌트 값 같은
            // 직렬화 대상에 매 틱 쓰기 때문에, 편집 모드에서 돌리면 씬이 계속 더러운 상태가 된다.
            // 그러면 씬을 저장할 때마다 관련 없는 오버라이드가 diff에 섞이고 테스트 실행이
            // "dirty scene"으로 막힌다. [ExecuteAlways]는 BuildGeometry 컨텍스트 메뉴와
            // OnValidate 미리보기 때문에 그대로 둔다.
            if (!Application.isPlaying) return;

            propBlock ??= new MaterialPropertyBlock();

            // 최댓값 0.90에 곱해도 0.99라 LDR 타깃에서 클리핑되지 않는다.
            float pulse = 1.0f + Mathf.Sin(Time.time * 2.2f) * 0.10f; // 은은한 오로라 호흡 펄스

            // 3개 보석 각각에 대해 부드러운 페이드 아웃/인 전환 보간 (0.4초)
            for (int i = 0; i < 3; i++)
            {
                float targetFade = (i < rollsRemaining) ? 1.0f : 0.0f;
                gemFadeProgress[i] = Mathf.MoveTowards(gemFadeProgress[i], targetFade, Time.deltaTime * 2.5f);
                float f = gemFadeProgress[i];

                // 1. 보석 본체 평면색 페이드. 라이팅에 기대지 않으므로 이 값이 그대로 화면에 나온다.
                if (i < gemRenderers.Count)
                {
                    SetRendererColor(gemRenderers[i], Color.Lerp(inactiveBodyColor, activeBodyColor * pulse, f));
                }

                // 2. 6방향 리지 라인. 가상 격자에서 서브픽셀이라 형태를 그리지는 못하므로,
                //    본체와 같은 방향으로만 움직여 어쩌다 찍히는 픽셀이 판독을 거스르지 않게 한다.
                if (i < gemRidgeRenderers.Count && gemRidgeRenderers[i] != null)
                {
                    Color ridge = Color.Lerp(inactiveRidgeColor, activeRidgeColor * pulse, f);
                    for (int r = 0; r < gemRidgeRenderers[i].Count; r++)
                    {
                        SetRendererColor(gemRidgeRenderers[i][r], ridge);
                    }
                }
            }
        }

        /// <summary>
        /// RunicSlateMatrix.SetRendererColor와 같은 경로다. _Color는 URP/Unlit에서 폐기된 프로퍼티라
        /// 셰이더가 읽지 않지만, 다른 프롭과 쓰기 지점을 맞추고 Lit 폴백에서도 동작하게 남겨 둔다.
        /// </summary>
        private void SetRendererColor(MeshRenderer renderer, Color color)
        {
            if (renderer == null) return;

            color.a = 1f; // Color.Lerp가 알파도 섞는다. Unlit 불투명 블렌드는 알파를 버리므로 고정한다.
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, color);
            propBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propBlock);
            propBlock.Clear();
        }

        public void BuildGeometry()
        {
            gemRenderers.Clear();
            gemRidgeRenderers.Clear();
            SeedFadeProgress();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying)
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? litShader;

            // 1. 스톤 베이스와 골드 트림 머티리얼. 예전에는 씬의 수정구에서 머티리얼을 빌려오고
            //    없을 때만 새로 만들었으나, 수정구가 성운 큐브로 대체되어 이 경로만 남았다.
            //    머티리얼 이름은 베이커가 에셋 파일명을 만드는 근거이므로 바꾸지 않는다.
            Material lowerStoneMat = CreateMat(unlitShader, "Orb_StoneRimMat", new Color(0.34f, 0.38f, 0.42f), 0.04f, 0.28f);
            Material upperStoneMat = CreateMat(unlitShader, "Orb_StoneBaseMat", new Color(0.52f, 0.56f, 0.60f), 0.05f, 0.32f);
            Material goldTrimMat = CreateMat(litShader, "Orb_GoldTrimMat", new Color(0.86f, 0.68f, 0.28f), 0.88f, 0.68f);
            Material goldDarkMat = CreateMat(litShader, "Orb_GoldDarkMat", new Color(0.58f, 0.44f, 0.16f), 0.85f, 0.52f);

            // 2. 보석 본체와 리지는 라이팅에 기대지 않는 Unlit 평면색이다. 색만으로 점등/소등을 표현한다.
            //    이 경로는 굽기 전 씬에서만 돈다. 구운 프리팹은
            //    Assets/Art/Generated/Tabletop/Materials/3D_Reroll_Counter_Bar_Counter_*.mat 두 개를 쓰므로
            //    셰이더나 색을 바꿀 때 그쪽도 같이 맞춰야 한다. 머티리얼 이름은 베이커가 에셋 파일명을
            //    만드는 근거이므로 바꾸지 않는다(TabletopPrefabBaker.ExtractMaterials).
            baseGemMat = CreateUnlitMat(unlitShader, "Counter_HexGemBaseMat", activeBodyColor);
            baseRidgeMat = CreateUnlitMat(unlitShader, "Counter_GemRidgeBaseMat", activeRidgeColor);

            // 3. 100도 부채꼴 스톤 베이스 지오메트리 생성 (RollOrb 외벽에서 바깥으로 확장되는 Sector Ring 구조 - Z-fighting 완전 차단)
            const float StartAngle = -50f;
            const float EndAngle = 50f;
            const int Segments = 24;

            // RollOrb LowerBase 외경 지름 2.55f (반지름 1.275f) -> RollOrb 내부로 0.03m 크롭 오버랩(1.245f)하여 1.785f까지 확장, 높이 0.080m
            const float LowerInnerRadius = 1.245f;
            const float LowerOuterRadius = 1.785f;
            const float LowerHeight = 0.080f;

            // RollOrb UpperBase 외경 지름 2.25f (반지름 1.125f) -> RollOrb 내부로 0.03m 크롭 오버랩(1.095f)하여 1.680f까지 확장, 높이 0.055m (Y: 0.080 ~ 0.135m)
            const float UpperInnerRadius = 1.095f;
            const float UpperOuterRadius = 1.680f;
            const float UpperHeight = 0.055f;

            GameObject platformRoot = new("Sector_100_Stone_Platform");
            platformRoot.layer = DecorationLayer;
            platformRoot.transform.SetParent(transform, false);

            // 3-1. 1단 하단 100도 부채꼴 스톤 링 플레이트 (LowerBase_Stone 외곽 확장 윙 - 동일 LowerBase BaseMat 적용)
            Mesh lowerMesh = CreateSectorRingPrismMesh(LowerInnerRadius, LowerOuterRadius, LowerHeight, StartAngle, EndAngle, Segments);
            GameObject lowerPlate = new("LowerBase_Stone_Sector");
            SetupMeshPart(lowerPlate, platformRoot.transform, new Vector3(0f, 0.0f, 0f), lowerMesh, lowerStoneMat);

            // 3-2. 2단 상단 100도 부채꼴 스톤 링 플레이트 (UpperBase_Stone 외곽 확장 윙 - 동일 UpperBase BaseMat 적용 + 높이 0.001m 추가)
            const float UpperBaseOffset = 0.001f;
            Mesh upperMesh = CreateSectorRingPrismMesh(UpperInnerRadius, UpperOuterRadius, UpperHeight, StartAngle, EndAngle, Segments);
            GameObject upperPlate = new("UpperBase_Stone_Sector");
            SetupMeshPart(upperPlate, platformRoot.transform, new Vector3(0f, LowerHeight + UpperBaseOffset, 0f), upperMesh, upperStoneMat);

            // 3-3. RollOrb의 Base_GoldRing과 연결되는 상단 외곽 100도 부채꼴 골드 트림 림 (Gold Trim Ribbon)
            Mesh goldRibbonMesh = CreateSectorRingPrismMesh(UpperOuterRadius - 0.075f, UpperOuterRadius, 0.015f, StartAngle, EndAngle, Segments);
            GameObject goldRibbon = new("UpperBase_Gold_Ribbon");
            SetupMeshPart(goldRibbon, platformRoot.transform, new Vector3(0f, LowerHeight + UpperHeight + UpperBaseOffset, 0f), goldRibbonMesh, goldTrimMat);

            // 3-4. RollOrb의 Base_Stud와 일치하는 부채꼴 외곽 골드 스터드 4개
            float[] studAngles = new float[] { -42f, -14f, 14f, 42f };
            float studRadius = UpperOuterRadius - 0.038f;
            float studY = LowerHeight + UpperHeight + UpperBaseOffset + 0.015f;
            for (int s = 0; s < studAngles.Length; s++)
            {
                float sRad = studAngles[s] * Mathf.Deg2Rad;
                Vector3 sPos = new(Mathf.Cos(sRad) * studRadius, studY, Mathf.Sin(sRad) * studRadius);
                GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stud.name = $"UpperBase_GoldStud_{s}";
                SetupPart(stud, platformRoot.transform, sPos, Vector3.zero, new Vector3(0.09f, 0.05f, 0.09f), goldTrimMat);
            }

            // 4. UpperBase_Stone(R=1.125m)과 UpperBase_Gold_Ribbon(R=1.605m) 사이 스톤 상판 정중앙(R=1.365m)에 안착되는 앤틱 골드 소켓 & 3D 사파이어 보석 3개
            float[] gemAngles = new float[] { -28f, 0f, 28f };
            float gemArcRadius = (UpperInnerRadius + (UpperOuterRadius - 0.075f)) * 0.5f; // R = 1.365m (내외측 여백 각 0.060m로 완벽 균형)
            float gemBaseY = LowerHeight + UpperHeight + UpperBaseOffset + 0.001f; // 상판 위 0.001m 오프셋으로 Z-fighting 원천 차단

            for (int i = 0; i < gemAngles.Length; i++)
            {
                float angleDeg = gemAngles[i];
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 gemPos = new(Mathf.Cos(rad) * gemArcRadius, gemBaseY, Mathf.Sin(rad) * gemArcRadius);

                GameObject gemRoot = new($"Faceted_Sapphire_Gem_{i}");
                gemRoot.layer = DecorationLayer;
                gemRoot.transform.SetParent(platformRoot.transform, false);
                gemRoot.transform.localPosition = gemPos;
                gemRoot.transform.localRotation = Quaternion.Euler(0f, -angleDeg, 0f);

                // 4-0. RollOrb의 상단 받침대와 일체화된 앤틱 골드 베젤 소켓 받침대 (Gem Socket Pedestal)
                // (1) 하단 다크 브라스 베이스 링 (지름 0.36m, R=0.18m -> 내경 1.185m, 외경 1.545m로 골드 리본 및 원형 베이스와 여유 확보)
                GameObject socketBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                socketBase.name = "Gem_Socket_Base";
                SetupPart(socketBase, gemRoot.transform, new Vector3(0f, 0.004f, 0f), Vector3.zero, new Vector3(0.36f, 0.004f, 0.36f), goldDarkMat);

                // (2) 상단 앤틱 골드 베젤 칼라 림 (보석 안착 림: 지름 0.32m)
                GameObject socketBezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                socketBezel.name = "Gem_Socket_BezelRing";
                SetupPart(socketBezel, gemRoot.transform, new Vector3(0f, 0.012f, 0f), Vector3.zero, new Vector3(0.32f, 0.004f, 0.32f), goldTrimMat);

                // (3) 4방향 미니 골드 스터드 클로 악센트 (보석 외곽 지지)
                for (int c = 0; c < 4; c++)
                {
                    float clawAngle = (c * 90f + 45f) * Mathf.Deg2Rad;
                    Vector3 clawPos = new(Mathf.Cos(clawAngle) * 0.155f, 0.016f, Mathf.Sin(clawAngle) * 0.155f);
                    GameObject claw = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    claw.name = $"Gem_Socket_Stud_{c}";
                    SetupPart(claw, gemRoot.transform, clawPos, Vector3.zero, new Vector3(0.050f, 0.050f, 0.050f), goldTrimMat);
                }

                // 4-1. 3D 입체 패싯 컷 사파이어 보석 본체 (반지름 0.145m, 피라미드 크라운 0.12m, 거들 0.030m)
                float gemElevation = 0.026f;
                Mesh facetedGemMesh = CreateFacetedHexGemMesh(0.145f, 0.120f, 0.030f);
                GameObject gemObj = new("Faceted_Gem_Mesh");
                SetupMeshPart(gemObj, gemRoot.transform, new Vector3(0f, gemElevation, 0f), facetedGemMesh, baseGemMat);

                MeshRenderer mr = gemObj.GetComponent<MeshRenderer>();
                if (mr != null) gemRenderers.Add(mr);

                // 4-2. 보석 중심(Apex)에서 6개 꼭짓점으로 이어지는 6방향 입체 리지 라인 (Ridge Lines)
                GameObject ridgesRoot = new("Facet_Ridge_Lines");
                ridgesRoot.layer = DecorationLayer;
                ridgesRoot.transform.SetParent(gemRoot.transform, false);

                List<MeshRenderer> ridges = new();
                Vector3 apexPos = new(0f, gemElevation + 0.120f + 0.030f, 0f);
                for (int v = 0; v < 6; v++)
                {
                    float a = (v * 60f + 30f) * Mathf.Deg2Rad;
                    Vector3 girdlePos = new(Mathf.Cos(a) * 0.145f, gemElevation + 0.030f, Mathf.Sin(a) * 0.145f);

                    GameObject ridgeLine = CreateCylinderBetweenPoints(apexPos, girdlePos, 0.0055f);
                    ridgeLine.name = $"Ridge_Line_{v}";
                    ridgeLine.transform.SetParent(ridgesRoot.transform, false);

                    MeshRenderer rmr = ridgeLine.GetComponent<MeshRenderer>();
                    if (rmr != null)
                    {
                        if (Application.isPlaying) rmr.material = baseRidgeMat;
                        else rmr.sharedMaterial = baseRidgeMat;
                        rmr.shadowCastingMode = ShadowCastingMode.TwoSided;
                        rmr.receiveShadows = true;
                        ridges.Add(rmr);
                    }
                }
                gemRidgeRenderers.Add(ridges);
            }
        }

        private static GameObject CreateCylinderBetweenPoints(Vector3 start, Vector3 end, float radius)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.layer = DecorationLayer;

            Collider col = cylinder.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            Vector3 dir = end - start;
            float length = dir.magnitude;
            Vector3 midPoint = start + dir * 0.5f;

            cylinder.transform.localPosition = midPoint;
            cylinder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            cylinder.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            return cylinder;
        }

        /// <summary>
        /// RollOrb 외벽(InnerRadius)에서 시작하여 바깥(OuterRadius)으로 확장되는 솔리드 부채꼴 링 프리즘 메쉬 생성
        /// - 상단 링 면, 하단 링 면, 외벽, 내벽, 시작/끝 절단면을 모두 포함하여 Z-fighting 없이 완벽 밀착
        /// </summary>
        private static Mesh CreateSectorRingPrismMesh(float innerRadius, float outerRadius, float height, float startAngleDeg, float endAngleDeg, int segments)
        {
            Mesh mesh = new() { name = $"SectorRingPrism_InR{innerRadius:F2}_OutR{outerRadius:F2}_H{height:F2}" };

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float endRad = endAngleDeg * Mathf.Deg2Rad;

            // 1. 상단 링 면 (Top Ring Face - Normal Up)
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                float a0 = Mathf.Lerp(startRad, endRad, t0);
                float a1 = Mathf.Lerp(startRad, endRad, t1);

                Vector3 pIn0 = new(Mathf.Cos(a0) * innerRadius, height, Mathf.Sin(a0) * innerRadius);
                Vector3 pOut0 = new(Mathf.Cos(a0) * outerRadius, height, Mathf.Sin(a0) * outerRadius);
                Vector3 pOut1 = new(Mathf.Cos(a1) * outerRadius, height, Mathf.Sin(a1) * outerRadius);
                Vector3 pIn1 = new(Mathf.Cos(a1) * innerRadius, height, Mathf.Sin(a1) * innerRadius);

                int idx = vertices.Count;
                vertices.Add(pIn0); normals.Add(Vector3.up); uvs.Add(new Vector2(0f, t0));
                vertices.Add(pOut0); normals.Add(Vector3.up); uvs.Add(new Vector2(1f, t0));
                vertices.Add(pOut1); normals.Add(Vector3.up); uvs.Add(new Vector2(1f, t1));
                vertices.Add(pIn1); normals.Add(Vector3.up); uvs.Add(new Vector2(0f, t1));

                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
                triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
            }

            // 2. 하단 링 면 (Bottom Ring Face - Normal Down)
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                float a0 = Mathf.Lerp(startRad, endRad, t0);
                float a1 = Mathf.Lerp(startRad, endRad, t1);

                Vector3 pIn0 = new(Mathf.Cos(a0) * innerRadius, 0f, Mathf.Sin(a0) * innerRadius);
                Vector3 pOut0 = new(Mathf.Cos(a0) * outerRadius, 0f, Mathf.Sin(a0) * outerRadius);
                Vector3 pOut1 = new(Mathf.Cos(a1) * outerRadius, 0f, Mathf.Sin(a1) * outerRadius);
                Vector3 pIn1 = new(Mathf.Cos(a1) * innerRadius, 0f, Mathf.Sin(a1) * innerRadius);

                int idx = vertices.Count;
                vertices.Add(pIn0); normals.Add(Vector3.down); uvs.Add(new Vector2(0f, t0));
                vertices.Add(pIn1); normals.Add(Vector3.down); uvs.Add(new Vector2(0f, t1));
                vertices.Add(pOut1); normals.Add(Vector3.down); uvs.Add(new Vector2(1f, t1));
                vertices.Add(pOut0); normals.Add(Vector3.down); uvs.Add(new Vector2(1f, t0));

                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
                triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
            }

            // 3. 외벽 (Outer Curved Wall)
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                float a0 = Mathf.Lerp(startRad, endRad, t0);
                float a1 = Mathf.Lerp(startRad, endRad, t1);

                Vector3 p0Top = new(Mathf.Cos(a0) * outerRadius, height, Mathf.Sin(a0) * outerRadius);
                Vector3 p1Top = new(Mathf.Cos(a1) * outerRadius, height, Mathf.Sin(a1) * outerRadius);
                Vector3 p0Bot = new(Mathf.Cos(a0) * outerRadius, 0f, Mathf.Sin(a0) * outerRadius);
                Vector3 p1Bot = new(Mathf.Cos(a1) * outerRadius, 0f, Mathf.Sin(a1) * outerRadius);

                Vector3 n0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)).normalized;
                Vector3 n1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)).normalized;

                int idx = vertices.Count;
                vertices.Add(p0Bot); normals.Add(n0); uvs.Add(new Vector2(t0, 0f));
                vertices.Add(p0Top); normals.Add(n0); uvs.Add(new Vector2(t0, 1f));
                vertices.Add(p1Top); normals.Add(n1); uvs.Add(new Vector2(t1, 1f));
                vertices.Add(p1Bot); normals.Add(n1); uvs.Add(new Vector2(t1, 0f));

                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
                triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
            }

            // 4. 내벽 (Inner Wall은 RollOrb 내부에 묻히므로 불필요한 면 렌더링 및 Z-fighting 방지를 위해 크롭 생략)

            // 5. 시작 절단면 (Start Cut Wall)
            Vector3 startNormal = new(-Mathf.Sin(startRad), 0f, Mathf.Cos(startRad));
            int sIdx = vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(startRad) * innerRadius, 0f, Mathf.Sin(startRad) * innerRadius)); normals.Add(startNormal); uvs.Add(new Vector2(0f, 0f));
            vertices.Add(new Vector3(Mathf.Cos(startRad) * innerRadius, height, Mathf.Sin(startRad) * innerRadius)); normals.Add(startNormal); uvs.Add(new Vector2(0f, 1f));
            vertices.Add(new Vector3(Mathf.Cos(startRad) * outerRadius, height, Mathf.Sin(startRad) * outerRadius)); normals.Add(startNormal); uvs.Add(new Vector2(1f, 1f));
            vertices.Add(new Vector3(Mathf.Cos(startRad) * outerRadius, 0f, Mathf.Sin(startRad) * outerRadius)); normals.Add(startNormal); uvs.Add(new Vector2(1f, 0f));

            triangles.Add(sIdx); triangles.Add(sIdx + 1); triangles.Add(sIdx + 2);
            triangles.Add(sIdx); triangles.Add(sIdx + 2); triangles.Add(sIdx + 3);

            // 6. 끝 절단면 (End Cut Wall)
            Vector3 endNormal = new(Mathf.Sin(endRad), 0f, -Mathf.Cos(endRad));
            int eIdx = vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(endRad) * outerRadius, 0f, Mathf.Sin(endRad) * outerRadius)); normals.Add(endNormal); uvs.Add(new Vector2(0f, 0f));
            vertices.Add(new Vector3(Mathf.Cos(endRad) * outerRadius, height, Mathf.Sin(endRad) * outerRadius)); normals.Add(endNormal); uvs.Add(new Vector2(0f, 1f));
            vertices.Add(new Vector3(Mathf.Cos(endRad) * innerRadius, height, Mathf.Sin(endRad) * innerRadius)); normals.Add(endNormal); uvs.Add(new Vector2(1f, 1f));
            vertices.Add(new Vector3(Mathf.Cos(endRad) * innerRadius, 0f, Mathf.Sin(endRad) * innerRadius)); normals.Add(endNormal); uvs.Add(new Vector2(1f, 0f));

            triangles.Add(eIdx); triangles.Add(eIdx + 1); triangles.Add(eIdx + 2);
            triangles.Add(eIdx); triangles.Add(eIdx + 2); triangles.Add(eIdx + 3);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFacetedHexGemMesh(float radius, float crownHeight, float girdleHeight)
        {
            Mesh mesh = new() { name = $"FacetedHexGem_R{radius:F2}_H{crownHeight:F2}" };
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            Vector3 apex = new(0f, crownHeight + girdleHeight, 0f);

            Vector3[] girdleTop = new Vector3[6];
            Vector3[] girdleBot = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                girdleTop[i] = new Vector3(Mathf.Cos(a) * radius, girdleHeight, Mathf.Sin(a) * radius);
                girdleBot[i] = new Vector3(Mathf.Cos(a) * radius, 0.0f, Mathf.Sin(a) * radius);
            }

            // 1. 상단 6개 삼각형 패싯 (Apex -> girdleTop)
            for (int i = 0; i < 6; i++)
            {
                Vector3 p0 = apex;
                Vector3 p1 = girdleTop[i];
                Vector3 p2 = girdleTop[(i + 1) % 6];
                Vector3 facetNormal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

                int idx = vertices.Count;
                vertices.Add(p0); normals.Add(facetNormal); uvs.Add(new Vector2(0.5f, 0.5f));
                vertices.Add(p1); normals.Add(facetNormal); uvs.Add(new Vector2(p1.x / (radius * 2f) + 0.5f, p1.z / (radius * 2f) + 0.5f));
                vertices.Add(p2); normals.Add(facetNormal); uvs.Add(new Vector2(p2.x / (radius * 2f) + 0.5f, p2.z / (radius * 2f) + 0.5f));

                triangles.Add(idx);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);
            }

            // 2. 측면 6개 거들 사각형 패싯 (girdleTop -> girdleBot)
            for (int i = 0; i < 6; i++)
            {
                Vector3 p0Top = girdleTop[i];
                Vector3 p1Top = girdleTop[(i + 1) % 6];
                Vector3 p0Bot = girdleBot[i];
                Vector3 p1Bot = girdleBot[(i + 1) % 6];

                Vector3 sideNormal = Vector3.Cross(Vector3.up, p1Top - p0Top).normalized;

                int idx = vertices.Count;
                vertices.Add(p0Bot); normals.Add(sideNormal); uvs.Add(new Vector2(0f, 0f));
                vertices.Add(p0Top); normals.Add(sideNormal); uvs.Add(new Vector2(0f, 1f));
                vertices.Add(p1Top); normals.Add(sideNormal); uvs.Add(new Vector2(1f, 1f));
                vertices.Add(p1Bot); normals.Add(sideNormal); uvs.Add(new Vector2(1f, 0f));

                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
                triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
            }

            // 3. 바닥 수평 평면 육각 면 (Bottom Flat Hexagon Cap - Normal Down)
            int botCenterIdx = vertices.Count;
            vertices.Add(Vector3.zero); normals.Add(Vector3.down); uvs.Add(new Vector2(0.5f, 0.5f));

            int botHexStart = vertices.Count;
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = girdleBot[i];
                vertices.Add(p); normals.Add(Vector3.down); uvs.Add(new Vector2(p.x / (radius * 2f) + 0.5f, p.z / (radius * 2f) + 0.5f));
            }

            for (int i = 0; i < 6; i++)
            {
                triangles.Add(botCenterIdx);
                triangles.Add(botHexStart + ((i + 1) % 6));
                triangles.Add(botHexStart + i);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMat(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            Material m = new(shader) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        /// <summary>라이팅을 받지 않는 평면색 재질. 색이 곧 화면 픽셀이므로 금속감·매끄러움을 두지 않는다.</summary>
        private static Material CreateUnlitMat(Shader shader, string name, Color color)
        {
            Material m = new(shader) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            return m;
        }

        private static void SetupMeshPart(GameObject obj, Transform parent, Vector3 localPos, Mesh mesh, Material mat)
        {
            obj.layer = DecorationLayer;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            if (Application.isPlaying) mr.material = mat;
            else mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            mr.receiveShadows = true;
        }

        private static void SetupPart(GameObject obj, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale, Material mat)
        {
            obj.layer = DecorationLayer;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(localRot);
            obj.transform.localScale = localScale;

            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (Application.isPlaying) mr.material = mat;
                else mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                mr.receiveShadows = true;
            }
        }
    }
}
