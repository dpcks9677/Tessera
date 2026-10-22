using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 테이블탑 우측 상단을 장식하는 독자적인 타원형 앤틱 브로치와 짧은 골드 체인
    /// - 굵은 에이지드 브론즈 프레임과 앤틱 골드 인셋
    /// - 픽셀 필터에서도 구분되는 8면 임페리얼 토파즈와 4개 프롱
    /// - 상단 단일 베일에서 브로치 주변으로 짧게 말리는 골드 체인
    /// - 마우스 클릭 시 묵직하게 들썩이며 달그락(Rattle/Wobble)거리는 인터랙션
    /// </summary>
    [ExecuteAlways]
    public sealed class TabletopTrinketBrooch : MonoBehaviour
    {
        private const int DecorationLayer = 11;

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
            // 프리팹 에셋 안에서는 재생성하지 않는다. Unity가 에셋의 Transform 부모 변경을 금지하므로
            // OnValidate가 프리팹 에셋에 대해 돌면 재생성이 실패하며 로그만 쏟아진다.
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;
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
            Transform body = transform.Find("Brooch_Body");
            Transform chain = transform.Find("Brooch_GoldChain");
            Transform gem = body != null ? body.Find("Brooch_FacetedGem") : null;
            Mesh gemMesh = gem != null ? gem.GetComponent<MeshFilter>()?.sharedMesh : null;
            bool usesLegacyGem = gemMesh == null ||
                gemMesh.name != "Procedural_Oval_StepCut_Topaz_v2" ||
                gemMesh.subMeshCount != 3;
            return body == null || chain == null || usesLegacyGem;
        }

        public static TabletopTrinketBrooch Create(Transform parent, Vector3 localPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("Trinket_OvalBrooch");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = rotation ?? Quaternion.Euler(0f, 15f, 0f);
            root.transform.localScale = scale ?? Vector3.one * 1.10f;

            TabletopTrinketBrooch comp = root.AddComponent<TabletopTrinketBrooch>();
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

            // 1. 머티리얼 구성 (명시적 색상 상수로 캐시 오염 방지)
            Color bodyWoodColor = new(0.34f, 0.23f, 0.13f);
            Color goldRimColor = new(0.96f, 0.78f, 0.25f);

            Material bodyMat = new(litShader)
            {
                name = "Trinket_Brooch_BodyMat"
            };
            bodyMat.SetColor("_BaseColor", bodyWoodColor);
            bodyMat.color = bodyWoodColor;
            bodyMat.SetFloat("_Metallic", 0.30f);
            bodyMat.SetFloat("_Smoothness", 0.50f);

            Material goldMat = new(litShader)
            {
                name = "Trinket_Brooch_GoldMat"
            };
            goldMat.SetColor("_BaseColor", goldRimColor);
            goldMat.color = goldRimColor;
            goldMat.SetFloat("_Metallic", 0.94f);
            goldMat.SetFloat("_Smoothness", 0.86f);

            // 2. 픽셀 양자화 뒤에도 분리되는 밝음/중간/어두움 3단 토파즈 머티리얼
            Material topazBrightMat = CreateTopazFacetMaterial(
                litShader, "Trinket_Brooch_Topaz_BrightMat",
                new Color(1.00f, 0.74f, 0.18f), new Color(0.10f, 0.045f, 0.006f));
            Material topazMidMat = CreateTopazFacetMaterial(
                litShader, "Trinket_Brooch_Topaz_MidMat",
                new Color(0.88f, 0.44f, 0.035f), new Color(0.055f, 0.018f, 0.003f));
            Material topazDarkMat = CreateTopazFacetMaterial(
                litShader, "Trinket_Brooch_Topaz_DarkMat",
                new Color(0.48f, 0.19f, 0.015f), new Color(0.018f, 0.004f, 0.0f));

            // 3. 픽셀 필터에서도 외곽이 읽히는 타원형 브로치 본체
            GameObject bodyGroup = new("Brooch_Body");
            bodyGroup.layer = DecorationLayer;
            bodyGroup.transform.SetParent(transform, false);

            GameObject basePlate = new("Brooch_BasePlate", typeof(MeshFilter), typeof(MeshRenderer));
            basePlate.name = "Brooch_BasePlate";
            basePlate.GetComponent<MeshFilter>().sharedMesh = BuildOvalPrismMesh(0.58f, 0.78f, 0.10f, 20);
            SetupPart(basePlate, bodyGroup.transform, new Vector3(0f, 0.05f, 0f), Vector3.zero, Vector3.one, bodyMat);

            // 3-1. 외곽 골드 프레임과 안쪽 브론즈 인셋의 굵은 명도 구분
            GameObject goldRim = new("Brooch_GoldRim", typeof(MeshFilter), typeof(MeshRenderer));
            goldRim.name = "Brooch_GoldRim";
            goldRim.GetComponent<MeshFilter>().sharedMesh = BuildOvalPrismMesh(0.52f, 0.70f, 0.055f, 20);
            SetupPart(goldRim, bodyGroup.transform, new Vector3(0f, 0.125f, 0f), Vector3.zero, Vector3.one, goldMat);

            GameObject inset = new("Brooch_BronzeInset", typeof(MeshFilter), typeof(MeshRenderer));
            inset.GetComponent<MeshFilter>().sharedMesh = BuildOvalPrismMesh(0.45f, 0.62f, 0.035f, 20);
            SetupPart(inset, bodyGroup.transform, new Vector3(0f, 0.170f, 0f), Vector3.zero, Vector3.one, bodyMat);

            // 3-2. 넓은 중앙 테이블과 큰 크라운 면으로 구성된 픽셀 가독형 오벌 스텝컷 토파즈
            GameObject gemObj = new("Brooch_FacetedGem", typeof(MeshFilter), typeof(MeshRenderer));
            gemObj.name = "Brooch_FacetedGem";
            gemObj.GetComponent<MeshFilter>().sharedMesh = BuildPixelReadableTopazMesh(8);
            SetupPart(gemObj, bodyGroup.transform, new Vector3(0f, 0.245f, 0f), new Vector3(0f, 12f, 0f), new Vector3(0.41f, 0.18f, 0.55f), topazMidMat, false);
            MeshRenderer gemRenderer = gemObj.GetComponent<MeshRenderer>();
            Material[] facetMaterials = { topazBrightMat, topazMidMat, topazDarkMat };
            if (Application.isPlaying) gemRenderer.materials = facetMaterials;
            else gemRenderer.sharedMaterials = facetMaterials;

            // 3-3. 픽셀 필터에서 사라지지 않는 굵은 4개 프롱
            CreateProng(bodyGroup.transform, new Vector3(-0.31f, 0.235f, 0f), new Vector3(0f, 0f, -18f), goldMat);
            CreateProng(bodyGroup.transform, new Vector3(0.31f, 0.235f, 0f), new Vector3(0f, 0f, 18f), goldMat);
            CreateProng(bodyGroup.transform, new Vector3(0f, 0.235f, -0.43f), new Vector3(18f, 0f, 0f), goldMat);
            CreateProng(bodyGroup.transform, new Vector3(0f, 0.235f, 0.43f), new Vector3(-18f, 0f, 0f), goldMat);

            // 3-4. 동물 머리 대신 사용하는 상단 단일 체인 베일
            GameObject bail = new("Brooch_ChainBail", typeof(MeshFilter), typeof(MeshRenderer));
            bail.GetComponent<MeshFilter>().sharedMesh = BuildTorusLinkMesh(0.11f, 0.035f, 12, 6);
            SetupPart(bail, bodyGroup.transform, new Vector3(0f, 0.15f, 0.82f), new Vector3(78f, 0f, 0f), Vector3.one, goldMat);

            // 4. 브로치 주변에서 끝나는 짧은 단일 골드 체인
            GameObject chainRoot = new("Brooch_GoldChain");
            chainRoot.layer = DecorationLayer;
            chainRoot.transform.SetParent(transform, false);

            BuildCompactChain(chainRoot.transform, goldMat);

            // 5. 클릭 감지용 콜라이더 (루트 오브젝트에 BoxCollider)
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.08f, 0f);
            boxCol.size = new Vector3(1.25f, 0.48f, 1.75f);
        }

        private static void CreateProng(Transform parent, Vector3 localPos, Vector3 localRot, Material mat)
        {
            GameObject prong = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prong.name = "Brooch_Prong";
            SetupPart(prong, parent, localPos, localRot, new Vector3(0.075f, 0.075f, 0.16f), mat);
        }

        private void BuildCompactChain(Transform chainParent, Material chainMat)
        {
            Vector3 p0 = new(0f, 0.055f, 0.83f);
            Vector3 p1 = new(0.48f, 0.055f, 1.10f);
            Vector3 p2 = new(0.78f, 0.055f, 0.48f);
            Vector3 p3 = new(0.58f, 0.055f, -0.10f);
            const int linkCount = 12;
            Mesh linkMesh = BuildTorusLinkMesh(0.085f, 0.026f, 10, 6);

            for (int i = 0; i < linkCount; i++)
            {
                float t = (float)i / (linkCount - 1);
                Vector3 pos = EvaluateCubicBezier(p0, p1, p2, p3, t);

                float tNext = Mathf.Clamp01(t + 0.02f);
                Vector3 posNext = EvaluateCubicBezier(p0, p1, p2, p3, tNext);
                Vector3 dir = (posNext - pos).normalized;
                if (dir == Vector3.zero) dir = Vector3.forward;

                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                Vector3 linkRot = i % 2 == 0 ? new Vector3(8f, yaw, 0f) : new Vector3(78f, yaw, 0f);
                GameObject link = new($"Link_{i:D2}", typeof(MeshFilter), typeof(MeshRenderer));
                link.name = $"Link_{i:D2}";
                link.GetComponent<MeshFilter>().sharedMesh = linkMesh;
                SetupPart(link, chainParent, pos, linkRot, Vector3.one, chainMat);
            }
        }

        private static Mesh BuildOvalPrismMesh(float radiusX, float radiusZ, float height, int segments)
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            float halfHeight = height * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 b0 = new(Mathf.Cos(a0) * radiusX, -halfHeight, Mathf.Sin(a0) * radiusZ);
                Vector3 b1 = new(Mathf.Cos(a1) * radiusX, -halfHeight, Mathf.Sin(a1) * radiusZ);
                Vector3 t0 = new(b0.x, halfHeight, b0.z);
                Vector3 t1 = new(b1.x, halfHeight, b1.z);
                AddFlatTriangle(vertices, triangles, Vector3.up * halfHeight, t0, t1);
                AddFlatTriangle(vertices, triangles, Vector3.down * halfHeight, b1, b0);
                AddFlatTriangle(vertices, triangles, b0, b1, t1);
                AddFlatTriangle(vertices, triangles, b0, t1, t0);
            }

            Mesh mesh = new() { name = "Procedural_Oval_Brooch_Plate" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildPixelReadableTopazMesh(int segments)
        {
            List<Vector3> vertices = new();
            List<int>[] triangles = { new(), new(), new() };
            const float tableRadius = 0.40f;
            const float tableHeight = 0.58f;
            const float girdleTop = 0.02f;
            const float girdleBottom = -0.10f;
            Vector3 tableCenter = new(0f, tableHeight, 0f);
            Vector3 pavilion = new(0f, -0.38f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 inner0 = new(Mathf.Cos(a0) * tableRadius, tableHeight, Mathf.Sin(a0) * tableRadius);
                Vector3 inner1 = new(Mathf.Cos(a1) * tableRadius, tableHeight, Mathf.Sin(a1) * tableRadius);
                Vector3 outer0 = new(Mathf.Cos(a0), girdleTop, Mathf.Sin(a0));
                Vector3 outer1 = new(Mathf.Cos(a1), girdleTop, Mathf.Sin(a1));
                Vector3 lower0 = new(outer0.x, girdleBottom, outer0.z);
                Vector3 lower1 = new(outer1.x, girdleBottom, outer1.z);

                // 중앙 테이블은 하나의 넓고 밝은 색 덩어리로 읽히게 합니다.
                AddFlatTriangle(vertices, triangles[0], tableCenter, inner1, inner0);

                // 카메라와 키라이트 방향에 따라 큰 크라운 면을 3단 명도로 고정 분류합니다.
                float midAngle = (a0 + a1) * 0.5f;
                float lightFacing = Mathf.Cos(midAngle - 2.35f);
                int crownTone = lightFacing > 0.35f ? 0 : lightFacing < -0.35f ? 2 : 1;
                AddFlatQuad(vertices, triangles[crownTone], inner0, inner1, outer1, outer0);

                // 얇은 거들과 파빌리온은 어두운 외곽선 역할을 하도록 같은 톤으로 묶습니다.
                AddFlatQuad(vertices, triangles[2], outer0, outer1, lower1, lower0);
                AddFlatTriangle(vertices, triangles[2], pavilion, lower0, lower1);
            }

            Mesh mesh = new() { name = "Procedural_Oval_StepCut_Topaz_v2", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFlatQuad(List<Vector3> vertices, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddFlatTriangle(vertices, triangles, a, b, c);
            AddFlatTriangle(vertices, triangles, a, c, d);
        }

        private static Material CreateTopazFacetMaterial(Shader shader, string name, Color color, Color emission)
        {
            Material material = new(shader) { name = name, color = color };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0.03f);
            material.SetFloat("_Smoothness", 0.74f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            return material;
        }

        private static Mesh BuildTorusLinkMesh(float mainRadius, float tubeRadius, int radialSegments, int tubularSegments)
        {
            Mesh mesh = new() { name = "Procedural_Brooch_Chain_Link" };
            int stride = tubularSegments + 1;
            Vector3[] vertices = new Vector3[(radialSegments + 1) * stride];
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = new int[radialSegments * tubularSegments * 6];

            for (int r = 0; r <= radialSegments; r++)
            {
                float u = r / (float)radialSegments * Mathf.PI * 2f;
                Vector3 center = new(Mathf.Cos(u) * mainRadius, 0f, Mathf.Sin(u) * mainRadius);
                for (int t = 0; t <= tubularSegments; t++)
                {
                    float v = t / (float)tubularSegments * Mathf.PI * 2f;
                    Vector3 normal = new(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    int index = r * stride + t;
                    vertices[index] = center + normal * tubeRadius;
                    normals[index] = normal;
                }
            }

            int tri = 0;
            for (int r = 0; r < radialSegments; r++)
            {
                for (int t = 0; t < tubularSegments; t++)
                {
                    int a = r * stride + t;
                    int b = (r + 1) * stride + t;
                    triangles[tri++] = a;
                    triangles[tri++] = b;
                    triangles[tri++] = b + 1;
                    triangles[tri++] = a;
                    triangles[tri++] = b + 1;
                    triangles[tri++] = a + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
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

        private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return (u * u * u * p0) + (3f * u * u * t * p1) + (3f * u * t * t * p2) + (t * t * t * p3);
        }

        private static void SetupPart(GameObject obj, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale, Material mat, bool receiveShadows = true)
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
                mr.receiveShadows = receiveShadows;
            }
        }

        /// <summary>
        /// 클릭 시 호출되는 묵직한 브로치 달그락(Rattle/Wobble) 흔들림 트리거
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
            const float duration = 0.52f;
            float elapsed = 0f;

            float freqX = UnityEngine.Random.Range(28f, 34f);
            float freqZ = UnityEngine.Random.Range(22f, 26f);
            float maxTilt = 16f;
            float maxBounce = 0.04f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float decay = Mathf.Exp(-t * 5.2f);

                float tiltX = Mathf.Sin(elapsed * freqX) * maxTilt * decay;
                float tiltZ = Mathf.Cos(elapsed * freqZ) * (maxTilt * 0.8f) * decay;
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
