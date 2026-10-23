using Tessera.Core;
using Tessera.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.Yacht.Presentation
{
    /// <summary>
    /// 테이블 표면—나무 상판·천 러너·트레이를 절차적으로 만든다(M10-T6d).
    ///
    /// 이 셀은 전용 컴포넌트가 없어 생성 코드가 컨트롤러 본문에 남아 있었다.
    /// M9 이후 평상에는 프리팩이 형상을 소유하므로 <b>어느 실행 경로에서도 불리지 않는다.</b>
    /// 형상 자체를 바꿔야 할 때만 컨트롤러의 컨텍스트 메뉴로 실행하고,
    /// 그에 따라 Tessera/Tabletop/Bake Tabletop Prefabs 로 프리팩을 다시 굽는다.
    ///
    /// 생성물은 전부 런타임 메시·머티리얼이다. 굽기 전까지는 에셋이 아니므로
    /// <see cref="RuntimeAssetGuard"/>를 거치지 않는다.
    /// </summary>
    public static class TabletopSurfaceBuilder
    {
        /// <summary>트레이 메시 UV 1이 덮는 메시 로컬 길이. 메시 UV는 로컬 좌표를 이 값으로 나눈다.</summary>
        public const float TrayUvSourceUnits = 50f;

        /// <summary>펠트 텍스처 가로(U) 텍셀 수.</summary>
        public const int FeltWidthTexels = 128;

        /// <summary>펠트 텍스처 세로(V) 텍셀 수. <see cref="FeltRibPeriodTexels"/>의 정수 배수여야 타일 이음매에서 골이 끊기지 않는다.</summary>
        public const int FeltHeightTexels = 96;

        /// <summary>코듀로이 골 하나가 V 방향으로 차지하는 텍셀 수.</summary>
        public const int FeltRibPeriodTexels = 3;

        /// <summary>판자 가로(X) 월드 길이.</summary>
        public const float PlankWidth = 38.0f;

        /// <summary>판자 세로(Z) 월드 길이.</summary>
        public const float PlankDepth = 4.90f;

        /// <summary>표면 세 개를 모두 지우고 다시 만든다. 베이킹 전용이다.</summary>
        public static void Regenerate(Transform layoutRoot, Mesh trayMesh, float centerSectionX, float trayVisualY, float trayScale)
        {
            if (layoutRoot == null) return;
            DestroyChild(layoutRoot, "3D Wood Planks Table");
            DestroyChild(layoutRoot, "3D Fabric Runner");
            DestroyChild(layoutRoot, "Yacht Tray Visual");

            BuildWoodPlanksTable(layoutRoot, centerSectionX);
            BuildFabricRunner(layoutRoot, centerSectionX);
            BuildTrayVisual(layoutRoot, trayMesh, centerSectionX, trayVisualY, trayScale);
            SyncTrayMaterial();
        }

        /// <summary>
        /// 씨에 있는 트레이의 UV와 펠트 텍스처를 다시 입힌다.
        ///
        /// 공유 머티리얼을 직접 쓰므로, 굽힌 에셋을 가리키는 상태에서 불리면 그 에셋이 변경된다.
        /// 베이킹 전용으로만 쓴다.
        /// </summary>
        public static void SyncTrayMaterial()
        {
            GameObject trayObj = GameObject.Find("Yacht Tray Visual");
            if (trayObj == null) return;
            MeshFilter mf = trayObj.GetComponent<MeshFilter>();
            MeshRenderer mr = trayObj.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;

            Mesh mesh = mf.sharedMesh;
            if (mesh != null && (mesh.uv == null || mesh.uv.Length == 0))
            {
                Mesh copy = Object.Instantiate(mesh);
                copy.uv = BuildPlanarUv(copy);
                mf.sharedMesh = copy;
            }

            Texture2D corduroyTex = CreateBurgundyCorduroyTexture();
            Material[] mats = mr.sharedMaterials;
            if (mats != null && mats.Length >= 2)
            {
                Material felt = mats[1];
                if (felt != null)
                {
                    felt.mainTexture = corduroyTex;
                    if (felt.HasProperty("_BaseMap")) felt.SetTexture("_BaseMap", corduroyTex);
                    if (felt.HasProperty("_MainTex")) felt.SetTexture("_MainTex", corduroyTex);
                    if (felt.HasProperty("_BaseColor")) felt.SetColor("_BaseColor", Color.white);
                    if (felt.HasProperty("_Color")) felt.SetColor("_Color", Color.white);
                    felt.mainTextureScale = FeltTiling();
                    felt.mainTextureOffset = FeltOffset();
                    felt.color = Color.white;
                    felt.SetFloat("_Smoothness", 0.12f);
                }
            }
        }

        private static void DestroyChild(Transform layoutRoot, string childName)
        {
            Transform child = layoutRoot != null ? layoutRoot.Find(childName) : null;
            if (child == null) return;

            if (Application.isPlaying) Object.Destroy(child.gameObject);
            else Object.DestroyImmediate(child.gameObject);
        }

        private static void BuildWoodPlanksTable(Transform layoutRoot, float centerSectionX)
        {
            GameObject tableRoot = new("3D Wood Planks Table");
            tableRoot.layer = TesseraLayers.Decoration;
            tableRoot.transform.SetParent(layoutRoot, false);
            tableRoot.transform.position = Vector3.zero;

            int plankCount = 4;
            float totalHeight = 20.0f;
            float plankHeight = PlankDepth;
            float gap = 0.10f;
            float plankWidth = PlankWidth;
            float plankThickness = 0.60f;
            float baseY = -0.72f;

            // 0. 판자 틈새 그림자 역할의 언더레이어 밑판 (틈새로 배경이 비치지 않고 자연스러운 음영 연출)
            GameObject underlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            underlay.name = "Table Shadow Underlay";
            underlay.layer = TesseraLayers.Decoration;
            underlay.transform.SetParent(tableRoot.transform, false);
            underlay.transform.position = new Vector3(centerSectionX, baseY - 0.20f, 0f);
            underlay.transform.localScale = new Vector3(plankWidth, 0.20f, totalHeight + 1.0f);

            Collider underlayCol = underlay.GetComponent<Collider>();
            if (underlayCol != null)
            {
                if (Application.isPlaying) Object.Destroy(underlayCol);
                else Object.DestroyImmediate(underlayCol);
            }

            Material underlayMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime Table Shadow Underlay Material",
                color = new Color32(20, 15, 12, 255)
            };
            if (underlayMat.HasProperty("_BaseColor")) underlayMat.SetColor("_BaseColor", new Color32(20, 15, 12, 255));
            if (underlayMat.HasProperty("_Color")) underlayMat.SetColor("_Color", new Color32(20, 15, 12, 255));
            underlayMat.SetFloat("_Smoothness", 0.05f);
            underlayMat.SetFloat("_Metallic", 0f);

            MeshRenderer underlayMr = underlay.GetComponent<MeshRenderer>();
            underlayMr.material = underlayMat;
            underlayMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            underlayMr.receiveShadows = true;

            float startZ = -totalHeight * 0.5f + plankHeight * 0.5f;

            for (int i = 0; i < plankCount; i++)
            {
                float z = startZ + i * (plankHeight + gap);
                float yOffset = ((i % 2 == 0) ? 0.008f : -0.008f); // 판자 간 자연스러운 3D 높낮이 단차
                Vector3 pos = new(centerSectionX, baseY + yOffset, z);
                Vector3 size = new(plankWidth, plankThickness, plankHeight);

                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Heavy Wood Plank {i + 1}";
                plank.layer = TesseraLayers.Decoration;
                plank.transform.SetParent(tableRoot.transform, false);
                plank.transform.position = pos;
                plank.transform.localScale = size;

                Collider col = plank.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Object.Destroy(col);
                    else Object.DestroyImmediate(col);
                }

                // 판자마다 전용 픽셀 나뭇결을 쓴다(M17-T16). 결·옹이·균열이 텍스처마다 다르므로
                // 예전처럼 한 장을 UV 오프셋으로 잘라 쓸 필요가 없다. 텍스처는
                // Tools/Tessera/Generate Wood Plank Textures 로 굽는다.
                Texture2D woodTexture = null;
#if UNITY_EDITOR
                woodTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/04.Art/Generated/Wood/wood_plank_{i + 1}.png");
#endif

                // 텍스처가 색을 통째로 들고 있다. 판자별 색을 곱하면 이중 착색이 되므로 흰색을 준다.
                Material mat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = $"Runtime Heavy Wood Plank {i + 1} Material",
                    color = Color.white
                };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

                if (woodTexture != null)
                {
                    // 타일링·오프셋은 기본값(1,1)·(0,0)을 그대로 둔다. 텍스처 한 장이 판자 한 장을
                    // 1:1로 덮어야 텍셀이 월드에서 정방형에 가까워지고 결이 가로로 문대지지 않는다.
                    mat.mainTexture = woodTexture;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", woodTexture);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", woodTexture);
                }

                mat.SetFloat("_Smoothness", 0.20f);
                mat.SetFloat("_Metallic", 0f);

                MeshRenderer mr = plank.GetComponent<MeshRenderer>();
                mr.material = mat;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                mr.receiveShadows = true;
            }
        }

        /// <summary>
        /// 펠트 텍스처의 타일링. 텍셀 하나가 바닥 위에서 필터 한 칸의 월드 크기와 같아지도록
        /// UV 1의 월드 길이를 (텍셀 수 × 텍셀 월드 크기)로 나눈다.
        /// </summary>
        public static Vector2 FeltTiling()
        {
            float uvWorldLength = TrayUvSourceUnits * DiceBoardMetrics.TrayScale;
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);
            return new Vector2(
                uvWorldLength / (FeltWidthTexels * texel.x),
                uvWorldLength / (FeltHeightTexels * texel.y));
        }

        /// <summary>펠트 텍스처의 오프셋. 트레이 바닥 중심이 타일 가운데에 오게 한다.</summary>
        public static Vector2 FeltOffset() => new(0.5f, 0.5f);

        /// <summary>펠트가 트레이 렌더러에서 차지하는 머티리얼 인덱스.</summary>
        private const int FeltMaterialIndex = 1;

        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        /// <summary>
        /// 펠트 텍셀 경계가 필터 칸 중심 샘플점과 겹치지 않도록 UV 오프셋을 보정한다.
        /// 트레이 중심에 가장 가까운 칸 중심에서 바닥에 광선을 쏴 그 점의 텍셀 소수부가 0.5가 되게 하는 값이다.
        /// 텍셀과 칸이 1:1이라 한 칸에서 맞으면 바닥 전체가 맞는다. 카메라나 트레이가 바뀌면 다시 구해야 한다.
        /// </summary>
        /// <param name="floorLocalY">트레이 메시 로컬에서 펠트 바닥의 Y.</param>
        public static Vector2 FeltPhaseOffset(Camera camera, Vector2Int resolution, Transform trayVisual, float floorLocalY)
        {
            Vector2 baseOffset = FeltOffset();
            if (camera == null || trayVisual == null || resolution.x < 1 || resolution.y < 1) return baseOffset;

            Vector3 floorCenter = trayVisual.TransformPoint(new Vector3(0f, floorLocalY, 0f));
            Vector3 viewport = camera.WorldToViewportPoint(floorCenter);
            float cellX = Mathf.Round(viewport.x * resolution.x - 0.5f);
            float cellY = Mathf.Round(viewport.y * resolution.y - 0.5f);

            Ray ray = camera.ViewportPointToRay(new Vector3((cellX + 0.5f) / resolution.x, (cellY + 0.5f) / resolution.y, 0f));
            Plane floor = new(trayVisual.up, floorCenter);
            if (!floor.Raycast(ray, out float enter)) return baseOffset;

            Vector3 local = trayVisual.InverseTransformPoint(ray.GetPoint(enter));
            Vector2 uv = new(local.x / TrayUvSourceUnits, local.z / TrayUvSourceUnits);
            Vector2 tiling = FeltTiling();
            Vector2 size = new(FeltWidthTexels, FeltHeightTexels);
            float tx = (uv.x * tiling.x + baseOffset.x) * size.x;
            float ty = (uv.y * tiling.y + baseOffset.y) * size.y;

            return new Vector2(
                baseOffset.x + (0.5f - (tx - Mathf.Floor(tx))) / size.x,
                baseOffset.y + (0.5f - (ty - Mathf.Floor(ty))) / size.y);
        }

        /// <summary>
        /// 보정한 오프셋을 펠트 렌더러의 MaterialPropertyBlock에 넣는다. 공유 머티리얼 에셋은 건드리지 않는다.
        /// 월드 카메라 위치·각도·크기가 확정된 뒤에 불러야 한다.
        /// </summary>
        public static void ApplyFeltPhase(Camera camera, Vector2Int resolution)
        {
            GameObject trayObj = GameObject.Find("Yacht Tray Visual");
            if (trayObj == null) return;
            MeshRenderer mr = trayObj.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterials.Length <= FeltMaterialIndex) return;

            Vector2 tiling = FeltTiling();
            Vector2 offset = FeltPhaseOffset(camera, resolution, trayObj.transform, DiceBoardMetrics.PlayFloorSourceY);
            MaterialPropertyBlock block = new();
            mr.GetPropertyBlock(block, FeltMaterialIndex);
            block.SetVector(BaseMapStId, new Vector4(tiling.x, tiling.y, offset.x, offset.y));
            mr.SetPropertyBlock(block, FeltMaterialIndex);
        }

        /// <summary>
        /// 128×96 Point 코듀로이. V 방향 3텍셀이 골 하나(밝음·중간·그늘)이다.
        /// 연속 성분은 타일 안에서 정수 주기라 Repeat 이음매가 없다.
        /// </summary>
        public static Texture2D CreateBurgundyCorduroyTexture()
        {
            int width = FeltWidthTexels;
            int height = FeltHeightTexels;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime Burgundy Corduroy Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            // 골 안 텍셀 순서별 명암(0 그늘 ~ 1 밝음)
            float[] ribProfile = { 1.0f, 0.55f, 0.08f };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float tRidge = ribProfile[y % FeltRibPeriodTexels];

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float s1 = Mathf.Sin((4f * u + 2f * v) * 2f * Mathf.PI) * Mathf.Cos((2f * u - 3f * v) * 2f * Mathf.PI);
                    float s2 = Mathf.Sin((8f * u - 6f * v) * 2f * Mathf.PI) * 0.5f;
                    float organicWave = (s1 + s2) / 1.5f;
                    float toneBlend = Mathf.Clamp01(0.5f + 0.5f * organicWave);
                    float microWeave = (TexelHash(x, y) - 0.5f) * 0.04f;

                    float r = Mathf.Clamp01((35f + 110f * tRidge + 35f * toneBlend + microWeave * 40f) / 255f);
                    float g = Mathf.Clamp01((4f + 26f * tRidge + 18f * toneBlend + microWeave * 25f) / 255f);
                    float b = Mathf.Clamp01((10f + 48f * tRidge + 24f * toneBlend + microWeave * 25f) / 255f);

                    pixels[y * width + x] = new Color(r, g, b, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        /// <summary>텍셀 좌표마다 고정된 0~1 값. 타일링과 무관하게 같은 텍셀은 항상 같은 값이다.</summary>
        private static float TexelHash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return (h & 0xffffu) / 65535f;
            }
        }

        private static void BuildFabricRunner(Transform layoutRoot, float centerSectionX)
        {
            GameObject runnerRoot = new("3D Fabric Runner");
            runnerRoot.layer = TesseraLayers.Decoration;
            runnerRoot.transform.SetParent(layoutRoot, false);
            runnerRoot.transform.position = new Vector3(centerSectionX, -0.40f, 0.4f);
            runnerRoot.transform.rotation = Quaternion.Euler(0f, 4.5f, 0f);

            // 1. 딥 크림슨 펠트 본체 (로우폴리 스타일라이즈드 솔리드 메쉬)
            GameObject feltBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feltBody.name = "Crimson Felt Body";
            feltBody.layer = TesseraLayers.Decoration;
            feltBody.transform.SetParent(runnerRoot.transform, false);
            feltBody.transform.localPosition = Vector3.zero;
            feltBody.transform.localScale = new Vector3(42.0f, 0.040f, 7.2f);

            Collider bodyCol = feltBody.GetComponent<Collider>();
            if (bodyCol != null)
            {
                if (Application.isPlaying) Object.Destroy(bodyCol);
                else Object.DestroyImmediate(bodyCol);
            }

            Color crimsonColor = new Color32(136, 45, 34, 255); // #882d22 (Deep Crimson)
            Material feltMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime 3D LowPoly Crimson Felt Material",
                color = crimsonColor
            };
            if (feltMat.HasProperty("_BaseColor")) feltMat.SetColor("_BaseColor", crimsonColor);
            if (feltMat.HasProperty("_Color")) feltMat.SetColor("_Color", crimsonColor);
            feltMat.SetFloat("_Smoothness", 0.12f);
            feltMat.SetFloat("_Metallic", 0f);

            MeshRenderer bodyMr = feltBody.GetComponent<MeshRenderer>();
            bodyMr.material = feltMat;
            bodyMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            bodyMr.receiveShadows = true;

            // 2. 상/하 앤틱 골드 리본 트림 2줄 (안쪽 인셋 ±2.75f)
            Color goldColor = new Color32(229, 169, 60, 255); // #e5a93c (Antique Gold)
            Material goldMat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime 3D LowPoly Antique Gold Ribbon Material",
                color = goldColor
            };
            if (goldMat.HasProperty("_BaseColor")) goldMat.SetColor("_BaseColor", goldColor);
            if (goldMat.HasProperty("_Color")) goldMat.SetColor("_Color", goldColor);
            goldMat.SetFloat("_Smoothness", 0.78f);
            goldMat.SetFloat("_Metallic", 0.88f);

            float[] trimZ = { -2.75f, 2.75f };
            for (int t = 0; t < 2; t++)
            {
                GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = $"Gold Trim {(t == 0 ? "Top" : "Bottom")}";
                trim.layer = TesseraLayers.Decoration;
                trim.transform.SetParent(runnerRoot.transform, false);
                trim.transform.localPosition = new Vector3(0f, 0.004f, trimZ[t]);
                trim.transform.localScale = new Vector3(42.0f, 0.044f, 0.20f);

                Collider trimCol = trim.GetComponent<Collider>();
                if (trimCol != null)
                {
                    if (Application.isPlaying) Object.Destroy(trimCol);
                    else Object.DestroyImmediate(trimCol);
                }

                MeshRenderer trimMr = trim.GetComponent<MeshRenderer>();
                trimMr.material = goldMat;
                trimMr.shadowCastingMode = ShadowCastingMode.TwoSided;
                trimMr.receiveShadows = true;
            }
        }

        private static void BuildTrayVisual(Transform layoutRoot, Mesh trayMesh, float centerSectionX, float trayVisualY, float trayScale)
        {
            if (trayMesh == null) return;
            GameObject tray = new("Yacht Tray Visual", typeof(MeshFilter), typeof(MeshRenderer));
            tray.transform.SetParent(layoutRoot, false);
            tray.transform.localPosition = new Vector3(
                centerSectionX + DiceBoardMetrics.TrayCenterX, trayVisualY, DiceBoardMetrics.TrayCenterZ);
            tray.transform.localRotation = Quaternion.identity;
            tray.transform.localScale = Vector3.one * trayScale;

            Mesh trayMeshInstance = trayMesh;
            if (trayMeshInstance.uv == null || trayMeshInstance.uv.Length == 0)
            {
                trayMeshInstance = Object.Instantiate(trayMesh);
                trayMeshInstance.uv = BuildPlanarUv(trayMeshInstance);
            }
            tray.GetComponent<MeshFilter>().sharedMesh = trayMeshInstance;

            Material rim = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rim.name = "Runtime Yacht Tray Rim Material";
            rim.color = new Color(0.045f, 0.045f, 0.05f);
            rim.SetFloat("_Smoothness", 0.22f);

            Material felt = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            felt.name = "Runtime Yacht Tray Felt Material";
            Texture2D corduroyTex = CreateBurgundyCorduroyTexture();
            felt.mainTexture = corduroyTex;
            if (felt.HasProperty("_BaseMap")) felt.SetTexture("_BaseMap", corduroyTex);
            if (felt.HasProperty("_MainTex")) felt.SetTexture("_MainTex", corduroyTex);
            if (felt.HasProperty("_BaseColor")) felt.SetColor("_BaseColor", Color.white);
            if (felt.HasProperty("_Color")) felt.SetColor("_Color", Color.white);
            felt.mainTextureScale = FeltTiling();
            felt.mainTextureOffset = FeltOffset();
            felt.color = Color.white;
            felt.SetFloat("_Smoothness", 0.12f);

            tray.GetComponent<MeshRenderer>().sharedMaterials = new[] { rim, felt };
            tray.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
            tray.GetComponent<MeshRenderer>().receiveShadows = true;
        }
        /// <summary>상하면은 XZ, 측면은 축 방향으로 펼친 평면 UV. 트레이 메시가 UV를 갖고 있지 않을 때 쓴다.</summary>
        private static Vector2[] BuildPlanarUv(Mesh mesh)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] norms = mesh.normals;
            Vector2[] uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                Vector3 n = (norms != null && i < norms.Length) ? norms[i] : Vector3.up;
                if (Mathf.Abs(n.y) >= 0.7f)
                    uvs[i] = new Vector2(v.x * (1f / TrayUvSourceUnits), v.z * (1f / TrayUvSourceUnits));
                else
                    uvs[i] = new Vector2((Mathf.Abs(n.x) > Mathf.Abs(n.z) ? v.z : v.x) * (1f / TrayUvSourceUnits), v.y * (1f / TrayUvSourceUnits));
            }
            return uvs;
        }
    }
}
