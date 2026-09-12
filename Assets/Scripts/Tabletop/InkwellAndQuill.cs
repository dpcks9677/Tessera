using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 중세 여관/서재 테이블탑 우측 하단을 장식하는 3D 고광택 블랙 세라믹 잉크통과 깃펜 오브젝트
    ///
    /// 잉크통은 프리미티브 원통 5개로 코드에서 만들고, 깃펜은 외부 로우폴리 모델
    /// <c>Assets/Art/Reference/quill_pen_low.fbx</c>를 인스턴스화해 쓴다. 깃펜을 절차적으로
    /// 만들던 시절에는 깃털 실루엣과 깃가지 틈을 코드 상수로 맞춰야 했는데, 화면이 480x270
    /// 가상 격자로 필터링되는 탓에 그 디테일이 대부분 남지 않아 투자 대비 효과가 없었다.
    /// </summary>
    [ExecuteAlways]
    public sealed class InkwellAndQuill : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        private const string QuillRootName = "Quill Pen Root";
        private const string QuillModelName = "Quill_Model";
        private const string QuillModelPath = "Assets/Art/Reference/quill_pen_low.fbx";
        private const string QuillMaterialPath = "Assets/Art/Reference/QuillPen.mat";

        /// <summary>
        /// 모델을 <see cref="QuillRootName"/> 로컬 좌표계에 맞추는 배율.
        ///
        /// <see cref="QuillHoverAnimator"/>는 이 루트의 원점을 닙 끝으로, +Y를 깃털 방향으로 보고
        /// 필기 자세를 만든다. 임포트된 모델의 전장은 11.634이고, 이 배율에서 깃펜의 루트 로컬
        /// 길이가 3.97이 된다(= 3.97 / 11.634). 절차적 깃펜이 쓰던 길이와 같은 값이며, 씬의
        /// 잉크통에 걸린 2.5배까지 곱하면 월드 전장 9.93이 된다.
        ///
        /// 이 전장은 <see cref="QuillHoverAnimator"/>가 필기 자세 크기를 정할 때 기준으로 삼는
        /// 값이라 임의로 키우면 안 된다. 점수표 가로 폭이 7.80이므로 깃펜은 이미 그보다 길고,
        /// 더 키우면 필기 중에 점수표를 덮는 면적이 그만큼 늘어난다.
        ///
        /// 이 값은 FBX 임포터의 <c>Use File Scale</c>이 꺼져 있다는 전제 위에 있다. 파일
        /// 스케일을 켜면 Unity가 0.01을 적용해 깃펜이 화면에서 점 하나로 줄어든다. 파일
        /// 헤더의 <c>UnitScaleFactor</c>는 1이라 이 축소는 헤더만 보고는 예측되지 않으니,
        /// 모델을 다시 임포트할 일이 있으면 임포터 설정을 직접 확인한다.
        /// </summary>
        private const float QuillModelScale = 0.3412f;

        /// <summary>
        /// 모델 로컬 닙 끝을 루트 원점으로 옮기는 오프셋. 위 배율을 곱한 값이다.
        ///
        /// 이 값은 <b>임포트된 메시</b>를 기준으로 잡아야 한다. Unity는 FBX를 읽으면서 메시마다
        /// 원점을 자기 바운드 중심으로 옮기고 그 이동량을 노드 Transform에 넣는다. 그래서 FBX
        /// 파일의 정점 좌표와 Unity 노드 위치를 섞어 계산하면 값이 어긋난다. 실제로 그렇게
        /// 계산해 닙이 월드에서 1.4만큼 밀린 적이 있다.
        ///
        /// 모델은 메시 셋(깃판·깃대·닙 장식)으로 나뉘고, 가장 아래는 깃대 메시의 정점
        /// (0.117, -5.817, -0.230)이다. 깃대가 펜촉부터 깃털 끝까지 관통하므로 위아래 끝이 모두
        /// 이 메시에 있다. 값을 다시 잡을 일이 있으면 각 노드의 localPosition과 MeshRenderer의
        /// localBounds를 읽어 확인한다.
        /// </summary>
        private static readonly Vector3 QuillModelOffset = new(-0.040f, 1.985f, 0.079f);

        /// <summary>
        /// 꽂힘 자세의 위치다. 닙 끝이 이 자리에 온다.
        ///
        /// 잉크 표면은 0.78에 있으므로 이 값은 그보다 0.08 아래, 즉 닙이 잉크에 잠긴 높이다.
        /// 표면과 같은 높이로 두면 펜이 잉크통에 꽂혔다기보다 입구에 얹힌 것처럼 보인다.
        /// x와 z가 0인 것은 잉크통 원통들이 모두 로컬 원점을 축으로 서 있기 때문이다.
        /// </summary>
        private static readonly Vector3 QuillRootLocalPosition = new(0f, 0.70f, 0f);

        /// <summary>꽂힘 자세의 기울기. 사선 틸트 Pitch 40도, Yaw -65도, Roll 20도.</summary>
        private static readonly Vector3 QuillRootLocalEuler = new(40f, -65f, 20f);

        [SerializeField] private GameObject quillModel;
        [SerializeField] private Material quillMaterial;

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
                return;
            }

            RealignQuillModel();
        }

        /// <summary>
        /// 이미 있는 깃펜 모델의 배치와 머티리얼을 상수에 맞춘다.
        ///
        /// 씬은 깃펜을 프리팹 인스턴스가 아니라 자기 오브젝트로 들고 있어서, 씬 파일에 예전
        /// 값이 저장돼 있으면 로드할 때마다 그 값이 되살아난다. 배치를 상수에서 한 번 더
        /// 강제하면 씬 파일이 무엇을 들고 있든 실행 시점의 자세가 코드와 같아진다. 닙 끝이
        /// 루트 원점에 오지 않으면 필기 자세에서 펜이 가리키는 칸이 어긋나므로 중요하다.
        /// </summary>
        private void RealignQuillModel()
        {
            Transform quillRoot = transform.Find(QuillRootName);
            Transform model = quillRoot != null ? quillRoot.Find(QuillModelName) : null;
            if (model == null) return;

            // 꽂힘 자세도 상수에서 다시 맞춘다. QuillHoverAnimator가 이 자세를 기준으로 삼으므로
            // 씬에 옛 값이 남아 있으면 복귀할 때마다 그 자리로 돌아간다.
            Quaternion rootRotation = Quaternion.Euler(QuillRootLocalEuler);
            if (quillRoot.localPosition != QuillRootLocalPosition) quillRoot.localPosition = QuillRootLocalPosition;
            if (quillRoot.localRotation != rootRotation) quillRoot.localRotation = rootRotation;

            ResolveQuillAssets();

            Vector3 scale = Vector3.one * QuillModelScale;
            if (model.localPosition != QuillModelOffset) model.localPosition = QuillModelOffset;
            if (model.localRotation != Quaternion.identity) model.localRotation = Quaternion.identity;
            if (model.localScale != scale) model.localScale = scale;

            if (quillMaterial == null) return;
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.sharedMaterial == quillMaterial) continue;
                if (Application.isPlaying) renderer.material = quillMaterial;
                else renderer.sharedMaterial = quillMaterial;
            }
        }

        /// <summary>
        /// 깃펜이 모델 인스턴스로 서 있는지 본다.
        ///
        /// <see cref="QuillModelName"/> 노드를 이름으로 확인하는 이유는, 씬 파일에 예전 절차적
        /// 깃펜(닙·칼라·깃대·깃판)이 저장돼 있을 수 있기 때문이다. 메시가 있는지만 보면 그것도
        /// 통과해 버려 옛 깃펜이 그대로 남는다. 모델 안쪽 계층은 임포트 설정에 따라 이름이
        /// 달라지므로 그 아래로는 이름을 보지 않는다.
        /// </summary>
        private bool IsGeometryMissing()
        {
            Transform quillRoot = transform.Find(QuillRootName);
            if (quillRoot == null) return true;
            if (quillRoot.childCount != 1) return true;

            Transform model = quillRoot.Find(QuillModelName);
            if (model == null) return true;

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0) return false;
            }

            return true;
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

            // 1. 잉크통 머티리얼 구성 (고광택 블랙 세라믹)
            Material blackCeramicBodyMat = CreateMaterial("Black Ceramic Body Material", litShader, new Color(0.08f, 0.08f, 0.09f), 0.25f, 0.90f);
            Material blackCeramicRimMat = CreateMaterial("Black Ceramic Rim Material", litShader, new Color(0.05f, 0.05f, 0.06f), 0.35f, 0.92f);
            Material liquidInkMat = CreateMaterial("Liquid Ink Material", litShader, new Color(0.02f, 0.02f, 0.02f), 0.10f, 0.96f);
            Material goldTrimMat = CreateMaterial("Antique Gold Trim Material", litShader, new Color(0.78f, 0.58f, 0.22f), 0.82f, 0.68f);

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
            // 이 루트의 자세가 곧 QuillHoverAnimator의 도킹 자세다.
            GameObject quillRoot = new(QuillRootName);
            quillRoot.layer = DecorationLayer;
            quillRoot.transform.SetParent(transform, false);
            quillRoot.transform.localPosition = QuillRootLocalPosition;
            quillRoot.transform.localRotation = Quaternion.Euler(QuillRootLocalEuler);

            AttachQuillModel(quillRoot.transform);
        }

        private void AttachQuillModel(Transform quillRoot)
        {
            ResolveQuillAssets();

            if (quillModel == null)
            {
                Debug.LogWarning($"[InkwellAndQuill] 깃펜 모델을 찾지 못해 잉크통만 만들었습니다. 경로={QuillModelPath}");
                return;
            }

            GameObject model = Instantiate(quillModel, quillRoot, false);
            model.name = QuillModelName;
            model.transform.localPosition = QuillModelOffset;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * QuillModelScale;

            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = DecorationLayer;
            }

            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (quillMaterial != null)
                {
                    if (Application.isPlaying) renderer.material = quillMaterial;
                    else renderer.sharedMaterial = quillMaterial;
                }

                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                renderer.receiveShadows = true;
            }
        }

        /// <summary>
        /// 직렬화 참조가 비어 있으면 에디터에서 경로로 채운다. 빌드에는 프리팹에 구워진 참조가
        /// 실려 있으므로 이 폴백이 필요 없다. 주사위 모델을 다루는
        /// <c>AugmentedYachtController.Awake</c>와 같은 방식이다.
        /// </summary>
        private void ResolveQuillAssets()
        {
#if UNITY_EDITOR
            if (quillModel == null)
            {
                quillModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(QuillModelPath);
            }

            if (quillMaterial == null)
            {
                quillMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(QuillMaterialPath);
            }
#endif
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
