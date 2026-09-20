using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;

[InitializeOnLoad]
public static class RunDicePhysicsValidation
{
    private const string ScenePath = "Assets/Scenes/Augmented Dice.unity";
    private const string ValidationRequestedKey = "Tessera.PhysicsKeepValidationRequested";
    private const string ValidationRunningKey = "Tessera.PhysicsKeepValidationRunning";

    // 굴림 궤적과 뒤이은 정렬 애니메이션까지 기다린다. 기존 5초는 그 둘을 합친 시간보다 짧았다.
    private const double SettleTimeoutSeconds = 12.0;

    private static int phase;
    private static double phaseStartedAt;
    private static int keepIndex;
    private static int firstKeptValue;
    private static int secondKeptValue;

    static RunDicePhysicsValidation()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update -= TryStartValidation;
        EditorApplication.update += TryStartValidation;
    }

    [MenuItem("Tools/Tessera/Run Physics And Keep Validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Please exit play mode first.");
            return;
        }

        SessionState.SetBool(ValidationRequestedKey, true);
        SessionState.SetBool(ValidationRunningKey, false);
        EnsureSceneLoaded();
        EditorApplication.isPlaying = true;
    }

    private static void EnsureSceneLoaded()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (SessionState.GetBool(ValidationRequestedKey, false))
            {
                SessionState.SetBool(ValidationRequestedKey, false);
                SessionState.SetBool(ValidationRunningKey, true);
                phase = 1;
                phaseStartedAt = EditorApplication.timeSinceStartup;
                Debug.Log("--- Starting Physics And Keep Validation ---");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ValidationRequestedKey, false);
            SessionState.SetBool(ValidationRunningKey, false);
        }
    }

    private static void TryStartValidation()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(ValidationRunningKey, false))
        {
            return;
        }

        AugmentedYachtController controller = Object.FindFirstObjectByType<AugmentedYachtController>();
        if (controller == null) return;

        double elapsed = EditorApplication.timeSinceStartup - phaseStartedAt;

        switch (phase)
        {
            case 1: // 게임 세션 시작. 시작하지 않으면 굴림 자체가 거부된다.
                if (elapsed > 0.5)
                {
                    YachtTurnFlowPresenter turnFlow = controller.TurnFlow;
                    if (turnFlow == null)
                    {
                        if (elapsed > SettleTimeoutSeconds) Fail("Turn flow presenter was never created.");
                        break;
                    }

                    // 플레이 진입 직후는 시작 오버레이가 떠 있는 Idle 단계다. 이 상태에서
                    // RollDice()는 게임 세션이 없어 조용히 반환하므로, 먼저 게임을 시작한다.
                    if (turnFlow.Phase == PresentationPhase.Idle)
                    {
                        turnFlow.StartNewGame(YachtGameMode.Normal);
                        break;
                    }

                    if (turnFlow.Phase == PresentationPhase.AwaitingRoll)
                    {
                        controller.ResetAndRollDice();
                        phase = 2;
                        phaseStartedAt = EditorApplication.timeSinceStartup;
                        break;
                    }

                    if (elapsed > SettleTimeoutSeconds)
                    {
                        Fail($"Game never reached AwaitingRoll. Current phase: {turnFlow.Phase}.");
                    }
                }
                break;

            case 2: // 첫 굴림 완료 및 결과 검증
                if (controller.IsSettled)
                {
                    // 5개 주사위 눈이 1~6 범위 내에 있는지 확인
                    for (int i = 0; i < 5; i++)
                    {
                        int value = controller.GetDieValue(i);
                        if (value < 1 || value > 6)
                        {
                            Fail($"Die {i + 1} has invalid value: {value}");
                            return;
                        }
                    }

                    firstKeptValue = controller.GetDieValue(0);
                    secondKeptValue = controller.GetDieValue(1);

                    keepIndex = 0;
                    phase = 3;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail($"Timed out waiting for first roll to settle. Current phase: {PhaseOf(controller)}.");
                }
                break;

            case 3: // 주사위를 한 개씩 킵한 뒤 부분 재굴림(Re-roll) 시작
                // 킵하면 정렬 애니메이션이 다시 돌아 단계가 Arranging으로 바뀐다.
                // SetDieKept는 Settled에서만 통과하므로 한 프레임에 두 개를 걸 수 없다.
                if (elapsed <= 0.5) break;

                if (keepIndex < 2)
                {
                    if (controller.IsSettled && controller.SetDieKept(keepIndex, true))
                    {
                        keepIndex++;
                        phaseStartedAt = EditorApplication.timeSinceStartup;
                    }
                    else if (elapsed > SettleTimeoutSeconds)
                    {
                        Fail($"Keep request for die {keepIndex + 1} never took. Current phase: {PhaseOf(controller)}.");
                    }
                    break;
                }

                if (controller.IsSettled)
                {
                    if (controller.KeptDieCount != 2)
                    {
                        Fail($"Expected 2 kept dice, got {controller.KeptDieCount}");
                        return;
                    }

                    controller.RollDice();
                    phase = 4;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail($"Timed out waiting to settle after keeping. Current phase: {PhaseOf(controller)}.");
                }
                break;

            case 4: // 재굴림 완료 및 킵 보존 검증
                if (controller.IsSettled)
                {
                    int currentFirst = controller.GetDieValue(0);
                    int currentSecond = controller.GetDieValue(1);

                    if (currentFirst != firstKeptValue || currentSecond != secondKeptValue)
                    {
                        Fail($"Kept dice values changed! First: {firstKeptValue}->{currentFirst}, Second: {secondKeptValue}->{currentSecond}");
                        return;
                    }

                    Pass("Dice Baked Preset & Keep Layout Validation PASSED successfully!");
                    phase = 5;
                }
                else if (elapsed > SettleTimeoutSeconds)
                {
                    Fail($"Timed out waiting for second roll to settle. Current phase: {PhaseOf(controller)}.");
                }
                break;
        }
    }

    /// <summary>실패 원인을 좁히기 위해 현재 프레젠테이션 단계를 문자열로 남긴다.</summary>
    private static string PhaseOf(AugmentedYachtController controller)
    {
        return controller.TurnFlow != null ? controller.TurnFlow.Phase.ToString() : "no turn flow";
    }

    private static void Pass(string message)
    {
        Debug.Log($"<color=green>[VALIDATION SUCCESS]</color> {message}");
        SessionState.SetBool(ValidationRunningKey, false);
        SessionState.SetBool(ValidationRequestedKey, false);
        EditorApplication.isPlaying = false;
    }

    private static void Fail(string reason)
    {
        Debug.LogError($"<color=red>[VALIDATION FAILED]</color> {reason}");
        SessionState.SetBool(ValidationRunningKey, false);
        SessionState.SetBool(ValidationRequestedKey, false);
        EditorApplication.isPlaying = false;
    }
}
