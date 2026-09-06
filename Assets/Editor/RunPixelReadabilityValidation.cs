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
            lastCaptureAt = 0.0;
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
                    controller.ResetAndRollDice();
                    rollingPrevious = null;
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

    /// <summary>고정 간격이 지났을 때만 캡처한다. 두 연출 방식이 같은 간격을 써야 비교가 성립한다.</summary>
    private static Color32[] CaptureThrottled(YachtCameraRig rig)
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - lastCaptureAt < CaptureIntervalSeconds) return null;

        lastCaptureAt = now;
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
        report.AppendLine("| Metric | Baseline | Cel |");
        report.AppendLine("|---|---|---|");
        report.AppendLine($"| Luminance bands | {Bands(baselineStill)} | {Bands(celStill)} |");
        report.AppendLine($"| Largest flat region | {Region(baselineStill):P2} | {Region(celStill):P2} |");
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

    private static void Fail(string message)
    {
        SessionState.SetBool(RunningKey, false);
        ReleaseCaptureBuffer();
        Debug.LogError($"Pixel Readability Validation failed: {message}");
        EditorApplication.isPlaying = false;
    }
}
