using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Tessera.Core;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    [ExecuteAlways]
    public sealed class ParchmentScoreSheet : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        private const string OverlayName = "HighRes Score Sheet Overlay";

        /// <summary>월드 1단위당 캔버스 좌표 단위. 폰트 크기를 픽셀 감각 그대로 쓰기 위한 배율이다.</summary>
        private const float CanvasUnitsPerWorldUnit = 100f;

        /// <summary>종이 표면과 캔버스 사이 z-fighting을 피하기 위한 최소 간격.</summary>
        private const float OverlayLift = 0.004f;

        [Header("Parchment Dimensions")]
        [SerializeField] private float sheetWidth = 5.20f;
        [SerializeField] private float sheetHeight = 8.80f;
        [SerializeField] private float sheetThickness = 0.015f;

        [Header("Font Settings")]
        [SerializeField] private Font scoreSheetFont;

        private GameObject topLayerObject;
        private Vector3 topLayerBaseLocalPos;
        private Quaternion topLayerBaseLocalRot;
        private RectTransform highResOverlayRect;

        private readonly Text[] p1ScoreLabels = new Text[14];
        private readonly Text[] p2ScoreLabels = new Text[14];
        private readonly Image[] p1ScoreSlots = new Image[14];
        private readonly Image[] p2ScoreSlots = new Image[14];
        private readonly Button[] p1ScoreButtons = new Button[14];
        private readonly Button[] p2ScoreButtons = new Button[14];
        private readonly Dictionary<ScoreCategory, int> candidateScores = new();
        private Text p1BonusProgressText;
        private Text p1HeaderText;
        private Text p2HeaderText;
        private int activePlayerIndex = -1;
        private bool selectionEnabled;

        /// <summary>
        /// 표에 그릴 점수다. 권위 상태를 가리키는 읽기 전용 뷰이며 이 컴포넌트는 읽기만 한다.
        ///
        /// 예전에는 <c>PlayerScoreData</c> 두 개를 직렬화 필드로 들고 그것을 권위에 넘겼다.
        /// 그래서 UI 컴포넌트가 권위 데이터의 저장소를 겸했고, 점수를 누가 바꿨는지 추적할 수 없었다.
        /// 이제 소유자는 <c>LocalGameAuthority</c> 하나이고 여기는 <see cref="BindPlayers"/>로 뷰만 받는다.
        ///
        /// 초깃값은 빈 점수표다. 편집 모드와 게임 시작 전에도 표가 그려져야 하기 때문이다.
        /// </summary>
        private IReadOnlyList<IReadOnlyPlayerScoreData> players =
            new IReadOnlyPlayerScoreData[] { new PlayerScoreData(), new PlayerScoreData() };

        public event Action<int, ScoreCategory> ScoreSelected;

        /// <summary>권위 세션이 만들어진 뒤 그 점수표 뷰를 연결한다.</summary>
        public void BindPlayers(IReadOnlyList<IReadOnlyPlayerScoreData> authorityPlayers)
        {
            if (authorityPlayers == null || authorityPlayers.Count < 2) return;
            players = authorityPlayers;
            RefreshAllScores();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tessera/Rebuild Score Sheet")]
        public static void RebuildScoreSheetMenuItem()
        {
            var sheets = UnityEngine.Object.FindObjectsByType<ParchmentScoreSheet>(FindObjectsSortMode.None);
            foreach (var s in sheets)
            {
                if (s != null)
                {
                    s.Build3DLayeredParchments(true);
                    s.RefreshAllScores();
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
#endif

        public static ParchmentScoreSheet Create(Transform parent, Vector3 worldPosition, Vector3? scale = null)
        {
            GameObject sheetRoot = new("3D Layered Parchment Score Sheet");
            sheetRoot.layer = DecorationLayer;
            sheetRoot.transform.SetParent(parent, false);
            sheetRoot.transform.position = worldPosition;
            sheetRoot.transform.rotation = Quaternion.identity;
            sheetRoot.transform.localScale = scale ?? Vector3.one;

            ParchmentScoreSheet comp = sheetRoot.AddComponent<ParchmentScoreSheet>();
            comp.Build3DLayeredParchments(true);
            return comp;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            EnsureStructure();
        }

        private void OnEnable()
        {
            // 에디터에서도 만든다. 오버레이는 DontSave라 씬을 더럽히지 않으므로,
            // 종이를 배치하는 동안 표가 보이는 편이 낫다.
            EnsureStructure();
            if (!Application.isPlaying) RefreshAllScores();
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            EnsureStructure();
            RefreshAllScores();
        }

        public void EnsureStructure()
        {
            if (!ResolveExistingElements())
            {
                Build3DLayeredParchments(true);
            }
            else
            {
                BuildHighResScoreSheetUI();
            }
        }

        public bool ResolveExistingElements()
        {
            if (transform.childCount < 5) return false;

            topLayerObject = transform.Find("Layer 5 - Top Game Score Sheet")?.gameObject;
            if (topLayerObject == null) return false;

            topLayerBaseLocalPos = new Vector3(0f, sheetThickness * 3.5f + 0.08f, 0f);
            topLayerBaseLocalRot = Quaternion.identity;

            // 오버레이는 이제 이 컴포넌트의 자식이다. 씬 전역 캔버스에 의존하지 않는다.
            highResOverlayRect = topLayerObject.transform.Find(OverlayName) as RectTransform;

            return true;
        }

        /// <summary>
        /// 이전 구조가 남긴 오버레이를 걷어낸다.
        ///
        /// 예전에는 이 오버레이가 씬 전역의 "Pixel Presentation" 캔버스 아래에 있었고,
        /// 이제는 이 컴포넌트의 자식이다. 두 위치를 모두 훑어야 중복 렌더링이 남지 않는다.
        /// </summary>
        private void DestroyStrayOverlays()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go == null || go.name != OverlayName) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
                go.SetActive(false);
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 월드 스페이스 캔버스의 이벤트 기준 카메라. 픽셀 필터를 거치지 않는 전용 카메라다.
        /// 아직 없으면 null을 돌려주고, 컨트롤러가 만든 뒤 <see cref="BindEventCamera"/>로 붙인다.
        /// </summary>
        private static Camera ResolveCrispUiCamera()
        {
            return GameObject.Find("Crisp UI Camera")?.GetComponent<Camera>();
        }

        /// <summary>컨트롤러가 Crisp UI 카메라를 만든 뒤 호출해 캔버스에 연결한다.</summary>
        public void BindEventCamera(Camera eventCamera)
        {
            if (highResOverlayRect == null) return;

            Canvas worldCanvas = highResOverlayRect.GetComponent<Canvas>();
            if (worldCanvas != null) worldCanvas.worldCamera = eventCamera;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
            }
        }

        /// <summary>
        /// 오버레이를 씬 직렬화 대상에서 뺀다.
        ///
        /// 이 UI는 <see cref="EnsureStructure"/>가 로드 때마다 다시 만든다. 에디터에서도 만들어
        /// 종이를 배치하는 동안 표가 보이게 하되, 씬 파일과 프리팹에는 남기지 않는다.
        /// hideFlags는 자식에게 상속되지 않으므로 재귀로 적용해야 한다.
        /// </summary>
        private static void MarkDontSaveRecursively(GameObject target)
        {
            target.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                MarkDontSaveRecursively(target.transform.GetChild(i).gameObject);
            }
        }

        public void Build3DLayeredParchments(bool force = true)
        {
            if (!force && ResolveExistingElements())
            {
                RefreshAllScores();
                return;
            }

            ClearChildren();

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Texture2D baseTex = null;
            Texture2D burntTex = null;
            Texture2D warmTex = null;

#if UNITY_EDITOR
            baseTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Parchment/parchment_base.png");
            burntTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Parchment/parchment_burnt_edge.png");
            warmTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Parchment/parchment_warm_sand.png");
#endif

            // Layer 1 (Bottom): -5.2° 회전 / 짙은 에크루 톤 / 그을린 모서리 (테이블 바닥 밀착)
            CreateParchmentLayer("Layer 1 - Bottom Burnt Parchment", -5.2f, new Vector3(-0.08f, 0.000f, -0.05f), 1.14f, 1.12f,
                burntTex, new Color(0.82f, 0.74f, 0.60f), 0.08f, litShader);

            // Layer 2: +3.6° 회전 / 웜 샌드 톤 (테이블 바닥 밀착)
            CreateParchmentLayer("Layer 2 - Warm Sand Parchment", 3.6f, new Vector3(0.06f, sheetThickness * 0.7f, 0.03f), 1.10f, 1.08f,
                warmTex ?? baseTex, new Color(0.88f, 0.81f, 0.69f), 0.09f, litShader);

            // Layer 3: -2.4° 회전 / 빈티지 에이지드 톤 (테이블 바닥 밀착)
            CreateParchmentLayer("Layer 3 - Aged Parchment", -2.4f, new Vector3(-0.04f, sheetThickness * 1.4f, -0.02f), 1.06f, 1.05f,
                baseTex, new Color(0.90f, 0.84f, 0.73f), 0.10f, litShader);

            // Layer 4: +1.2° 회전 / 밝은 크림 톤 (테이블 바닥 밀착)
            CreateParchmentLayer("Layer 4 - Cream Parchment", 1.2f, new Vector3(0.03f, sheetThickness * 2.1f, 0.01f), 1.03f, 1.02f,
                baseTex, new Color(0.94f, 0.89f, 0.79f), 0.11f, litShader);

            // Layer 5 (Top - Stable Score Sheet): 바닥과 평행(0°) 유지 + 완벽한 정적 고정
            topLayerBaseLocalPos = new Vector3(0f, sheetThickness * 3.5f + 0.08f, 0f);
            topLayerBaseLocalRot = Quaternion.identity;

            topLayerObject = new GameObject("Layer 5 - Top Game Score Sheet");
            topLayerObject.layer = DecorationLayer;
            topLayerObject.transform.SetParent(transform, false);
            topLayerObject.transform.localPosition = topLayerBaseLocalPos;
            topLayerObject.transform.localRotation = topLayerBaseLocalRot;
            topLayerObject.transform.localScale = Vector3.one;

            // 자식 1: 3D 비주얼 메쉬 큐브
            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "Visual Mesh";
            visualCube.layer = DecorationLayer;
            visualCube.transform.SetParent(topLayerObject.transform, false);
            visualCube.transform.localPosition = Vector3.zero;
            visualCube.transform.localRotation = Quaternion.identity;
            visualCube.transform.localScale = new Vector3(sheetWidth, sheetThickness, sheetHeight);
            RemoveCollider(visualCube);

            Material mat = new(litShader)
            {
                name = "Layer 5 Material",
                color = Color.white
            };
            if (baseTex != null)
            {
                mat.mainTexture = baseTex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
            }
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0f);
            ApplyRendererSettings(visualCube, mat);

            // 픽셀 필터를 거치지 않는 고해상도(High-Res) Screen Overlay UI 구축
            BuildHighResScoreSheetUI();
            RefreshAllScores();
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private GameObject CreateParchmentLayer(string layerName, float rotY, Vector3 localPos, float scaleXMul, float scaleZMul,
            Texture2D texture, Color tintColor, float smoothness, Shader shader)
        {
            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = layerName;
            layer.layer = DecorationLayer;
            layer.transform.SetParent(transform, false);
            layer.transform.localPosition = localPos;
            layer.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            layer.transform.localScale = new Vector3(sheetWidth * scaleXMul, sheetThickness, sheetHeight * scaleZMul);
            RemoveCollider(layer);

            Material mat = new(shader)
            {
                name = $"{layerName} Material",
                color = tintColor
            };
            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            ApplyRendererSettings(layer, mat);

            return layer;
        }

        public void BuildHighResScoreSheetUI()
        {
            Array.Clear(p1ScoreLabels, 0, p1ScoreLabels.Length);
            Array.Clear(p2ScoreLabels, 0, p2ScoreLabels.Length);
            Array.Clear(p1ScoreSlots, 0, p1ScoreSlots.Length);
            Array.Clear(p2ScoreSlots, 0, p2ScoreSlots.Length);
            Array.Clear(p1ScoreButtons, 0, p1ScoreButtons.Length);
            Array.Clear(p2ScoreButtons, 0, p2ScoreButtons.Length);

            DestroyStrayOverlays();

            // 종이 최상단 레이어의 자식으로 월드 스페이스 캔버스를 만든다(M9.5).
            // 계층 관계가 곧 위치이므로, 종이를 옮기거나 돌리면 표가 그대로 따라온다.
            // 픽셀 필터는 CrispUI 레이어를 월드 카메라에서 제외하는 방식으로 우회한다.
            if (topLayerObject == null) topLayerObject = transform.Find("Layer 5 - Top Game Score Sheet")?.gameObject;
            if (topLayerObject == null) return;

            GameObject overlayObj = new(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            // 이 UI는 로드할 때마다 다시 만들어진다. 씬에 저장하면 저장할 때마다 대량 diff가 난다.
            overlayObj.hideFlags = HideFlags.DontSave;
            overlayObj.transform.SetParent(topLayerObject.transform, false);

            highResOverlayRect = overlayObj.GetComponent<RectTransform>();
            highResOverlayRect.anchorMin = highResOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            highResOverlayRect.pivot = new Vector2(0.5f, 0.5f);

            // 캔버스 좌표를 픽셀 단위로 쓰고 0.01 배로 줄여 월드 단위에 맞춘다.
            // 폰트 크기와 정규화 앵커 레이아웃을 그대로 유지하기 위한 관례다.
            highResOverlayRect.sizeDelta = new Vector2(sheetWidth, sheetHeight) * CanvasUnitsPerWorldUnit;
            highResOverlayRect.localScale = Vector3.one / CanvasUnitsPerWorldUnit;
            highResOverlayRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            highResOverlayRect.localPosition = new Vector3(0f, sheetThickness * 0.5f + OverlayLift, 0f);

            Canvas worldCanvas = overlayObj.GetComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = ResolveCrispUiCamera();

            SetLayerRecursively(overlayObj, TesseraLayers.CrispUI);

            // 폰트 로드 (Alagard)
            Font fontMain = scoreSheetFont;
#if UNITY_EDITOR
            if (fontMain == null)
            {
                fontMain = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/alagard.ttf")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/m6x11.ttf");
            }
#endif
            if (fontMain == null) fontMain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font fontHeader = fontMain;

            // 선명하고 묵직한 도트 잉크 색상 팔레트
            Color headerBandColor = new Color32(32, 18, 10, 245);    // 딥 에보니 골드 밴드
            Color headerTextGold = new Color32(245, 225, 195, 255);
            Color playerHeaderGold = new Color32(245, 180, 70, 255);

            Color footerBandColor = new Color32(32, 18, 10, 245);
            Color footerTextGold = new Color32(245, 225, 195, 255);
            Color footerScoreGold = new Color32(250, 190, 75, 255);

            Color bonusBandColor = new Color32(195, 160, 120, 95);
            Color bonusTextDark = new Color32(35, 20, 12, 255);
            Color bonusScoreGold = new Color32(185, 95, 20, 255);

            Color zebraTint = new Color32(140, 105, 75, 30);
            Color slotInsetColor = new Color32(150, 115, 80, 55);

            Color inkMain = new Color32(28, 16, 8, 255); // 100% 선명한 딥 챠콜 브라운 잉크

            // --- 정규화 비율 좌표계 (Normalized Anchor System) ---
            // U (가로 비율): [ 족보명/아이콘(52%) | P1 점수(24%) | P2 점수(24%) ]
            float mX = 0.055f; // 좌우 여백 5.5% (양피지 내부 안정적 안착)
            float wX = 1.0f - mX * 2f;
            float u0 = mX;
            float u1 = u0 + wX * 0.52f;
            float u2 = u1 + wX * 0.24f;
            float u3 = u2 + wX * 0.24f;

            // V (세로 비율): 상단 누름돌(Paperweight) 아래 공간 확보 및 15개 행 완벽 균등 분할
            float mY_Top = 0.070f;    // 상단 누름돌(Paperweight) 여백 7.0% (누름돌 하단과 쾌적한 거리 확보)
            float mY_Bottom = 0.040f; // 하단 푸터 밴드 여백 4.0%
            float hY = 1.0f - (mY_Top + mY_Bottom);
            float rowH_norm = hY / 15f; // 15개 행 (Row 0 ~ 14)

            float VTop(int r) => 1.0f - (mY_Top + r * rowH_norm);
            float VBottom(int r) => 1.0f - (mY_Top + (r + 1) * rowH_norm);

            // 1. 짝수 행 제브라 틴트 (Row 1..13 중 짝수)
            for (int r = 1; r < 14; r++)
            {
                if (r == 7) continue;
                if (r % 2 == 0)
                {
                    CreateBox(overlayObj.transform, $"Zebra_{r}", new Vector2(u0, VBottom(r)), new Vector2(u3, VTop(r)), Vector2.zero, Vector2.zero, zebraTint);
                }
            }

            // 2. Row 0: Header Band (CATEGORIES, P1, P2) - 빈 칸 없이 1:1 완벽 통합
            CreateBox(overlayObj.transform, "Header_Band", new Vector2(u0, VBottom(0)), new Vector2(u3, VTop(0)), Vector2.zero, Vector2.zero, headerBandColor);
            CreateLabel(overlayObj.transform, "Header_Categories", fontHeader, "CATEGORIES", new Vector2(u0, VBottom(0)), new Vector2(u1, VTop(0)), new Vector2(16f, 0f), new Vector2(-4f, 0f), 24, FontStyle.Normal, headerTextGold, TextAnchor.MiddleLeft);
            p1HeaderText = CreateLabel(overlayObj.transform, "Header_P1", fontHeader, "P1", new Vector2(u1, VBottom(0)), new Vector2(u2, VTop(0)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, playerHeaderGold, TextAnchor.MiddleCenter);
            p2HeaderText = CreateLabel(overlayObj.transform, "Header_P2", fontHeader, "P2", new Vector2(u2, VBottom(0)), new Vector2(u3, VTop(0)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, playerHeaderGold, TextAnchor.MiddleCenter);

            // 3. 족보 데이터
            string[] upperNames = { "Aces", "Deuces", "Threes", "Fours", "Fives", "Sixes" };
            string[] upperIcons = { "dice_1", "dice_2", "dice_3", "dice_4", "dice_5", "dice_6" };
            string[] lowerNames = { "Choice", "4 of a Kind", "Full House", "S. Straight", "L. Straight", "Yacht" };
            string[] lowerIcons = { "choice", "4oak", "fullhouse", "s_straight", "l_straight", "yacht" };

            // 상단 섹션 (Row 1..6: Aces ~ Sixes)
            for (int i = 0; i < 6; i++)
            {
                int r = i + 1;

                // 점수 슬롯 배경 박스
                ConfigureScoreSlot(CreateBox(overlayObj.transform, $"P1_Slot_Box_{r}", new Vector2(u1, VBottom(r)), new Vector2(u2, VTop(r)), new Vector2(3f, 3f), new Vector2(-3f, -3f), slotInsetColor), 0, (ScoreCategory)i);
                ConfigureScoreSlot(CreateBox(overlayObj.transform, $"P2_Slot_Box_{r}", new Vector2(u2, VBottom(r)), new Vector2(u3, VTop(r)), new Vector2(3f, 3f), new Vector2(-3f, -3f), slotInsetColor), 1, (ScoreCategory)i);

                // Col 0: 아이콘 + 족보명
                CreateIcon(overlayObj.transform, upperIcons[i], new Vector2(u0, VBottom(r)), new Vector2(u1, VTop(r)), 22f, inkMain);
                CreateLabel(overlayObj.transform, $"Label_Upper_{i}", fontMain, upperNames[i], new Vector2(u0, VBottom(r)), new Vector2(u1, VTop(r)), new Vector2(44f, 0f), new Vector2(-4f, 0f), 24, FontStyle.Normal, inkMain, TextAnchor.MiddleLeft);

                // Col 1 & 2: 점수 슬롯 라벨
                p1ScoreLabels[i] = CreateLabel(overlayObj.transform, $"P1_Score_Label_{i}", fontHeader, "-", new Vector2(u1, VBottom(r)), new Vector2(u2, VTop(r)), Vector2.zero, Vector2.zero, 28, FontStyle.Normal, inkMain, TextAnchor.MiddleCenter);
                p2ScoreLabels[i] = CreateLabel(overlayObj.transform, $"P2_Score_Label_{i}", fontHeader, "-", new Vector2(u2, VBottom(r)), new Vector2(u3, VTop(r)), Vector2.zero, Vector2.zero, 28, FontStyle.Normal, inkMain, TextAnchor.MiddleCenter);
            }

            // 4. Row 7: Bonus Row
            CreateBox(overlayObj.transform, "Bonus_Band", new Vector2(u0, VBottom(7)), new Vector2(u3, VTop(7)), Vector2.zero, Vector2.zero, bonusBandColor);
            p1BonusProgressText = CreateLabel(overlayObj.transform, "Bonus_Progress_Text", fontMain, "Bonus (0/63)", new Vector2(u0, VBottom(7)), new Vector2(u1, VTop(7)), new Vector2(16f, 0f), new Vector2(-4f, 0f), 23, FontStyle.Normal, bonusTextDark, TextAnchor.MiddleLeft);
            p1ScoreLabels[6] = CreateLabel(overlayObj.transform, "P1_Score_Label_6", fontHeader, "+35", new Vector2(u1, VBottom(7)), new Vector2(u2, VTop(7)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, bonusScoreGold, TextAnchor.MiddleCenter);
            p2ScoreLabels[6] = CreateLabel(overlayObj.transform, "P2_Score_Label_6", fontHeader, "+35", new Vector2(u2, VBottom(7)), new Vector2(u3, VTop(7)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, bonusScoreGold, TextAnchor.MiddleCenter);

            // 5. 하단 섹션 (Row 8..13: Choice ~ Yacht)
            for (int i = 0; i < 6; i++)
            {
                int r = i + 8;

                // 점수 슬롯 배경 박스
                ScoreCategory category = (ScoreCategory)((int)ScoreCategory.Choice + i);
                ConfigureScoreSlot(CreateBox(overlayObj.transform, $"P1_Slot_Box_{r}", new Vector2(u1, VBottom(r)), new Vector2(u2, VTop(r)), new Vector2(3f, 3f), new Vector2(-3f, -3f), slotInsetColor), 0, category);
                ConfigureScoreSlot(CreateBox(overlayObj.transform, $"P2_Slot_Box_{r}", new Vector2(u2, VBottom(r)), new Vector2(u3, VTop(r)), new Vector2(3f, 3f), new Vector2(-3f, -3f), slotInsetColor), 1, category);

                // Col 0: 아이콘 + 족보명
                CreateIcon(overlayObj.transform, lowerIcons[i], new Vector2(u0, VBottom(r)), new Vector2(u1, VTop(r)), 22f, inkMain);
                CreateLabel(overlayObj.transform, $"Label_Lower_{i}", fontMain, lowerNames[i], new Vector2(u0, VBottom(r)), new Vector2(u1, VTop(r)), new Vector2(44f, 0f), new Vector2(-4f, 0f), 24, FontStyle.Normal, inkMain, TextAnchor.MiddleLeft);

                // Col 1 & 2: 점수 슬롯 라벨
                p1ScoreLabels[i + 7] = CreateLabel(overlayObj.transform, $"P1_Score_Label_{i + 7}", fontHeader, "-", new Vector2(u1, VBottom(r)), new Vector2(u2, VTop(r)), Vector2.zero, Vector2.zero, 28, FontStyle.Normal, inkMain, TextAnchor.MiddleCenter);
                p2ScoreLabels[i + 7] = CreateLabel(overlayObj.transform, $"P2_Score_Label_{i + 7}", fontHeader, "-", new Vector2(u2, VBottom(r)), new Vector2(u3, VTop(r)), Vector2.zero, Vector2.zero, 28, FontStyle.Normal, inkMain, TextAnchor.MiddleCenter);
            }

            // 6. Row 14: Footer Band (TOTAL) - 푸터 밴드 내에 TOTAL 및 점수 완벽 통합
            CreateBox(overlayObj.transform, "Footer_Band", new Vector2(u0, VBottom(14)), new Vector2(u3, VTop(14)), Vector2.zero, Vector2.zero, footerBandColor);
            CreateLabel(overlayObj.transform, "Footer_Total", fontHeader, "TOTAL", new Vector2(u0, VBottom(14)), new Vector2(u1, VTop(14)), new Vector2(16f, 0f), new Vector2(-4f, 0f), 26, FontStyle.Normal, footerTextGold, TextAnchor.MiddleLeft);
            p1ScoreLabels[13] = CreateLabel(overlayObj.transform, "P1_Score_Label_13", fontHeader, "0", new Vector2(u1, VBottom(14)), new Vector2(u2, VTop(14)), Vector2.zero, Vector2.zero, 32, FontStyle.Normal, footerScoreGold, TextAnchor.MiddleCenter);
            p2ScoreLabels[13] = CreateLabel(overlayObj.transform, "P2_Score_Label_13", fontHeader, "0", new Vector2(u2, VBottom(14)), new Vector2(u3, VTop(14)), Vector2.zero, Vector2.zero, 32, FontStyle.Normal, footerScoreGold, TextAnchor.MiddleCenter);

            // UI를 다 만든 뒤 레이어와 직렬화 제외를 자식까지 한 번에 적용한다.
            SetLayerRecursively(overlayObj, TesseraLayers.CrispUI);
            MarkDontSaveRecursively(overlayObj);

            RefreshAllScores();
        }

        private void ConfigureScoreSlot(GameObject slotObject, int playerIndex, ScoreCategory category)
        {
            if (slotObject == null) return;

            Image image = slotObject.GetComponent<Image>();
            if (image == null) return;
            image.raycastTarget = true;

            Button button = slotObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(226, 178, 82, 180);
            colors.pressedColor = new Color32(162, 91, 38, 210);
            colors.disabledColor = image.color;
            button.colors = colors;
            button.onClick.AddListener(() => OnScoreSlotClicked(playerIndex, category));

            int categoryIndex = (int)category;
            Image[] slots = playerIndex == 0 ? p1ScoreSlots : p2ScoreSlots;
            Button[] buttons = playerIndex == 0 ? p1ScoreButtons : p2ScoreButtons;
            slots[categoryIndex] = image;
            buttons[categoryIndex] = button;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject box = new(name, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);

            RectTransform rect = box.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image img = box.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return box;
        }

        private static Image CreateIcon(Transform parent, string iconName, Vector2 anchorMin, Vector2 anchorMax, float size, Color? tint = null)
        {
            GameObject obj = new($"Icon_{iconName}", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMin.x, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.anchorMax = new Vector2(anchorMin.x, (anchorMin.y + anchorMax.y) * 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(16f, 0f);
            rect.sizeDelta = new Vector2(size, size);

            Image img = obj.GetComponent<Image>();
#if UNITY_EDITOR
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/Parchment/Icons/{iconName}.png");
            if (sp != null) img.sprite = sp;
#endif
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateLabel(Transform parent, string name, Font font, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text txt = obj.GetComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            Shadow shadow = obj.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return txt;
        }

        private static void SetLabelColor(Text label, Color color)
        {
            if (label == null) return;
            label.color = color;
        }

        public void SetActivePlayer(int playerIndex, bool canSelectScore)
        {
            activePlayerIndex = playerIndex >= 0 && playerIndex <= 1 ? playerIndex : -1;
            selectionEnabled = canSelectScore;
            RefreshAllScores();
        }

        public void ShowCandidateScores(int playerIndex, IReadOnlyDictionary<ScoreCategory, int> scores)
        {
            activePlayerIndex = playerIndex >= 0 && playerIndex <= 1 ? playerIndex : -1;
            candidateScores.Clear();
            if (scores != null)
            {
                foreach (KeyValuePair<ScoreCategory, int> entry in scores)
                {
                    if (YachtScoreCalculator.IsScorable(entry.Key)) candidateScores[entry.Key] = entry.Value;
                }
            }
            selectionEnabled = activePlayerIndex >= 0 && candidateScores.Count > 0;
            RefreshAllScores();
        }

        public void ClearCandidateScores()
        {
            candidateScores.Clear();
            selectionEnabled = false;
            RefreshAllScores();
        }

        public bool IsScoreFilled(int playerIndex, ScoreCategory category)
        {
            if (playerIndex < 0 || playerIndex > 1) return true;
            IReadOnlyPlayerScoreData data = players[playerIndex];
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5) return data.UpperScores[categoryIndex] >= 0;
            if (categoryIndex >= 7 && categoryIndex <= 12) return data.LowerScores[categoryIndex - 7] >= 0;
            return true;
        }

        private void OnScoreSlotClicked(int playerIndex, ScoreCategory category)
        {
            if (!Application.isPlaying || !selectionEnabled || playerIndex != activePlayerIndex) return;
            if (IsScoreFilled(playerIndex, category) || !candidateScores.ContainsKey(category)) return;
            ScoreSelected?.Invoke(playerIndex, category);
        }

        public void RefreshAllScores()
        {
            UpdatePlayerScoreUI(players[0], p1ScoreLabels, p1BonusProgressText);
            UpdatePlayerScoreUI(players[1], p2ScoreLabels, null);
            ApplyCandidatePreviews();
            RefreshInteractionVisuals();
        }

        private void ApplyCandidatePreviews()
        {
            if (activePlayerIndex < 0 || activePlayerIndex > 1) return;
            Text[] labels = activePlayerIndex == 0 ? p1ScoreLabels : p2ScoreLabels;
            Color previewInk = new Color32(27, 111, 122, 255);
            foreach (KeyValuePair<ScoreCategory, int> entry in candidateScores)
            {
                int categoryIndex = (int)entry.Key;
                if (categoryIndex < 0 || categoryIndex >= labels.Length || IsScoreFilled(activePlayerIndex, entry.Key)) continue;
                if (labels[categoryIndex] == null) continue;
                labels[categoryIndex].text = entry.Value.ToString();
                SetLabelColor(labels[categoryIndex], previewInk);
            }
        }

        private void RefreshInteractionVisuals()
        {
            Color inactiveSlot = new Color32(150, 115, 80, 55);
            Color activeSlot = new Color32(223, 172, 78, 105);
            Color activeHeader = new Color32(255, 205, 100, 255);
            Color inactiveHeader = new Color32(170, 135, 90, 220);

            SetLabelColor(p1HeaderText, activePlayerIndex == 0 ? activeHeader : inactiveHeader);
            SetLabelColor(p2HeaderText, activePlayerIndex == 1 ? activeHeader : inactiveHeader);

            foreach (ScoreCategory category in YachtScoreCalculator.ScorableCategories)
            {
                int categoryIndex = (int)category;
                UpdateSlotState(0, category, p1ScoreSlots[categoryIndex], p1ScoreButtons[categoryIndex], inactiveSlot, activeSlot);
                UpdateSlotState(1, category, p2ScoreSlots[categoryIndex], p2ScoreButtons[categoryIndex], inactiveSlot, activeSlot);
            }
        }

        private void UpdateSlotState(int playerIndex, ScoreCategory category, Image slot, Button button, Color inactiveColor, Color activeColor)
        {
            Color desiredColor = playerIndex == activePlayerIndex ? activeColor : inactiveColor;
            if (slot != null) slot.color = desiredColor;
            if (button == null) return;
            ColorBlock colors = button.colors;
            colors.normalColor = desiredColor;
            colors.disabledColor = desiredColor;
            colors.highlightedColor = playerIndex == activePlayerIndex
                ? new Color32(235, 186, 88, 180)
                : inactiveColor;
            colors.pressedColor = new Color32(162, 91, 38, 210);
            button.colors = colors;
            button.interactable = selectionEnabled
                && playerIndex == activePlayerIndex
                && !IsScoreFilled(playerIndex, category)
                && candidateScores.ContainsKey(category);
        }

        private void UpdatePlayerScoreUI(IReadOnlyPlayerScoreData data, Text[] labels, Text bonusText)
        {
            if (labels == null || labels.Length < 14) return;

            Color inkMain = new Color32(28, 16, 8, 255);
            Color inkScoreEmpty = new Color32(120, 95, 75, 230); // 또렷하고 선명한 미드 챠콜 브라운
            Color bonusScoreGold = new Color32(185, 95, 20, 255);
            Color footerScoreGold = new Color32(250, 190, 75, 255);

            for (int i = 0; i < 6; i++)
            {
                if (labels[i] != null)
                {
                    labels[i].text = data.UpperScores[i] >= 0 ? data.UpperScores[i].ToString() : "-";
                    SetLabelColor(labels[i], data.UpperScores[i] >= 0 ? inkMain : inkScoreEmpty);
                }
            }

            int upperSum = data.CalculateUpperSum();
            if (bonusText != null)
            {
                bonusText.text = $"Bonus ({upperSum}/63)";
                SetLabelColor(bonusText, upperSum >= 63 ? bonusScoreGold : new Color32(35, 20, 12, 255));
            }
            if (labels[6] != null)
            {
                labels[6].text = "+35";
                SetLabelColor(labels[6], data.HasBonus ? bonusScoreGold : new Color32(140, 115, 95, 200));
            }

            for (int i = 0; i < 6; i++)
            {
                if (labels[i + 7] != null)
                {
                    labels[i + 7].text = data.LowerScores[i] >= 0 ? data.LowerScores[i].ToString() : "-";
                    SetLabelColor(labels[i + 7], data.LowerScores[i] >= 0 ? inkMain : inkScoreEmpty);
                }
            }

            if (labels[13] != null)
            {
                labels[13].text = data.TotalScore.ToString();
                SetLabelColor(labels[13], footerScoreGold);
            }
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private static void ApplyRendererSettings(GameObject go, Material mat)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = mat;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                mr.receiveShadows = true;
            }
        }
    }
}
