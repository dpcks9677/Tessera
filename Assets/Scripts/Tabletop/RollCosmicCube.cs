using System;
using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 주사위 트레이 하단 우측에 배치되는 3D 스타일라이즈드 코스믹 큐브(Cosmic Cube) 롤 오브젝트
    /// - 외부 라이트 편차를 제거하여 3개 보이는 면 모두 100% 균등하고 깊은 비비드 아쿠아 성운 조도 유지
    /// - 씬 재생 전과 후의 큐브 각도가 100% 동일하게 동기화 (에디터 & 런타임 완전 일치)
    /// - 픽셀 필터에 최적화된 굵직한 매크로 성운(Chunky Macro Nebula) & 고대비 가스 마블링
    /// - 호버링 시 면 중심 발광을 배제하고 카메라 시선 기준 육각형 외곽 테두리(Hexagon Silhouette Edges) 집중 발광
    /// - 기존 수정구의 고풍스러운 받침대(사각 대리석 기둥 & 상단 골드 칼라 브래킷) 완벽 복원
    /// - 받침대와 약 0.32f의 충분한 공중 이격(BaseCenterY = 2.22f)을 확보하여 부유 및 회전 시에도 절대 겹치지 않음
    /// - 큐브 하단 꼭짓점을 축으로 유유히 팽이처럼 천천히 회전하는 기본 Idle 자전 애니메이션 (초당 8도)
    /// - 주사위 롤 클릭 시 기본 자전에 더해 0.7바퀴(252도) 빠르게 회전하는 감속 스핀 애니메이션
    /// - 6개 면(Face)마다 밀착 배치된 은하 별가루(Cosmic Stardust) 파티클 시스템
    /// </summary>
    [ExecuteAlways]
    public sealed class RollCosmicCube : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("Hover & Glow State")]
        [SerializeField] private bool isHovered;
        [SerializeField] private bool isInteractable = true;
        private float hoverLerp;
        private float clickFlashLerp;
        private Coroutine clickFeedbackRoutine;

        [Header("Rotation & Spin State")]
        [SerializeField] private float idleRotationSpeed = 8.0f; // 초당 8도 기본 자전
        private float idleSpinAngle;
        private float rollSpinAngleOffset;
        private Coroutine rollSpinRoutine;

        [Header("Zodiac & Cosmic State")]
        [SerializeField, Range(0, 11)] private int currentZodiacIndex = 0;
        private Material constellationMaterial;
        private MeshRenderer constellationRenderer;
        private Coroutine zodiacTransitionRoutine;

        public bool IsHovered => isHovered;
        public bool IsInteractable => isInteractable;
        public int CurrentZodiacIndex => currentZodiacIndex;
        public string CurrentZodiacName => ZodiacConstellationData.GetDefinition(currentZodiacIndex).nameKr;
        public string CurrentZodiacNameEn => ZodiacConstellationData.GetDefinition(currentZodiacIndex).nameEn;
        public event Action OnClicked;

        // 렌더러 및 트랜스폼 레퍼런스
        private Transform floatingCubeRoot;
        private Transform cubeBodyTransform;
        private Material cosmicCubeMaterial;
        private Material crystalFrontMaterial;
        private Material crystalInnerMaterialA;
        private Material crystalInnerMaterialB;
        private Material energyCoreMaterial;
        private Material hoverOutlineMaterial;
        private Material outerHaloMaterial;
        private Transform tesseractRoot;
        private Mesh tesseractMesh;
        private Material tesseractMaterial;
        private Vector3[] tesseractVertices;
        private Color[] tesseractColors;
        private Vector2[] tesseractUvs;
        private int[] tesseractTriangles;
        private readonly Vector3[] tesseractOuterCorners = new Vector3[8];
        private readonly Vector3[] tesseractInnerCorners = new Vector3[8];
        private readonly List<ParticleSystem> faceParticleSystems = new();

        private static readonly int[] TesseractInnerEdges =
        {
            0, 1, 0, 2, 0, 4,
            1, 3, 1, 5,
            2, 3, 2, 6,
            3, 7,
            4, 5, 4, 6,
            5, 7,
            6, 7
        };

        // 받침대 상단 브래킷(Y=0.88)과 부유 큐브 하단 꼭짓점(Y=1.20) 사이의 넉넉한 공중 이격(0.32f) 확보
        private const float BaseCenterY = 2.22f;
        private const float BobbingAmplitude = 0.045f;
        private const float BobbingSpeed = 1.6f;

        // 75도 직교 카메라 화면 평면(Screen Plane) 수직 직교 정렬 쿼터니언 (재생 전/후 100% 일치)
        private static readonly Quaternion CameraScreenTilt = Quaternion.Euler(15.0f, 0f, 0f);
        private static readonly Quaternion SymmetricalAlignment = Quaternion.Euler(0f, 45.0f, 0f);

        // 결정 외곽은 가까운 시안과 넓은 코발트 후광을 겹쳐 네온 깊이를 만든다.
        private readonly Color nearHaloColor = new(0.02f, 1.80f, 3.60f, 0.44f);
        private readonly Color outerHaloColor = new(0.00f, 0.45f, 2.40f, 0.20f);

        private void Awake()
        {
            EnsureGeometry();
        }

        private void OnEnable()
        {
            EnsureGeometry();
        }

        public void RebuildGeometry()
        {
            ZodiacConstellationData.ClearCache();
            while (transform.childCount > 0)
            {
                GameObject child = transform.GetChild(0).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
            BuildGeometry();
        }

        public void EnsureGeometry()
        {
            if (transform.childCount == 0)
            {
                BuildGeometry();
                return;
            }

            CacheGeometryReferences();
        }

        private void CacheGeometryReferences()
        {
            floatingCubeRoot = transform.Find("Cosmic_Cube_Floating_Root");
            cubeBodyTransform = floatingCubeRoot != null
                ? floatingCubeRoot.Find("Cosmic_Cube_Body")
                : null;

            MeshRenderer cubeRenderer = cubeBodyTransform != null
                ? cubeBodyTransform.GetComponent<MeshRenderer>()
                : null;
            if (cubeRenderer != null)
            {
                cosmicCubeMaterial = RuntimeAssetGuard.GetWritableMaterial(cubeRenderer);

                Shader volumeShader = Shader.Find("DicePoC/CosmicVolume");
                if (volumeShader != null &&
                    (cosmicCubeMaterial == null || cosmicCubeMaterial.shader != volumeShader))
                {
                    cosmicCubeMaterial = CreateCosmicVolumeMaterial(volumeShader);
                    if (Application.isPlaying) cubeRenderer.material = cosmicCubeMaterial;
                    else cubeRenderer.sharedMaterial = cosmicCubeMaterial;
                }

                ConfigureCosmicVolumeMaterial(cosmicCubeMaterial);
            }

            CacheOrCreateCrystalLayers();
            CacheOrCreateHaloLayers();
            CacheOrCreateTesseract();

            Transform constellationTransform = floatingCubeRoot != null
                ? floatingCubeRoot.Find("Cosmic_Constellation_Plane")
                : null;
            constellationRenderer = constellationTransform != null
                ? constellationTransform.GetComponent<MeshRenderer>()
                : null;
            constellationMaterial = RuntimeAssetGuard.GetWritableMaterial(constellationRenderer);

            // 별자리 연출은 폐기 상태다. 프리팹의 평면은 재사용 가능성 때문에 남겨 두되,
            // 게임에서는 렌더러를 끄고 머티리얼 참조를 버려 텍스처가 구워지지 않게 한다.
            // 렌더러를 재생 중에만 끄는 이유는, 편집 모드에서 끄면 씬·프리팹이 더러워지기 때문이다.
            if (!ZodiacConstellationData.EnabledInGame)
            {
                if (Application.isPlaying && constellationRenderer != null) constellationRenderer.enabled = false;
                constellationMaterial = null;
            }

            UpgradeInternalParticles();
            faceParticleSystems.Clear();
            if (cubeBodyTransform != null)
            {
                ParticleSystem[] particles = cubeBodyTransform.GetComponentsInChildren<ParticleSystem>(true);
                faceParticleSystems.AddRange(particles);
            }
        }

        private void UpgradeInternalParticles()
        {
            if (cubeBodyTransform == null || cubeBodyTransform.Find("Cosmic_Internal_Stars") != null) return;

            for (int index = cubeBodyTransform.childCount - 1; index >= 0; index--)
            {
                Transform child = cubeBodyTransform.GetChild(index);
                if (!child.name.StartsWith("Cosmic_Face_Particles_", StringComparison.Ordinal)) continue;

                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            CreateInternalParticles(cubeBodyTransform);
        }

        private void CacheOrCreateCrystalLayers()
        {
            if (cubeBodyTransform == null) return;

            Transform frontShell = cubeBodyTransform.Find("Crystal_Outer_Shell");
            Transform innerLayerA = cubeBodyTransform.Find("Crystal_Inner_Layer_A");
            Transform legacyBackFacets = cubeBodyTransform.Find("Crystal_Back_Facets");
            if (innerLayerA == null && legacyBackFacets != null)
            {
                legacyBackFacets.name = "Crystal_Inner_Layer_A";
                innerLayerA = legacyBackFacets;
            }
            Transform innerLayerB = cubeBodyTransform.Find("Crystal_Inner_Layer_B");
            Transform energyCore = cubeBodyTransform.Find("Energy_Core");

            if (frontShell == null || innerLayerA == null || innerLayerB == null || energyCore == null)
            {
                CreateCrystalLayers(cubeBodyTransform);
                frontShell = cubeBodyTransform.Find("Crystal_Outer_Shell");
                innerLayerA = cubeBodyTransform.Find("Crystal_Inner_Layer_A");
                innerLayerB = cubeBodyTransform.Find("Crystal_Inner_Layer_B");
                energyCore = cubeBodyTransform.Find("Energy_Core");
            }

            ConfigureCrystalLayerTransforms(frontShell, innerLayerA, innerLayerB);
            crystalFrontMaterial = GetRendererMaterial(frontShell);
            crystalInnerMaterialA = GetRendererMaterial(innerLayerA);
            crystalInnerMaterialB = GetRendererMaterial(innerLayerB);
            energyCoreMaterial = GetRendererMaterial(energyCore);
            ConfigureCrystalShellMaterial(crystalFrontMaterial, 0);
            ConfigureCrystalShellMaterial(crystalInnerMaterialA, 1);
            ConfigureCrystalShellMaterial(crystalInnerMaterialB, 2);
            ConfigureEnergyCoreMaterial(energyCoreMaterial);
        }

        private void CacheOrCreateHaloLayers()
        {
            if (cubeBodyTransform == null) return;

            hoverOutlineMaterial = CacheOrCreateHaloLayer("Cosmic_Cube_Hover_Outline", false);
            outerHaloMaterial = CacheOrCreateHaloLayer("Cosmic_Cube_Outer_Halo", true);
        }

        private Material CacheOrCreateHaloLayer(string objectName, bool outer)
        {
            Transform haloTransform = cubeBodyTransform.Find(objectName);
            if (haloTransform == null)
            {
                return CreateHaloLayer(cubeBodyTransform, objectName, outer);
            }

            Material material = GetRendererMaterial(haloTransform);
            ConfigureHaloMaterial(material, outer);
            return material;
        }

        private static Material GetRendererMaterial(Transform target)
        {
            if (target == null) return null;
            return RuntimeAssetGuard.GetWritableMaterial(target.GetComponent<MeshRenderer>());
        }

        public static RollCosmicCube Create(Transform parent, Vector3 worldPosition, Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("3D Roll Cosmic Cube");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition;
            root.transform.rotation = rotation ?? Quaternion.identity;
            root.transform.localScale = scale ?? Vector3.one;

            RollCosmicCube comp = root.AddComponent<RollCosmicCube>();
            // AddComponent 직후 Awake에서 이미 생성되므로 중복 재구성하지 않는다.
            comp.EnsureGeometry();
            return comp;
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        public void TriggerClickFeedback()
        {
            OnClicked?.Invoke();

            if (!gameObject.activeInHierarchy) return;

            // 1. 발광 플래시 및 6개 면 파티클 버스트
            if (clickFeedbackRoutine != null)
            {
                StopCoroutine(clickFeedbackRoutine);
            }
            clickFeedbackRoutine = StartCoroutine(ClickFeedbackAnimationRoutine());

            // 2. 0.7바퀴(252도) 고속 스핀 애니메이션
            if (rollSpinRoutine != null)
            {
                StopCoroutine(rollSpinRoutine);
            }
            rollSpinRoutine = StartCoroutine(RollSpinAnimationRoutine());
        }

        private System.Collections.IEnumerator ClickFeedbackAnimationRoutine()
        {
            clickFlashLerp = 1f;

            for (int i = 0; i < faceParticleSystems.Count; i++)
            {
                if (faceParticleSystems[i] != null)
                {
                    faceParticleSystems[i].Emit(8);
                }
            }

            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                clickFlashLerp = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }

            clickFlashLerp = 0f;
            clickFeedbackRoutine = null;
        }

        private System.Collections.IEnumerator RollSpinAnimationRoutine()
        {
            const float targetSpinAngle = 0.7f * 360.0f; // 252.0도
            const float spinDuration = 0.55f;
            float elapsed = 0f;

            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);
                float easeOut = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
                rollSpinAngleOffset = easeOut * targetSpinAngle;
                yield return null;
            }

            idleSpinAngle = (idleSpinAngle + targetSpinAngle) % 360.0f;
            rollSpinAngleOffset = 0f;
            rollSpinRoutine = null;
        }

        public void AdvanceZodiac()
        {
            int nextIndex = (currentZodiacIndex + 1) % 12;
            SetZodiac(nextIndex);
        }

        public void SetZodiac(int index, bool immediate = false)
        {
            // 폐기된 연출이므로 텍스처 베이킹까지 이어지는 경로를 여기서 끊는다.
            if (!ZodiacConstellationData.EnabledInGame) return;

            int targetIndex = Mathf.Clamp(index, 0, 11);
            if (targetIndex == currentZodiacIndex && !immediate) return;

            int prevIndex = currentZodiacIndex;
            currentZodiacIndex = targetIndex;

            if (immediate || !gameObject.activeInHierarchy || !Application.isPlaying)
            {
                if (zodiacTransitionRoutine != null)
                {
                    StopCoroutine(zodiacTransitionRoutine);
                    zodiacTransitionRoutine = null;
                }
                if (constellationMaterial != null)
                {
                    Texture2D targetTex = ZodiacConstellationData.GetZodiacTexture(currentZodiacIndex);
                    if (constellationMaterial.HasProperty("_CurrentTex")) constellationMaterial.SetTexture("_CurrentTex", targetTex);
                    if (constellationMaterial.HasProperty("_NextTex")) constellationMaterial.SetTexture("_NextTex", targetTex);
                    if (constellationMaterial.HasProperty("_Transition")) constellationMaterial.SetFloat("_Transition", 0.0f);
                }
                return;
            }

            if (zodiacTransitionRoutine != null)
            {
                StopCoroutine(zodiacTransitionRoutine);
            }
            zodiacTransitionRoutine = StartCoroutine(ZodiacTransitionAnimationRoutine(prevIndex, targetIndex));
        }

        private System.Collections.IEnumerator ZodiacTransitionAnimationRoutine(int fromIdx, int toIdx, float duration = 0.85f)
        {
            Texture2D fromTex = ZodiacConstellationData.GetZodiacTexture(fromIdx);
            Texture2D toTex = ZodiacConstellationData.GetZodiacTexture(toIdx);

            if (constellationMaterial != null)
            {
                if (constellationMaterial.HasProperty("_CurrentTex")) constellationMaterial.SetTexture("_CurrentTex", fromTex);
                if (constellationMaterial.HasProperty("_NextTex")) constellationMaterial.SetTexture("_NextTex", toTex);
                if (constellationMaterial.HasProperty("_Transition")) constellationMaterial.SetFloat("_Transition", 0.0f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (constellationMaterial != null && constellationMaterial.HasProperty("_Transition"))
                {
                    constellationMaterial.SetFloat("_Transition", smoothT);
                }
                yield return null;
            }

            if (constellationMaterial != null)
            {
                if (constellationMaterial.HasProperty("_CurrentTex")) constellationMaterial.SetTexture("_CurrentTex", toTex);
                if (constellationMaterial.HasProperty("_Transition")) constellationMaterial.SetFloat("_Transition", 0.0f);
            }

            zodiacTransitionRoutine = null;
        }

        private void OnMouseEnter()
        {
            isHovered = true;
        }

        private void OnMouseExit()
        {
            isHovered = false;
        }

        // 매 프레임 도는 경로라 셰이더 프로퍼티 이름을 미리 ID로 바꿔 둔다.
        // 1회성 생성 경로의 문자열 접근은 그대로 둔다. 거기서는 조회 비용이 의미가 없다.
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int StarIntensityId = Shader.PropertyToID("_StarIntensity");
        private static readonly int CoreIntensityId = Shader.PropertyToID("_CoreIntensity");
        private static readonly int TwinkleSpeedId = Shader.PropertyToID("_TwinkleSpeed");
        private static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");
        private static readonly int ThicknessIntensityId = Shader.PropertyToID("_ThicknessIntensity");
        private static readonly int RefractionStrengthId = Shader.PropertyToID("_RefractionStrength");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int OutlineIntensityId = Shader.PropertyToID("_OutlineIntensity");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");

        private void Update()
        {
            // 편집 모드에서는 연출을 돌리지 않는다. 이 애니메이션은 트랜스폼과 컴포넌트 값 같은
            // 직렬화 대상에 매 틱 쓰기 때문에, 편집 모드에서 돌리면 씬이 계속 더러운 상태가 된다.
            // 그러면 씬을 저장할 때마다 관련 없는 오버라이드가 diff에 섞이고 테스트 실행이
            // "dirty scene"으로 막힌다. [ExecuteAlways]는 BuildGeometry 컨텍스트 메뉴와
            // OnValidate 미리보기 때문에 그대로 둔다.
            if (!Application.isPlaying) return;

            float dt = Time.deltaTime;
            float time = Time.time;

            // 1. 공중 부유(Bobbing) 애니메이션 및 기본 X=-25도 회전 유지 (BaseCenterY = 2.22f)
            if (floatingCubeRoot != null)
            {
                float bobbingOffset = Mathf.Sin(time * BobbingSpeed) * BobbingAmplitude;
                floatingCubeRoot.localPosition = new Vector3(0f, BaseCenterY + bobbingOffset, 0f);
                floatingCubeRoot.localRotation = Quaternion.Euler(-25.0f, 0f, 0f);
            }

            // 2. 75도 직교 카메라 화면 평면 수직 직교 정렬 및 자전 연산
            idleSpinAngle = (idleSpinAngle + dt * idleRotationSpeed) % 360.0f;
            float totalSpinAngle = idleSpinAngle + rollSpinAngleOffset;

            if (cubeBodyTransform != null)
            {
                Vector3 diagonal = new Vector3(1f, 1f, 1f).normalized;
                Quaternion cornerDownRot = Quaternion.FromToRotation(diagonal, Vector3.down);

                // 카메라 화면 수직축 기준 자전 회전 결합
                Quaternion spinRot = Quaternion.AngleAxis(totalSpinAngle, Vector3.up);
                cubeBodyTransform.localRotation = CameraScreenTilt * spinRot * SymmetricalAlignment * cornerDownRot;
            }

            UpdateTesseractGeometry(time);

            // 3. 호버링 보간
            float target = (isHovered && isInteractable) ? 1f : 0f;
            hoverLerp = Mathf.MoveTowards(hoverLerp, target, dt * 5f);

            // 4. 내부 체적은 깊은 인디고와 HDR 시안의 대비를 유지하며 상태에 따라 점화된다.
            if (cosmicCubeMaterial != null)
            {
                float baseBrightness = isInteractable ? 1.18f : 0.58f;
                float brightness = baseBrightness + (hoverLerp * 0.10f) + (clickFlashLerp * 0.32f);
                cosmicCubeMaterial.SetFloat(BrightnessId, brightness);
                float starIntensity = 2.65f + (hoverLerp * 0.35f) + (clickFlashLerp * 1.35f);
                cosmicCubeMaterial.SetFloat(StarIntensityId, starIntensity);
                float coreIntensity = 2.45f + (hoverLerp * 0.38f) + (clickFlashLerp * 0.75f);
                cosmicCubeMaterial.SetFloat(CoreIntensityId, coreIntensity);
                cosmicCubeMaterial.SetFloat(TwinkleSpeedId, Mathf.Lerp(2.20f, 3.15f, hoverLerp));
            }

            // 투명 외피는 기본 상태에서도 네온 모서리를 유지한다.
            float shellEdgeIntensity = 2.35f + (hoverLerp * 0.55f) + (clickFlashLerp * 0.90f);
            if (crystalFrontMaterial != null)
            {
                crystalFrontMaterial.SetFloat(EdgeIntensityId, shellEdgeIntensity);
                crystalFrontMaterial.SetFloat(ThicknessIntensityId, 0.70f + hoverLerp * 0.12f);
                crystalFrontMaterial.SetFloat(RefractionStrengthId, 0.18f + hoverLerp * 0.06f);
            }
            if (crystalInnerMaterialA != null)
            {
                crystalInnerMaterialA.SetFloat(EdgeIntensityId, 0.10f + hoverLerp * 0.03f);
                crystalInnerMaterialA.SetFloat(ThicknessIntensityId, 0.56f + hoverLerp * 0.07f);
                crystalInnerMaterialA.SetFloat(RefractionStrengthId, 0.22f + hoverLerp * 0.04f);
            }
            if (crystalInnerMaterialB != null)
            {
                crystalInnerMaterialB.SetFloat(EdgeIntensityId, 0.04f + hoverLerp * 0.02f);
                crystalInnerMaterialB.SetFloat(ThicknessIntensityId, 0.42f + hoverLerp * 0.05f);
                crystalInnerMaterialB.SetFloat(RefractionStrengthId, 0.17f + hoverLerp * 0.03f);
            }

            // 중앙 에너지 코어는 레퍼런스처럼 호버 시 은은하게 점화된다.
            if (energyCoreMaterial != null)
            {
                float coreGlow = 2.70f + (hoverLerp * 0.65f) + (clickFlashLerp * 1.15f);
                energyCoreMaterial.SetFloat(CoreIntensityId, coreGlow);
                energyCoreMaterial.SetFloat(PulseAmountId, Mathf.Lerp(0.08f, 0.14f, hoverLerp));
            }

            // 두 겹의 확장 렌더러가 기본 발광과 호버 시 넓어지는 육각 후광을 만든다.
            if (hoverOutlineMaterial != null)
            {
                float enabled = isInteractable ? 1.0f : 0.35f;
                hoverOutlineMaterial.SetFloat(OutlineIntensityId,
                    0.42f * enabled + hoverLerp * 0.70f + clickFlashLerp * 0.60f);
            }
            if (outerHaloMaterial != null)
            {
                float enabled = isInteractable ? 1.0f : 0.30f;
                outerHaloMaterial.SetFloat(OutlineIntensityId,
                    0.20f * enabled + hoverLerp * 0.38f + clickFlashLerp * 0.30f);
            }

            if (tesseractMaterial != null)
            {
                float enabled = isInteractable ? 1.0f : 0.38f;
                tesseractMaterial.SetFloat(IntensityId,
                    0.90f * enabled + hoverLerp * 0.48f + clickFlashLerp * 0.88f);
                tesseractMaterial.SetFloat(OpacityId, Mathf.Lerp(0.62f, 0.78f, hoverLerp) * enabled);
                tesseractMaterial.SetFloat(FlowSpeedId, Mathf.Lerp(1.15f, 1.90f, hoverLerp));
            }

            // 5. 내부 별자리 심볼 갱신
            if (constellationMaterial != null)
            {
                float baseIntensity = isInteractable ? 0.85f : 0.50f;
                float constIntensity = baseIntensity + (clickFlashLerp * 1.0f);
                constellationMaterial.SetFloat(IntensityId, constIntensity);
            }
        }

        public void BuildGeometry()
        {
            faceParticleSystems.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader stoneShader = Shader.Find("Universal Render Pipeline/Unlit") ?? litShader;

            // 1. 머티리얼 구성
            Material stoneBaseMat = CreateMat(stoneShader, "Cosmic_StoneBaseMat", new Color(0.48f, 0.52f, 0.58f), 0.05f, 0.32f);
            Material stoneRimMat = CreateMat(stoneShader, "Cosmic_StoneRimMat", new Color(0.30f, 0.34f, 0.40f), 0.04f, 0.28f);
            Material marblePillarMat = CreateMat(stoneShader, "Cosmic_MarblePillarMat", new Color(0.55f, 0.58f, 0.64f), 0.05f, 0.40f);
            Material goldTrimMat = CreateMat(litShader, "Cosmic_GoldTrimMat", new Color(0.86f, 0.68f, 0.28f), 0.88f, 0.68f);
            Material goldDarkMat = CreateMat(litShader, "Cosmic_GoldDarkMat", new Color(0.58f, 0.42f, 0.18f), 0.85f, 0.60f);

            // 1-2. 모든 면을 통과해 같은 내부 공간을 보여 주는 연속 체적 머티리얼
            Shader cubeShader = Shader.Find("DicePoC/CosmicVolume")
                ?? Shader.Find("DicePoC/CosmicCube")
                ?? litShader;
            cosmicCubeMaterial = CreateCosmicVolumeMaterial(cubeShader);

            // 1-3. 내부 별자리 머티리얼
            Shader constellationShader = Shader.Find("DicePoC/OrbConstellation") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? litShader;
            constellationMaterial = new Material(constellationShader) { name = "Cosmic_Constellation_Mat" };
            Texture2D curZodiacTex = ZodiacConstellationData.EnabledInGame
                ? ZodiacConstellationData.GetZodiacTexture(currentZodiacIndex)
                : null;
            if (constellationMaterial.HasProperty("_ConstellationColor")) constellationMaterial.SetColor("_ConstellationColor", new Color(0.92f, 0.96f, 1.00f, 0.55f));
            if (constellationMaterial.HasProperty("_CurrentTex")) constellationMaterial.SetTexture("_CurrentTex", curZodiacTex);
            if (constellationMaterial.HasProperty("_NextTex")) constellationMaterial.SetTexture("_NextTex", curZodiacTex);
            if (constellationMaterial.HasProperty("_Transition")) constellationMaterial.SetFloat("_Transition", 0.0f);
            if (constellationMaterial.HasProperty("_Intensity")) constellationMaterial.SetFloat("_Intensity", 0.85f);
            if (constellationMaterial.HasProperty("_TwinkleSpeed")) constellationMaterial.SetFloat("_TwinkleSpeed", 2.0f);
            if (constellationMaterial.HasProperty("_TwinkleAmount")) constellationMaterial.SetFloat("_TwinkleAmount", 0.50f);

            // 2. 계단식 원형 스톤 받침대 (Tiered Stepped Base)
            GameObject baseRoot = new("Base_Platform");
            baseRoot.layer = DecorationLayer;
            baseRoot.transform.SetParent(transform, false);

            GameObject lowerBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lowerBase.name = "LowerBase_Stone";
            SetupPart(lowerBase, baseRoot.transform, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(2.55f, 0.04f, 2.55f), stoneRimMat);

            GameObject upperBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperBase.name = "UpperBase_Stone";
            SetupPart(upperBase, baseRoot.transform, new Vector3(0f, 0.10f, 0f), Vector3.zero, new Vector3(2.25f, 0.035f, 2.25f), stoneBaseMat);

            GameObject baseGoldRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGoldRing.name = "Base_GoldRing";
            SetupPart(baseGoldRing, baseRoot.transform, new Vector3(0f, 0.135f, 0f), Vector3.zero, new Vector3(1.95f, 0.018f, 1.95f), goldTrimMat);

            int studCount = 8;
            for (int i = 0; i < studCount; i++)
            {
                float angle = i * (360f / studCount);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 studPos = new(Mathf.Sin(rad) * 1.05f, 0.135f, Mathf.Cos(rad) * 1.05f);
                GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stud.name = $"Base_Stud_{i}";
                SetupPart(stud, baseRoot.transform, studPos, Vector3.zero, new Vector3(0.14f, 0.08f, 0.14f), goldTrimMat);
            }

            // 3. 사각 대리석/스톤 기둥 (Square Marble Pedestal)
            GameObject pillarRoot = new("Pillar_Pedestal");
            pillarRoot.layer = DecorationLayer;
            pillarRoot.transform.SetParent(transform, false);

            GameObject pillarFoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarFoot.name = "Pillar_Foot";
            SetupPart(pillarFoot, pillarRoot.transform, new Vector3(0f, 0.20f, 0f), Vector3.zero, new Vector3(1.48f, 0.08f, 1.48f), goldDarkMat);

            GameObject pillarBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarBody.name = "Pillar_MarbleBody";
            SetupPart(pillarBody, pillarRoot.transform, new Vector3(0f, 0.44f, 0f), Vector3.zero, new Vector3(1.30f, 0.40f, 1.30f), marblePillarMat);

            GameObject pillarCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarCap.name = "Pillar_Cap";
            SetupPart(pillarCap, pillarRoot.transform, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(1.52f, 0.08f, 1.52f), goldTrimMat);

            // 4. 상단 원형 골드 링 받침대 (Top Golden Collar Bracket)
            GameObject collarRoot = new("Collar_Bracket");
            collarRoot.layer = DecorationLayer;
            collarRoot.transform.SetParent(transform, false);

            GameObject collarRing1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collarRing1.name = "Collar_Ring_Lower";
            SetupPart(collarRing1, collarRoot.transform, new Vector3(0f, 0.75f, 0f), Vector3.zero, new Vector3(1.65f, 0.045f, 1.65f), goldDarkMat);

            GameObject collarRing2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collarRing2.name = "Collar_Ring_Upper";
            SetupPart(collarRing2, collarRoot.transform, new Vector3(0f, 0.83f, 0f), Vector3.zero, new Vector3(1.45f, 0.055f, 1.45f), goldTrimMat);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 studPos = new(Mathf.Sin(rad) * 0.65f, 0.88f, Mathf.Cos(rad) * 0.65f);
                GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stud.name = $"Collar_Stud_{i}";
                SetupPart(stud, collarRoot.transform, studPos, Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), goldTrimMat);
            }

            // 5. 공중 부유 코스믹 큐브 루트 (BaseCenterY = 2.22f, 기본 X=-25도 설정)
            GameObject floatRoot = new("Cosmic_Cube_Floating_Root");
            floatRoot.layer = DecorationLayer;
            floatRoot.transform.SetParent(transform, false);
            floatRoot.transform.localPosition = new Vector3(0f, BaseCenterY, 0f);
            floatRoot.transform.localRotation = Quaternion.Euler(-25.0f, 0f, 0f);
            floatingCubeRoot = floatRoot.transform;

            // 5-1. 메인 코스믹 큐브 본체 (에디터/런타임 100% 일치 정밀 수직 직교 정렬)
            Vector3 diagonal = new Vector3(1f, 1f, 1f).normalized;
            Quaternion cornerDownRot = Quaternion.FromToRotation(diagonal, Vector3.down);
            Quaternion initialRot = CameraScreenTilt * SymmetricalAlignment * cornerDownRot;

            GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = "Cosmic_Cube_Body";
            cubeObj.layer = DecorationLayer;
            cubeObj.transform.SetParent(floatingCubeRoot, false);
            cubeObj.transform.localPosition = Vector3.zero;
            cubeBodyTransform = cubeObj.transform;
            cubeBodyTransform.localRotation = initialRot;
            cubeBodyTransform.localScale = Vector3.one * 1.35f;

            Collider cubeCol = cubeObj.GetComponent<Collider>();
            if (cubeCol != null)
            {
                if (Application.isPlaying) Destroy(cubeCol);
                else DestroyImmediate(cubeCol);
            }

            MeshRenderer cubeMr = cubeObj.GetComponent<MeshRenderer>();
            if (cubeMr != null)
            {
                if (Application.isPlaying) cubeMr.material = cosmicCubeMaterial;
                else cubeMr.sharedMaterial = cosmicCubeMaterial;
                cubeMr.shadowCastingMode = ShadowCastingMode.TwoSided;
                cubeMr.receiveShadows = true;
            }

            CreateCrystalLayers(cubeBodyTransform);
            CreateHoverOutline(cubeBodyTransform);
            CreateTesseract(cubeBodyTransform);

            // 5-2. 표면이 아닌 내부 공간에 부유하는 별가루 파티클 시스템
            CreateInternalParticles(cubeBodyTransform);

            // 5-3. 내부 별자리 심볼 평면 (75도 직교 카메라 정면)
            GameObject constellationPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            constellationPlane.name = "Cosmic_Constellation_Plane";
            SetupPart(constellationPlane, floatingCubeRoot, new Vector3(0f, 0f, 0.01f), new Vector3(75.0f, 0f, 0f), new Vector3(1.15f, 1.15f, 1.0f), constellationMaterial);
            constellationRenderer = constellationPlane.GetComponent<MeshRenderer>();
            if (constellationRenderer != null)
            {
                constellationRenderer.enabled = ZodiacConstellationData.EnabledInGame;
                constellationRenderer.shadowCastingMode = ShadowCastingMode.Off;
                constellationRenderer.receiveShadows = false;
            }

            // 6. 마우스 레이캐스트 감지를 위한 Sphere Collider (중심 Y=2.22f)
            SphereCollider col = gameObject.GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, BaseCenterY, 0f);
            col.radius = 1.30f;
        }

        private void CreateHoverOutline(Transform cubeBody)
        {
            hoverOutlineMaterial = CreateHaloLayer(cubeBody, "Cosmic_Cube_Hover_Outline", false);
            outerHaloMaterial = CreateHaloLayer(cubeBody, "Cosmic_Cube_Outer_Halo", true);
        }

        private Material CreateHaloLayer(Transform cubeBody, string objectName, bool outer)
        {
            Shader outlineShader = Shader.Find("DicePoC/CosmicCubeHoverOutline");
            if (outlineShader == null) return null;

            Material material = new(outlineShader)
            {
                name = outer ? "Cosmic_Cube_Outer_Halo_Mat" : "Cosmic_Cube_Near_Halo_Mat"
            };
            ConfigureHaloMaterial(material, outer);

            GameObject outlineObject = new(objectName);
            outlineObject.layer = DecorationLayer;
            outlineObject.transform.SetParent(cubeBody, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            MeshFilter sourceFilter = cubeBody.GetComponent<MeshFilter>();
            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            if (Application.isPlaying) outlineRenderer.material = material;
            else outlineRenderer.sharedMaterial = material;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            return material;
        }

        private void ConfigureHaloMaterial(Material material, bool outer)
        {
            if (material == null) return;
            material.SetColor("_OutlineColor", outer ? outerHaloColor : nearHaloColor);
            material.SetFloat("_OutlineWidth", outer ? 0.120f : 0.045f);
            material.SetFloat("_OutlineIntensity", outer ? 0.20f : 0.42f);
            material.renderQueue = (int)RenderQueue.Transparent + (outer ? 1 : 2);
        }

        private void CacheOrCreateTesseract()
        {
            if (cubeBodyTransform == null) return;

            Transform existingRoot = cubeBodyTransform.Find("Cosmic_Tesseract_Root");
            if (existingRoot == null)
            {
                CreateTesseract(cubeBodyTransform);
                return;
            }

            tesseractRoot = existingRoot;
            MeshFilter filter = tesseractRoot.GetComponent<MeshFilter>();
            MeshRenderer renderer = tesseractRoot.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("DicePoC/CosmicTesseract");

            if (filter == null) filter = tesseractRoot.gameObject.AddComponent<MeshFilter>();
            if (renderer == null) renderer = tesseractRoot.gameObject.AddComponent<MeshRenderer>();

            // 이 메시는 애니메이션으로 매 갱신마다 정점을 다시 쓴다. 에셋이면 사본으로 갈아 끼운다.
            tesseractMesh = RuntimeAssetGuard.GetWritableMesh(filter);
            if (tesseractMesh == null)
            {
                tesseractMesh = new Mesh { name = "Cosmic_Tesseract_Line_Mesh" };
                filter.sharedMesh = tesseractMesh;
            }

            tesseractMaterial = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (shader != null && (tesseractMaterial == null || tesseractMaterial.shader != shader))
            {
                tesseractMaterial = new Material(shader) { name = "Cosmic_Tesseract_Mat" };
                if (Application.isPlaying) renderer.material = tesseractMaterial;
                else renderer.sharedMaterial = tesseractMaterial;
            }

            ConfigureTesseractMaterial(tesseractMaterial);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            InitializeTesseractMesh();
            UpdateTesseractGeometry(Time.time);
        }

        private void CreateTesseract(Transform cubeBody)
        {
            Shader shader = Shader.Find("DicePoC/CosmicTesseract");
            if (cubeBody == null || shader == null) return;

            GameObject rootObject = new("Cosmic_Tesseract_Root");
            rootObject.layer = DecorationLayer;
            rootObject.transform.SetParent(cubeBody, false);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            tesseractRoot = rootObject.transform;

            tesseractMesh = new Mesh { name = "Cosmic_Tesseract_Line_Mesh" };
            if (Application.isPlaying) tesseractMesh.MarkDynamic();

            MeshFilter filter = rootObject.AddComponent<MeshFilter>();
            filter.sharedMesh = tesseractMesh;

            tesseractMaterial = new Material(shader) { name = "Cosmic_Tesseract_Mat" };
            ConfigureTesseractMaterial(tesseractMaterial);

            MeshRenderer renderer = rootObject.AddComponent<MeshRenderer>();
            if (Application.isPlaying) renderer.material = tesseractMaterial;
            else renderer.sharedMaterial = tesseractMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            InitializeTesseractMesh();
            UpdateTesseractGeometry(Time.time);
        }

        private static void ConfigureTesseractMaterial(Material material)
        {
            if (material == null) return;
            material.SetColor("_LineColor", new Color(0.04f, 1.55f, 3.20f, 1.0f));
            material.SetColor("_HotColor", new Color(0.72f, 2.35f, 3.40f, 1.0f));
            material.SetFloat("_Intensity", 0.90f);
            material.SetFloat("_Opacity", 0.62f);
            material.SetFloat("_FlowSpeed", 1.15f);
            material.SetFloat("_FlowScale", 8.0f);
            material.renderQueue = (int)RenderQueue.Transparent - 8;
        }

        private void InitializeTesseractMesh()
        {
            if (tesseractMesh == null) return;

            const int segmentCount = 20;
            const int verticesPerSegment = 8;
            const int triangleIndicesPerSegment = 36;
            tesseractVertices = new Vector3[segmentCount * verticesPerSegment];
            tesseractColors = new Color[tesseractVertices.Length];
            tesseractUvs = new Vector2[tesseractVertices.Length];
            tesseractTriangles = new int[segmentCount * triangleIndicesPerSegment];

            for (int segment = 0; segment < segmentCount; segment++)
            {
                int vertexOffset = segment * verticesPerSegment;
                float opacity = segment < 12 ? 0.72f : 0.46f;
                float phase = segment * 0.37f;
                for (int vertex = 0; vertex < verticesPerSegment; vertex++)
                {
                    tesseractColors[vertexOffset + vertex] = new Color(1f, 1f, 1f, opacity);
                    tesseractUvs[vertexOffset + vertex] = new Vector2(vertex < 4 ? 0f : 1f, phase);
                }

                WritePrismTriangles(segment);
            }

            tesseractMesh.Clear();
            tesseractMesh.vertices = tesseractVertices;
            tesseractMesh.colors = tesseractColors;
            tesseractMesh.uv = tesseractUvs;
            tesseractMesh.triangles = tesseractTriangles;
        }

        private void UpdateTesseractGeometry(float time)
        {
            if (tesseractMesh == null || tesseractVertices == null) return;

            float pulse = 1.0f + Mathf.Sin(time * 1.35f) * 0.025f;
            Quaternion innerRotation = Quaternion.Euler(
                Mathf.Sin(time * 0.31f) * 5.0f,
                time * 5.5f,
                Mathf.Cos(time * 0.27f) * 4.0f);

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 direction = new(
                    (corner & 1) == 0 ? -0.5f : 0.5f,
                    (corner & 2) == 0 ? -0.5f : 0.5f,
                    (corner & 4) == 0 ? -0.5f : 0.5f);
                tesseractOuterCorners[corner] = direction * 0.90f;
                tesseractInnerCorners[corner] = innerRotation * (direction * (0.46f * pulse));
            }

            for (int edge = 0; edge < 12; edge++)
            {
                int pairIndex = edge * 2;
                WritePrismSegment(edge,
                    tesseractInnerCorners[TesseractInnerEdges[pairIndex]],
                    tesseractInnerCorners[TesseractInnerEdges[pairIndex + 1]],
                    0.0125f);
            }

            for (int corner = 0; corner < 8; corner++)
            {
                WritePrismSegment(12 + corner,
                    tesseractOuterCorners[corner],
                    tesseractInnerCorners[corner],
                    0.0095f);
            }

            tesseractMesh.vertices = tesseractVertices;
            tesseractMesh.RecalculateBounds();
        }

        private void WritePrismSegment(int segment, Vector3 start, Vector3 end, float halfWidth)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.90f
                ? Vector3.right
                : Vector3.up;
            Vector3 side = Vector3.Cross(direction, reference).normalized * halfWidth;
            Vector3 up = Vector3.Cross(side, direction).normalized * halfWidth;
            int offset = segment * 8;

            tesseractVertices[offset] = start - side - up;
            tesseractVertices[offset + 1] = start + side - up;
            tesseractVertices[offset + 2] = start + side + up;
            tesseractVertices[offset + 3] = start - side + up;
            tesseractVertices[offset + 4] = end - side - up;
            tesseractVertices[offset + 5] = end + side - up;
            tesseractVertices[offset + 6] = end + side + up;
            tesseractVertices[offset + 7] = end - side + up;
        }

        private void WritePrismTriangles(int segment)
        {
            int vertex = segment * 8;
            int triangle = segment * 36;
            int[] localIndices =
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            for (int index = 0; index < localIndices.Length; index++)
            {
                tesseractTriangles[triangle + index] = vertex + localIndices[index];
            }
        }

        private Material CreateCosmicVolumeMaterial(Shader shader)
        {
            Material material = new(shader) { name = "Cosmic_Inner_Volume_Mat" };
            ConfigureCosmicVolumeMaterial(material);
            return material;
        }

        private static void ConfigureCosmicVolumeMaterial(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_AbyssColor")) material.SetColor("_AbyssColor", new Color(0.003f, 0.015f, 0.09f, 1.0f));
            if (material.HasProperty("_NebulaColor")) material.SetColor("_NebulaColor", new Color(0.00f, 0.20f, 1.25f, 1.0f));
            if (material.HasProperty("_CloudColor")) material.SetColor("_CloudColor", new Color(0.00f, 1.55f, 2.40f, 1.0f));
            if (material.HasProperty("_CoreColor")) material.SetColor("_CoreColor", new Color(0.55f, 1.80f, 2.80f, 1.0f));
            if (material.HasProperty("_StarColor")) material.SetColor("_StarColor", new Color(0.65f, 2.00f, 3.00f, 1.0f));
            if (material.HasProperty("_Brightness")) material.SetFloat("_Brightness", 1.18f);
            if (material.HasProperty("_Density")) material.SetFloat("_Density", 0.94f);
            if (material.HasProperty("_Opacity")) material.SetFloat("_Opacity", 0.84f);
            if (material.HasProperty("_NoiseScale")) material.SetFloat("_NoiseScale", 3.2f);
            if (material.HasProperty("_NoiseSpeed")) material.SetFloat("_NoiseSpeed", 0.06f);
            if (material.HasProperty("_CoreIntensity")) material.SetFloat("_CoreIntensity", 2.45f);
            if (material.HasProperty("_CoreRadius")) material.SetFloat("_CoreRadius", 0.39f);
            if (material.HasProperty("_StarIntensity")) material.SetFloat("_StarIntensity", 2.65f);
            if (material.HasProperty("_TwinkleSpeed")) material.SetFloat("_TwinkleSpeed", 2.20f);
        }

        private void CreateCrystalLayers(Transform cubeBody)
        {
            if (cubeBody == null) return;

            MeshFilter sourceFilter = cubeBody.GetComponent<MeshFilter>();
            Mesh cubeMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            Shader shellShader = Shader.Find("DicePoC/CosmicCrystalShell");
            Shader coreShader = Shader.Find("DicePoC/CosmicCore");

            Transform innerLayerA = cubeBody.Find("Crystal_Inner_Layer_A");
            Transform legacyBackFacets = cubeBody.Find("Crystal_Back_Facets");
            if (innerLayerA == null && legacyBackFacets != null)
            {
                legacyBackFacets.name = "Crystal_Inner_Layer_A";
                innerLayerA = legacyBackFacets;
            }

            if (shellShader != null && cubeMesh != null && innerLayerA == null)
            {
                crystalInnerMaterialA = CreateCrystalShellMaterial(shellShader, "Cosmic_Crystal_Inner_A_Mat", 1);
                crystalInnerMaterialA.renderQueue = (int)RenderQueue.Transparent - 10;
                CreateMeshLayer("Crystal_Inner_Layer_A", cubeBody, cubeMesh, crystalInnerMaterialA);
            }

            if (shellShader != null && cubeMesh != null && cubeBody.Find("Crystal_Outer_Shell") == null)
            {
                crystalFrontMaterial = CreateCrystalShellMaterial(shellShader, "Cosmic_Crystal_Front_Mat", 0);
                crystalFrontMaterial.renderQueue = (int)RenderQueue.Transparent + 10;
                CreateMeshLayer("Crystal_Outer_Shell", cubeBody, cubeMesh, crystalFrontMaterial);
            }

            if (shellShader != null && cubeMesh != null && cubeBody.Find("Crystal_Inner_Layer_B") == null)
            {
                crystalInnerMaterialB = CreateCrystalShellMaterial(shellShader, "Cosmic_Crystal_Inner_B_Mat", 2);
                crystalInnerMaterialB.renderQueue = (int)RenderQueue.Transparent - 20;
                CreateMeshLayer("Crystal_Inner_Layer_B", cubeBody, cubeMesh, crystalInnerMaterialB);
            }

            ConfigureCrystalLayerTransforms(
                cubeBody.Find("Crystal_Outer_Shell"),
                cubeBody.Find("Crystal_Inner_Layer_A"),
                cubeBody.Find("Crystal_Inner_Layer_B"));

            if (coreShader != null && cubeBody.Find("Energy_Core") == null)
            {
                energyCoreMaterial = new Material(coreShader) { name = "Cosmic_Energy_Core_Mat" };
                ConfigureEnergyCoreMaterial(energyCoreMaterial);
                energyCoreMaterial.renderQueue = (int)RenderQueue.Transparent - 5;

                GameObject coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coreObject.name = "Energy_Core";
                SetupPart(coreObject, cubeBody, Vector3.zero, Vector3.zero, Vector3.one * 0.45f, energyCoreMaterial);
                MeshRenderer coreRenderer = coreObject.GetComponent<MeshRenderer>();
                if (coreRenderer != null)
                {
                    coreRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    coreRenderer.receiveShadows = false;
                }
            }
        }

        private Material CreateCrystalShellMaterial(Shader shader, string materialName, int layerIndex)
        {
            Material material = new(shader) { name = materialName };
            ConfigureCrystalShellMaterial(material, layerIndex);
            return material;
        }

        private static void ConfigureCrystalShellMaterial(Material material, int layerIndex)
        {
            if (material == null) return;
            bool outerLayer = layerIndex == 0;
            bool firstInnerLayer = layerIndex == 1;
            material.SetColor("_CrystalColor", outerLayer
                ? new Color(0.00f, 0.55f, 1.65f, 1.0f)
                : firstInnerLayer
                    ? new Color(0.00f, 0.44f, 1.32f, 1.0f)
                    : new Color(0.02f, 0.34f, 1.02f, 1.0f));
            material.SetColor("_EdgeColor", outerLayer
                ? new Color(0.20f, 1.85f, 2.80f, 1.0f)
                : firstInnerLayer
                    ? new Color(0.08f, 1.18f, 2.05f, 1.0f)
                    : new Color(0.05f, 0.86f, 1.62f, 1.0f));
            material.SetColor("_WarmReflectionColor", new Color(1.00f, 0.42f, 0.10f, 1.0f));
            material.SetColor("_ThicknessColor", outerLayer
                ? new Color(0.00f, 0.18f, 1.10f, 1.0f)
                : new Color(0.00f, 0.24f, firstInnerLayer ? 1.22f : 0.92f, 1.0f));
            material.SetFloat("_SurfaceAlpha", outerLayer ? 0.070f : firstInnerLayer ? 0.105f : 0.085f);
            material.SetFloat("_ThicknessIntensity", outerLayer ? 0.70f : firstInnerLayer ? 0.56f : 0.42f);
            material.SetFloat("_ThicknessWidth", outerLayer ? 0.075f : firstInnerLayer ? 0.090f : 0.105f);
            material.SetFloat("_RefractionStrength", outerLayer ? 0.18f : firstInnerLayer ? 0.22f : 0.17f);
            material.SetFloat("_FresnelIntensity", outerLayer ? 1.15f : firstInnerLayer ? 0.82f : 0.66f);
            material.SetFloat("_EdgeIntensity", outerLayer ? 2.35f : firstInnerLayer ? 0.10f : 0.04f);
            material.SetFloat("_EdgeWidth", outerLayer ? 0.043f : firstInnerLayer ? 0.028f : 0.024f);
            material.SetFloat("_ShellExpansion", outerLayer ? 0.018f : 0.0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
        }

        private static void ConfigureCrystalLayerTransforms(
            Transform outerLayer,
            Transform innerLayerA,
            Transform innerLayerB)
        {
            if (outerLayer != null)
            {
                outerLayer.localPosition = Vector3.zero;
                outerLayer.localRotation = Quaternion.identity;
                outerLayer.localScale = Vector3.one;
            }

            FitInnerCrystalLayer(innerLayerA, Quaternion.Euler(4.0f, -5.0f, 3.0f), 0.035f);
            FitInnerCrystalLayer(innerLayerB, Quaternion.Euler(-6.0f, 8.0f, -4.0f), 0.055f);
        }

        private static void FitInnerCrystalLayer(Transform layer, Quaternion rotation, float margin)
        {
            if (layer == null) return;

            Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
            float extentX = Mathf.Abs(matrix.m00) + Mathf.Abs(matrix.m01) + Mathf.Abs(matrix.m02);
            float extentY = Mathf.Abs(matrix.m10) + Mathf.Abs(matrix.m11) + Mathf.Abs(matrix.m12);
            float extentZ = Mathf.Abs(matrix.m20) + Mathf.Abs(matrix.m21) + Mathf.Abs(matrix.m22);
            float maximumExtent = Mathf.Max(extentX, Mathf.Max(extentY, extentZ));
            float safeScale = (1.0f - margin * 2.0f) / maximumExtent;

            layer.localPosition = Vector3.zero;
            layer.localRotation = rotation;
            layer.localScale = Vector3.one * safeScale;
        }

        private static void ConfigureEnergyCoreMaterial(Material material)
        {
            if (material == null) return;
            material.SetColor("_CoreColor", new Color(0.12f, 1.35f, 2.65f, 1.0f));
            material.SetColor("_CoreHotColor", new Color(0.90f, 2.40f, 3.20f, 1.0f));
            material.SetFloat("_CoreIntensity", 2.70f);
            material.SetFloat("_PulseSpeed", 1.35f);
            material.SetFloat("_PulseAmount", 0.08f);
        }

        private static void CreateMeshLayer(string objectName, Transform parent, Mesh mesh, Material material)
        {
            GameObject layerObject = new(objectName);
            layerObject.layer = DecorationLayer;
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = Vector3.zero;
            layerObject.transform.localRotation = Quaternion.identity;
            layerObject.transform.localScale = Vector3.one;

            MeshFilter filter = layerObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = layerObject.AddComponent<MeshRenderer>();
            if (Application.isPlaying) renderer.material = material;
            else renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateInternalParticles(Transform cubeBody)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard");

            Material psMat = new(particleShader) { name = "Cosmic_Internal_Star_Mat" };
            if (psMat.HasProperty("_Surface")) psMat.SetFloat("_Surface", 1);
            if (psMat.HasProperty("_Blend")) psMat.SetFloat("_Blend", 0);
            if (psMat.HasProperty("_BaseColor")) psMat.SetColor("_BaseColor", new Color(0.85f, 0.98f, 1f, 1f));
            if (psMat.HasProperty("_Color")) psMat.SetColor("_Color", new Color(0.85f, 0.98f, 1f, 1f));
            if (psMat.HasProperty("_EmissionColor"))
            {
                psMat.EnableKeyword("_EMISSION");
                psMat.SetColor("_EmissionColor", new Color(0.55f, 0.92f, 1.00f) * 2.2f);
            }
            psMat.renderQueue = (int)RenderQueue.Transparent - 4;

            Gradient grad = new();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.35f, 0.85f, 1f), 0f),
                    new GradientColorKey(Color.white, 0.35f),
                    new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0.7f),
                    new GradientColorKey(new Color(0.25f, 0.75f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1.0f, 0.25f),
                    new GradientAlphaKey(0.4f, 0.50f),
                    new GradientAlphaKey(1.0f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            AnimationCurve sizeCurve = new(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.25f, 1.0f),
                new Keyframe(0.50f, 0.4f),
                new Keyframe(0.75f, 1.1f),
                new Keyframe(1f, 0f)
            );

            GameObject particleObject = new("Cosmic_Internal_Stars");
            particleObject.layer = DecorationLayer;
            particleObject.transform.SetParent(cubeBody, false);
            particleObject.transform.localPosition = Vector3.zero;
            particleObject.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 44;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.006f, 0.024f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.40f, 0.90f, 1.00f, 0.92f), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 10.0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.one * 0.72f;

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalX = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            vel.radial = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = grad;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer psRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = psMat;
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;

            faceParticleSystems.Add(ps);
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
