using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 테이블탑 우측 상단을 장식하는 3D 절삭 마나 크리스탈 & 샤프 받침대 (Faceted Mana Crystal on Sharp Stand)
    /// - 돌처럼 날카롭게 패싯 절삭된 다면체 푸른색 마나 원석 (Flat Normals 적용)
    /// - 날렵하고 샤프한 4발 지지대(Sharp Claws)와 다크 슬레이트 메탈 베이스
    /// - 마우스 클릭 시 1초 동안 빛이 차올랐다 서서히 사라지는 마나 펄스(1-Second Mana Glow) 애니메이션
    /// </summary>
    [ExecuteAlways]
    public sealed class TabletopTrinketManaCrystal : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("Colors & Lighting")]
        [SerializeField] private Color crystalColor = new(0.28f, 0.74f, 1.0f);
        [SerializeField] private Color standColor = new(0.18f, 0.20f, 0.26f);
        [SerializeField] private Color standTrimColor = new(0.32f, 0.38f, 0.48f);
        [SerializeField, ColorUsage(true, true)] private Color idleEmissionColor = new(0.03f, 0.10f, 0.24f);
        [SerializeField, ColorUsage(true, true)] private Color glowEmissionColor = new(0.30f, 0.75f, 1.25f);

        private Material crystalMat;
        private Light crystalLight;
        private Coroutine glowRoutine;
        private Transform crystalTransform;
        private Vector3 initialCrystalScale = Vector3.one;

        private void Awake()
        {
            EnsureGeometry();
        }

        private void OnEnable()
        {
            EnsureGeometry();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= DelayEnsureGeometry;
                UnityEditor.EditorApplication.delayCall += DelayEnsureGeometry;
            }
        }

        private void DelayEnsureGeometry()
        {
            if (this == null || gameObject == null) return;
            EnsureGeometry();
        }
