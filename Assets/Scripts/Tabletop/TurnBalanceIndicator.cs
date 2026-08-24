using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tessera.Tabletop
{
    public enum TurnSide
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// 화면 관점의 왼쪽(나)과 오른쪽(상대)을 표시하는 앤틱 실버 턴 천칭입니다.
    /// 플레이어 번호를 보관하지 않고, 활성 방향만 표현합니다.
    /// </summary>
    [ExecuteAlways]
    public sealed class TurnBalanceIndicator : MonoBehaviour
    {
        private const int DecorationLayer = 11;
        private const float ActiveAngle = 9f;
        private const float BeamHeight = 1.30f;
        private const float PanOffsetX = 1.12f;
        private const float PanOffsetY = -0.76f;
        private const float PanOffsetZ = -0.43f;
        private const float AnimationDuration = 0.60f;
        private const float SealLiftHeight = 0.11f;
        private const float SealArcHeight = 0.22f;
        private const float LiftPhaseEnd = 0.22f;
        private const float TransferPhaseEnd = 0.78f;

        public static readonly Vector3 DefaultPosition = new(2.86f, 0.10f, 6.01f);
        public static readonly Vector3 DefaultEulerAngles = new(0f, 50f, 0f);

        [SerializeField] private TurnSide currentSide = TurnSide.None;
        [SerializeField] private Color silverColor = new(0.55f, 0.59f, 0.65f, 1f);
        [SerializeField] private Color charcoalColor = new(0.10f, 0.11f, 0.14f, 1f);
        [SerializeField] private Color waxColor = new(0.533f, 0.176f, 0.133f, 1f);
        [SerializeField] private Color runeColor = new(1.00f, 0.55f, 0.16f, 1f);

        private Transform beamPivot;
        private Transform leftPan;
        private Transform rightPan;
        private Transform seal;
        private Coroutine transitionRoutine;

        public TurnSide CurrentSide => currentSide;
        public float CurrentBeamAngle => beamPivot != null ? NormalizeAngle(beamPivot.localEulerAngles.z) : 0f;
        public Transform Seal => seal;

        private void Awake()
        {
            EnsureGeometry();
            ApplyStateImmediate(currentSide);
        }

        private void OnEnable()
        {
            EnsureGeometry();
            ApplyStateImmediate(currentSide);
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
            ApplyStateImmediate(currentSide);
        }
#endif

        public static TurnBalanceIndicator Create(Transform parent, Vector3? localPosition = null,
            Quaternion? rotation = null, Vector3? scale = null)
        {
            GameObject root = new("3D Turn Balance Indicator");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition ?? DefaultPosition;
            root.transform.localRotation = rotation ?? Quaternion.Euler(DefaultEulerAngles);
            root.transform.localScale = scale ?? Vector3.one;

            TurnBalanceIndicator indicator = root.AddComponent<TurnBalanceIndicator>();
            indicator.BuildGeometry();
            indicator.ApplyStateImmediate(TurnSide.None);
            return indicator;
        }

        public void EnsureGeometry()
        {
            BindGeometry();
            if (beamPivot == null || leftPan == null || rightPan == null || seal == null)
            {
                BuildGeometry();
            }
        }

        [ContextMenu("Rebuild Turn Balance")]
        public void BuildGeometry()
        {
            StopTransition();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material silver = CreateMaterial(lit, "TurnBalance_AntiqueSilver", silverColor, 0.86f, 0.52f);
            Material darkSilver = CreateMaterial(lit, "TurnBalance_CharcoalRecess", charcoalColor, 0.72f, 0.30f);
            Material wax = CreateMaterial(lit, "TurnBalance_CrimsonWax", waxColor, 0.02f, 0.28f);
            Material amberRune = CreateMaterial(lit, "TurnBalance_AmberRune", runeColor, 0.08f, 0.72f);
            if (amberRune.HasProperty("_EmissionColor"))
            {
                amberRune.EnableKeyword("_EMISSION");
                amberRune.SetColor("_EmissionColor", runeColor * 1.65f);
            }

            BuildBase(silver, darkSilver);
            BuildBeamAndPans(silver, darkSilver, amberRune);
            BuildSeal(wax, amberRune, darkSilver);
            BindGeometry();
            ApplyStateImmediate(currentSide);
        }

        public void SetActiveSide(TurnSide side, bool animate = true)
        {
            EnsureGeometry();
            StopTransition();

            if (!animate || !Application.isPlaying || !isActiveAndEnabled)
            {
                ApplyStateImmediate(side);
                return;
            }

            transitionRoutine = StartCoroutine(TransitionRoutine(side));
        }

        private IEnumerator TransitionRoutine(TurnSide targetSide)
        {
            currentSide = targetSide;
            float startAngle = CurrentBeamAngle;
            Vector3 startSealPosition = seal.localPosition;
            Vector3 targetSealPosition = GetSealPosition(targetSide, 0f);
            Vector3 liftPosition = startSealPosition + Vector3.up * SealLiftHeight;
            Vector3 landingHoverPosition = targetSealPosition + Vector3.up * SealLiftHeight;
            float targetAngle = GetBeamAngle(targetSide);

            float elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / AnimationDuration);

                if (t < LiftPhaseEnd)
                {
                    float phase = Smooth(t / LiftPhaseEnd);
                    SetBeamAngle(Mathf.Lerp(startAngle, 0f, phase));
                    // 출발 접시 위에서 먼저 수직으로 분리해, 빔의 수평화가 인장을 끌고 가는 인상을 없앱니다.
                    seal.localPosition = Vector3.Lerp(startSealPosition, liftPosition, phase);
                }
                else if (t < TransferPhaseEnd)
                {
                    float phase = Smooth((t - LiftPhaseEnd) / (TransferPhaseEnd - LiftPhaseEnd));
                    SetBeamAngle(0f);
                    seal.localPosition = EvaluateTransferArc(liftPosition, landingHoverPosition, phase);
                }
                else
                {
                    float phase = Smooth((t - TransferPhaseEnd) / (1f - TransferPhaseEnd));
                    float angle = Mathf.Lerp(0f, targetAngle, phase);
                    SetBeamAngle(angle);
                    // 인장이 접시의 현재 위치를 계속 따라가면서 남은 높이만 줄여 착지점의 스냅을 방지합니다.
                    seal.localPosition = GetSealPosition(targetSide, angle)
                        + Vector3.up * Mathf.Lerp(SealLiftHeight, 0f, phase);
                }

                seal.localRotation = Quaternion.identity;
                yield return null;
            }

            ApplyStateImmediate(targetSide);
            transitionRoutine = null;
        }

        private static Vector3 EvaluateTransferArc(Vector3 start, Vector3 end, float t)
        {
            t = Mathf.Clamp01(t);
            Vector3 control = (start + end) * 0.5f;
            control.y = Mathf.Max(start.y, end.y) + SealArcHeight;
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private void ApplyStateImmediate(TurnSide side)
        {
            currentSide = side;
            if (beamPivot == null || seal == null) return;
            float angle = GetBeamAngle(side);
            SetBeamAngle(angle);
            seal.localPosition = GetSealPosition(side, angle);
            seal.localRotation = Quaternion.identity;
        }

        private void SetBeamAngle(float angle)
        {
            if (beamPivot != null) beamPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private Vector3 GetSealPosition(TurnSide side, float beamAngle)
        {
            if (side == TurnSide.None) return new Vector3(0f, 0.31f, -0.02f);

            float x = side == TurnSide.Left ? -PanOffsetX : PanOffsetX;
            Vector3 panCenter = Quaternion.Euler(0f, 0f, beamAngle) * new Vector3(x, BeamHeight + PanOffsetY, PanOffsetZ);
            return panCenter + new Vector3(0f, 0.15f, 0f);
        }

        private void BuildBase(Material silver, Material darkSilver)
        {
            CreateMeshPart("Balance_Ornate_Base_Lower", transform, new Vector3(0f, 0.09f, 0f),
                CreateBeveledBoxMesh(1.18f, 0.16f, 0.66f, 0.07f), silver);
            CreateMeshPart("Balance_Ornate_Base_Recess", transform, new Vector3(0f, 0.205f, -0.015f),
                CreateBeveledBoxMesh(1.02f, 0.10f, 0.57f, 0.045f), darkSilver);
            CreateMeshPart("Balance_Ornate_Base_Upper", transform, new Vector3(0f, 0.285f, 0f),
                CreateBeveledBoxMesh(0.88f, 0.09f, 0.49f, 0.035f), silver);

            // 정면에서 읽히는 큼직한 켈틱 마름모 문양. 미세한 텍스트 대신 낮은 해상도에서도 남는 부조를 사용합니다.
            for (int i = -2; i <= 2; i++)
            {
                CreateCube($"Balance_Base_Rune_{i + 3}", transform,
                    new Vector3(i * 0.19f, 0.21f, -0.307f), new Vector3(0.055f, 0.055f, 0.012f),
                    silver, new Vector3(0f, 0f, 45f));
                CreateCube($"Balance_Base_Rune_Inset_{i + 3}", transform,
                    new Vector3(i * 0.19f, 0.21f, -0.321f), new Vector3(0.025f, 0.025f, 0.007f),
                    darkSilver, new Vector3(0f, 0f, 45f));
            }

            Vector2[] columnProfile =
            {
                new(0.34f, 0.31f), new(0.38f, 0.37f), new(0.29f, 0.45f),
                new(0.23f, 0.51f), new(0.16f, 0.59f), new(0.13f, 1.08f),
                new(0.19f, 1.15f), new(0.25f, 1.22f), new(0.21f, 1.28f)
            };
            CreateMeshPart("Balance_Turned_Column", transform, Vector3.zero,
                CreateLatheMesh("TurnBalance_TurnedColumn", columnProfile, 20), silver);
            CreateCylinder("Balance_Column_Shadow_Collar", transform, new Vector3(0f, 0.54f, 0f),
                new Vector3(0.20f, 0.027f, 0.20f), darkSilver);
            CreateCylinder("Balance_Column_Capital_Recess", transform, new Vector3(0f, 1.17f, 0f),
                new Vector3(0.21f, 0.025f, 0.21f), darkSilver);
            CreateSphere("Balance_Fulcrum", transform, new Vector3(0f, BeamHeight, 0.005f),
                new Vector3(0.20f, 0.18f, 0.15f), darkSilver);

            CreateCylinder("Balance_Finial_Base", transform, new Vector3(0f, 1.48f, 0.03f),
                new Vector3(0.19f, 0.055f, 0.19f), silver);
            CreateSphere("Balance_Finial_Orb", transform, new Vector3(0f, 1.60f, 0.03f),
                new Vector3(0.15f, 0.13f, 0.15f), silver);
            CreateCylinder("Balance_Finial_Crown", transform, new Vector3(0f, 1.70f, 0.03f),
                new Vector3(0.07f, 0.04f, 0.07f), darkSilver);

            CreateCylinder("Seal_Central_Cradle", transform, new Vector3(0f, 0.255f, -0.02f),
                new Vector3(0.27f, 0.035f, 0.27f), silver);
            CreateTorusPart("Seal_Central_Cradle_Rim", transform, new Vector3(0f, 0.30f, -0.02f),
                Vector3.zero, 0.22f, 0.022f, darkSilver);
        }

        private void BuildBeamAndPans(Material silver, Material darkSilver, Material amberRune)
        {
            beamPivot = new GameObject("Balance_Beam_Pivot").transform;
            SetupTransform(beamPivot.gameObject, transform, new Vector3(0f, BeamHeight, 0f), Vector3.zero, Vector3.one);

            Vector2[] beamProfile =
            {
                new(-1.42f, 0.00f), new(-1.30f, 0.09f), new(-0.72f, 0.16f),
                new(0f, 0.22f), new(0.72f, 0.16f), new(1.30f, 0.09f), new(1.42f, 0.00f),
                new(1.32f, -0.11f), new(0.70f, -0.08f), new(0f, -0.025f),
                new(-0.70f, -0.08f), new(-1.32f, -0.11f)
            };
            Mesh beamMesh = CreateExtrudedProfileMesh("TurnBalance_CurvedBeam", beamProfile, 0.20f);
            CreateMeshPart("Balance_Beam", beamPivot, Vector3.zero, beamMesh, silver);
            CreateMeshPart("Balance_Beam_Inlay", beamPivot, new Vector3(0f, 0.005f, -0.112f),
                beamMesh, darkSilver, new Vector3(0.84f, 0.48f, 0.14f));
            CreateCube("Balance_Beam_Relief_L", beamPivot, new Vector3(-0.58f, 0.055f, -0.128f),
                new Vector3(0.10f, 0.035f, 0.010f), silver, new Vector3(0f, 0f, 22f));
            CreateCube("Balance_Beam_Relief_R", beamPivot, new Vector3(0.58f, 0.055f, -0.128f),
                new Vector3(0.10f, 0.035f, 0.010f), silver, new Vector3(0f, 0f, -22f));

            CreateSphere("Balance_Beam_Leaf_L", beamPivot, new Vector3(-1.36f, 0.035f, 0f),
                new Vector3(0.16f, 0.10f, 0.13f), darkSilver);
            CreateSphere("Balance_Beam_Leaf_R", beamPivot, new Vector3(1.36f, 0.035f, 0f),
                new Vector3(0.16f, 0.10f, 0.13f), darkSilver);

            Vector2[] shieldProfile =
            {
                new(-0.23f, 0.20f), new(0.23f, 0.20f), new(0.20f, -0.12f),
                new(0f, -0.28f), new(-0.20f, -0.12f)
            };
            CreateMeshPart("Balance_Center_Shield", beamPivot, new Vector3(0f, 0.02f, -0.16f),
                CreateExtrudedProfileMesh("TurnBalance_Shield", shieldProfile, 0.09f), darkSilver);
            CreateForwardCylinder("Balance_Center_Medallion", beamPivot, new Vector3(0f, 0.04f, -0.225f),
                0.14f, 0.035f, silver);
            CreateTorusPart("Balance_Center_Rune_Ring", beamPivot, new Vector3(0f, 0.04f, -0.265f),
                new Vector3(90f, 0f, 0f), 0.095f, 0.012f, darkSilver);
            CreateCube("Balance_Center_Rune", beamPivot, new Vector3(0f, 0.04f, -0.292f),
                new Vector3(0.045f, 0.045f, 0.009f), amberRune, new Vector3(0f, 0f, 45f));

            leftPan = BuildPan("Left", -PanOffsetX, silver, darkSilver);
            rightPan = BuildPan("Right", PanOffsetX, silver, darkSilver);
        }

        private Transform BuildPan(string sideName, float x, Material silver, Material darkSilver)
        {
            Transform panRoot = new GameObject($"Balance_{sideName}_Pan").transform;
            SetupTransform(panRoot.gameObject, beamPivot, new Vector3(x, PanOffsetY, PanOffsetZ), Vector3.zero, Vector3.one);

            CreateMeshPart($"Balance_{sideName}_Pan_Bowl", panRoot, Vector3.zero,
                CreateBowlMesh($"TurnBalance_{sideName}Bowl", 0.44f, 0.12f, 28), silver,
                new Vector3(1f, 1f, 0.86f));
            CreateTorusPart($"Balance_{sideName}_Pan_Rim", panRoot, new Vector3(0f, 0.008f, 0f),
                Vector3.zero, 0.44f, 0.032f, darkSilver, new Vector3(1f, 1f, 0.86f));

            float sign = Mathf.Sign(x);
            Vector3 leftStart = new(x - 0.12f * sign, -0.02f, -0.01f);
            Vector3 rightStart = new(x + 0.12f * sign, -0.02f, -0.01f);
            Vector3 leftEnd = new(x - 0.29f, PanOffsetY + 0.04f, PanOffsetZ);
            Vector3 rightEnd = new(x + 0.29f, PanOffsetY + 0.04f, PanOffsetZ);
            CreateChainStrand($"Balance_{sideName}_Chain_Inner", beamPivot, leftStart, leftEnd, 5, darkSilver, false);
            CreateChainStrand($"Balance_{sideName}_Chain_Outer", beamPivot, rightStart, rightEnd, 5, darkSilver, true);
            return panRoot;
        }

        private void BuildSeal(Material wax, Material amberRune, Material darkSilver)
        {
            seal = new GameObject("Turn_Wax_Seal").transform;
            SetupTransform(seal.gameObject, transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreateMeshPart("Wax_Seal_Body", seal, Vector3.zero,
                CreateWaxSealMesh("TurnBalance_WaxSeal", 0.25f, 0.075f, 12), wax);
            CreateMeshPart("Wax_Seal_Shadow_Rim", seal, new Vector3(0f, 0.046f, 0f),
                CreateWaxSealMesh("TurnBalance_WaxSealInset", 0.205f, 0.018f, 12), darkSilver);

            GameObject runeRing = new("Wax_Seal_Knot_Ring", typeof(MeshFilter), typeof(MeshRenderer));
            runeRing.GetComponent<MeshFilter>().sharedMesh = CreateTorusMesh(0.14f, 0.018f, 24, 6);
            SetupPart(runeRing, seal, new Vector3(0f, 0.073f, 0f), Vector3.zero, Vector3.one, amberRune);
            CreateCube("Wax_Seal_Knot_Center", seal, new Vector3(0f, 0.078f, 0f),
                new Vector3(0.075f, 0.012f, 0.075f), amberRune, new Vector3(0f, 45f, 0f));
        }

        private void BindGeometry()
        {
            beamPivot = transform.Find("Balance_Beam_Pivot");
            leftPan = beamPivot != null ? beamPivot.Find("Balance_Left_Pan") : null;
            rightPan = beamPivot != null ? beamPivot.Find("Balance_Right_Pan") : null;
            seal = transform.Find("Turn_Wax_Seal");
        }

        private void StopTransition()
        {
            if (transitionRoutine == null) return;
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        private static float GetBeamAngle(TurnSide side)
        {
            return side == TurnSide.Left ? ActiveAngle : side == TurnSide.Right ? -ActiveAngle : 0f;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static Material CreateMaterial(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static void CreateMeshPart(string name, Transform parent, Vector3 position, Mesh mesh,
            Material material, Vector3? scale = null, Vector3? rotation = null)
        {
            GameObject obj = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            SetupPart(obj, parent, position, rotation ?? Vector3.zero, scale ?? Vector3.one, material);
        }

        private static void CreateTorusPart(string name, Transform parent, Vector3 position, Vector3 rotation,
            float radius, float tubeRadius, Material material, Vector3? scale = null)
        {
            CreateMeshPart(name, parent, position, CreateTorusMesh(radius, tubeRadius, 24, 7), material,
                scale ?? Vector3.one, rotation);
        }

        private static void CreateForwardCylinder(string name, Transform parent, Vector3 position,
            float radius, float depth, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            SetupPart(obj, parent, position, new Vector3(90f, 0f, 0f),
                new Vector3(radius * 2f, depth * 0.5f, radius * 2f), material);
        }

        private static void CreateChainStrand(string name, Transform parent, Vector3 start, Vector3 end,
            int linkCount, Material material, bool alternatePhase)
        {
            Transform strand = new GameObject(name).transform;
            SetupTransform(strand.gameObject, parent, Vector3.zero, Vector3.zero, Vector3.one);
            Vector3 direction = (end - start).normalized;
            Quaternion alongChain = Quaternion.FromToRotation(Vector3.right, direction);

            for (int i = 0; i < linkCount; i++)
            {
                float t = (i + 0.5f) / linkCount;
                GameObject link = new($"{name}_Link_{i + 1}", typeof(MeshFilter), typeof(MeshRenderer));
                link.GetComponent<MeshFilter>().sharedMesh = CreateTorusMesh(0.055f, 0.012f, 14, 5);
                SetupPart(link, strand, Vector3.Lerp(start, end, t), Vector3.zero,
                    new Vector3(1.25f, 1f, 0.72f), material);
                float twist = ((i & 1) == 0) ^ alternatePhase ? 0f : 90f;
                link.transform.localRotation = alongChain * Quaternion.AngleAxis(twist, Vector3.right);
            }
        }

        private static Mesh CreateBeveledBoxMesh(float width, float height, float depth, float bevel)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float halfDepth = depth * 0.5f;
            float verticalBevel = Mathf.Min(bevel, height * 0.35f);
            Vector3[] vertices = new Vector3[34];
            int vertex = 0;

            for (int ring = 0; ring < 4; ring++)
            {
                bool inset = ring == 0 || ring == 3;
                float y = ring switch
                {
                    0 => -halfHeight,
                    1 => -halfHeight + verticalBevel,
                    2 => halfHeight - verticalBevel,
                    _ => halfHeight
                };
                float x = halfWidth - (inset ? verticalBevel : 0f);
                float z = halfDepth - (inset ? verticalBevel : 0f);
                Vector2[] points = CreateChamferedRectangle(x, z, bevel);
                for (int i = 0; i < 8; i++) vertices[vertex++] = new Vector3(points[i].x, y, points[i].y);
            }

            vertices[32] = new Vector3(0f, -halfHeight, 0f);
            vertices[33] = new Vector3(0f, halfHeight, 0f);
            int[] triangles = new int[(3 * 8 * 6) + (8 * 3 * 2)];
            int triangle = 0;
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int next = (i + 1) % 8;
                    int a = ring * 8 + i;
                    int b = ring * 8 + next;
                    int c = (ring + 1) * 8 + next;
                    int d = (ring + 1) * 8 + i;
                    triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
                    triangles[triangle++] = a; triangles[triangle++] = d; triangles[triangle++] = c;
                }
            }
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                triangles[triangle++] = 32; triangles[triangle++] = next; triangles[triangle++] = i;
                triangles[triangle++] = 33; triangles[triangle++] = 24 + i; triangles[triangle++] = 24 + next;
            }

            return FinalizeMesh("Procedural_TurnBalance_BeveledBox", vertices, triangles);
        }

        private static Vector2[] CreateChamferedRectangle(float halfWidth, float halfDepth, float bevel)
        {
            float b = Mathf.Min(bevel, Mathf.Min(halfWidth, halfDepth) * 0.45f);
            return new[]
            {
                new Vector2(-halfWidth + b, -halfDepth), new Vector2(halfWidth - b, -halfDepth),
                new Vector2(halfWidth, -halfDepth + b), new Vector2(halfWidth, halfDepth - b),
                new Vector2(halfWidth - b, halfDepth), new Vector2(-halfWidth + b, halfDepth),
                new Vector2(-halfWidth, halfDepth - b), new Vector2(-halfWidth, -halfDepth + b)
            };
        }

        private static Mesh CreateLatheMesh(string name, Vector2[] profile, int segments)
        {
            int columns = profile.Length;
            Vector3[] vertices = new Vector3[(segments + 1) * columns];
            int[] triangles = new int[segments * (columns - 1) * 6];

            for (int segment = 0; segment <= segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                for (int p = 0; p < columns; p++)
                    vertices[segment * columns + p] = new Vector3(profile[p].x * cos, profile[p].y, profile[p].x * sin);
            }

            int triangle = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                for (int p = 0; p < columns - 1; p++)
                {
                    int a = segment * columns + p;
                    int b = (segment + 1) * columns + p;
                    int c = b + 1;
                    int d = a + 1;
                    triangles[triangle++] = a; triangles[triangle++] = b; triangles[triangle++] = c;
                    triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = d;
                }
            }
            return FinalizeMesh(name, vertices, triangles);
        }

        private static Mesh CreateExtrudedProfileMesh(string name, Vector2[] profile, float depth)
        {
            int count = profile.Length;
            Vector3[] vertices = new Vector3[count * 2 + 2];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(profile[i].x, profile[i].y, -depth * 0.5f);
                vertices[count + i] = new Vector3(profile[i].x, profile[i].y, depth * 0.5f);
            }
            vertices[count * 2] = new Vector3(0f, 0f, -depth * 0.5f);
            vertices[count * 2 + 1] = new Vector3(0f, 0f, depth * 0.5f);

            int[] triangles = new int[count * 12];
            int triangle = 0;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                triangles[triangle++] = i; triangles[triangle++] = next; triangles[triangle++] = count + next;
                triangles[triangle++] = i; triangles[triangle++] = count + next; triangles[triangle++] = count + i;
                triangles[triangle++] = count * 2; triangles[triangle++] = next; triangles[triangle++] = i;
                triangles[triangle++] = count * 2 + 1; triangles[triangle++] = count + i; triangles[triangle++] = count + next;
            }
            return FinalizeMesh(name, vertices, triangles);
        }

        private static Mesh CreateBowlMesh(string name, float radius, float depth, int segments)
        {
            Vector2[] profile =
            {
                new(0f, -depth * 0.72f), new(radius * 0.52f, -depth * 0.76f),
                new(radius * 0.90f, -depth * 0.36f), new(radius, 0f),
                new(radius * 0.86f, -depth * 0.20f), new(radius * 0.48f, -depth * 0.53f),
                new(0f, -depth * 0.60f)
            };
            return CreateLatheMesh(name, profile, segments);
        }

        private static Mesh CreateWaxSealMesh(string name, float radius, float height, int segments)
        {
            Vector3[] vertices = new Vector3[segments * 2 + 2];
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float irregularity = 1f + Mathf.Sin(i * 2.41f) * 0.055f + Mathf.Cos(i * 1.37f) * 0.035f;
                float r = radius * irregularity;
                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                vertices[i] = new Vector3(x, 0f, z);
                vertices[segments + i] = new Vector3(x, height, z);
            }
            vertices[segments * 2] = Vector3.zero;
            vertices[segments * 2 + 1] = new Vector3(0f, height, 0f);

            int[] triangles = new int[segments * 12];
            int triangle = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[triangle++] = i; triangles[triangle++] = segments + next; triangles[triangle++] = next;
                triangles[triangle++] = i; triangles[triangle++] = segments + i; triangles[triangle++] = segments + next;
                triangles[triangle++] = segments * 2; triangles[triangle++] = next; triangles[triangle++] = i;
                triangles[triangle++] = segments * 2 + 1; triangles[triangle++] = segments + i; triangles[triangle++] = segments + next;
            }
            return FinalizeMesh(name, vertices, triangles);
        }

        private static Mesh FinalizeMesh(string name, Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new() { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 halfScale,
            Material material, Vector3? rotation = null)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            SetupPart(obj, parent, position, rotation ?? Vector3.zero, halfScale * 2f, material);
        }

        private static void CreateCylinder(string name, Transform parent, Vector3 position, Vector3 halfScale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            SetupPart(obj, parent, position, Vector3.zero,
                new Vector3(halfScale.x * 2f, halfScale.y, halfScale.z * 2f), material);
        }

        private static void CreateSphere(string name, Transform parent, Vector3 position, Vector3 halfScale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            SetupPart(obj, parent, position, Vector3.zero, halfScale * 2f, material);
        }

        private static void CreateRod(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 direction = end - start;
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            SetupPart(obj, parent, (start + end) * 0.5f, Vector3.zero,
                new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f), material);
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static void SetupPart(GameObject obj, Transform parent, Vector3 position, Vector3 rotation,
            Vector3 scale, Material material)
        {
            SetupTransform(obj, parent, position, rotation, scale);
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            renderer.receiveShadows = true;
        }

        private static void SetupTransform(GameObject obj, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            obj.layer = DecorationLayer;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(rotation);
            obj.transform.localScale = scale;
        }

        private static Mesh CreateTorusMesh(float radius, float tubeRadius, int radialSegments, int tubeSegments)
        {
            int columns = tubeSegments + 1;
            Vector3[] vertices = new Vector3[(radialSegments + 1) * columns];
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = new int[radialSegments * tubeSegments * 6];

            for (int radial = 0; radial <= radialSegments; radial++)
            {
                float u = radial / (float)radialSegments * Mathf.PI * 2f;
                Vector3 center = new(Mathf.Cos(u) * radius, 0f, Mathf.Sin(u) * radius);
                for (int tube = 0; tube <= tubeSegments; tube++)
                {
                    float v = tube / (float)tubeSegments * Mathf.PI * 2f;
                    Vector3 normal = new(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    int index = radial * columns + tube;
                    vertices[index] = center + normal * tubeRadius;
                    normals[index] = normal;
                }
            }

            int triangle = 0;
            for (int radial = 0; radial < radialSegments; radial++)
            {
                for (int tube = 0; tube < tubeSegments; tube++)
                {
                    int a = radial * columns + tube;
                    int b = (radial + 1) * columns + tube;
                    int c = b + 1;
                    int d = a + 1;
                    triangles[triangle++] = a;
                    triangles[triangle++] = b;
                    triangles[triangle++] = c;
                    triangles[triangle++] = a;
                    triangles[triangle++] = c;
                    triangles[triangle++] = d;
                }
            }

            Mesh mesh = new() { name = "Procedural_TurnBalance_Torus" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
