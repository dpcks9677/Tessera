using System.Collections.Generic;
using Tessera.Dice;
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

    /// <summary>드래프트 카드가 화면을 가리지 않는 상태로 주사위만 보고 싶을 때 쓴다.</summary>
    [MenuItem("Tessera/Validation/Visual Check/Start Normal Game")]
    public static void StartNormalGame()
    {
        ResolveTurnFlow().StartNewGame(YachtGameMode.Normal);
    }

    [MenuItem("Tessera/Validation/Visual Check/Select First Augment")]
    public static void SelectFirstAugment()
    {
        ResolveTurnFlow().SelectDraftOption(0);
    }

    /// <summary>
    /// 특수 주사위 증강이 제시될 때까지 새 게임을 다시 열고 그 증강을 고른다(M7-T5 검증).
    ///
    /// 권위 계층은 드래프트에 제시된 증강만 받아들이므로 임의로 부여할 수 없다.
    /// 실제 굴림 경로(프리셋 슬롯 순서·착지 회전)를 확인하려면 정식으로 획득해야 한다.
    /// </summary>
    [MenuItem("Tessera/Validation/Visual Check/Draft Special Dice")]
    public static void DraftSpecialDice()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[검증] 플레이 모드에서만 실행할 수 있습니다.");
            return;
        }

        // 8면 주사위를 가장 먼저 노린다. 형상까지 바뀌는 유일한 종류라 검증 가치가 크다.
        string[] wanted = { "8-sided", "sevens-dice", "golden-die", "weighted-dice", "couple-dice", "promotion-die" };
        YachtTurnFlowPresenter flow = ResolveTurnFlow();

        for (int attempt = 1; attempt <= 300; attempt++)
        {
            flow.StartNewGame(YachtGameMode.Augmented);
            YachtGameSession session = flow.Session;
            if (session == null || !session.IsDrafting) continue;

            string[] options = session.State.Draft.Options;
            if (options == null) continue;

            foreach (string target in wanted)
            {
                int index = System.Array.IndexOf(options, target);
                if (index < 0) continue;

                flow.SelectDraftOption(index);
                Debug.Log($"[검증] {target} 증강을 {attempt}번째 드래프트에서 획득했습니다.");
                return;
            }
        }

        Debug.LogWarning("[검증] 300번 안에 특수 주사위 증강이 제시되지 않았습니다.");
    }

    /// <summary>굴림 한 번. 실제 굴림 경로를 통과시켜 착지 자세와 눈을 확인할 때 쓴다.</summary>
    [MenuItem("Tessera/Validation/Visual Check/Roll Dice Once")]
    public static void RollDiceOnce()
    {
        ResolveTurnFlow().RollDice();
    }

    /// <summary>
    /// 특수 주사위 외형을 한 화면에 늘어놓는다(M7-T5 시각 검수).
    ///
    /// 화면 사본에만 종류를 강제로 입히므로 권위 상태는 바뀌지 않는다. 다음 굴림이나 턴 전환에서
    /// 원래 종류로 되돌아가며, 그동안 스크린샷으로 6종을 한 번에 대조할 수 있다.
    /// </summary>
    [MenuItem("Tessera/Validation/Visual Check/Showcase Special Dice")]
    public static void ShowcaseSpecialDice()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[검증] 플레이 모드에서만 실행할 수 있습니다.");
            return;
        }

        DiceVisualPool pool = Object.FindFirstObjectByType<DiceVisualPool>();
        if (pool == null || pool.DiceRoot == null)
        {
            Debug.LogError("[검증] 주사위 비주얼 풀을 찾지 못했습니다.");
            return;
        }

        DieType[] showcase =
        {
            DieType.Octahedron,
            DieType.Golden,
            DieType.HeavyRed,
            DieType.Promotion,
            DieType.Couple,
            DieType.Sevens
        };

        int applied = 0;
        foreach (Transform die in pool.DiceRoot)
        {
            if (applied >= showcase.Length) break;
            pool.ApplyDieType(die.gameObject, showcase[applied]);
            applied++;
        }

        // 종류가 바뀌면 면 값 표도 바뀐다. 다시 정렬해야 각 주사위가 제 눈을 위로 올린다.
        YachtDiceRoundPresenter round = Object.FindFirstObjectByType<YachtDiceRoundPresenter>();
        if (round != null) round.ResetForTurn(null);

        Debug.Log($"[검증] 특수 주사위 {applied}종을 화면 사본에 적용했습니다. 주사위가 {showcase.Length}개보다 적으면 그만큼만 보입니다.");
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
