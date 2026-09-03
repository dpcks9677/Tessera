using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Tessera.Games.Yacht;

namespace Tessera.Games.AugmentedYacht
{
    [ExecuteAlways]
    public sealed class ParchmentScoreSheet : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("Parchment Dimensions")]
        [SerializeField] private float sheetWidth = 5.20f;
        [SerializeField] private float sheetHeight = 8.80f;
        [SerializeField] private float sheetThickness = 0.015f;

        [Header("Score Sheet State")]
        [SerializeField] private PlayerScoreData player1Data = new();
        [SerializeField] private PlayerScoreData player2Data = new();

        [Header("Font Settings")]
        [SerializeField] private Font scoreSheetFont;

        private GameObject topLayerObject;
        private Vector3 topLayerBaseLocalPos;
        private Quaternion topLayerBaseLocalRot;
        private RectTransform highResOverlayRect;
        private Canvas cachedCanvas;
        private Camera targetWorldCamera;

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

        public PlayerScoreData Player1 => player1Data;
        public PlayerScoreData Player2 => player2Data;
        public event Action<int, ScoreCategory> ScoreSelected;

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
            if (!Application.isPlaying) return;
            EnsureStructure();
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

            cachedCanvas = GameObject.Find("Pixel Presentation")?.GetComponent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (cachedCanvas == null) return false;

            targetWorldCamera = GameObject.Find("Full Field World Camera")?.GetComponent<Camera>()
                ?? GameObject.Find("Low Resolution World Camera")?.GetComponent<Camera>()
                ?? Camera.main;

            return true;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            SyncOverlayTransform();
        }

        public void SyncOverlayTransform()
        {
            if (highResOverlayRect == null || cachedCanvas == null)
            {
                if (!ResolveExistingElements()) return;
            }
            if (topLayerObject == null) topLayerObject = transform.Find("Layer 5 - Top Game Score Sheet")?.gameObject;
            if (topLayerObject == null || highResOverlayRect == null) return;

            Transform visualT = topLayerObject.transform.Find("Visual Mesh");
            if (visualT == null) return;

            if (targetWorldCamera == null)
            {
                targetWorldCamera = GameObject.Find("Full Field World Camera")?.GetComponent<Camera>()
                    ?? GameObject.Find("Low Resolution World Camera")?.GetComponent<Camera>()
                    ?? Camera.main;
            }
            if (targetWorldCamera == null) return;

            // Visual Mesh 상단 표면(+Y: 0.5f)의 4개 로컬 코너 점을 실제 3D 월드 좌표로 정확히 변환
            Vector3 p0 = visualT.TransformPoint(new Vector3(-0.5f, 0.5f, -0.5f));
            Vector3 p1 = visualT.TransformPoint(new Vector3( 0.5f, 0.5f, -0.5f));
            Vector3 p2 = visualT.TransformPoint(new Vector3( 0.5f, 0.5f,  0.5f));
            Vector3 p3 = visualT.TransformPoint(new Vector3(-0.5f, 0.5f,  0.5f));

            Vector3 s0 = targetWorldCamera.WorldToScreenPoint(p0);
            Vector3 s1 = targetWorldCamera.WorldToScreenPoint(p1);
            Vector3 s2 = targetWorldCamera.WorldToScreenPoint(p2);
            Vector3 s3 = targetWorldCamera.WorldToScreenPoint(p3);

            float minX = Mathf.Min(s0.x, s1.x, s2.x, s3.x);
            float maxX = Mathf.Max(s0.x, s1.x, s2.x, s3.x);
            float minY = Mathf.Min(s0.y, s1.y, s2.y, s3.y);
            float maxY = Mathf.Max(s0.y, s1.y, s2.y, s3.y);

            float width = maxX - minX;
            float height = maxY - minY;
            Vector3 screenCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

            highResOverlayRect.anchorMin = highResOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            highResOverlayRect.pivot = new Vector2(0.5f, 0.5f);
            highResOverlayRect.position = screenCenter;
            highResOverlayRect.sizeDelta = new Vector2(width, height);
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
            cachedCanvas = GameObject.Find("Pixel Presentation")?.GetComponent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (cachedCanvas == null) return;

            Array.Clear(p1ScoreLabels, 0, p1ScoreLabels.Length);
            Array.Clear(p2ScoreLabels, 0, p2ScoreLabels.Length);
            Array.Clear(p1ScoreSlots, 0, p1ScoreSlots.Length);
            Array.Clear(p2ScoreSlots, 0, p2ScoreSlots.Length);
            Array.Clear(p1ScoreButtons, 0, p1ScoreButtons.Length);
            Array.Clear(p2ScoreButtons, 0, p2ScoreButtons.Length);

            // 씬 전체의 모든 구버전 HighRes Score Sheet Overlay 전수 검색 및 즉시 비활성화 후 파괴 (중복 렌더링 원천 방지)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go == null) continue;
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go)) continue;
#endif
                if (go.name == "HighRes Score Sheet Overlay")
                {
                    go.SetActive(false);
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }

            // Screen Space Overlay Canvas 하위에 고해상도 오버레이 루트 생성 (픽셀 필터 완전 바이패스)
            GameObject overlayObj = new("HighRes Score Sheet Overlay", typeof(RectTransform));
            overlayObj.transform.SetParent(cachedCanvas.transform, false);

            highResOverlayRect = overlayObj.GetComponent<RectTransform>();
            highResOverlayRect.anchorMin = highResOverlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            highResOverlayRect.pivot = new Vector2(0.5f, 0.5f);

            targetWorldCamera = GameObject.Find("Full Field World Camera")?.GetComponent<Camera>()
                ?? GameObject.Find("Low Resolution World Camera")?.GetComponent<Camera>()
                ?? Camera.main;

            SyncOverlayTransform();

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

        public void SetPlayerScore(int playerIndex, ScoreCategory category, int score)
        {
            if (playerIndex < 0 || playerIndex > 1 || !YachtScoreCalculator.IsScorable(category)) return;
            PlayerScoreData data = playerIndex == 0 ? player1Data : player2Data;
            int catIdx = (int)category;

            if (catIdx >= 0 && catIdx <= 5)
            {
                data.upperScores[catIdx] = score;
            }
            else if (catIdx >= 7 && catIdx <= 12)
            {
                data.lowerScores[catIdx - 7] = score;
            }

            data.RecalculateTotal();
            RefreshAllScores();
        }

        public void ResetScores()
        {
            player1Data.Reset();
            player2Data.Reset();
            candidateScores.Clear();
            activePlayerIndex = -1;
            selectionEnabled = false;
            RefreshAllScores();
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
            PlayerScoreData data = playerIndex == 0 ? player1Data : player2Data;
            int categoryIndex = (int)category;
            if (categoryIndex >= 0 && categoryIndex <= 5) return data.upperScores[categoryIndex] >= 0;
            if (categoryIndex >= 7 && categoryIndex <= 12) return data.lowerScores[categoryIndex - 7] >= 0;
            return true;
        }

        private void OnScoreSlotClicked(int playerIndex, ScoreCategory category)
        {
            if (!Application.isPlaying || !selectionEnabled || playerIndex != activePlayerIndex) return;
            if (IsScoreFilled(playerIndex, category) || !candidateScores.ContainsKey(category)) return;
            ScoreSelected?.Invoke(playerIndex, category);
        }

        /// <summary>
        /// 증강 효과가 이미 기록된 족보를 덮어쓸 때만 사용하는 명시적 경로.
        /// 일반 점수 기록에서는 추가 턴이 발생하지 않도록 호출부를 분리한다.
        /// </summary>
        public bool OverwriteScoreFromAugment(int playerIndex, ScoreCategory category, int score)
        {
            if (playerIndex < 0 || playerIndex > 1) return false;

            PlayerScoreData data = playerIndex == 0 ? player1Data : player2Data;
            int categoryIndex = (int)category;
            bool hasRecordedScore;

            if (categoryIndex >= 0 && categoryIndex <= 5)
            {
                hasRecordedScore = data.upperScores[categoryIndex] >= 0;
            }
            else if (categoryIndex >= 7 && categoryIndex <= 12)
            {
                hasRecordedScore = data.lowerScores[categoryIndex - 7] >= 0;
            }
            else
            {
                return false;
            }

            if (!hasRecordedScore) return false;
            SetPlayerScore(playerIndex, category, score);
            return true;
        }

        public void RefreshAllScores()
        {
            UpdatePlayerScoreUI(player1Data, p1ScoreLabels, p1BonusProgressText);
            UpdatePlayerScoreUI(player2Data, p2ScoreLabels, null);
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

        private void UpdatePlayerScoreUI(PlayerScoreData data, Text[] labels, Text bonusText)
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
                    labels[i].text = data.upperScores[i] >= 0 ? data.upperScores[i].ToString() : "-";
                    SetLabelColor(labels[i], data.upperScores[i] >= 0 ? inkMain : inkScoreEmpty);
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
                SetLabelColor(labels[6], data.hasBonus ? bonusScoreGold : new Color32(140, 115, 95, 200));
            }

            for (int i = 0; i < 6; i++)
            {
                if (labels[i + 7] != null)
                {
                    labels[i + 7].text = data.lowerScores[i] >= 0 ? data.lowerScores[i].ToString() : "-";
                    SetLabelColor(labels[i + 7], data.lowerScores[i] >= 0 ? inkMain : inkScoreEmpty);
                }
            }

            if (labels[13] != null)
            {
                labels[13].text = data.totalScore.ToString();
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
