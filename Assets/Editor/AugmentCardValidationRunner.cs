using System.Collections.Generic;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class AugmentCardValidationRunner
{
    [MenuItem("Tessera/Validation/Run Augment Card EditMode Tests")]
    public static void Run()
    {
        RunTests(new[] { "^Augment(CardView|ParchmentState)Tests" }, "증강 카드");
    }

    [MenuItem("Tessera/Validation/Run All EditMode Tests")]
    public static void RunAll()
    {
        RunTests(null, "전체");
    }

    [MenuItem("Tessera/Validation/Visual Check/Start Augmented Game")]
    public static void StartAugmentedGame()
    {
        ResolveTurnFlow().StartNewGame(YachtGameMode.Augmented);
    }

    [MenuItem("Tessera/Validation/Visual Check/Select First Augment")]
    public static void SelectFirstAugment()
    {
        ResolveTurnFlow().SelectDraftOption(0);
    }

    private static void RunTests(string[] groupNames, string label)
    {
        TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new ValidationCallbacks(label));
        var filter = new Filter
        {
            testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
            groupNames = groupNames
        };
        api.Execute(new ExecutionSettings(filter) { runSynchronously = true });
        Object.DestroyImmediate(api);
    }

    /// <summary>
    /// 플레이 모드에서 증강 요트 한 판을 끝까지 자동으로 진행한다.
    ///
    /// 점수표 클릭을 흉내 낼 수단이 없어 흐름 프레젠터의 공개 진입점을 직접 부른다.
    /// 드래프트는 첫 후보를 고르고, 굴림이 끝나면 남은 후보 중 최고점 칸에 기입한다.
    /// </summary>
    [MenuItem("Tessera/Validation/Visual Check/Auto Play Augmented Game")]
    public static void AutoPlayAugmentedGame()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[검증] 플레이 모드에서만 실행할 수 있습니다.");
            return;
        }

        YachtTurnFlowPresenter flow = ResolveTurnFlow();
        flow.StartNewGame(YachtGameMode.Augmented);

        int ticks = 0;
        int commits = 0;
        EditorApplication.CallbackFunction driver = null;
        driver = () =>
        {
            if (!EditorApplication.isPlaying || flow == null)
            {
                EditorApplication.update -= driver;
                Debug.LogWarning($"[검증] 자동 진행이 중단되었습니다. 기입 {commits}회, 틱 {ticks}회");
                return;
            }

            YachtGameSession session = flow.Session;
            if (session == null) return;

            if (++ticks > MaxAutoPlayTicks)
            {
                EditorApplication.update -= driver;
                Debug.LogError($"[검증] 자동 진행이 {MaxAutoPlayTicks}틱을 넘겨 중단되었습니다. 기입 {commits}회, 라운드 {session.CurrentRound}, 화면 단계 {flow.Phase}");
                return;
            }

            if (session.Phase == YachtGamePhase.GameOver)
            {
                EditorApplication.update -= driver;
                Debug.Log($"[검증] 자동 완주 성공. 기입 {commits}회, P1 {session.GetPlayer(0).totalScore}점, P2 {session.GetPlayer(1).totalScore}점");
                return;
            }

            if (session.IsDrafting)
            {
                flow.SelectDraftOption(0);
                return;
            }

            if (session.Phase == YachtGamePhase.ScoreSelection && flow.Phase == PresentationPhase.Settled)
            {
                ScoreCategory best = ScoreCategory.Aces;
                int bestScore = int.MinValue;
                foreach (KeyValuePair<ScoreCategory, int> candidate in session.CurrentCandidates)
                {
                    if (candidate.Value <= bestScore) continue;
                    best = candidate.Key;
                    bestScore = candidate.Value;
                }
                if (bestScore == int.MinValue) return;

                commits++;
                flow.CommitScore(session.CurrentPlayerIndex, best);
                return;
            }

            if (flow.CanInitiateRoll()) flow.RollDice();
        };
        EditorApplication.update += driver;
    }

    private const int MaxAutoPlayTicks = 40000;

    private static YachtTurnFlowPresenter ResolveTurnFlow()
    {
        AugmentedYachtController controller = Object.FindFirstObjectByType<AugmentedYachtController>();
        if (controller == null) throw new System.InvalidOperationException("증강 요트 컨트롤러를 찾을 수 없습니다.");
        if (controller.TurnFlow == null) throw new System.InvalidOperationException("턴 흐름 프레젠터가 아직 준비되지 않았습니다. 플레이 모드에서 실행하십시오.");
        return controller.TurnFlow;
    }

    private sealed class ValidationCallbacks : ICallbacks
    {
        private readonly string label;

        public ValidationCallbacks(string label) => this.label = label;

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string summary = $"{label} EditMode 검증: {result.PassCount} 통과, {result.FailCount} 실패, {result.SkipCount} 건너뜀";
            if (result.FailCount > 0) Debug.LogError(summary);
            else Debug.Log(summary);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.FailCount > 0 && !result.HasChildren)
                Debug.LogError($"{label} 테스트 실패: {result.FullName}\n{result.Message}\n{result.StackTrace}");
        }
    }
}
