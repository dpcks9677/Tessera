using Tessera.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 저장 앞뒤로 <see cref="RuntimeAssetGuard"/>의 에디트 모드 사본을 갈아 끼운다.
///
/// ExecuteAlways 연출 컴포넌트는 에디트 모드에서도 머티리얼·메시를 사본으로 바꿔 쓴다.
/// 그 사본은 DontSave라 씬에 직렬화되지 않으므로, 그대로 저장하면 참조가 null로 기록되고
/// 프리팹 인스턴스에는 null 오버라이드가 남는다. 코스믹 큐브가 통째로 기본 머티리얼로
/// 보이던 것이 그 결과였다. 저장 직전에만 구운 에셋으로 돌려 놓아 이를 막는다.
/// </summary>
[InitializeOnLoad]
public static class RuntimeAssetGuardSceneHook
{
    static RuntimeAssetGuardSceneHook()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorSceneManager.sceneSaved -= OnSceneSaved;
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        RuntimeAssetGuard.RestoreBakedAssets();
    }

    private static void OnSceneSaved(Scene scene)
    {
        RuntimeAssetGuard.ReapplyEditorClones();
    }
}
