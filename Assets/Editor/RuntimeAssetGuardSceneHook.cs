using Tessera.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        RestoreNullMaterialsFromPrefabs(scene);
    }

    /// <summary>
    /// 프리팹 인스턴스의 빈 머티리얼 슬롯을 프리팹 원본 값으로 메운다.
    ///
    /// <see cref="RuntimeAssetGuard"/>의 사본은 DontSave라 도메인 리로드에 파괴되고, 사본과
    /// 구운 에셋을 짝지어 둔 매핑도 static이라 같이 사라진다. 그러면 렌더러 참조가 null로
    /// 남고 <see cref="RuntimeAssetGuard.RestoreBakedAssets"/>는 되돌릴 대상을 잃는다.
    /// 스크립트를 고친 뒤 씬을 저장하면 이 경로를 그대로 밟아 프리팹 인스턴스에 null
    /// 오버라이드가 박히고, 씬을 다시 열어도 기본 머티리얼로 보인다.
    ///
    /// 매핑에 기대지 않고 프리팹 원본에서 직접 메우므로 리로드 시점과 무관하게 막힌다.
    /// </summary>
    private static void RestoreNullMaterialsFromPrefabs(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (!HasNull(materials)) continue;

                Renderer source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                if (source == null) continue;

                Material[] sourceMaterials = source.sharedMaterials;
                bool filled = false;
                for (int i = 0; i < materials.Length && i < sourceMaterials.Length; i++)
                {
                    if (materials[i] != null || sourceMaterials[i] == null) continue;
                    materials[i] = sourceMaterials[i];
                    filled = true;
                }

                if (filled) renderer.sharedMaterials = materials;
            }
        }
    }

    private static bool HasNull(Material[] materials)
    {
        foreach (Material material in materials)
        {
            if (material == null) return true;
        }
        return false;
    }

    private static void OnSceneSaved(Scene scene)
    {
        RuntimeAssetGuard.ReapplyEditorClones();
    }
}