#endif

        public void EnsureGeometry()
        {
            if (transform.childCount == 0 || IsGeometryMissing())
            {
                BuildGeometry();
            }
        }

        private bool IsGeometryMissing()
        {
            Transform stand = transform.Find("Crystal_Stand");
            Transform crystal = transform.Find("Crystal_Gem");
            return stand == null || crystal == null;
        }

        public static TabletopTrinketManaCrystal Create(Transform parent, Vector3 localPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("Trinket_ManaCrystal");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = rotation ?? Quaternion.Euler(0f, -15f, 0f);
            root.transform.localScale = scale ?? Vector3.one * 1.10f;

            TabletopTrinketManaCrystal comp = root.AddComponent<TabletopTrinketManaCrystal>();
            comp.BuildGeometry();
            return comp;
        }

        [ContextMenu("Rebuild Geometry")]
        public void BuildGeometry()
        {
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

            // 1. 머티리얼 구성
            crystalMat = new Material(litShader)
            {
                name = "Trinket_ManaCrystal_GemMat",
                color = crystalColor
            };
            crystalMat.SetFloat("_Metallic", 0.15f);
            crystalMat.SetFloat("_Smoothness", 0.96f);
            crystalMat.EnableKeyword("_EMISSION");
            crystalMat.SetColor("_EmissionColor", idleEmissionColor);

            Material standMat = new(litShader)
            {
                name = "Trinket_ManaCrystal_StandMat",
                color = standColor
            };
            standMat.SetFloat("_Metallic", 0.82f);
            standMat.SetFloat("_Smoothness", 0.65f);

            Material trimMat = new(litShader)
            {
                name = "Trinket_ManaCrystal_TrimMat",
                color = standTrimColor
            };
            trimMat.SetFloat("_Metallic", 0.88f);
            trimMat.SetFloat("_Smoothness", 0.74f);

            // 2. 샤프한 4발 받침대 그룹
            GameObject standGroup = new("Crystal_Stand");
            standGroup.layer = DecorationLayer;
            standGroup.transform.SetParent(transform, false);

            // 2-1. 정사각 토큰 느낌을 줄인 낮은 팔각 받침대
            GameObject basePlate = new("Stand_BasePlate", typeof(MeshFilter), typeof(MeshRenderer));
            basePlate.name = "Stand_BasePlate";
            basePlate.GetComponent<MeshFilter>().sharedMesh = BuildOctagonalPrismMesh(0.48f, 0.12f);
            SetupPart(basePlate, standGroup.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0f, 22.5f, 0f), Vector3.one, standMat);

            // 2-2. 중앙 소켓 마운트
            GameObject centerSocket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerSocket.name = "Stand_Socket";
            SetupPart(centerSocket, standGroup.transform, new Vector3(0f, 0.13f, 0f), Vector3.zero, new Vector3(0.54f, 0.045f, 0.54f), trimMat);

            // 2-3. 샤프한 4발 지지대 (4 Sharpened Claws / Spires)
            Mesh clawMesh = BuildSharpClawMesh();

            float clawDist = 0.30f;
            float clawY = 0.10f;

            CreateClaw(standGroup.transform, clawMesh, standMat, new Vector3(clawDist, clawY, 0f), new Vector3(0f, -90f, 15f));
            CreateClaw(standGroup.transform, clawMesh, standMat, new Vector3(-clawDist, clawY, 0f), new Vector3(0f, 90f, 15f));
            CreateClaw(standGroup.transform, clawMesh, standMat, new Vector3(0f, clawY, clawDist), new Vector3(15f, 0f, 0f));
            CreateClaw(standGroup.transform, clawMesh, standMat, new Vector3(0f, clawY, -clawDist), new Vector3(-15f, 0f, 0f));

            // 3. 돌처럼 날카롭게 절삭된 다면체 마나 크리스탈 원석 메쉬
            GameObject crystalObj = new("Crystal_Gem");
            crystalTransform = crystalObj.transform;
            MeshFilter crystalMf = crystalObj.AddComponent<MeshFilter>();
            crystalMf.sharedMesh = BuildFacetedCrystalMesh();
            MeshRenderer crystalMr = crystalObj.AddComponent<MeshRenderer>();
            crystalMr.sharedMaterial = crystalMat;
            SetupPart(crystalObj, transform, new Vector3(0f, 0.50f, 0f), new Vector3(7f, 18f, -5f), new Vector3(0.50f, 0.74f, 0.50f), crystalMat);
            initialCrystalScale = crystalObj.transform.localScale;

            // 4. 클릭 발광용 포인트 라이트
            GameObject lightObj = new("Crystal_GlowLight");
            lightObj.layer = DecorationLayer;
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            crystalLight = lightObj.AddComponent<Light>();
            crystalLight.type = LightType.Point;
            crystalLight.color = new Color(0.35f, 0.75f, 1.0f);
            crystalLight.intensity = 0.08f;
            crystalLight.range = 1.10f;
            crystalLight.shadows = LightShadows.None;

            // 5. 클릭 감지용 콜라이더 (루트 오브젝트에 BoxCollider)
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.45f, 0f);
            boxCol.size = new Vector3(0.95f, 0.95f, 0.95f);
        }

        private static void CreateClaw(Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 localRot)
        {
            GameObject clawObj = new("Stand_SharpClaw");
            MeshFilter mf = clawObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = clawObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            SetupPart(clawObj, parent, localPos, localRot, new Vector3(0.16f, 0.30f, 0.16f), mat);
        }

        private static Mesh BuildSharpClawMesh()
        {
            Mesh mesh = new() { name = "Procedural_Sharp_Claw" };

            Vector3[] verts = new Vector3[]
            {
                new(-0.5f, 0f, -0.5f),
                new( 0.5f, 0f, -0.5f),
                new( 0.5f, 0f,  0.5f),
                new(-0.5f, 0f,  0.5f),
                new(0f, 1.0f, -0.22f)
            };

            int[] tris = new int[]
            {
                0, 4, 1,
                1, 4, 2,
                2, 4, 3,
                3, 4, 0,
                0, 3, 2,
                0, 2, 1
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildOctagonalPrismMesh(float radius, float height)
        {
            const int segments = 8;
            List<Vector3> vertices = new();
            List<int> triangles = new();
            float halfHeight = height * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 b0 = new(Mathf.Cos(a0) * radius, -halfHeight, Mathf.Sin(a0) * radius);
                Vector3 b1 = new(Mathf.Cos(a1) * radius, -halfHeight, Mathf.Sin(a1) * radius);
                Vector3 t0 = new(b0.x, halfHeight, b0.z);
                Vector3 t1 = new(b1.x, halfHeight, b1.z);
                AddTriangle(vertices, triangles, Vector3.up * halfHeight, t0, t1);
                AddTriangle(vertices, triangles, Vector3.down * halfHeight, b1, b0);
                AddTriangle(vertices, triangles, b0, b1, t1);
                AddTriangle(vertices, triangles, b0, t1, t0);
            }

            Mesh mesh = new() { name = "Procedural_Octagonal_Crystal_Stand" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildFacetedCrystalMesh()
        {
            Mesh mesh = new() { name = "Procedural_Faceted_Mana_Crystal" };

            Vector3 topApex = new(0.08f, 1.0f, -0.05f);
            Vector3 botApex = new(-0.04f, -0.82f, 0.05f);

            Vector3[] equator = new Vector3[]
            {
                new( 0.64f,  0.08f,  0.02f),
                new( 0.47f,  0.18f,  0.46f),
                new( 0.02f,  0.06f,  0.66f),
                new(-0.46f,  0.14f,  0.48f),
                new(-0.63f,  0.02f, -0.04f),
                new(-0.42f, -0.06f, -0.50f),
                new( 0.04f,  0.10f, -0.62f),
                new( 0.45f, -0.02f, -0.42f)
            };

            List<Vector3> flatVerts = new();
            List<int> flatTris = new();

            for (int i = 0; i < equator.Length; i++)
            {
                int next = (i + 1) % equator.Length;
                AddTriangle(flatVerts, flatTris, topApex, equator[i], equator[next]);
            }

            for (int i = 0; i < equator.Length; i++)
            {
                int next = (i + 1) % equator.Length;
                AddTriangle(flatVerts, flatTris, equator[next], equator[i], botApex);
            }

            mesh.vertices = flatVerts.ToArray();
            mesh.triangles = flatTris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(List<Vector3> verts, List<int> tris, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            int baseIdx = verts.Count;
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);
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

        /// <summary>
        /// 마우스 클릭 시 1초 동안 빛이 들어왔다가 사라지는 마나 발광 애니메이션 트리거
        /// </summary>
        [ContextMenu("Trigger Glow")]
        public void TriggerGlow()
        {
            if (!gameObject.activeInHierarchy) return;

            if (glowRoutine != null)
            {
                StopCoroutine(glowRoutine);
            }
            glowRoutine = StartCoroutine(GlowAnimationRoutine());
        }

        private IEnumerator GlowAnimationRoutine()
        {
            const float totalDuration = 1.0f; // 정확히 1초
            const float fadeInDuration = 0.18f;
            float elapsed = 0f;

            if (crystalMat == null)
            {
                Transform gem = transform.Find("Crystal_Gem");
                if (gem != null)
                {
                    MeshRenderer mr = gem.GetComponent<MeshRenderer>();
                    if (mr != null) crystalMat = mr.material;
                }
            }

            if (crystalLight == null)
            {
                Transform lightT = transform.Find("Crystal_GlowLight");
                if (lightT != null) crystalLight = lightT.GetComponent<Light>();
            }

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float intensityFactor;

                if (elapsed < fadeInDuration)
                {
                    float tIn = elapsed / fadeInDuration;
                    intensityFactor = Mathf.SmoothStep(0f, 1f, tIn);
                }
                else
                {
                    float tOut = (elapsed - fadeInDuration) / (totalDuration - fadeInDuration);
                    intensityFactor = Mathf.Pow(1f - tOut, 2.2f);
                }

                if (crystalMat != null)
                {
                    Color currentEmission = Color.Lerp(idleEmissionColor, glowEmissionColor * 2.8f, intensityFactor);
                    crystalMat.SetColor("_EmissionColor", currentEmission);
                }

                if (crystalLight != null)
                {
                    crystalLight.intensity = Mathf.Lerp(0.08f, 1.40f, intensityFactor);
                }

                if (crystalTransform != null)
                {
                    float scaleMultiplier = 1.0f + intensityFactor * 0.06f;
                    crystalTransform.localScale = initialCrystalScale * scaleMultiplier;
                }

                yield return null;
            }

            if (crystalMat != null)
            {
                crystalMat.SetColor("_EmissionColor", idleEmissionColor);
            }
            if (crystalLight != null)
            {
                crystalLight.intensity = 0.08f;
            }
            if (crystalTransform != null)
            {
                crystalTransform.localScale = initialCrystalScale;
            }

            glowRoutine = null;
        }
    }
}
