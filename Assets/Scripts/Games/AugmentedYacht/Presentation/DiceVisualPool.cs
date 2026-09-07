using System;
using System.Collections;
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Dice;
using Tessera.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 주사위 시각 오브젝트를 만들고 재질과 초기 배치를 관리한다(M10-T5).
    ///
    /// 주사위의 개수·킵 여부·눈금 값 같은 게임 상태는 컨트롤러가 계속 소유한다.
    /// 이 클래스는 그 상태를 인자로 받아 화면에 반영만 한다. 상태를 나눠 갖지 않는 편이
    /// 턴 흐름을 읽을 때 혼란이 적다.
    ///
    /// 굴림 애니메이션(<c>AnimateDiceLayout</c>)은 아직 컨트롤러에 있다.
    /// 진행 단계 플래그와 상태 문구 갱신에 얽혀 있어 M10-T6에서 함께 옮긴다.
    /// </summary>
    public sealed class DiceVisualPool : MonoBehaviour
    {
        private const int DiceLayer = TesseraLayers.Dice;
        private const string CrispDepthMaskShaderName = "DicePoC/CrispUiDepthMask";
        private const string CrispTextShaderName = "DicePoC/CrispUiText";
        private const string CrispDepthMaskName = "Crisp Depth Mask";

        /// <summary>깊이만 남기는 판은 색을 쓰지 않으므로 주사위마다 나눌 이유가 없다.</summary>
        private static Material crispDepthMaskMaterial;

        /// <summary>면 숫자 재질. 색은 TextMesh가 정점 색으로 실으므로 종류별로 나누지 않는다.</summary>
        private static Material crispTextMaterial;
        private static Font crispTextFont;

        private GameObject diceModel;
        private GameObject octahedronModel;
        private GameObject sevensModel;
        private Transform layoutRoot;
        private float centerSectionX;

        private DieType selectedDieType = DieType.Normal;
        private Transform diceRoot;
        private RenderStyle renderStyle = RenderStyle.Baseline;

        public Transform DiceRoot => diceRoot;
        public DieType SelectedDieType => selectedDieType;
        public RenderStyle RenderStyle => renderStyle;

        /// <summary>
        /// 연출 방식을 바꾸고 이미 만들어진 주사위에도 즉시 반영한다(M10.8).
        /// 주사위는 풀에서 재사용되므로 새로 만드는 경로만 고쳐서는 화면이 바뀌지 않는다.
        /// </summary>
        public void SetRenderStyle(RenderStyle style)
        {
            if (renderStyle == style) return;
            renderStyle = style;

            if (diceRoot == null) return;
            foreach (Transform die in diceRoot)
            {
                foreach (Transform visual in die)
                {
                    // 절차적 폴백 경로의 면 오버레이 쿼드는 몸체가 아니므로 건너뛴다.
                    if (visual.name.EndsWith("Overlay", StringComparison.OrdinalIgnoreCase)) continue;
                    ApplyDiceMaterialsToFbx(visual.gameObject, selectedDieType);
                }
            }
        }

        /// <summary>컨트롤러가 모델과 배치 기준을 넘겨준다.</summary>
        public void Bind(GameObject model, Transform layout, float centerX, DieType initialDieType)
        {
            diceModel = model;
            layoutRoot = layout;
            centerSectionX = centerX;
            selectedDieType = initialDieType;
        }

        /// <summary>형상이 다른 특수 주사위 모델을 넘겨준다(M7-T5). 없으면 기본 D6로 대체된다.</summary>
        public void BindSpecialModels(GameObject octahedron, GameObject sevens)
        {
            octahedronModel = octahedron;
            sevensModel = sevens;
        }

        /// <summary>주사위 하나에 종류를 반영한다. 종류는 주사위마다 다를 수 있다(M7-T5).</summary>
        public void ApplyDieType(GameObject die, DieType type)
        {
            if (die == null) return;

            DiceKeepTarget target = die.GetComponent<DiceKeepTarget>();
            DieType previous = target != null ? target.Type : DieType.Normal;
            if (target != null) target.Type = type;

            Transform visual = die.transform.Find("Visual");
            if (visual == null || ModelFor(previous) != ModelFor(type))
            {
                visual = BuildVisual(die, type);
            }

            if (visual != null) ApplyDiceMaterialsToFbx(visual.gameObject, type);
        }

        /// <summary>종류별 형상 모델. 세븐스와 8면 주사위만 몸체가 다르다.</summary>
        private GameObject ModelFor(DieType type)
        {
            return type switch
            {
                DieType.Octahedron => octahedronModel != null ? octahedronModel : diceModel,
                DieType.Sevens => sevensModel != null ? sevensModel : diceModel,
                _ => diceModel
            };
        }

        /// <summary>모델 인스턴스를 새로 만들어 Visual 자리에 끼운다. 기존 Visual은 버린다.</summary>
        private Transform BuildVisual(GameObject die, DieType type)
        {
            DiscardVisuals(die.transform);

            GameObject model = ModelFor(type);
            if (model == null) return null;

            GameObject visual = Instantiate(model, die.transform);
            visual.name = "Visual";
            DisableImportedSceneComponents(visual);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = MeasureBaseCorrection(visual.transform, type);
            NormalizeVisual(visual.transform, NormalizedSizeFor(type));
            SetLayerRecursively(die, DiceLayer);
            if (type == DieType.Octahedron) PromoteOctaDigitsToCrispUi(visual.transform);
            return visual.transform;
        }

        /// <summary>
        /// 이전 몸체를 모두 버린다.
        ///
        /// 재생 중 <see cref="Object.Destroy"/>는 프레임이 끝날 때 처리된다. 이름을 그대로 두면
        /// 같은 프레임의 다음 호출이 파괴 예약된 오브젝트를 "Visual"로 다시 찾아 그 뒤에 새 몸체를
        /// 덧붙이므로, 종류를 연달아 바꾸면 8면체와 D6가 겹쳐 쌓인다. 이름을 먼저 바꾸고 꺼 둔다.
        /// </summary>
        private static void DiscardVisuals(Transform die)
        {
            for (int i = die.childCount - 1; i >= 0; i--)
            {
                Transform child = die.GetChild(i);
                if (child.name != "Visual") continue;

                child.name = "Visual (Discarded)";
                child.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// 정규화 목표 크기. 8면체만 1.2로 키운다.
        ///
        /// 정규화는 큐브 원본 규격(1.62)으로 나누는 고정 비율이라 8면체는 마주 보는 두 면 사이가
        /// D6 한 변의 0.70배로 줄어든다. 실루엣은 꼭짓점 덕에 커 보이지만 숫자를 새기는 평면은
        /// 그만큼 좁아진다. 트레이 간격(1.5)이 허용하는 선에서 키워 면을 넓힌다.
        /// </summary>
        private static float NormalizedSizeFor(DieType type)
        {
            return type == DieType.Octahedron ? 1.2f : 1.0f;
        }

        /// <summary>
        /// 8면 주사위의 면 숫자만 픽셀 필터 밖으로 옮긴다(M17-T8).
        ///
        /// 면 평면의 내접원이 640×480 격자에서 약 7.6px라, 새긴 숫자는 획을 어떻게 잡아도 축소에서
        /// 사라지거나 흰 덩어리로 뭉친다. 몸체는 픽셀 패스에 그대로 두고 숫자만
        /// <see cref="TesseraLayers.CrispUI"/> 레이어로 올리면 점수표·증강 카드 글자와 같은 경로로
        /// 원본 해상도에 합성돼, 숫자가 면에 붙은 채로 획이 살아난다.
        ///
        /// Crisp 카메라는 월드 물체를 하나도 찍지 않아 깊이 버퍼가 비어 있다. 그대로 두면 뒤쪽 면의
        /// 숫자가 몸체를 뚫고 보이므로, 색을 쓰지 않고 깊이만 남기는 몸체 사본을 같은 레이어에 함께 둔다.
        /// </summary>
        private static void PromoteOctaDigitsToCrispUi(Transform visual)
        {
            Mesh bodyMesh = null;
            for (int i = 0; i < visual.childCount; i++)
            {
                Transform child = visual.GetChild(i);
                if (child.name == "Body")
                {
                    MeshFilter bodyFilter = child.GetComponent<MeshFilter>();
                    if (bodyFilter != null) bodyMesh = bodyFilter.sharedMesh;
                    continue;
                }

                if (!child.name.StartsWith("Pip_", StringComparison.Ordinal)) continue;
                SetLayerRecursively(child.gameObject, TesseraLayers.CrispUI);
            }

            if (bodyMesh == null) return;

            Material maskMaterial = EnsureCrispDepthMaskMaterial();
            if (maskMaterial == null) return;

            GameObject maskObject = new(CrispDepthMaskName, typeof(MeshFilter), typeof(MeshRenderer))
            {
                layer = TesseraLayers.CrispUI,
                hideFlags = HideFlags.DontSave
            };
            maskObject.transform.SetParent(visual, false);
            maskObject.GetComponent<MeshFilter>().sharedMesh = bodyMesh;

            MeshRenderer maskRenderer = maskObject.GetComponent<MeshRenderer>();
            maskRenderer.sharedMaterial = maskMaterial;
            maskRenderer.shadowCastingMode = ShadowCastingMode.Off;
            maskRenderer.receiveShadows = false;
        }

        /// <summary>
        /// 면 숫자용 재질. 글리프 아틀라스는 폰트가 들고 있는 것을 그대로 빌려 쓴다.
        ///
        /// 동적 폰트는 새 글자를 요구받으면 아틀라스를 다시 굽고, 그때 텍스처 객체 자체가 바뀔 수
        /// 있다. 이 씬은 같은 폰트로 증강 카드의 한글도 찍으므로 실제로 일어난다. 재굽기 통지를
        /// 받아 텍스처를 다시 물려 두지 않으면 그 순간부터 숫자가 빈칸이 된다.
        /// </summary>
        private static Material EnsureCrispTextMaterial(Font font)
        {
            if (font == null) return null;

            if (crispTextMaterial == null)
            {
                Shader shader = Shader.Find(CrispTextShaderName);
                if (shader == null) return null;

                crispTextMaterial = new Material(shader)
                {
                    name = "Runtime Octa Crisp Text",
                    hideFlags = HideFlags.HideAndDontSave
                };
                Font.textureRebuilt += OnFontTextureRebuilt;
            }

            crispTextFont = font;
            crispTextMaterial.mainTexture = font.material != null ? font.material.mainTexture : null;
            return crispTextMaterial;
        }

        private static void OnFontTextureRebuilt(Font font)
        {
            if (crispTextMaterial == null || font != crispTextFont || font.material == null) return;
            crispTextMaterial.mainTexture = font.material.mainTexture;
        }

        private static Material EnsureCrispDepthMaskMaterial()
        {
            if (crispDepthMaskMaterial != null) return crispDepthMaskMaterial;

            Shader shader = Shader.Find(CrispDepthMaskShaderName);
            if (shader == null) return null;

            crispDepthMaskMaterial = new Material(shader)
            {
                name = "Runtime Octa Crisp Depth Mask",
                hideFlags = HideFlags.HideAndDontSave
            };
            return crispDepthMaskMaterial;
        }

        /// <summary>
        /// 모델 자체의 기울기를 직교로 되돌리는 회전.
        /// 8면 주사위는 베이커가 이미 직교로 구웠고 눈이 축 방향에 있지도 않아 측정하지 않는다.
        /// </summary>
        private static Quaternion MeasureBaseCorrection(Transform visual, DieType type)
        {
            return type == DieType.Octahedron
                ? Quaternion.identity
                : DiceFaceOrientation.MeasureModelBasis(visual);
        }

        public void SetDieType(DieType type, System.Collections.Generic.IReadOnlyList<GameObject> activeDice)
        {
            selectedDieType = type;
            foreach (GameObject die in activeDice) ApplyDieType(die, type);
        }

        public void EnsureDiceRoot()
        {
            if (diceRoot != null) return;
            GameObject root = GameObject.Find("Dice Visual Root");
            if (root == null)
            {
                root = new GameObject("Dice Visual Root");
                root.transform.SetParent(layoutRoot != null ? layoutRoot : transform, false);
                root.transform.position = new Vector3(centerSectionX, 0f, 0f);
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
            }
            diceRoot = root.transform;
        }

        public GameObject CreateVisualDie(int index)
        {
            GameObject root = new($"Die_{index}", typeof(BoxCollider), typeof(DiceKeepTarget));
            root.layer = DiceLayer;
            root.transform.SetParent(diceRoot, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * DiceBoardMetrics.DieSize;

            // FBX 자체의 isometric 기울기(333, 318, 0)를 0도로 직교 보정하고 1단위 큐브로 정규화한다.
            Transform visual = ModelFor(selectedDieType) != null ? BuildVisual(root, selectedDieType) : null;
            if (visual != null)
            {
                ApplyDiceMaterialsToFbx(visual.gameObject, selectedDieType);
            }
            else
            {
                Mesh mesh = DiceMeshFactory.Create();
                MeshFilter mf = root.AddComponent<MeshFilter>();
                MeshRenderer mr = root.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterials = DiceMaterialFactory.GetNormalMaterials();
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = false;
                DiceMaterialFactory.AttachFaceOverlays(root.transform);
            }

            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.center = Vector3.zero;

            DiceKeepTarget target = root.GetComponent<DiceKeepTarget>();
            target.Index = index - 1;
            target.Type = selectedDieType;

            return root;
        }

        private static void NormalizeVisual(Transform visual, float targetLocalSize = 1.0f)
        {
            visual.localPosition = Vector3.zero;
            // 큐브 원본 규격 크기(DiceBoardMetrics.SourceDiceSize = 1.62f) 기준으로 고정 정규화하여 모델링 변경 시 크기 오차 방지
            float rawBodySize = DiceBoardMetrics.SourceDiceSize;
            visual.localScale = Vector3.one * (targetLocalSize / rawBodySize);
        }

        private void ApplyDiceMaterialsToFbx(GameObject visual, DieType type)
        {
            Material diceBodyMaterial = DicePaletteCatalog.GetBodyMaterial(type, renderStyle);
            Material dicePipMaterial = DicePaletteCatalog.GetPipMaterial(type);

            // 솔리드 그림자 프록시(ShadowProxy) 확인 및 설정 (음각 홈으로 인한 그림자 구멍 완전 차단)
            EnsureShadowProxy(visual.transform);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                // 깊이 전용 판은 색을 한 픽셀도 쓰지 않는 것이 존재 이유다. 몸체 재질로 덮으면
                // Crisp 카메라가 몸체를 원본 해상도로 한 번 더 그려, 주사위에만 픽셀 필터가
                // 걸리지 않은 것처럼 보인다.
                if (renderer.name == CrispDepthMaskName) continue;

                if (renderer.name.Equals("ShadowProxy", StringComparison.OrdinalIgnoreCase))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    renderer.receiveShadows = false;
                    renderer.sharedMaterial = diceBodyMaterial;
                    continue;
                }

                // 8면체 면 숫자는 폰트 아틀라스를 쓰는 TextMesh다. 눈 재질로 덮으면 글리프가 사라지고,
                // 폰트 기본 재질을 그대로 두면 뒷면 글자가 몸체를 뚫고 나온다. 색은 정점 색으로 싣고
                // 깊이를 검사하는 전용 재질로 바꾼다.
                TextMesh digitText = renderer.GetComponent<TextMesh>();
                if (digitText != null)
                {
                    digitText.color = DicePaletteCatalog.GetDefinition(type).PipColor;
                    Material textMaterial = EnsureCrispTextMaterial(digitText.font);
                    if (textMaterial != null) renderer.sharedMaterial = textMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else if (renderer.name.StartsWith("Pip", StringComparison.OrdinalIgnoreCase))
                {
                    renderer.sharedMaterial = dicePipMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; // Pip 메시 그림자 캐스팅 제외
                }
                else
                {
                    // Plain_D6 몸체: 슬롯 0(바탕 Body), 슬롯 1(음각 홈 내부 Pip)
                    if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 1)
                    {
                        renderer.sharedMaterials = new Material[] { diceBodyMaterial, dicePipMaterial };
                    }
                    else
                    {
                        renderer.sharedMaterial = diceBodyMaterial;
                    }
                    renderer.shadowCastingMode = ShadowCastingMode.Off; // 시각 메시는 렌더 전용, 그림자는 프록시가 담당
                }

                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void DisableImportedSceneComponents(GameObject visual)
        {
            foreach (Camera cam in visual.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
            foreach (Light l in visual.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (AudioListener al in visual.GetComponentsInChildren<AudioListener>(true)) al.enabled = false;
            foreach (Collider c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }

        public void ArrangeInitialPositions(System.Collections.Generic.IReadOnlyList<GameObject> activeDice, System.Collections.Generic.IReadOnlyList<int> diceValues)
        {
            for (int i = 0; i < activeDice.Count; i++)
            {
                if (activeDice[i] == null) continue;
                Vector3 targetPos = DiceBoardMetrics.GetActivePosition(i, activeDice.Count);
                activeDice[i].transform.localPosition = targetPos;
                activeDice[i].transform.localScale = Vector3.one * DiceBoardMetrics.ActiveDieSize;
                // 면 값이 1~6이 아닌 종류가 있어 값을 면 인덱스로 옮겨 넘긴다(M7-T5).
                DieType dieType = DiceFaceValues.TypeOf(activeDice[i].transform);
                int faceIndex = DiceFaceValues.FaceIndexOf(dieType, diceValues[i]);

                if (dieType == DieType.Octahedron)
                {
                    activeDice[i].transform.localRotation = DiceFaceOrientation.GetOctaCameraFacingRotation(faceIndex, 75.0f);
                    Transform octaVisual = activeDice[i].transform.Find("Visual");
                    if (octaVisual != null) octaVisual.localRotation = Quaternion.identity;
                    continue;
                }

                faceIndex = Mathf.Clamp(faceIndex, 1, 6);
                Quaternion targetRot = DiceFaceOrientation.GetCameraFacingRotation(faceIndex, 75.0f);
                activeDice[i].transform.localRotation = targetRot;

                Transform visual = activeDice[i].transform.Find("Visual");
                if (visual != null)
                {
                    visual.localRotation = DiceFaceOrientation.MeasureModelBasis(visual);
                }
                else
                {
                    DiceMaterialFactory.ApplyPredictedTopValue(activeDice[i].transform, targetRot, faceIndex);
                }
            }
        }
        private static void EnsureShadowProxy(Transform visual)
        {
            Transform existing = visual.Find("ShadowProxy");
            if (existing != null) return;

            GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proxy.name = "ShadowProxy";
            proxy.transform.SetParent(visual, false);
            proxy.transform.localPosition = Vector3.zero;
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one * DiceBoardMetrics.SourceDiceSize;

            Collider col = proxy.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            MeshRenderer mr = proxy.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            mr.receiveShadows = false;
        }

        /// <summary>주사위 재질을 해제한다. 컨트롤러의 OnDestroy에서 부른다.</summary>
        public void Dispose()
        {
            // 재질은 종류별로 DicePaletteCatalog가 캐시한다. 개별 주사위가 소유하지 않으므로
            // 여기서는 카탈로그 캐시만 비운다.
            DicePaletteCatalog.ClearCache();
        }
        /// <summary>
        /// 킵된 주사위와 활성 주사위를 각자 자리로 부드럽게 옮긴다(M10-T6a).
        ///
        /// 진행 단계 표시와 상태 문구 갱신은 호출한 쪽이 맡는다. 이 코루틴은 화면만 다룬다.
        /// </summary>
        public IEnumerator AnimateLayout(
            float duration,
            IReadOnlyList<GameObject> activeDice,
            IReadOnlyList<bool> keptDice,
            IReadOnlyList<int> keptSlotIndices,
            IReadOnlyList<int> diceValues,
            BakedDiceController bakedDiceController)
        {

            var diceTransforms = new Transform[activeDice.Count];
            var targetPositions = new Vector3[activeDice.Count];
            var targetRotations = new Quaternion[activeDice.Count];
            var targetScales = new Vector3[activeDice.Count];

            var unkeptIndices = new List<int>();

            // 1. 킵된 주사위와 활성(킵되지 않은) 주사위 분류 및 카메라 정면 틸트 정렬 목표 회전 계산
            for (int i = 0; i < activeDice.Count; i++)
            {
                diceTransforms[i] = activeDice[i] != null ? activeDice[i].transform : null;
                float normalScale = DiceBoardMetrics.DieSize;

                // 현재 주사위 루트의 착지 회전으로부터 윗면(Top)을 유지한 채 카메라 렌즈를 정면으로 바라보도록 회전 계산
                Quaternion currentRot = activeDice[i] != null ? activeDice[i].transform.localRotation : Quaternion.identity;
                Quaternion cameraFacingRot = DiceFaceValues.TypeOf(diceTransforms[i]) == DieType.Octahedron
                    ? DiceFaceOrientation.GetOctaCameraFacingRotation(DiceFaceOrientation.GetOctaTopFace(currentRot), 75.0f)
                    : DiceFaceOrientation.GetCameraFacingUprightRotation(currentRot, 75.0f);

                if (keptDice[i])
                {
                    int slot = (keptSlotIndices.Count > i && keptSlotIndices[i] >= 0) ? keptSlotIndices[i] : 0;
                    targetPositions[i] = DiceBoardMetrics.GetKeepPosition(slot);
                    targetScales[i] = Vector3.one * (normalScale * DiceBoardMetrics.KeepDieScale);
                    targetRotations[i] = cameraFacingRot;
                }
                else
                {
                    unkeptIndices.Add(i);
                    targetRotations[i] = cameraFacingRot;
                }
            }

            // 2. 킵되지 않은 활성 주사위들을 왼쪽부터 오른쪽으로 작은 눈 -> 큰 눈 오름차순 정렬
            unkeptIndices.Sort((a, b) =>
            {
                int cmp = diceValues[a].CompareTo(diceValues[b]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (int slot = 0; slot < unkeptIndices.Count; slot++)
            {
                int dieIndex = unkeptIndices[slot];
                targetPositions[dieIndex] = DiceBoardMetrics.GetActivePosition(slot, unkeptIndices.Count);
                targetScales[dieIndex] = Vector3.one * DiceBoardMetrics.ActiveDieSize;
            }

            // 3. 머티리얼 방식 렌더러 fallback
            for (int i = 0; i < activeDice.Count; i++)
            {
                if (activeDice[i] == null) continue;
                Transform visual = activeDice[i].transform.Find("Visual");
                if (visual == null)
                {
                    DieType fallbackType = DiceFaceValues.TypeOf(activeDice[i].transform);
                    int fallbackFace = Mathf.Clamp(DiceFaceValues.FaceIndexOf(fallbackType, diceValues[i]), 1, 6);
                    DiceMaterialFactory.ApplyPredictedTopValue(activeDice[i].transform, targetRotations[i], fallbackFace);
                }
            }

            // 4. 부드러운 위치/회전/스케일 보간 애니메이션 수행 (순수 Yaw 수평 슬라이딩)
            yield return bakedDiceController.AnimateKeptDice(
                diceTransforms,
                keptDice,
                diceValues,
                targetPositions,
                targetRotations,
                targetScales,
                duration);

        }
    }
}
