using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 중세 여관/서재 테이블탑 우측 하단을 장식하는 3D 고광택 블랙 세라믹 잉크통과 깃펜 오브젝트
    /// </summary>
    [ExecuteAlways]
    public sealed class InkwellAndQuill : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        // 깃펜 로컬 좌표계: +Y가 닙 끝(y=0)에서 깃털 팁(y≈3.97) 방향.
        private const float NibLength = 0.59f;
        private const float CollarBandY = 0.60f;
        private const float RachisStartY = 0.71f;
        private const float RachisLength = 3.26f;
        private const float BladeStartY = 0.75f;
        private const float BladeLength = 3.22f;
        private const float SpineCurveX = 0.035f;
        private const float SpineCurveZ = 0.012f;

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
            Transform quillRoot = transform.Find("Quill Pen Root");
            if (quillRoot == null) return true;
            Transform nib = quillRoot.Find("Quill_Nib");
            Transform shaft = quillRoot.Find("Quill_Curved_Shaft");
            Transform blade = quillRoot.Find("Quill_Feather_Blade");
            if (nib == null || shaft == null || blade == null) return true;
            if (IsMeshMissing(nib) || IsMeshMissing(shaft) || IsMeshMissing(blade)) return true;
            return false;
        }

        private static bool IsMeshMissing(Transform target)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            return filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0;
        }

        public static InkwellAndQuill Create(Transform parent, Vector3 worldPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("3D Inkwell and Quill Decoration");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition;
            root.transform.rotation = rotation ?? Quaternion.identity;
            root.transform.localScale = scale ?? Vector3.one;

            InkwellAndQuill comp = root.AddComponent<InkwellAndQuill>();
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
            // 검은색 잉크통 머티리얼 (고광택 블랙 세라믹)
            Material blackCeramicBodyMat = CreateMaterial("Black Ceramic Body Material", litShader, new Color(0.08f, 0.08f, 0.09f), 0.25f, 0.90f);
            Material blackCeramicRimMat = CreateMaterial("Black Ceramic Rim Material", litShader, new Color(0.05f, 0.05f, 0.06f), 0.35f, 0.92f);
            Material liquidInkMat = CreateMaterial("Liquid Ink Material", litShader, new Color(0.02f, 0.02f, 0.02f), 0.10f, 0.96f);
            Material goldTrimMat = CreateMaterial("Antique Gold Trim Material", litShader, new Color(0.78f, 0.58f, 0.22f), 0.82f, 0.68f);

            // 백랍/은 금속 (깃펜 닙 & 삼엽 장식 칼라)
            Material pewterSilverMat = CreateMaterial("Pewter Silver Material", litShader, new Color(0.72f, 0.74f, 0.78f), 0.90f, 0.75f);

            // 깃털 펜 머티리얼 (깃대 뼈대 & 스타일라이즈드 깃털 텍스처)
            Material quillShaftMat = CreateMaterial("Quill Shaft Material", litShader, new Color(0.93f, 0.89f, 0.80f), 0.04f, 0.45f);
            Material quillFeatherMat = CreateMaterial("Quill Feather Material", litShader, Color.white, 0.01f, 0.16f);

            // 핸드페인티드 스타일의 깃털 알베도 & 노멀 텍스처 생성
            Texture2D featherTexture = GenerateStylizedFeatherTexture();
            Texture2D featherNormal = GenerateFeatherNormalMap();

            quillFeatherMat.mainTexture = featherTexture;
            if (quillFeatherMat.HasProperty("_BaseMap"))
            {
                quillFeatherMat.SetTexture("_BaseMap", featherTexture);
            }
            if (quillFeatherMat.HasProperty("_BumpMap"))
            {
                quillFeatherMat.SetTexture("_BumpMap", featherNormal);
                quillFeatherMat.EnableKeyword("_NORMALMAP");
                quillFeatherMat.SetFloat("_BumpScale", 0.75f);
            }

            // 2. 원통형 블랙 잉크통 (Cylindrical Black Inkwell)
            GameObject inkwellGroup = new("Inkwell Body");
            inkwellGroup.layer = DecorationLayer;
            inkwellGroup.transform.SetParent(transform, false);

            // 2-1. 하단 받침대 (Base Rim)
            GameObject baseRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRim.name = "Inkwell_BaseRim";
            SetupPart(baseRim, inkwellGroup.transform, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(1.35f, 0.08f, 1.35f), blackCeramicRimMat);

            // 2-2. 중앙 원통형 메인 바디 (빛 반사 하이라이트가 맺히는 본체)
            GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainBody.name = "Inkwell_MainBody";
            SetupPart(mainBody, inkwellGroup.transform, new Vector3(0f, 0.40f, 0f), Vector3.zero, new Vector3(1.05f, 0.28f, 1.05f), blackCeramicBodyMat);

            // 2-3. 입구 골드 림 액센트 링 (Antique Gold Ring)
            GameObject goldRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goldRing.name = "Inkwell_GoldRing";
            SetupPart(goldRing, inkwellGroup.transform, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.82f, 0.02f, 0.82f), goldTrimMat);

            // 2-4. 상단 병목 및 입구 림 (Neck Rim)
            GameObject neckRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neckRim.name = "Inkwell_NeckRim";
            SetupPart(neckRim, inkwellGroup.transform, new Vector3(0f, 0.74f, 0f), Vector3.zero, new Vector3(0.72f, 0.07f, 0.72f), blackCeramicRimMat);

            // 2-5. 입구 내부 액체 잉크 표면 (Liquid Ink Surface)
            GameObject inkSurface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inkSurface.name = "Inkwell_LiquidInk";
            SetupPart(inkSurface, inkwellGroup.transform, new Vector3(0f, 0.78f, 0f), Vector3.zero, new Vector3(0.56f, 0.01f, 0.56f), liquidInkMat);

            // 3. 2시 방향으로 우아하게 기울어진 깃펜 (Feather Quill Pen)
            GameObject quillRoot = new("Quill Pen Root");
            quillRoot.layer = DecorationLayer;
            quillRoot.transform.SetParent(transform, false);
            quillRoot.transform.localPosition = new Vector3(0f, 0.78f, 0f);

            // 사선 틸트: Pitch 40°, Yaw -65°, Roll 20°
            quillRoot.transform.localRotation = Quaternion.Euler(40f, -65f, 20f);

            // 3-1. 펜촉 (길고 가느다란 원뿔형 은 닙, 펜 전체 길이의 약 40%)
            GameObject nibObj = new("Quill_Nib");
            MeshFilter nibMf = nibObj.AddComponent<MeshFilter>();
            nibMf.sharedMesh = BuildNibMesh();
            nibObj.AddComponent<MeshRenderer>();
            SetupPart(nibObj, quillRoot.transform, Vector3.zero, Vector3.zero, Vector3.one, pewterSilverMat);

            // 3-2. 펜대-깃털 연결 장식 칼라 (밴드 + 비드 + 삼엽 장식)
            GameObject collarBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collarBand.name = "Quill_Collar_Band";
            SetupPart(collarBand, quillRoot.transform, new Vector3(0f, CollarBandY, 0f), Vector3.zero, new Vector3(0.105f, 0.055f, 0.105f), pewterSilverMat);

            GameObject collarBead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            collarBead.name = "Quill_Collar_Bead";
            SetupPart(collarBead, quillRoot.transform, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.13f, 0.10f, 0.13f), pewterSilverMat);

            GameObject leafLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafLeft.name = "Quill_Collar_Leaf_L";
            SetupPart(leafLeft, quillRoot.transform, new Vector3(-0.075f, 0.75f, 0f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.055f), pewterSilverMat);

            GameObject leafRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafRight.name = "Quill_Collar_Leaf_R";
            SetupPart(leafRight, quillRoot.transform, new Vector3(0.075f, 0.75f, 0f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.055f), pewterSilverMat);

            GameObject leafCenter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafCenter.name = "Quill_Collar_Leaf_C";
            SetupPart(leafCenter, quillRoot.transform, new Vector3(0f, 0.81f, 0f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.055f), pewterSilverMat);

            // 3-3. 프로시저럴 곡선 깃대 (Tapered Curved Spine / Rachis)
            GameObject shaftObj = new("Quill_Curved_Shaft");
            MeshFilter shaftMf = shaftObj.AddComponent<MeshFilter>();
            shaftMf.sharedMesh = BuildCurvedShaftMesh();
            MeshRenderer shaftMr = shaftObj.AddComponent<MeshRenderer>();
            shaftMr.sharedMaterial = quillShaftMat;
            SetupPart(shaftObj, quillRoot.transform, Vector3.zero, Vector3.zero, Vector3.one, quillShaftMat);

            // 3-4. 프로시저럴 정교한 3D 깃판 (Procedural Stylized Feather Blade)
            GameObject featherObj = new("Quill_Feather_Blade");
            MeshFilter featherMf = featherObj.AddComponent<MeshFilter>();
            featherMf.sharedMesh = BuildProceduralFeatherMesh();
            MeshRenderer featherMr = featherObj.AddComponent<MeshRenderer>();
            featherMr.sharedMaterial = quillFeatherMat;
            SetupPart(featherObj, quillRoot.transform, Vector3.zero, Vector3.zero, Vector3.one, quillFeatherMat);
        }

        /// <summary>
        /// 깃대(Rachis)와 깃판이 공유하는 스파인 오프셋. 두 메쉬가 같은 곡선을 따라야 깃판이 깃대에서 떨어지지 않는다.
        /// 칼라 아래(닙 구간)는 t가 0으로 클램프되어 오프셋이 없다.
        /// </summary>
        private static Vector3 SpineOffset(float y)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(RachisStartY, RachisStartY + RachisLength, y));
            return new Vector3(
                Mathf.Pow(t, 1.35f) * SpineCurveX,
                0f,
                Mathf.Sin(t * Mathf.PI * 0.85f) * SpineCurveZ);
        }

        /// <summary>
        /// 길고 가느다란 원뿔형 은 닙. 아래쪽 끝(y=0)이 펜촉이다.
        /// </summary>
        private static Mesh BuildNibMesh()
        {
            return BuildTaperedTubeMesh("Procedural_Quill_Nib", 0f, NibLength, 0.004f, 0.050f, 1.35f, 20);
        }

        /// <summary>
        /// 깃털 중심을 따라 완만하게 위로 뻗어나가며 가늘어지는 곡선형 깃대(Rachis) 3D 메쉬 생성
        /// </summary>
        private static Mesh BuildCurvedShaftMesh()
        {
            return BuildTaperedTubeMesh("Procedural_Quill_Shaft", RachisStartY, RachisLength, 0.030f, 0.004f, 1f, 28);
        }

        /// <summary>
        /// 스파인을 따라 굵기가 변하는 개방형 튜브 메쉬. 닙과 깃대가 공유한다.
        /// radiusGamma가 1보다 크면 시작 굵기를 더 오래 유지한다.
        /// </summary>
        private static Mesh BuildTaperedTubeMesh(
            string meshName, float startY, float totalLength,
            float baseRadius, float tipRadius, float radiusGamma, int segments)
        {
            Mesh mesh = new() { name = meshName };

            const int radialSegments = 8;

            int vertCount = (segments + 1) * radialSegments;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int s = 0; s <= segments; s++)
            {
                float t = (float)s / segments;
                float y = startY + t * totalLength;

                Vector3 spine = SpineOffset(y);
                Vector3 center = new(spine.x, y, spine.z);

                float radius = Mathf.Lerp(baseRadius, tipRadius, Mathf.Pow(t, radiusGamma));

                for (int r = 0; r < radialSegments; r++)
                {
                    float angle = ((float)r / radialSegments) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 normal = new(cos, 0f, sin);
                    Vector3 pos = center + new Vector3(cos * radius, 0f, sin * radius);

                    int idx = s * radialSegments + r;
                    vertices[idx] = pos;
                    normals[idx] = normal;
                    uvs[idx] = new Vector2((float)r / radialSegments, t);
                }
            }

            int triCount = segments * radialSegments * 6;
            int[] triangles = new int[triCount];
            int triIdx = 0;

            for (int s = 0; s < segments; s++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int nextR = (r + 1) % radialSegments;

                    int i0 = s * radialSegments + r;
                    int i1 = (s + 1) * radialSegments + r;
                    int i2 = (s + 1) * radialSegments + nextR;
                    int i3 = s * radialSegments + nextR;

                    triangles[triIdx++] = i0;
                    triangles[triIdx++] = i1;
                    triangles[triIdx++] = i2;

                    triangles[triIdx++] = i0;
                    triangles[triIdx++] = i2;
                    triangles[triIdx++] = i3;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 실제 조류 깃털(Primary Feather)의 우아한 비대칭 타원형 실루엣과 부드러운 아치 곡면을 가진 3D 깃판 메쉬 생성
        /// </summary>
        private static Mesh BuildProceduralFeatherMesh()
        {
            Mesh mesh = new() { name = "Procedural_Quill_Feather_Blade" };

            const int slices = 96;          // 높이 방향 세그먼트 (깃가지 6개 × 16슬라이스로 노치 경계 정렬)
            const int cols = 7;             // 횡단면 정점 수 (더 둥글고 부드러운 날개 곡면)
            const int barbCount = 6;        // 넓은 깃면 가장자리에 드러나는 갈라진 깃가지 수
            const float startY = BladeStartY;
            const float totalLength = BladeLength;

            int vertCount = (slices + 1) * cols;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int s = 0; s <= slices; s++)
            {
                float t = (float)s / slices; // 0.0 (하단) ~ 1.0 (최상단 팁)
                float y = startY + t * totalLength;

                Vector3 spine = SpineOffset(y);
                Vector3 center = new(spine.x, y, spine.z);

                // 1. 좁고 길쭉한 깃털 실루엣
                // - 하단 기저부(0.0~0.12): 부드러운 확장
                // - 최대 폭(0.30f)은 30% 높이
                // - 상단(0.30~1.0): 긴 코사인 수렴으로 날카로운 팁 마감
                float baseWidth;
                if (t < 0.12f)
                {
                    float u = t / 0.12f;
                    baseWidth = Mathf.Sin(u * Mathf.PI * 0.5f) * 0.20f;
                }
                else if (t < 0.30f)
                {
                    float u = (t - 0.12f) / 0.18f;
                    baseWidth = Mathf.Lerp(0.20f, 0.30f, Mathf.Sin(u * Mathf.PI * 0.5f));
                }
                else
                {
                    float u = (t - 0.30f) / 0.70f;
                    // 부동소수 오차로 cos가 미세 음수가 되면 Pow가 NaN을 낸다. 반드시 클램프한다.
                    float taper = Mathf.Max(0f, Mathf.Cos(u * Mathf.PI * 0.5f));
                    baseWidth = Mathf.Pow(taper, 0.9f) * 0.30f;
                }

                // 2. 강한 비대칭 폭 (좌측: 바깥 날개 1.20, 우측: 안쪽 날개 0.72)
                float leftWidth = baseWidth * 1.20f;
                float rightWidth = baseWidth * 0.72f;

                // 3. 넓은 깃면 가장자리의 갈라진 깃가지 노치.
                //    k가 1에서 0으로 감기는 지점에서 폭이 급격히 복귀하며 V자 컷이 생긴다.
                float k = Mathf.Repeat(t * barbCount, 1f);
                float notchFade = 1f - Mathf.InverseLerp(0.55f, 0.95f, t); // 팁 근처는 갈라짐 없음
                float notchDepth = 0.22f * k * notchFade;

                // 4. 횡단면 7개 정점 계산 (중심 깃대에서 외곽으로 완만하게 둥글어지는 파라볼릭 아치)
                for (int c = 0; c < cols; c++)
                {
                    float colFactor = (c - 3) / 3.0f; // -1.0(좌외곽) ~ 0(중심) ~ 1.0(우외곽)
                    float spanX;
                    if (colFactor < 0f)
                    {
                        // 깃대에 가까운 컬럼일수록 노치를 약하게 먹여 뿌리 쪽은 붙어 있게 한다.
                        float notch = 1f - notchDepth * -colFactor;
                        spanX = colFactor * leftWidth * notch;
                    }
                    else
                    {
                        spanX = colFactor * rightWidth;
                    }

                    // 깃대 중심에서 외곽으로 갈수록 뒤쪽(-Z)으로 완만하게 굽어지는 부드러운 돔 곡면
                    float camberZ = -Mathf.Pow(Mathf.Abs(colFactor), 1.6f) * 0.038f;

                    Vector3 pos = center + new Vector3(spanX, 0f, camberZ);

                    int idx = s * cols + c;
                    vertices[idx] = pos;

                    // UV 매핑: U는 0(좌) ~ 1(우), V는 0(하) ~ 1(상)
                    float uCoord = (float)c / (cols - 1);
                    uvs[idx] = new Vector2(uCoord, t);
                }
            }

            // 앞면 인덱스로 노멀을 먼저 계산한다.
            int quadCount = slices * (cols - 1);
            int[] frontTriangles = new int[quadCount * 6];
            int triIdx = 0;

            for (int s = 0; s < slices; s++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int i0 = s * cols + c;
                    int i1 = (s + 1) * cols + c;
                    int i2 = (s + 1) * cols + (c + 1);
                    int i3 = s * cols + (c + 1);

                    frontTriangles[triIdx++] = i0;
                    frontTriangles[triIdx++] = i1;
                    frontTriangles[triIdx++] = i2;

                    frontTriangles[triIdx++] = i0;
                    frontTriangles[triIdx++] = i2;
                    frontTriangles[triIdx++] = i3;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = frontTriangles;
            mesh.RecalculateNormals();
            Vector3[] frontNormals = mesh.normals;

            // 양면 렌더링: 뒷면은 정점을 복제해 반대 노멀을 준다.
            // 정점을 공유하면 RecalculateNormals가 앞뒤 노멀을 평균 내 음영이 무너진다.
            Vector3[] doubledVertices = new Vector3[vertCount * 2];
            Vector3[] doubledNormals = new Vector3[vertCount * 2];
            Vector2[] doubledUvs = new Vector2[vertCount * 2];
            for (int i = 0; i < vertCount; i++)
            {
                doubledVertices[i] = vertices[i];
                doubledVertices[i + vertCount] = vertices[i];
                doubledNormals[i] = frontNormals[i];
                doubledNormals[i + vertCount] = -frontNormals[i];
                doubledUvs[i] = uvs[i];
                doubledUvs[i + vertCount] = uvs[i];
            }

            int[] triangles = new int[frontTriangles.Length * 2];
            Array.Copy(frontTriangles, triangles, frontTriangles.Length);
            for (int i = 0; i < frontTriangles.Length; i += 3)
            {
                int target = frontTriangles.Length + i;
                triangles[target] = frontTriangles[i] + vertCount;
                triangles[target + 1] = frontTriangles[i + 2] + vertCount;
                triangles[target + 2] = frontTriangles[i + 1] + vertCount;
            }

            mesh.Clear();
            mesh.vertices = doubledVertices;
            mesh.normals = doubledNormals;
            mesh.uv = doubledUvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 40° 사선 깃털 결(Fine Barbs)과 3단 웜 판타지 그라데이션이 적용된 512x1024 깃털 텍스처 생성
        /// </summary>
        private static Texture2D GenerateStylizedFeatherTexture()
        {
            const int width = 512;
            const int height = 1024;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
            {
                name = "Stylized_Quill_Feather_Albedo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            // 수채화 톤 웜 브라운 팔레트
            Color wideVaneCream = new(0.90f, 0.82f, 0.70f, 1.0f);    // 넓은 깃면 안쪽 크림
            Color middleWarmToffee = new(0.72f, 0.50f, 0.30f, 1.0f); // 웜 토피 브라운 / 골든 앰버
            Color edgeMahogany = new(0.34f, 0.20f, 0.12f, 1.0f);     // 앤틱 마호가니 에스프레소
            Color spineBright = new(0.96f, 0.93f, 0.86f, 1.0f);      // 중심 깃대 하이라이트

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height; // 0 (하단) ~ 1 (상단)

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width; // 0 (넓은 깃면 외곽) ~ 0.5 (깃대) ~ 1 (좁은 깃면 외곽)
                    float distFromCenter = Mathf.Abs(u - 0.5f) * 2.0f; // 0.0 (중심) ~ 1.0 (외곽)
                    bool isNarrowVane = u > 0.5f;

                    // 1. 깃대에서 바깥쪽으로 뻗어나가는 40° 사선 결(Barb) 좌표
                    float barbLine = v - distFromCenter * 0.35f;

                    // 굵은 수채화 붓결 (다중 주파수 합성)
                    float barbNoise1 = Mathf.Sin(barbLine * 90f * Mathf.PI * 2f);
                    float barbNoise2 = Mathf.Sin(barbLine * 180f * Mathf.PI * 2f);
                    float barbPattern = (barbNoise1 * 0.6f + barbNoise2 * 0.4f) * 0.08f;

                    // 2. 비대칭 그라데이션.
                    //    넓은 깃면(u < 0.5)은 크림 -> 토피, 좁은 깃면(u > 0.5)은 토피 -> 마호가니로 빠르게 어두워진다.
                    float blendDist = Mathf.Clamp01(Mathf.Pow(distFromCenter, 1.15f) + barbPattern);
                    Color col = isNarrowVane
                        ? Color.Lerp(middleWarmToffee, edgeMahogany, blendDist)
                        : Color.Lerp(wideVaneCream, middleWarmToffee, blendDist);

                    // 3. 상단 팁 앤틱 마호가니 블렌드
                    if (v > 0.62f)
                    {
                        float tipFactor = Mathf.Clamp01((v - 0.62f) / 0.38f);
                        col = Color.Lerp(col, edgeMahogany, tipFactor * 0.80f);
                    }

                    // 4. 중심 깃대(Spine) 하이라이트
                    if (distFromCenter < 0.08f)
                    {
                        float spineBlend = 1.0f - (distFromCenter / 0.08f);
                        col = Color.Lerp(col, spineBright, spineBlend * 0.60f);
                    }

                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// 깃털 결(Barb)을 입체적으로 살려주는 512x1024 프로시저럴 노멀맵 생성
        /// </summary>
        private static Texture2D GenerateFeatherNormalMap()
        {
            const int width = 512;
            const int height = 1024;
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
            {
                name = "Stylized_Quill_Feather_Normal",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float distFromCenter = Mathf.Abs(u - 0.5f) * 2.0f;
                    float sign = u >= 0.5f ? 1f : -1f;

                    // 40° 사선 방향의 결 노멀 벡터 계산
                    float barbLine = v - distFromCenter * 0.35f;
                    float barbSlope = Mathf.Cos(barbLine * 90f * Mathf.PI * 2f);

                    float nx = -sign * 0.25f + barbSlope * 0.15f;
                    float ny = barbSlope * 0.12f;
                    float nz = 1.0f;

                    Vector3 norm = new Vector3(nx, ny, nz).normalized;
                    // [-1, 1] -> [0, 1]
                    pixels[y * width + x] = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1.0f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
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

        private static Material CreateMaterial(string name, Shader shader, Color color, float metallic, float smoothness)
        {
            Material mat = new(shader)
            {
                name = name,
                color = color
            };
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
    }
}
