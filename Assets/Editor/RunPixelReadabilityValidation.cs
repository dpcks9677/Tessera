using System.Collections.Generic;
using System.Text;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using Tessera.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Baseline과 Cel을 한 번에 재서 나란히 보고한다(M10.8-T8).
///
/// 사람이 두 번 실행해 눈으로 비교하는 방식이면 비교가 기록으로 남지 않는다. 그래서 한 실행 안에서
/// 연출 방식을 전환하며 같은 지표를 두 번 잰다.
///
/// 두 방식은 렌더 타깃 크기가 다르므로(Baseline 1920x1080, Cel 내부 해상도) 캡처를 모두 가상 격자로
/// 리샘플한 뒤 비교한다. 업스케일 셰이더가 화면에 하는 것과 같은 셀 중심 점 샘플링이다.
/// </summary>
[InitializeOnLoad]
public static class RunPixelReadabilityValidation
{
    private const string ScenePath = "Assets/Scenes/Augmented Dice.unity";
    private const string RequestedKey = "Tessera.PixelReadabilityRequested";
    private const string RunningKey = "Tessera.PixelReadabilityRunning";

    private const double SettleTimeoutSeconds = 12.0;

    private static int phase;
    private static double phaseStartedAt;

    private static Color32[] baselineStill;
    private static Color32[] celStill;
    private static Color32[] rollingPrevious;
    private static float baselineCrawl;
    private static float celCrawl;
    private static Vector2Int gridSize;

    // 캡처는 텍스처 생성과 GPU->CPU 동기 읽기를 함께 일으킨다. 매 틱 하면 프레임이 느려지고,
    // 하필 이 도구가 재는 지표가 "프레임 간 변화율"이라 측정이 측정 대상을 흔든다. 게다가
    // Baseline 쪽 렌더 타깃이 훨씬 커서 편향이 한쪽으로만 생긴다. 고정 간격으로 제한하고
    // 텍스처는 크기별로 재사용한다.
    private const double CaptureIntervalSeconds = 0.1;

    private static double lastCaptureAt;
    private static Texture2D captureBuffer;

    // 연출 방식을 바꾸면 렌더 타깃을 다시 만든다. 그 직후 한두 프레임은 아직 그려지지 않은 내용을
    // 읽어 모든 셀이 달라진 것처럼 보인다. 실제로 Cel 변화율이 100.00%로 나왔다. 워밍업으로 버린다.
    private const int CaptureWarmupFrames = 2;

    private static int captureWarmupLeft;

    // 주사위만 잘라 낸 지표. 화면 전체는 나무결 베이스맵과 아직 셀로 바꾸지 않은 특수 연출 셰이더가
    // 밴드 수를 지배해서, 셰이딩이 실제로 계단화됐는지를 가리지 못한다. 판단 근거는 이쪽이다.
    private static Color32[] baselineDiceCrop;
    private static Color32[] celDiceCrop;
    private static RectInt diceCropRect;

    // 주사위 몸체 안쪽만 잘라 낸 지표. 바운딩 박스 크롭에는 주사위 사이 간격과 위아래 테이블이
    // 섞여 들어와, 셀 셰이딩이 조명 응답을 실제로 계단화했는지를 가리지 못한다. 이쪽이 그 질문에
    // 직접 답한다. 밴드 수는 주사위 다섯 개의 안쪽 패치를 이어 붙여 세고, 평면 영역 비율은
    // 연결성이 필요하므로 첫 주사위 패치 하나에서만 잰다.
    private const float DieInteriorFactor = 0.35f;

    private static Color32[] baselineDieInteriors;
    private static Color32[] celDieInteriors;
    private static Color32[] baselineFirstDiePatch;
    private static Color32[] celFirstDiePatch;

    // 두 캡처가 사각형 하나를 공유하면 안 된다. 주사위 자세가 달라 패치 크기가 1셀만 어긋나도
    // 나중 캡처가 앞 캡처의 사각형을 덮어써, 면적 계산이 조용히 0을 돌려준다. 실제로 그랬다.
    private static RectInt baselineFirstDieRect;
    private static RectInt celFirstDieRect;
    private static int celConvertedRenderers;

