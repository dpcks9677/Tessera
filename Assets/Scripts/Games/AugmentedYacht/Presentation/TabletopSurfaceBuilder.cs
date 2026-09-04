using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.AugmentedYacht
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
                    felt.mainTextureScale = new Vector2(1f, 1f);
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
            float plankHeight = 4.90f;
            float gap = 0.10f;
            float plankWidth = 38.0f;
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

            Color[] plankColors = new Color[]
            {
                new Color32(110, 67, 42, 255), // Plank 1: #6e432a (Warm Honey Brown)
                new Color32(120, 73, 46, 255), // Plank 2: #78492e (Amber Toast Brown)
                new Color32(99, 60, 37, 255),  // Plank 3: #633c25 (Deep Toffee Walnut)
                new Color32(115, 69, 43, 255)  // Plank 4: #73452b (Warm Walnut Brown)
            };

            // 판자마다 서로 다른 옹이(Knot)와 결 위치를 위한 UV Offset & Scale
            Vector2[] uvOffsets = new Vector2[]
            {
                new(0.00f, 0.00f),
                new(0.40f, 0.20f),
                new(0.80f, 0.60f),
                new(0.20f, 0.40f)
            };

            Texture2D woodTexture = null;
#if UNITY_EDITOR
            woodTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Wood/wood_grain_knots.png");
#endif

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

                Material mat = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = $"Runtime Heavy Wood Plank {i + 1} Material",
                    color = plankColors[i % plankColors.Length]
                };

                if (woodTexture != null)
                {
                    mat.mainTexture = woodTexture;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", woodTexture);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", woodTexture);

                    Vector2 tiling = new(1.5f, 1.0f);
                    Vector2 offset = uvOffsets[i % uvOffsets.Length];
                    mat.mainTextureScale = tiling;
                    mat.mainTextureOffset = offset;
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTextureScale("_BaseMap", tiling);
                        mat.SetTextureOffset("_BaseMap", offset);
                    }
                    if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTextureScale("_MainTex", tiling);
                        mat.SetTextureOffset("_MainTex", offset);
                    }
                }

                mat.SetFloat("_Smoothness", 0.20f);
                mat.SetFloat("_Metallic", 0f);

                MeshRenderer mr = plank.GetComponent<MeshRenderer>();
                mr.material = mat;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                mr.receiveShadows = true;
            }
        }

        private static Texture2D CreateBurgundyCorduroyTexture()
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, true)
            {
                name = "Runtime Burgundy Corduroy Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            int numRibs = 20; // 20개의 선명하고 굵은 가로 코듀로이 골

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float phase = v * numRibs * 2f * Mathf.PI;
                float sinVal = Mathf.Sin(phase);
                float ridgeProfile = Mathf.Sign(sinVal) * Mathf.Pow(Mathf.Abs(sinVal), 0.55f);
                float tRidge = (ridgeProfile + 1f) * 0.5f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float s1 = Mathf.Sin(u * 3.7f * 2f * Mathf.PI + v * 2.1f * 2f * Mathf.PI) * Mathf.Cos(u * 1.9f * 2f * Mathf.PI - v * 3.4f * 2f * Mathf.PI);
                    float s2 = Mathf.Sin(u * 8.3f * 2f * Mathf.PI - v * 6.5f * 2f * Mathf.PI) * 0.5f;
                    float organicWave = (s1 + s2) / 1.5f;
                    float toneBlend = Mathf.Clamp01(0.5f + 0.5f * organicWave);
                    float microWeave = ((Mathf.Sin(x * 0.85f) + Mathf.Cos(y * 0.85f)) * 0.5f) * 0.04f;

                    float r = Mathf.Clamp01((35f + 110f * tRidge + 35f * toneBlend + microWeave * 40f) / 255f);
                    float g = Mathf.Clamp01((4f + 26f * tRidge + 18f * toneBlend + microWeave * 25f) / 255f);
                    float b = Mathf.Clamp01((10f + 48f * tRidge + 24f * toneBlend + microWeave * 25f) / 255f);

                    pixels[y * width + x] = new Color(r, g, b, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            return tex;
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
            tray.transform.localPosition = new Vector3(centerSectionX, trayVisualY, DiceBoardMetrics.TrayCenterZ);
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
            felt.mainTextureScale = new Vector2(1f, 1f);
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
                    uvs[i] = new Vector2(v.x * (1f / 50f), v.z * (1f / 50f));
                else
                    uvs[i] = new Vector2((Mathf.Abs(n.x) > Mathf.Abs(n.z) ? v.z : v.x) * (1f / 50f), v.y * (1f / 50f));
            }
            return uvs;
        }
    }
}
