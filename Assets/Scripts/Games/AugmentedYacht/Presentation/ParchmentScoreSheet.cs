using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        /// <summary>버건디 스티커 위에 올리는 글자·아이콘 색이다. 양피지 크림톤이라 대비가 산다.</summary>
        private static readonly Color StickerInk = new(0.97f, 0.94f, 0.87f, 1f);

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

        /// <summary>
        /// 변형 증강 러너 스티커 자리다. 족보 행마다 플레이어별로 하나씩 있고 기본은 꺼져 있다.
        /// 두 사람이 각자 Categories 열을 가지므로 두 벌을 동시에 띄운다.
        /// </summary>
        private readonly StickerSlotView[][] stickerSlots = { new StickerSlotView[14], new StickerSlotView[14] };

        /// <summary>
        /// 진행 중인 스티커 연출이다. 같은 칸에 새 연출이 오면 이전 것을 끊는다.
        /// 키는 <see cref="StickerKey"/>가 만드는 플레이어·족보 조합이다.
        /// </summary>
        private readonly Dictionary<int, Coroutine> stickerAnimations = new();

        /// <summary>베이스 색마다 한 장씩 굽는 천 재질이다. 칸 크기가 같으므로 색만으로 구분한다.</summary>
        private readonly Dictionary<uint, Material> stickerMaterials = new();

        /// <summary>펼쳐진 천 한 장의 텍셀 크기다. 화면 픽셀과 1:1이 되도록 굽는다.</summary>
        private Vector2Int stickerPatchPixels = new(59, 15);

        /// <summary>원래 칸의 족보 아이콘과 이름이다. 스티커가 덮으면 감춘다.</summary>
        private readonly Image[][] categoryIcons = { new Image[14], new Image[14] };
        private readonly Text[][] categoryLabels = { new Text[14], new Text[14] };

        /// <summary>스티커 천이 종이 위로 떠오르는 높이와 두께다. 표보다 살짝 튀어나와야 부피가 읽힌다.</summary>
        private const float StickerLift = 0.004f;
        private const float StickerThickness = 0.018f;

        /// <summary>천이 칸 밖으로 삐져나오는 폭이다. 표에 딱 맞으면 인쇄처럼 보여 얹은 느낌이 안 난다.</summary>
        private const float StickerOverhangX = 0.055f;
        private const float StickerOverhangZ = 0.030f;

        /// <summary>
        /// 천 텍스처를 굽는 밀도다. 한 텍셀이 픽셀 필터 화면의 한 픽셀이 되도록 맞춘다.
        ///
        /// 캔버스는 1920 화면 픽셀당 100 캔버스 단위이고(<see cref="CanvasUnitsPerWorldUnit"/>),
        /// 픽셀 필터의 굵은 프리셋(<c>PixelFilterSettings.ResolutionB</c>, 480×270)이 그 1920을 4로
        /// 나눈다. 그래서 4로 나눈 밀도로 굽는다.
        ///
        /// 이보다 촘촘히 구우면 필터가 축소하면서 금테 한 줄이 텍셀 사이에 끼고, 행마다 서브픽셀
        /// 위치가 달라 같은 금테가 행마다 다른 자리에 나타난다. 굵은 쪽 프리셋에 맞췄으므로 가는
        /// 프리셋(640×360)에서는 확대만 일어나 금테가 사라지지 않는다.
        /// </summary>
        private const float StickerPixelsPerUnit = CanvasUnitsPerWorldUnit / 4f;

        // --- 열 배치 ---
        //
        // 표는 가로로 여섯 칸이다:
        //   [P1 아이콘][P1 Categories][P1 점수][P2 점수][P2 Categories][P2 아이콘]
        //
        // 아이콘 섹터와 점수 열은 폭이 고정이고, Categories 예산 하나를 두 사람이 나눠 쓴다.
        // 현재 턴인 쪽이 예산을 전부 가져가므로 반대쪽은 0으로 접힌다. 아이콘 섹터는 접히지
        // 않으므로 상대의 변형 증강 스티커가 아이콘 폭만큼 남아 계속 읽힌다.

        /// <summary>표 좌우 여백. 양피지 안쪽에 안정적으로 앉히는 비율이다.</summary>
        private const float SideMargin = 0.055f;

        /// <summary>본문 폭 대비 아이콘 섹터 한 칸의 비율이다. 22px 아이콘과 좌우 여백이 들어간다.</summary>
        private const float IconSectorRatio = 0.085f;

        /// <summary>본문 폭 대비 점수 열 한 칸의 비율이다.</summary>
        private const float ScoreColumnRatio = 0.215f;

        /// <summary>두 사람이 나눠 쓰는 Categories 예산이다. 나머지 전부를 가져간다.</summary>
        private const float CategoryBudgetRatio = 1f - (IconSectorRatio + ScoreColumnRatio) * 2f;

        private const int ColumnCount = 6;
        private const int ColumnP1Icons = 0;
        private const int ColumnP1Categories = 1;
        private const int ColumnP1Scores = 2;
        private const int ColumnP2Scores = 3;
        private const int ColumnP2Categories = 4;
        private const int ColumnP2Icons = 5;

        /// <summary>접힘·펴짐 전환 시간이다. 스티커 부착 연출과 같은 결로 맞춘다.</summary>
        private const float ExpandSeconds = 0.35f;

        /// <summary>
        /// 이름 열이 이 정도 열려야 글자가 보이기 시작한다. 접히는 쪽은 폭이 줄기 전에 먼저
        /// 사라지고, 펴지는 쪽은 폭이 거의 다 열린 뒤 나타난다.
        /// </summary>
        private const float CategoryFadeIn = 0.35f;
        private const float CategoryFadeFull = 0.90f;

        private readonly RectTransform[] columnRects = new RectTransform[ColumnCount];
        private readonly CanvasGroup[] categoryGroups = new CanvasGroup[2];
        private readonly float[] columnBounds = new float[ColumnCount + 1];

        /// <summary>0이면 P1 이름 열이 펴진 상태, 1이면 P2 쪽이다. 그 사이는 전환 중이다.</summary>
        private float expandT;
        private float expandTarget;
        private Coroutine expandAnimation;

        /// <summary>붙는 연출. 손에 든 천을 비스듬히 내려 눌러 붙이는 동작이다.</summary>
        private const float StickerDropHeight = 0.26f;
        private const float StickerDropSlideX = -0.07f;
        private const float StickerDropSlideZ = 0.04f;
        private const float StickerTiltPitch = 7f;
        private const float StickerTiltRoll = 9f;
        private const float StickerAttachSeconds = 0.42f;

        /// <summary>내려앉은 뒤 한 번 꾹 눌리는 깊이와 시간이다. 낙인 연출도 이 눌림만 쓴다.</summary>
        private const float StickerPressDepth = 0.006f;
        private const float StickerPressSeconds = 0.14f;
        private const float StickerStampSeconds = 0.20f;

        /// <summary>
        /// 스티커 한 장을 이루는 조각들이다.
        ///
        /// 천 자체는 월드 오브젝트라 조명을 받고 픽셀 필터를 통과한다. 표 위에 러너를 얹은 느낌이
        /// 나야 하므로 평면 UI 이미지로 두지 않는다. 반대로 아이콘과 이름은 원래 족보 표기와 똑같이
        /// 읽혀야 하므로 <c>CrispUI</c> 레이어에 남겨 픽셀 필터를 비껴간다.
        /// </summary>
        private sealed class StickerSlotView
        {
            public GameObject Patch;
            public MeshRenderer PatchRenderer;
            public Image Icon;
            public Text Label;

            /// <summary>
            /// 천이 제자리에 앉았을 때의 위치·자세·크기다. 연출이 중간에 끊겨도 여기로 되돌린다.
            /// 애니메이션 시작 시점의 값을 기준으로 삼으면, 이전 연출이 끊긴 자세를
            /// 제자리로 착각해 어긋난 채 굳는다. 열이 접히고 펴질 때마다 가로 성분이 갱신되므로
            /// 연출 코루틴도 시작할 때 붙잡지 말고 매 프레임 다시 읽어야 한다.
            /// </summary>
            public Vector3 PatchRestScale = Vector3.one;
            public Vector3 PatchRestPosition;
            public Quaternion PatchRestRotation = Quaternion.identity;
        }

        /// <summary>
        /// 점수 칸 하나의 포인터 출입을 표에 알린다. 칸마다 하나씩 붙는다.
        ///
        /// <see cref="Button.interactable"/>이 꺼져 있어도 포인터 이벤트 자체는 들어오므로 여기서
        /// 직접 걸러 낸다. 이 검사 하나로 상대 열, 이미 채운 칸, 선택 비활성 구간이 모두 빠진다.
        /// 기입 가능 조건은 <see cref="UpdateSlotState"/>가 이미 그 플래그에 넣어 두었다.
        /// </summary>
        private sealed class ScoreSlotHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public ParchmentScoreSheet Sheet;
            public Button Button;
            public int PlayerIndex;
            public ScoreCategory Category;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Sheet == null || Button == null || !Button.interactable) return;
                Sheet.SetHoveredSlot(PlayerIndex, Category);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Sheet == null) return;
                Sheet.ExitHoveredSlot(PlayerIndex, Category);
            }
        }

        /// <summary>
        /// 점수 열 전체의 포인터 출입을 표에 알린다. 열 컨테이너에 하나 붙는다.
        ///
        /// 칸은 족보 행에만 있고 보너스 행·합계 행·헤더 행에는 없다. 그래서 칸만 보고 있으면
        /// 위아래 칸을 오갈 때 그 사이를 지나는 동안 호버가 끊긴다. 열 안에 머물러 있는 한
        /// 마지막 칸을 놓지 않도록, 진짜로 열을 벗어났는지를 이쪽이 판정한다.
        /// </summary>
        private sealed class ScoreColumnHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public ParchmentScoreSheet Sheet;
            public int PlayerIndex;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Sheet != null) Sheet.SetScoreColumnHovered(PlayerIndex, true);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Sheet != null) Sheet.SetScoreColumnHovered(PlayerIndex, false);
            }
        }
        private readonly Dictionary<ScoreCategory, int> candidateScores = new();

        /// <summary>보너스 진행도다. 각자 자기 Categories 열에 들어간다.</summary>
        private readonly Text[] bonusProgressTexts = new Text[2];
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

        /// <summary>
        /// 기입 가능한 칸에 포인터가 들어오거나 나갔다. 나갔을 때는 <c>category</c>가 <c>null</c>이고
        /// <c>playerIndex</c>는 직전까지 가리키던 칸의 것이다. 깃펜 연출(<c>M17-T18</c>)이 듣는다.
        /// </summary>
        public event Action<int, ScoreCategory?> ScoreSlotHoverChanged;

        /// <summary>지금 포인터가 올라와 있는 칸이다. 없으면 <see cref="hoveredPlayerIndex"/>가 -1이다.</summary>
        private int hoveredPlayerIndex = -1;
        private ScoreCategory hoveredCategory;

        /// <summary>포인터가 그 사람의 점수 열 안에 있는지. 칸과 칸 사이에서도 참이다.</summary>
        private readonly bool[] scoreColumnHovered = new bool[2];

        private void SetHoveredSlot(int playerIndex, ScoreCategory category)
        {
            if (hoveredPlayerIndex == playerIndex && hoveredCategory == category) return;
            hoveredPlayerIndex = playerIndex;
            hoveredCategory = category;
            ScoreSlotHoverChanged?.Invoke(playerIndex, category);
        }

        /// <summary>
        /// 이 칸에서 포인터가 나갔다. 열 안에 머물러 있으면 놓지 않는다. 보너스 행·합계 행·헤더 행에는
        /// 칸이 없고 칸 사이에도 틈이 있어서, 그때마다 놓으면 깃펜이 잉크통으로 돌아갔다 온다.
        ///
        /// 인접한 칸으로 옮겨 갈 때 새 칸의 Enter가 먼저 오는 경우가 있어, 지금 기억하는 칸과 다르면
        /// 무시한다. 그러지 않으면 방금 들어온 호버를 지워 버린다.
        /// </summary>
        private void ExitHoveredSlot(int playerIndex, ScoreCategory category)
        {
            if (hoveredPlayerIndex != playerIndex || hoveredCategory != category) return;
            if (scoreColumnHovered[playerIndex]) return;
            ReleaseHoveredSlot(playerIndex, category);
        }

        /// <summary>열 안에 있든 없든 놓는다. 칸이 잠기거나 표가 다시 만들어질 때 쓴다.</summary>
        private void ReleaseHoveredSlot(int playerIndex, ScoreCategory category)
        {
            if (hoveredPlayerIndex != playerIndex || hoveredCategory != category) return;
            hoveredPlayerIndex = -1;
            ScoreSlotHoverChanged?.Invoke(playerIndex, null);
        }

        /// <summary>포인터가 점수 열에 드나들었다. 열을 벗어나는 순간이 곧 깃펜을 놓는 순간이다.</summary>
        private void SetScoreColumnHovered(int playerIndex, bool inside)
        {
            if (playerIndex < 0 || playerIndex > 1) return;
            scoreColumnHovered[playerIndex] = inside;
            if (!inside) ReleaseHoveredSlot(playerIndex, hoveredCategory);
        }

        /// <summary>
        /// 칸 중앙의 월드 좌표와 칸의 월드 폭이다. 칸이 아직 만들어지지 않았거나 레이아웃이 잡히기
        /// 전이면 false다. 열이 접히고 펴지는 동안 폭이 매 프레임 바뀌므로 캐시하지 않는다.
        /// </summary>
        public bool TryGetSlotAnchor(int playerIndex, ScoreCategory category, out Vector3 worldPoint, out float cellWidth)
        {
            worldPoint = default;
            cellWidth = 0f;

            if (playerIndex < 0 || playerIndex > 1) return false;
            int categoryIndex = (int)category;
            Image[] slots = playerIndex == 0 ? p1ScoreSlots : p2ScoreSlots;
            if (categoryIndex < 0 || categoryIndex >= slots.Length) return false;

            Image slot = slots[categoryIndex];
            if (slot == null) return false;

            RectTransform rect = slot.rectTransform;
            cellWidth = rect.rect.width * rect.lossyScale.x;
            if (cellWidth <= 0f) return false;

            worldPoint = rect.position;
            return true;
        }

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
            if (expandAnimation != null)
            {
                StopCoroutine(expandAnimation);
                expandAnimation = null;
            }
            expandT = expandTarget;

            // 칸이 통째로 다시 만들어지므로 붙잡고 있던 호버도 놓는다. 지금 슬롯은 곧 사라진다.
            if (hoveredPlayerIndex >= 0) ReleaseHoveredSlot(hoveredPlayerIndex, hoveredCategory);
            scoreColumnHovered[0] = scoreColumnHovered[1] = false;

            Array.Clear(p1ScoreLabels, 0, p1ScoreLabels.Length);
            Array.Clear(p2ScoreLabels, 0, p2ScoreLabels.Length);
            DestroyStickerPatches();
            for (int p = 0; p < 2; p++)
            {
                Array.Clear(stickerSlots[p], 0, stickerSlots[p].Length);
                Array.Clear(categoryIcons[p], 0, categoryIcons[p].Length);
                Array.Clear(categoryLabels[p], 0, categoryLabels[p].Length);
                bonusProgressTexts[p] = null;
                categoryGroups[p] = null;
            }
            Array.Clear(columnRects, 0, columnRects.Length);
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
            // U (가로 비율): 열 컨테이너 여섯 개가 나눠 가진다. 경계는 ResolveColumnBounds가 정하고
            // 행 요소는 자기 열 안에서 0..1로 앵커된다. 그래서 접힘·펴짐 전환이 컨테이너 여섯 개의
            // 가로 앵커만 건드리면 되고, 행을 하나하나 다시 계산하지 않는다.

            // V (세로 비율): 상단 누름돌(Paperweight) 아래 공간 확보 및 15개 행 완벽 균등 분할
            float mY_Top = 0.070f;    // 상단 누름돌(Paperweight) 여백 7.0% (누름돌 하단과 쾌적한 거리 확보)
            float mY_Bottom = 0.040f; // 하단 푸터 밴드 여백 4.0%
            float hY = 1.0f - (mY_Top + mY_Bottom);
            float rowH_norm = hY / 15f; // 15개 행 (Row 0 ~ 14)

            float VTop(int r) => 1.0f - (mY_Top + r * rowH_norm);
            float VBottom(int r) => 1.0f - (mY_Top + (r + 1) * rowH_norm);

            // 천 바탕은 다 펴진 천 한 장 크기로 굽는다. 칸이 아니라 천이어야 한다 — 천은 오버행만큼
            // 칸보다 크고, 텍스처는 칸이 아니라 천 면에 입혀지므로 칸 크기로 구우면 늘어난다.
            float bodyWidth = 1f - SideMargin * 2f;
            float patchWidth = ((IconSectorRatio + CategoryBudgetRatio) * bodyWidth * sheetWidth) + (StickerOverhangX * 2f);
            float patchHeight = (rowH_norm * sheetHeight) + (StickerOverhangZ * 2f);
            stickerPatchPixels = new Vector2Int(
                Mathf.Max(8, Mathf.RoundToInt(patchWidth * StickerPixelsPerUnit)),
                Mathf.Max(8, Mathf.RoundToInt(patchHeight * StickerPixelsPerUnit)));

            // 1. 전체 폭 밴드는 열 경계와 무관하다. 오버레이 직속으로 두고 먼저 그려 바닥에 깐다.
            float bandLeft = SideMargin;
            float bandRight = 1f - SideMargin;

            for (int r = 1; r < 14; r++)
            {
                if (r == 7) continue;
                if (r % 2 == 0)
                {
                    CreateBox(overlayObj.transform, $"Zebra_{r}", new Vector2(bandLeft, VBottom(r)), new Vector2(bandRight, VTop(r)), Vector2.zero, Vector2.zero, zebraTint);
                }
            }

            CreateBox(overlayObj.transform, "Header_Band", new Vector2(bandLeft, VBottom(0)), new Vector2(bandRight, VTop(0)), Vector2.zero, Vector2.zero, headerBandColor);
            CreateBox(overlayObj.transform, "Bonus_Band", new Vector2(bandLeft, VBottom(7)), new Vector2(bandRight, VTop(7)), Vector2.zero, Vector2.zero, bonusBandColor);
            CreateBox(overlayObj.transform, "Footer_Band", new Vector2(bandLeft, VBottom(14)), new Vector2(bandRight, VTop(14)), Vector2.zero, Vector2.zero, footerBandColor);

            // 2. 열 컨테이너 여섯 개. 이름 열만 접히므로 잘라내기와 알파를 가진다.
            columnRects[ColumnP1Icons] = CreateColumn(overlayObj.transform, "P1_Icons", false);
            columnRects[ColumnP1Categories] = CreateColumn(overlayObj.transform, "P1_Categories", true);
            columnRects[ColumnP1Scores] = CreateColumn(overlayObj.transform, "P1_Scores", false);
            columnRects[ColumnP2Scores] = CreateColumn(overlayObj.transform, "P2_Scores", false);
            columnRects[ColumnP2Categories] = CreateColumn(overlayObj.transform, "P2_Categories", true);
            columnRects[ColumnP2Icons] = CreateColumn(overlayObj.transform, "P2_Icons", false);

            categoryGroups[0] = columnRects[ColumnP1Categories].GetComponent<CanvasGroup>();
            categoryGroups[1] = columnRects[ColumnP2Categories].GetComponent<CanvasGroup>();

            ConfigureScoreColumnHover(columnRects[ColumnP1Scores], 0);
            ConfigureScoreColumnHover(columnRects[ColumnP2Scores], 1);

            Transform[] iconColumns = { columnRects[ColumnP1Icons], columnRects[ColumnP2Icons] };
            Transform[] nameColumns = { columnRects[ColumnP1Categories], columnRects[ColumnP2Categories] };
            Transform[] scoreColumns = { columnRects[ColumnP1Scores], columnRects[ColumnP2Scores] };
            Text[][] scoreLabels = { p1ScoreLabels, p2ScoreLabels };

            // 3. 족보 데이터
            string[] upperNames = { "Aces", "Deuces", "Threes", "Fours", "Fives", "Sixes" };
            string[] upperIcons = { "dice_1", "dice_2", "dice_3", "dice_4", "dice_5", "dice_6" };
            string[] lowerNames = { "Choice", "4 of a Kind", "Full House", "S. Straight", "L. Straight", "Yacht" };
            string[] lowerIcons = { "choice", "4oak", "fullhouse", "s_straight", "l_straight", "yacht" };

            // 4. 족보 행 12개 (Row 1..6 Aces~Sixes, Row 8..13 Choice~Yacht)
            for (int row = 0; row < 12; row++)
            {
                bool upper = row < 6;
                int offset = upper ? row : row - 6;
                int r = upper ? offset + 1 : offset + 8;
                ScoreCategory category = upper
                    ? (ScoreCategory)offset
                    : (ScoreCategory)((int)ScoreCategory.Choice + offset);
                int categoryIndex = (int)category;
                int labelIndex = upper ? offset : offset + 7;
                string iconName = upper ? upperIcons[offset] : lowerIcons[offset];
                string displayName = upper ? upperNames[offset] : lowerNames[offset];

                for (int p = 0; p < 2; p++)
                {
                    // 점수 슬롯 배경 박스
                    ConfigureScoreSlot(CreateBox(scoreColumns[p], $"P{p + 1}_Slot_Box_{r}", new Vector2(0f, VBottom(r)), new Vector2(1f, VTop(r)), new Vector2(3f, 3f), new Vector2(-3f, -3f), slotInsetColor), p, category);

                    categoryIcons[p][categoryIndex] = CreateIcon(iconColumns[p], iconName, VBottom(r), VTop(r), 22f, inkMain);
                    categoryLabels[p][categoryIndex] = CreateLabel(nameColumns[p], $"Label_{category}", fontMain, displayName, new Vector2(0f, VBottom(r)), new Vector2(1f, VTop(r)), new Vector2(8f, 0f), new Vector2(-8f, 0f), 24, FontStyle.Normal, inkMain, NameAlignment(p));

                    scoreLabels[p][labelIndex] = CreateLabel(scoreColumns[p], $"P{p + 1}_Score_Label_{labelIndex}", fontHeader, "-", new Vector2(0f, VBottom(r)), new Vector2(1f, VTop(r)), Vector2.zero, Vector2.zero, 28, FontStyle.Normal, inkMain, TextAnchor.MiddleCenter);

                    // 스티커는 그 행의 표기를 덮는 것이므로 같은 열에서 뒤에 만든다.
                    stickerSlots[p][categoryIndex] = CreateStickerSlot(
                        p, $"P{p + 1}_Sticker_{r}", iconColumns[p], nameColumns[p], VBottom(r), VTop(r), fontMain);
                }
            }

            // 5. Row 0 헤더. 플레이어 이름은 자기 점수 열에, CATEGORIES는 자기 이름 열에 물린다.
            //    헤더가 열 밖에 있으면 열이 접힐 때 제 칸을 잃고 밀려난다.
            p1HeaderText = CreateLabel(scoreColumns[0], "Header_P1", fontHeader, "P1", new Vector2(0f, VBottom(0)), new Vector2(1f, VTop(0)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, playerHeaderGold, TextAnchor.MiddleCenter);
            p2HeaderText = CreateLabel(scoreColumns[1], "Header_P2", fontHeader, "P2", new Vector2(0f, VBottom(0)), new Vector2(1f, VTop(0)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, playerHeaderGold, TextAnchor.MiddleCenter);

            // 6. Row 7 보너스 · Row 14 합계. 라벨은 각자 이름 열에, 값은 각자 점수 열에 들어간다.
            for (int p = 0; p < 2; p++)
            {
                CreateLabel(nameColumns[p], "Header_Categories", fontHeader, "CATEGORIES", new Vector2(0f, VBottom(0)), new Vector2(1f, VTop(0)), new Vector2(8f, 0f), new Vector2(-8f, 0f), 24, FontStyle.Normal, headerTextGold, NameAlignment(p));

                bonusProgressTexts[p] = CreateLabel(nameColumns[p], "Bonus_Progress_Text", fontMain, "Bonus (0/63)", new Vector2(0f, VBottom(7)), new Vector2(1f, VTop(7)), new Vector2(8f, 0f), new Vector2(-8f, 0f), 23, FontStyle.Normal, bonusTextDark, NameAlignment(p));
                scoreLabels[p][6] = CreateLabel(scoreColumns[p], $"P{p + 1}_Score_Label_6", fontHeader, "+35", new Vector2(0f, VBottom(7)), new Vector2(1f, VTop(7)), Vector2.zero, Vector2.zero, 26, FontStyle.Normal, bonusScoreGold, TextAnchor.MiddleCenter);

                CreateLabel(nameColumns[p], "Footer_Total", fontHeader, "TOTAL", new Vector2(0f, VBottom(14)), new Vector2(1f, VTop(14)), new Vector2(8f, 0f), new Vector2(-8f, 0f), 26, FontStyle.Normal, footerTextGold, NameAlignment(p));
                scoreLabels[p][13] = CreateLabel(scoreColumns[p], $"P{p + 1}_Score_Label_13", fontHeader, "0", new Vector2(0f, VBottom(14)), new Vector2(1f, VTop(14)), Vector2.zero, Vector2.zero, 32, FontStyle.Normal, footerScoreGold, TextAnchor.MiddleCenter);
            }

            ApplyColumnLayout(expandT);

            // UI를 다 만든 뒤 레이어와 직렬화 제외를 자식까지 한 번에 적용한다.
            SetLayerRecursively(overlayObj, TesseraLayers.CrispUI);
            MarkDontSaveRecursively(overlayObj);

            RefreshAllScores();
        }

        /// <summary>P2 쪽은 좌우가 뒤집힌 배치라 이름이 아이콘 섹터를 향해 붙어야 대칭이 맞는다.</summary>
        private static TextAnchor NameAlignment(int playerIndex) =>
            playerIndex == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

        /// <summary>
        /// 열 하나를 담는 빈 컨테이너다. 행 요소는 이 안에서 0..1로 앵커되므로 접힘·펴짐이
        /// 이 컨테이너의 가로 앵커 하나로 끝난다.
        ///
        /// 이름 열은 접히는 동안 글자가 점수 열로 삐져나오면 안 되므로 잘라내고, 폭이 줄기 전에
        /// 알파로 먼저 사라진다. 폭만 줄이면 접히는 마지막 순간까지 글자가 뭉개진 채 남는다.
        /// </summary>
        private static RectTransform CreateColumn(Transform parent, string name, bool collapsible)
        {
            GameObject column = new(name, typeof(RectTransform));
            column.transform.SetParent(parent, false);

            RectTransform rect = column.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (collapsible)
            {
                column.AddComponent<RectMask2D>();
                column.AddComponent<CanvasGroup>();
            }
            return rect;
        }

        /// <summary>
        /// 펴짐 계수로 여섯 열의 가로 경계 일곱 개를 구한다.
        /// <paramref name="expandT"/>가 0이면 P1 이름 열이 예산을 전부 가져가고, 1이면 P2 쪽이다.
        /// 아이콘 섹터와 점수 열은 폭이 고정이라 계수와 무관하다.
        /// </summary>
        public static void ResolveColumnBounds(float expandT, float[] bounds)
        {
            if (bounds == null || bounds.Length < ColumnCount + 1) return;

            float t = Mathf.Clamp01(expandT);
            float body = 1f - SideMargin * 2f;
            float icon = IconSectorRatio * body;
            float score = ScoreColumnRatio * body;
            float budget = CategoryBudgetRatio * body;

            bounds[0] = SideMargin;
            bounds[1] = bounds[0] + icon;
            bounds[2] = bounds[1] + budget * (1f - t);
            bounds[3] = bounds[2] + score;
            bounds[4] = bounds[3] + score;
            bounds[5] = bounds[4] + budget * t;
            bounds[6] = 1f - SideMargin;
        }

        private void ApplyColumnLayout(float t)
        {
            ResolveColumnBounds(t, columnBounds);

            for (int c = 0; c < ColumnCount; c++)
            {
                RectTransform rect = columnRects[c];
                if (rect == null) continue;
                rect.anchorMin = new Vector2(columnBounds[c], 0f);
                rect.anchorMax = new Vector2(columnBounds[c + 1], 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            float clamped = Mathf.Clamp01(t);
            if (categoryGroups[0] != null) categoryGroups[0].alpha = CategoryAlpha(1f - clamped);
            if (categoryGroups[1] != null) categoryGroups[1].alpha = CategoryAlpha(clamped);

            UpdateStickerPatchBounds();
        }

        private static float CategoryAlpha(float openRatio) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CategoryFadeIn, CategoryFadeFull, openRatio));

        /// <summary>
        /// 천 조각은 캔버스 밖 월드 오브젝트라 열이 접혀도 저 혼자 따라오지 않는다.
        /// 덮는 범위는 [아이콘 섹터 + 이름 열]이므로 이름 열이 접히면 아이콘 폭만 남는다.
        /// </summary>
        private void UpdateStickerPatchBounds()
        {
            for (int p = 0; p < 2; p++)
            {
                float left = p == 0 ? columnBounds[ColumnP1Icons] : columnBounds[ColumnP2Categories];
                float right = p == 0 ? columnBounds[ColumnP1Scores] : columnBounds[ColumnCount];
                float centerX = (((left + right) * 0.5f) - 0.5f) * sheetWidth;
                float width = ((right - left) * sheetWidth) + (StickerOverhangX * 2f);

                for (int i = 0; i < stickerSlots[p].Length; i++)
                {
                    StickerSlotView slot = stickerSlots[p][i];
                    if (slot?.Patch == null) continue;

                    Vector3 rest = slot.PatchRestPosition;
                    rest.x = centerX;
                    slot.PatchRestPosition = rest;

                    Vector3 scale = slot.PatchRestScale;
                    scale.x = width;
                    slot.PatchRestScale = scale;

                    slot.Patch.transform.localScale = scale;

                    // 연출 중인 천은 코루틴이 매 프레임 제자리를 다시 읽어 쓴다. 여기서 덮으면 튄다.
                    if (!stickerAnimations.ContainsKey(StickerKey(p, i)))
                    {
                        slot.Patch.transform.localPosition = rest;
                    }
                }
            }
        }

        /// <summary>
        /// 이름 열을 이 플레이어 쪽으로 편다. 목표가 그대로면 아무것도 하지 않는다 —
        /// 턴 흐름이 같은 값으로 여러 번 부르기 때문이다.
        /// </summary>
        private void SetExpandTarget(int playerIndex)
        {
            float target = playerIndex == 1 ? 1f : 0f;
            if (Mathf.Approximately(expandTarget, target) && expandAnimation == null) return;
            expandTarget = target;

            // 에디터이거나 비활성일 때는 코루틴이 돌지 않으므로 최종 상태만 맞춘다.
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                if (expandAnimation != null)
                {
                    StopCoroutine(expandAnimation);
                    expandAnimation = null;
                }
                expandT = target;
                ApplyColumnLayout(expandT);
                return;
            }

            if (expandAnimation != null) StopCoroutine(expandAnimation);
            expandAnimation = StartCoroutine(AnimateColumnExpand());
        }

        private IEnumerator AnimateColumnExpand()
        {
            float from = expandT;
            float elapsed = 0f;

            while (elapsed < ExpandSeconds)
            {
                float t = Mathf.Clamp01(elapsed / ExpandSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // 빠르게 벌어지고 끝에서 잦아든다.

                expandT = Mathf.LerpUnclamped(from, expandTarget, eased);
                ApplyColumnLayout(expandT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            expandT = expandTarget;
            ApplyColumnLayout(expandT);
            expandAnimation = null;
        }

        private static int StickerKey(int playerIndex, int categoryIndex) => playerIndex * 14 + categoryIndex;

        /// <summary>
        /// 점수 열을 통째로 포인터 판정 대상으로 만든다. 보이지 않는 판이라 색은 완전히 투명하다.
        ///
        /// 열 컨테이너는 칸들의 부모이므로, 칸 위에 있을 때도 이 판이 함께 들어온 것으로 잡힌다.
        /// 그래서 칸을 벗어나 보너스 행으로 내려가도 열은 계속 들어온 상태로 남고, 열 밖으로 나가야
        /// 비로소 나간 것으로 판정된다. 칸보다 뒤에 있으므로 클릭은 그대로 칸이 받는다.
        /// </summary>
        private void ConfigureScoreColumnHover(RectTransform column, int playerIndex)
        {
            if (column == null) return;

            Image surface = column.gameObject.AddComponent<Image>();
            surface.color = Color.clear;
            surface.raycastTarget = true;

            ScoreColumnHoverRelay relay = column.gameObject.AddComponent<ScoreColumnHoverRelay>();
            relay.Sheet = this;
            relay.PlayerIndex = playerIndex;
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

            ScoreSlotHoverRelay relay = slotObject.AddComponent<ScoreSlotHoverRelay>();
            relay.Sheet = this;
            relay.Button = button;
            relay.PlayerIndex = playerIndex;
            relay.Category = category;

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

        /// <summary>아이콘 섹터 한 칸의 한가운데에 놓는다. 섹터 폭이 아이콘에 딱 맞게 좁다.</summary>
        private static Image CreateIcon(Transform parent, string iconName, float vBottom, float vTop, float size, Color? tint = null)
        {
            GameObject obj = new($"Icon_{iconName}", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            float centerV = (vBottom + vTop) * 0.5f;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, centerV);
            rect.anchorMax = new Vector2(0.5f, centerV);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
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

        /// <summary>
        /// 변형 증강 스티커 자리를 만든다. 천은 월드 오브젝트로, 아이콘과 이름은 CrispUI로 나눈다.
        /// 아이콘은 아이콘 섹터에, 이름은 이름 열에 들어가므로 이름 열이 접히면 아이콘만 남는다.
        /// </summary>
        private StickerSlotView CreateStickerSlot(int playerIndex, string name, Transform iconColumn, Transform nameColumn, float vBottom, float vTop, Font font)
        {
            var view = new StickerSlotView
            {
                Patch = CreateStickerPatch(name, vBottom, vTop),
                Icon = CreateStickerIcon(iconColumn, name),
                Label = CreateStickerLabel(nameColumn, name, vBottom, vTop, font, NameAlignment(playerIndex))
            };

            view.PatchRenderer = view.Patch != null ? view.Patch.GetComponent<MeshRenderer>() : null;
            if (view.Patch != null)
            {
                view.PatchRestScale = view.Patch.transform.localScale;
                view.PatchRestPosition = view.Patch.transform.localPosition;
                view.PatchRestRotation = view.Patch.transform.localRotation;
            }
            SetStickerActive(view, false);
            return view;
        }

        /// <summary>
        /// 표 위에 얹는 천 조각이다. 칸보다 조금 크고 두께가 있어 얹은 부피가 읽힌다.
        /// <see cref="DecorationLayer"/>에 두어 월드 카메라가 그리므로 픽셀 필터를 그대로 통과한다.
        /// </summary>
        private GameObject CreateStickerPatch(string name, float vBottom, float vTop)
        {
            if (topLayerObject == null) return null;

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = $"{name}_Cloth";
            patch.layer = DecorationLayer;
            patch.hideFlags = HideFlags.DontSave;
            patch.transform.SetParent(topLayerObject.transform, false);
            RemoveCollider(patch);

            float centerV = (vBottom + vTop) * 0.5f;

            // 가로는 열이 접히고 펴질 때마다 바뀌므로 ApplyColumnLayout이 잡는다. 여기선 세로만 정한다.
            patch.transform.localPosition = new Vector3(
                0f,
                (sheetThickness * 0.5f) + StickerLift,
                (centerV - 0.5f) * sheetHeight);
            patch.transform.localRotation = Quaternion.identity;
            patch.transform.localScale = new Vector3(
                1f,
                StickerThickness,
                ((vTop - vBottom) * sheetHeight) + (StickerOverhangZ * 2f));

            return patch;
        }

        /// <summary>원래 칸의 족보 아이콘과 같은 자리·크기다.</summary>
        private static Image CreateStickerIcon(Transform parent, string name)
        {
            GameObject obj = new($"{name}_Icon", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(22f, 22f);

            Image image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        /// <summary>원래 칸의 족보 이름과 같은 자리·크기다. 버건디 천 위라 글자색만 밝게 바꾼다.</summary>
        private static Text CreateStickerLabel(Transform parent, string name, float vBottom, float vTop, Font font, TextAnchor alignment)
        {
            GameObject obj = new($"{name}_Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, vBottom);
            rect.anchorMax = new Vector2(1f, vTop);
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);

            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = 24;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = StickerInk;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 변형 증강 스티커를 칸에 붙입니다. 바탕은 색상코드로 그 자리에서 굽고,
        /// 그 위에 증강 아이콘과 이름을 원래 칸과 같은 배치로 올립니다.
        /// </summary>
        public void SetSticker(int playerIndex, ScoreCategory category, Color32 baseColor, Color32 borderColor, Sprite icon, string label)
        {
            StickerSlotView slot = StickerSlot(playerIndex, category);
            if (slot == null) return;

            if (slot.PatchRenderer != null) slot.PatchRenderer.sharedMaterial = ResolveStickerMaterial(baseColor, borderColor);

            // 원래 족보 표기는 감춘다. 천이 그 위를 덮었는데 글자만 뚫고 나오면 겹쳐 읽힌다.
            SetCategoryRowVisible(playerIndex, category, false);

            AlignStickerIcon(slot, playerIndex, category);
            slot.Icon.sprite = icon;
            slot.Icon.color = icon != null ? StickerInk : new Color(1f, 1f, 1f, 0f);
            slot.Icon.enabled = icon != null;

            slot.Label.text = label ?? string.Empty;
            ResetStickerTransform(slot);
            SetStickerActive(slot, true);
        }

        /// <summary>스티커 아이콘을 원래 족보 아이콘과 같은 자리에 맞춘다.</summary>
        private void AlignStickerIcon(StickerSlotView slot, int playerIndex, ScoreCategory category)
        {
            Image origin = CategoryIcon(playerIndex, category);
            if (origin == null || slot.Icon == null) return;

            var source = origin.transform as RectTransform;
            var target = slot.Icon.transform as RectTransform;
            if (source == null || target == null) return;

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
        }

        private void SetCategoryRowVisible(int playerIndex, ScoreCategory category, bool visible)
        {
            if (playerIndex < 0 || playerIndex > 1) return;
            int index = (int)category;
            if (index < 0 || index >= categoryIcons[playerIndex].Length) return;
            if (categoryIcons[playerIndex][index] != null) categoryIcons[playerIndex][index].enabled = visible;
            if (categoryLabels[playerIndex][index] != null) categoryLabels[playerIndex][index].enabled = visible;
        }

        private Image CategoryIcon(int playerIndex, ScoreCategory category)
        {
            if (playerIndex < 0 || playerIndex > 1) return null;
            int index = (int)category;
            return index >= 0 && index < categoryIcons[playerIndex].Length ? categoryIcons[playerIndex][index] : null;
        }

        private static void SetStickerActive(StickerSlotView slot, bool active)
        {
            if (slot == null) return;
            if (slot.Patch != null) slot.Patch.SetActive(active);
            if (slot.Icon != null) slot.Icon.gameObject.SetActive(active);
            if (slot.Label != null) slot.Label.gameObject.SetActive(active);
        }

        /// <summary>
        /// 한 칸의 스티커를 뗍니다. 표시가 실제로 바뀐 칸만 건드려야 합니다.
        /// 갱신 때마다 전부 뗐다 붙이면 방금 시작한 부착 연출이 곧바로 끊깁니다.
        /// </summary>
        public void ClearSticker(int playerIndex, ScoreCategory category)
        {
            StickerSlotView slot = StickerSlot(playerIndex, category);
            if (slot == null) return;

            int key = StickerKey(playerIndex, (int)category);
            if (stickerAnimations.TryGetValue(key, out Coroutine running))
            {
                if (running != null) StopCoroutine(running);
                stickerAnimations.Remove(key);
            }

            ResetStickerTransform(slot);
            SetStickerActive(slot, false);
            SetCategoryRowVisible(playerIndex, category, true);
        }

        /// <summary>표를 다시 만들 때 이전 천 조각을 걷어낸다. 오버레이와 달리 종이 레이어의 자식이다.</summary>
        private void DestroyStickerPatches()
        {
            for (int p = 0; p < stickerSlots.Length; p++)
            {
                for (int i = 0; i < stickerSlots[p].Length; i++)
                {
                    GameObject patch = stickerSlots[p][i]?.Patch;
                    if (patch == null) continue;
                    if (Application.isPlaying) Destroy(patch);
                    else DestroyImmediate(patch);
                }
            }
        }

        /// <summary>스티커 자리를 돌려줍니다. 없으면 null입니다.</summary>
        private StickerSlotView StickerSlot(int playerIndex, ScoreCategory category)
        {
            if (playerIndex < 0 || playerIndex > 1) return null;
            int index = (int)category;
            return index >= 0 && index < stickerSlots[playerIndex].Length ? stickerSlots[playerIndex][index] : null;
        }

        /// <summary>그 칸에 스티커 자리가 준비돼 있는지 봅니다.</summary>
        public bool HasStickerSlot(int playerIndex, ScoreCategory category) => StickerSlot(playerIndex, category) != null;

        /// <summary>
        /// 천 재질이다. 조명을 받아야 부피가 읽히므로 Lit 셰이더를 쓰고, 광택은 천답게 낮춘다.
        /// 그림자 바깥 두 귀퉁이만 비므로 알파 컷아웃으로 잘라 낸다.
        /// </summary>
        private Material ResolveStickerMaterial(Color32 baseColor, Color32 borderColor)
        {
            uint key = ((uint)baseColor.r << 24) | ((uint)baseColor.g << 16) | ((uint)baseColor.b << 8) | borderColor.r;
            if (stickerMaterials.TryGetValue(key, out Material cached) && cached != null) return cached;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Texture2D texture = AugmentStickerTexture.Create(
                baseColor, borderColor, stickerPatchPixels.x, stickerPatchPixels.y);

            var material = new Material(litShader) { name = "Augment Sticker Cloth" };
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.06f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            material.EnableKeyword("_ALPHATEST_ON");
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.5f);
            material.renderQueue = (int)RenderQueue.AlphaTest;

            stickerMaterials[key] = material;
            return material;
        }

        /// <summary>`S0` 부착. 천이 비스듬히 내려와 칸 위에 눌러 붙습니다.</summary>
        public void PlayStickerAttach(int playerIndex, ScoreCategory category) => PlaySticker(playerIndex, category, attach: true);

        /// <summary>`S2` 낙인. 그 칸으로 점수를 확정한 순간 천이 한 번 꾹 눌립니다.</summary>
        public void PlayStickerStamp(int playerIndex, ScoreCategory category) => PlaySticker(playerIndex, category, attach: false);

        private void PlaySticker(int playerIndex, ScoreCategory category, bool attach)
        {
            StickerSlotView slot = StickerSlot(playerIndex, category);
            if (slot?.Patch == null || !slot.Patch.activeSelf) return;

            int key = StickerKey(playerIndex, (int)category);
            if (stickerAnimations.TryGetValue(key, out Coroutine running) && running != null) StopCoroutine(running);

            // 에디터이거나 비활성일 때는 코루틴이 돌지 않으므로 최종 상태만 맞춘다.
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                ResetStickerTransform(slot);
                stickerAnimations.Remove(key);
                return;
            }

            stickerAnimations[key] = StartCoroutine(
                attach ? AnimateStickerAttach(slot, key) : AnimateStickerPress(slot, key, StickerStampSeconds));
        }

        /// <summary>
        /// 천을 비스듬히 들고 내려와 칸 위에 얹는 동작이다. 크기는 건드리지 않는다.
        /// 위에서 떨어져 기울기가 펴지며 닿고, 닿은 뒤 한 번 꾹 눌러 마무리한다.
        /// </summary>
        private IEnumerator AnimateStickerAttach(StickerSlotView slot, int key)
        {
            Transform patch = slot.Patch.transform;
            var dropOffset = new Vector3(StickerDropSlideX, StickerDropHeight, StickerDropSlideZ);

            // 글자와 아이콘은 천이 거의 닿을 때쯤 드러나야 같이 내려온 것처럼 보인다.
            SetStickerAlpha(slot, 0f);

            float elapsed = 0f;
            while (elapsed < StickerAttachSeconds)
            {
                if (slot.Patch == null) yield break;

                float t = Mathf.Clamp01(elapsed / StickerAttachSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // 빠르게 떨어지고 끝에서 잦아든다.

                // 제자리는 매 프레임 다시 읽는다. 연출 도중에 열이 접히거나 펴지면 자리가 움직인다.
                Vector3 rest = slot.PatchRestPosition;
                Quaternion restRotation = slot.PatchRestRotation;
                Quaternion tilt = restRotation * Quaternion.Euler(StickerTiltPitch, 0f, StickerTiltRoll);

                patch.localPosition = Vector3.LerpUnclamped(rest + dropOffset, rest, eased);
                patch.localRotation = Quaternion.SlerpUnclamped(tilt, restRotation, eased);
                SetStickerAlpha(slot, Mathf.InverseLerp(0.55f, 0.95f, t));

                elapsed += Time.deltaTime;
                yield return null;
            }

            patch.localPosition = slot.PatchRestPosition;
            patch.localRotation = slot.PatchRestRotation;
            SetStickerAlpha(slot, 1f);

            yield return AnimateStickerPress(slot, key, StickerPressSeconds);
        }

        /// <summary>천을 한 번 꾹 눌렀다 놓는다. 닿는 순간의 마무리이자 낙인 연출이다.</summary>
        private IEnumerator AnimateStickerPress(StickerSlotView slot, int key, float duration)
        {
            Transform patch = slot.Patch.transform;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (slot.Patch == null) yield break;

                float t = Mathf.Clamp01(elapsed / duration);
                float press = Mathf.Sin(t * Mathf.PI); // 눌렸다가 제자리로 돌아온다.
                patch.localPosition = slot.PatchRestPosition + (Vector3.down * (StickerPressDepth * press));

                elapsed += Time.deltaTime;
                yield return null;
            }

            ResetStickerTransform(slot);
            stickerAnimations.Remove(key);
        }

        private static void SetStickerAlpha(StickerSlotView slot, float alpha)
        {
            SetGraphicAlpha(slot.Icon, alpha);
            SetGraphicAlpha(slot.Label, alpha);
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void ResetStickerTransform(StickerSlotView slot)
        {
            if (slot == null) return;
            if (slot.Patch != null)
            {
                slot.Patch.transform.localScale = slot.PatchRestScale;
                slot.Patch.transform.localPosition = slot.PatchRestPosition;
                slot.Patch.transform.localRotation = slot.PatchRestRotation;
            }
            if (slot.Icon != null) slot.Icon.color = slot.Icon.sprite != null ? StickerInk : new Color(1f, 1f, 1f, 0f);
            if (slot.Label != null) slot.Label.color = StickerInk;
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

        /// <summary>
        /// 현재 턴인 쪽으로 표를 돌린다. 그 사람 이름 열이 펴지고 반대쪽은 접힌다.
        /// <paramref name="playerIndex"/>가 -1이면 아무도 활성이 아닌 상태이므로 열은 그대로 둔다.
        /// </summary>
        public void SetActivePlayer(int playerIndex, bool canSelectScore)
        {
            activePlayerIndex = playerIndex >= 0 && playerIndex <= 1 ? playerIndex : -1;
            selectionEnabled = canSelectScore;
            if (activePlayerIndex >= 0) SetExpandTarget(activePlayerIndex);
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
            if (activePlayerIndex >= 0) SetExpandTarget(activePlayerIndex);
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
            UpdatePlayerScoreUI(players[0], p1ScoreLabels, bonusProgressTexts[0]);
            UpdatePlayerScoreUI(players[1], p2ScoreLabels, bonusProgressTexts[1]);
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

            // 포인터가 올라와 있는 채로 칸이 잠길 수 있다. 점수를 확정한 직후가 그렇다.
            // 이때 Exit는 오지 않으므로 여기서 놓아 주지 않으면 호버가 영영 남는다.
            if (!button.interactable) ReleaseHoveredSlot(playerIndex, category);
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
