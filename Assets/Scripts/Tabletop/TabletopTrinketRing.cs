using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 테이블탑 우측 상단을 장식하는 3D 스털링 실버 룬 반지 (Silver Ring with Amber Gemstone)
    /// - 토러스 밴드를 비스듬히 눕혀 링 구멍과 앰버 보석 측면이 함께 보이는 자연스러운 테이블 배치
    /// - 마우스 클릭 시 가볍게 위로 튀며 달그락(Rattle/Wobble)거리는 감쇠 진동 인터랙션
    /// </summary>
    [ExecuteAlways]
    public sealed class TabletopTrinketRing : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("Materials & Rendering")]
        [SerializeField] private Color silverColor = new(0.92f, 0.94f, 0.98f);
        [SerializeField] private Color goldAccentColor = new(0.90f, 0.72f, 0.30f);
        [SerializeField] private Color gemAmberColor = new(1.00f, 0.58f, 0.12f);

        private Coroutine rattleRoutine;
        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private bool isTransformCached;

        private void Awake()
        {
            CacheInitialTransform();
            EnsureGeometry();
        }

        private void OnEnable()
        {
            CacheInitialTransform();
            EnsureGeometry();
        }

        public void CacheInitialTransform(bool force = false)
        {
            if (!isTransformCached || force || (initialLocalPos == Vector3.zero && transform.localPosition != Vector3.zero))
            {
                initialLocalPos = transform.localPosition;
                initialLocalRot = transform.localRotation;
                isTransformCached = true;
            }
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
            Transform band = transform.Find("Ring_Band");
            Transform gem = transform.Find("Ring_Gem");
            Transform bezel = transform.Find("Ring_Bezel");
            bool usesLegacySideGem = gem != null && (gem.localPosition.y < 0.40f || Mathf.Abs(gem.localPosition.z) > 0.05f);
            return band == null || gem == null || bezel == null || usesLegacySideGem;
        }

        public static TabletopTrinketRing Create(Transform parent, Vector3 localPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("Trinket_SilverRing");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = rotation ?? Quaternion.Euler(62f, -25f, 0f);
            root.transform.localScale = scale ?? Vector3.one * 1.20f;

            TabletopTrinketRing comp = root.AddComponent<TabletopTrinketRing>();
            comp.CacheInitialTransform(true);
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
            Material silverMat = new(litShader)
            {
                name = "Trinket_SilverRing_SilverMat",
                color = silverColor
            };
            silverMat.SetFloat("_Metallic", 0.95f);
            silverMat.SetFloat("_Smoothness", 0.90f);

            Material goldMat = new(litShader)
            {
                name = "Trinket_SilverRing_GoldAccentMat",
                color = goldAccentColor
            };
            goldMat.SetFloat("_Metallic", 0.90f);
            goldMat.SetFloat("_Smoothness", 0.78f);

            Material gemMat = new(litShader)
            {
                name = "Trinket_SilverRing_AmberGemMat",
                color = gemAmberColor
            };
            gemMat.SetFloat("_Metallic", 0.12f);
            gemMat.SetFloat("_Smoothness", 0.96f);

            // 2. 보석 쪽으로 자연스럽게 굵어지는 테이퍼 링 밴드
            GameObject bandObj = new("Ring_Band");
            MeshFilter bandMf = bandObj.AddComponent<MeshFilter>();
            bandMf.sharedMesh = BuildTaperedTorusMesh(0.36f, 0.062f, 0.092f, 28, 12);
            MeshRenderer bandMr = bandObj.AddComponent<MeshRenderer>();
            bandMr.sharedMaterial = silverMat;
            SetupPart(bandObj, transform, Vector3.zero, Vector3.zero, Vector3.one, silverMat);

            // 3. 세로 밴드의 정수리에서 월드 위쪽을 향하는 베젤 마운트
            GameObject bezelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bezelObj.name = "Ring_Bezel";
            SetupPart(bezelObj, transform, new Vector3(0f, 0.43f, 0f), Vector3.zero, new Vector3(0.31f, 0.055f, 0.28f), goldMat);

            // 4. 베젤 중심에 정렬된 굵은 8면 앰버 패싯 보석
            GameObject gemObj = new("Ring_Gem");
            gemObj.name = "Ring_Gem";
            gemObj.AddComponent<MeshFilter>().sharedMesh = BuildFacetedGemMesh(8);
            gemObj.AddComponent<MeshRenderer>();
            SetupPart(gemObj, transform, new Vector3(0f, 0.52f, 0f), Vector3.zero, new Vector3(0.17f, 0.14f, 0.15f), gemMat);

            // 5. 밴드-보석 좌우 숄더 브릿지 마운트 (Shoulders)
            GameObject leftShoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftShoulder.name = "Ring_Shoulder_L";
            SetupPart(leftShoulder, transform, new Vector3(-0.15f, 0.34f, 0f), new Vector3(0f, 0f, -18f), new Vector3(0.12f, 0.15f, 0.10f), silverMat);

            GameObject rightShoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightShoulder.name = "Ring_Shoulder_R";
            SetupPart(rightShoulder, transform, new Vector3(0.15f, 0.34f, 0f), new Vector3(0f, 0f, 18f), new Vector3(0.12f, 0.15f, 0.10f), silverMat);

            // 6. 클릭 감지용 콜라이더 (루트 오브젝트에 단일 BoxCollider)
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.06f, 0f);
            boxCol.size = new Vector3(0.90f, 1.10f, 0.42f);
        }

        private static Mesh BuildTaperedTorusMesh(float mainRadius, float backTubeRadius, float frontTubeRadius, int radialSegments, int tubularSegments)
        {
            Mesh mesh = new() { name = "Procedural_Trinket_Torus" };

            int vertCount = (radialSegments + 1) * (tubularSegments + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int j = 0; j <= radialSegments; j++)
            {
                float u = (float)j / radialSegments * Mathf.PI * 2f;
                float cosU = Mathf.Cos(u);
                float sinU = Mathf.Sin(u);
                float frontWeight = Mathf.Pow(Mathf.Clamp01((1f + sinU) * 0.5f), 2.2f);
                float tubeRadius = Mathf.Lerp(backTubeRadius, frontTubeRadius, frontWeight);

                for (int i = 0; i <= tubularSegments; i++)
                {
                    float v = (float)i / tubularSegments * Mathf.PI * 2f;
                    float cosV = Mathf.Cos(v);
                    float sinV = Mathf.Sin(v);

                    Vector3 pos = new(
                        (mainRadius + tubeRadius * cosV) * cosU,
                        (mainRadius + tubeRadius * cosV) * sinU,
                        tubeRadius * sinV
                    );

                    Vector3 center = new(mainRadius * cosU, mainRadius * sinU, 0f);
                    Vector3 norm = (pos - center).normalized;

                    int idx = j * (tubularSegments + 1) + i;
                    vertices[idx] = pos;
                    normals[idx] = norm;
                    uvs[idx] = new Vector2((float)j / radialSegments, (float)i / tubularSegments);
                }
            }

            int triCount = radialSegments * tubularSegments * 6;
            int[] triangles = new int[triCount];
            int triIdx = 0;

            for (int j = 0; j < radialSegments; j++)
            {
                for (int i = 0; i < tubularSegments; i++)
                {
                    int a = j * (tubularSegments + 1) + i;
                    int b = (j + 1) * (tubularSegments + 1) + i;
                    int c = (j + 1) * (tubularSegments + 1) + (i + 1);
                    int d = j * (tubularSegments + 1) + (i + 1);

                    triangles[triIdx++] = a;
                    triangles[triIdx++] = b;
                    triangles[triIdx++] = c;

                    triangles[triIdx++] = a;
                    triangles[triIdx++] = c;
                    triangles[triIdx++] = d;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildFacetedGemMesh(int segments)
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            Vector3 top = new(0f, 0.55f, 0f);
            Vector3 bottom = new(0f, -0.45f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 p0 = new(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 p1 = new(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                AddFlatTriangle(vertices, triangles, top, p1, p0);
                AddFlatTriangle(vertices, triangles, bottom, p0, p1);
            }

            Mesh mesh = new() { name = "Procedural_Ring_FacetedGem" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFlatTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
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
        /// 클릭 시 호출되는 달그락(Rattle/Wobble) 감쇠 진동 애니메이션 트리거
        /// </summary>
        [ContextMenu("Trigger Rattle")]
        public void TriggerRattle()
        {
            CacheInitialTransform();
            if (!gameObject.activeInHierarchy) return;

            if (rattleRoutine != null)
            {
                StopCoroutine(rattleRoutine);
            }
            rattleRoutine = StartCoroutine(RattleAnimationRoutine());
        }

        private IEnumerator RattleAnimationRoutine()
        {
            const float duration = 0.48f;
            float elapsed = 0f;

            float freqX = UnityEngine.Random.Range(32f, 38f);
            float freqZ = UnityEngine.Random.Range(24f, 30f);
            float maxTilt = 18f;
            float maxBounce = 0.05f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float decay = Mathf.Exp(-t * 5.8f);

                float tiltX = Mathf.Sin(elapsed * freqX) * maxTilt * decay;
                float tiltZ = Mathf.Cos(elapsed * freqZ) * (maxTilt * 0.75f) * decay;
                Quaternion wobbleRot = initialLocalRot * Quaternion.Euler(tiltX, 0f, tiltZ);

                float bounceY = Mathf.Abs(Mathf.Sin(elapsed * freqX * 0.5f)) * maxBounce * decay;
                Vector3 bouncePos = initialLocalPos + new Vector3(0f, bounceY, 0f);

                transform.localPosition = bouncePos;
                transform.localRotation = wobbleRot;

                yield return null;
            }

            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;
            rattleRoutine = null;
        }
    }
}
