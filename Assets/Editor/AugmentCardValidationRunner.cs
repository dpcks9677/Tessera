using System.Reflection;
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
        AugmentedYachtController controller = Object.FindFirstObjectByType<AugmentedYachtController>();
        Invoke(controller, "StartNewGame", new[] { typeof(YachtGameMode) }, new object[] { YachtGameMode.Augmented });
    }

    [MenuItem("Tessera/Validation/Visual Check/Select First Augment")]
    public static void SelectFirstAugment()
    {
        AugmentedYachtController controller = Object.FindFirstObjectByType<AugmentedYachtController>();
        Invoke(controller, "SelectDraftOption", new[] { typeof(int) }, new object[] { 0 });
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

    private static void Invoke(object target, string methodName, System.Type[] parameterTypes, object[] arguments)
    {
        if (target == null) throw new System.InvalidOperationException("증강 요트 컨트롤러를 찾을 수 없습니다.");
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            parameterTypes,
            null);
        if (method == null) throw new System.MissingMethodException(target.GetType().Name, methodName);
        method.Invoke(target, arguments);
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