    static RunPixelReadabilityValidation()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Tessera/Run Pixel Readability Validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Please exit play mode first.");
            return;
        }

        SessionState.SetBool(RequestedKey, true);
        SessionState.SetBool(RunningKey, false);

        if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RequestedKey, false))
        {
            SessionState.SetBool(RequestedKey, false);
            SessionState.SetBool(RunningKey, true);
            baselineStill = null;
            celStill = null;
            rollingPrevious = null;
            baselineCrawl = 0f;
            celCrawl = 0f;
            baselineDiceCrop = null;
            celDiceCrop = null;
            diceCropRect = default;
            baselineDieInteriors = null;
            celDieInteriors = null;
            baselineFirstDiePatch = null;
            celFirstDiePatch = null;
            baselineFirstDieRect = default;
            celFirstDieRect = default;
            celConvertedRenderers = 0;
            lastCaptureAt = 0.0;
            captureWarmupLeft = 0;
            EnterPhase(1);
            Debug.Log("--- Starting Pixel Readability Validation ---");
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(RequestedKey, false);
            SessionState.SetBool(RunningKey, false);
        }
    }

    private static void EnterPhase(int next)
    {
        phase = next;
        phaseStartedAt = EditorApplication.timeSinceStartup;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(RunningKey, false)) return;

        AugmentedYachtController controller = Object.FindFirstObjectByType<AugmentedYachtController>();
        YachtCameraRig rig = Object.FindFirstObjectByType<YachtCameraRig>();
        if (controller == null || rig == null) return;

        double elapsed = EditorApplication.timeSinceStartup - phaseStartedAt;
        gridSize = rig.InternalResolution;

        switch (phase)
        {
            case 1:
                if (elapsed > 0.5) StartGameThenRoll(controller, elapsed);
                break;

            case 2: // Baseline 정지 프레임
                if (controller.IsSettled)
                {
                    baselineStill = Capture(rig);
                    baselineDiceCrop = CaptureDiceCrop(rig);
                    CaptureDieInteriors(rig, out baselineDieInteriors, out baselineFirstDiePatch, out baselineFirstDieRect);
                    controller.ResetAndRollDice();
                    rollingPrevious = null;
                    captureWarmupLeft = CaptureWarmupFrames;
                    EnterPhase(3);
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail("Baseline roll never settled.");
                }
                break;

            case 3: // Baseline 굴림 중 프레임 간 변화율
                if (!controller.IsSettled)
                {
                    Color32[] current = CaptureThrottled(rig);
                    if (current != null)
                    {
                        if (rollingPrevious != null)
                        {
                            baselineCrawl = Mathf.Max(baselineCrawl, PixelReadabilityMetrics.ChangedCellRatio(rollingPrevious, current));
                        }
                        rollingPrevious = current;
                    }
                }
                else if (rollingPrevious != null)
                {
                    // Cel로 전환하고 다시 굴린다.
                    controller.ToggleRenderStyle();
                    controller.ResetAndRollDice();
                    rollingPrevious = null;
                    captureWarmupLeft = CaptureWarmupFrames;
                    EnterPhase(4);
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail("Baseline roll produced no frames.");
                }
                break;

            case 4: // Cel 굴림 중 변화율, 이어서 정지 프레임
                if (!controller.IsSettled)
                {
                    Color32[] current = CaptureThrottled(rig);
                    if (current != null)
                    {
                        if (rollingPrevious != null)
                        {
                            celCrawl = Mathf.Max(celCrawl, PixelReadabilityMetrics.ChangedCellRatio(rollingPrevious, current));
                        }
                        rollingPrevious = current;
                    }
                }
                else if (rollingPrevious != null)
                {
                    celStill = Capture(rig);
                    celDiceCrop = CaptureDiceCrop(rig);
                    CaptureDieInteriors(rig, out celDieInteriors, out celFirstDiePatch, out celFirstDieRect);
                    celConvertedRenderers = controller.CelConvertedRendererCount;
                    Report(rig);
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail("Cel roll produced no frames.");
                }
                break;
        }
    }

    private static void StartGameThenRoll(AugmentedYachtController controller, double elapsed)
    {
        YachtTurnFlowPresenter turnFlow = controller.TurnFlow;
        if (turnFlow == null)
        {
            if (elapsed > SettleTimeoutSeconds) Fail("Turn flow presenter was never created.");
            return;
        }

        if (turnFlow.Phase == PresentationPhase.Idle)
        {
            turnFlow.StartNewGame(YachtGameMode.Normal);
            return;
        }

        if (turnFlow.Phase == PresentationPhase.AwaitingRoll)
        {
            controller.ResetAndRollDice();
            EnterPhase(2);
            return;
        }

        if (elapsed > SettleTimeoutSeconds) Fail($"Game never reached AwaitingRoll. Current phase: {turnFlow.Phase}.");
    }

    /// <summary>
    /// 월드 카메라의 렌더 타깃을 가상 격자 크기로 리샘플해 읽는다. 두 연출 방식의 타깃 크기가
    /// 달라도 같은 크기의 배열이 나와야 비교가 성립한다.
    /// </summary>
    private static Color32[] Capture(YachtCameraRig rig)
    {
        RenderTexture target = rig.WorldCamera != null ? rig.WorldCamera.targetTexture : null;
        if (target == null) return null;

        if (captureBuffer == null || captureBuffer.width != target.width || captureBuffer.height != target.height)
        {
            if (captureBuffer != null) Object.DestroyImmediate(captureBuffer);
            captureBuffer = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        }

        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            captureBuffer.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            captureBuffer.Apply(false, false);
        }
        finally
        {
            RenderTexture.active = previous;
        }

        return SampleToGrid(captureBuffer.GetPixels32(), target.width, target.height, gridSize);
    }

    /// <summary>
    /// 주사위가 차지하는 영역만 잘라 낸다.
    ///
    /// 화면 전체를 재면 나무결 베이스맵처럼 알베도가 연속으로 변하는 표면과 아직 셀로 바꾸지 않은
    /// 특수 연출 셰이더가 밴드 수를 지배해, 조명 응답이 실제로 계단화됐는지를 가리지 못한다.
    /// 주사위는 단색 바디라 셀 셰이딩의 효과가 그대로 드러난다.
    /// </summary>
    private static Color32[] CaptureDiceCrop(YachtCameraRig rig)
    {
        Color32[] frame = Capture(rig);
        if (frame == null) return null;

        if (!TryBuildDiceRect(rig, out diceCropRect)) return null;

        Color32[] crop = new Color32[diceCropRect.width * diceCropRect.height];
        for (int y = 0; y < diceCropRect.height; y++)
        {
            int sourceRow = (diceCropRect.y + y) * gridSize.x;
            int targetRow = y * diceCropRect.width;
            for (int x = 0; x < diceCropRect.width; x++)
            {
                crop[targetRow + x] = frame[sourceRow + diceCropRect.x + x];
            }
        }
        return crop;
    }

    /// <summary>
    /// 주사위 몸체 안쪽 정사각 패치를 주사위마다 잘라 낸다.
    ///
    /// <paramref name="allInteriors"/>는 다섯 개 패치를 이어 붙인 것으로 밴드 수를 세는 데 쓴다.
    /// 밴드 수는 순서와 무관해서 이어 붙여도 뜻이 변하지 않는다.
    /// <paramref name="firstPatch"/>는 첫 주사위 패치 하나로, 연결성이 필요한 평면 영역 비율에 쓴다.
    /// </summary>
    private static void CaptureDieInteriors(YachtCameraRig rig, out Color32[] allInteriors, out Color32[] firstPatch, out RectInt firstRect)
    {
        allInteriors = null;
        firstPatch = null;
        firstRect = default;

        Color32[] frame = Capture(rig);
        Camera camera = rig.WorldCamera;
        DiceVisualPool pool = Object.FindFirstObjectByType<DiceVisualPool>();
        Transform diceRoot = pool != null ? pool.DiceRoot : null;
        if (frame == null || camera == null || diceRoot == null) return;

        List<Color32> combined = new();

        foreach (Transform die in diceRoot)
        {
            if (!die.gameObject.activeInHierarchy) continue;
            if (!TryBuildInteriorRect(camera, die, out RectInt rect)) continue;

            Color32[] patch = CropFrame(frame, rect);
            combined.AddRange(patch);

            if (firstPatch == null)
            {
                firstPatch = patch;
                firstRect = rect;
            }
        }

        if (combined.Count > 0) allInteriors = combined.ToArray();
    }

    /// <summary>
    /// 주사위 한 개의 실루엣 안쪽에 확실히 들어가는 사각형. 어떤 자세에서도 배경이 섞이지 않도록
    /// 반지름의 일부만 쓴다.
    /// </summary>
    private static bool TryBuildInteriorRect(Camera camera, Transform die, out RectInt rect)
    {
        rect = default;

        float radius = die.lossyScale.x * DieInteriorFactor;
        Vector3 center = die.position;

        Vector3 minViewport = camera.WorldToViewportPoint(center - camera.transform.right * radius - camera.transform.up * radius);
        Vector3 maxViewport = camera.WorldToViewportPoint(center + camera.transform.right * radius + camera.transform.up * radius);

        int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(minViewport.x, maxViewport.x) * gridSize.x), 0, gridSize.x - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(minViewport.x, maxViewport.x) * gridSize.x), 0, gridSize.x - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(minViewport.y, maxViewport.y) * gridSize.y), 0, gridSize.y - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(minViewport.y, maxViewport.y) * gridSize.y), 0, gridSize.y - 1);

        if (x1 <= x0 || y1 <= y0) return false;

        rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        return true;
    }

    private static Color32[] CropFrame(Color32[] frame, RectInt rect)
    {
        Color32[] crop = new Color32[rect.width * rect.height];
        for (int y = 0; y < rect.height; y++)
        {
            int sourceRow = (rect.y + y) * gridSize.x;
            int targetRow = y * rect.width;
            for (int x = 0; x < rect.width; x++)
            {
                crop[targetRow + x] = frame[sourceRow + rect.x + x];
            }
        }
        return crop;
    }

    /// <summary>
    /// 주사위 전체를 감싸는 격자 사각형을 만든다. 직교 카메라라 주사위 중심을 뷰포트로 옮기고
    /// 반지름만큼 넓히면 충분하다.
    /// </summary>
    private static bool TryBuildDiceRect(YachtCameraRig rig, out RectInt rect)
    {
        rect = default;

        DiceVisualPool pool = Object.FindFirstObjectByType<DiceVisualPool>();
        Transform diceRoot = pool != null ? pool.DiceRoot : null;
        Camera camera = rig.WorldCamera;
        if (diceRoot == null || camera == null || diceRoot.childCount == 0) return false;

        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        bool any = false;

        foreach (Transform die in diceRoot)
        {
            if (!die.gameObject.activeInHierarchy) continue;

            // 대각선 반지름이라 어느 자세에서도 주사위가 사각형 밖으로 나가지 않는다.
            float radius = die.lossyScale.x * 0.87f;
            Vector3 center = die.position;

            foreach (Vector3 corner in Corners(camera, center, radius))
            {
                Vector3 viewport = camera.WorldToViewportPoint(corner);
                minX = Mathf.Min(minX, viewport.x);
                maxX = Mathf.Max(maxX, viewport.x);
                minY = Mathf.Min(minY, viewport.y);
                maxY = Mathf.Max(maxY, viewport.y);
                any = true;
            }
        }

        if (!any) return false;

        int x0 = Mathf.Clamp(Mathf.FloorToInt(minX * gridSize.x), 0, gridSize.x - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX * gridSize.x), 0, gridSize.x - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(minY * gridSize.y), 0, gridSize.y - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY * gridSize.y), 0, gridSize.y - 1);

        if (x1 <= x0 || y1 <= y0) return false;

        rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        return true;
    }

    /// <summary>
    /// 카메라의 화면 축으로 네 귀퉁이를 만든다.
    ///
    /// 월드 X/Y로 만들면 안 된다. 이 카메라는 테이블을 내려다보게 기울어져 있어 월드 Y 오프셋이
    /// 화면에서 크게 눌린다. 실제로 그렇게 재 봤더니 주사위 크롭이 129x10셀로 납작하게 나왔다.
    /// </summary>
    private static Vector3[] Corners(Camera camera, Vector3 center, float radius)
    {
        Vector3 right = camera.transform.right * radius;
        Vector3 up = camera.transform.up * radius;

        return new[]
        {
            center - right - up,
            center + right - up,
            center - right + up,
            center + right + up
        };
    }

    /// <summary>고정 간격이 지났을 때만 캡처한다. 두 연출 방식이 같은 간격을 써야 비교가 성립한다.</summary>
    private static Color32[] CaptureThrottled(YachtCameraRig rig)
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - lastCaptureAt < CaptureIntervalSeconds) return null;

        lastCaptureAt = now;

        if (captureWarmupLeft > 0)
        {
            captureWarmupLeft--;
            Capture(rig);
            return null;
        }

        return Capture(rig);
    }

    private static void ReleaseCaptureBuffer()
    {
        if (captureBuffer == null) return;
        Object.DestroyImmediate(captureBuffer);
        captureBuffer = null;
    }

    private static Color32[] SampleToGrid(Color32[] source, int width, int height, Vector2Int grid)
    {
        if (source == null || grid.x <= 0 || grid.y <= 0) return source;
        if (width == grid.x && height == grid.y) return source;

        Color32[] result = new Color32[grid.x * grid.y];
        for (int y = 0; y < grid.y; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / grid.y * height), 0, height - 1);
            for (int x = 0; x < grid.x; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / grid.x * width), 0, width - 1);
                result[y * grid.x + x] = source[sourceY * width + sourceX];
            }
        }
        return result;
    }

    private static void Report(YachtCameraRig rig)
    {
        SessionState.SetBool(RunningKey, false);
        ReleaseCaptureBuffer();

        StringBuilder report = new();
        report.AppendLine("--- Pixel Readability Validation ---");
        report.AppendLine($"Grid: {gridSize.x}x{gridSize.y}, Quantize: {rig.QuantizeModeName}");
        report.AppendLine($"Cel converted renderers: {celConvertedRenderers}");
        report.AppendLine($"Dice crop: {diceCropRect.width}x{diceCropRect.height} cells, die interior patch: baseline {baselineFirstDieRect.width}x{baselineFirstDieRect.height}, cel {celFirstDieRect.width}x{celFirstDieRect.height}");
        report.AppendLine("| Metric | Baseline | Cel |");
        report.AppendLine("|---|---|---|");
        report.AppendLine($"| Die interior bands (primary) | {Bands(baselineDieInteriors)} | {Bands(celDieInteriors)} |");
        report.AppendLine($"| Die interior largest flat region | {PatchRegion(baselineFirstDiePatch, baselineFirstDieRect):P2} | {PatchRegion(celFirstDiePatch, celFirstDieRect):P2} |");
        report.AppendLine($"| Dice bounding-box bands | {Bands(baselineDiceCrop)} | {Bands(celDiceCrop)} |");
        report.AppendLine($"| Dice largest flat region | {CropRegion(baselineDiceCrop):P2} | {CropRegion(celDiceCrop):P2} |");
        report.AppendLine($"| Full frame bands | {Bands(baselineStill)} | {Bands(celStill)} |");
        report.AppendLine($"| Full frame largest flat region | {Region(baselineStill):P2} | {Region(celStill):P2} |");
        report.AppendLine($"| Peak changed cells while rolling | {baselineCrawl:P2} | {celCrawl:P2} |");
        Debug.Log(report.ToString());

        EditorApplication.isPlaying = false;
    }

    private static string Bands(Color32[] pixels)
    {
        return pixels == null ? "n/a" : PixelReadabilityMetrics.CountLuminanceBands(pixels).ToString();
    }

    private static float Region(Color32[] pixels)
    {
        return pixels == null ? 0f : PixelReadabilityMetrics.LargestUniformRegionRatio(pixels, gridSize.x, gridSize.y);
    }

    private static float PatchRegion(Color32[] pixels, RectInt rect)
    {
        if (pixels == null || rect.width <= 0) return 0f;
        return PixelReadabilityMetrics.LargestUniformRegionRatio(pixels, rect.width, rect.height);
    }

    private static float CropRegion(Color32[] pixels)
    {
        if (pixels == null || diceCropRect.width <= 0) return 0f;
        return PixelReadabilityMetrics.LargestUniformRegionRatio(pixels, diceCropRect.width, diceCropRect.height);
    }

    private static void Fail(string message)
    {
        SessionState.SetBool(RunningKey, false);
        ReleaseCaptureBuffer();
        Debug.LogError($"Pixel Readability Validation failed: {message}");
        EditorApplication.isPlaying = false;
    }
}
