using System.Collections.Generic;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 픽셀 엣지 렌더러 피처를 URP 렌더러 에셋에 등록한다(M10.5-T4).
///
/// 렌더러 에셋은 YAML이라 손으로 고치면 서브에셋 로컬 ID와 피처 목록이 어긋난다.
/// 인스펙터가 하는 일과 같은 순서로 서브에셋을 만들고 목록·맵을 함께 갱신한다.
/// 한 번 실행하면 에셋에 저장되므로 이후에는 다시 부를 필요가 없다.
/// </summary>
public static class RegisterPixelEdgeFeature
{
    private const string ShaderAssetPath = "Assets/Rendering/Shaders/DicePixelEdge.shader";

    private static readonly string[] RendererDataPaths =
    {
        "Assets/Settings/PC_Renderer.asset",
        "Assets/Settings/Mobile_Renderer.asset"
    };

    [MenuItem("Tools/Tessera/Register Pixel Edge Renderer Feature")]
    public static void Register()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderAssetPath);
        if (shader == null)
        {
            Debug.LogError($"픽셀 엣지 셰이더를 찾지 못했습니다: {ShaderAssetPath}");
            return;
        }

        foreach (string path in RendererDataPaths)
        {
            ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null)
            {
                Debug.LogError($"렌더러 에셋을 찾지 못했습니다: {path}");
                continue;
            }

            if (FindExistingFeature(rendererData) != null)
            {
                Debug.Log($"{path}: 픽셀 엣지 피처가 이미 등록돼 있습니다.");
                continue;
            }

            AddFeature(rendererData, shader);
            Debug.Log($"{path}: 픽셀 엣지 피처를 등록했습니다.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static PixelEdgeRendererFeature FindExistingFeature(ScriptableRendererData rendererData)
    {
        List<ScriptableRendererFeature> features = rendererData.rendererFeatures;
        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] is PixelEdgeRendererFeature existing) return existing;
        }
        return null;
    }

    private static void AddFeature(ScriptableRendererData rendererData, Shader shader)
    {
        PixelEdgeRendererFeature feature = ScriptableObject.CreateInstance<PixelEdgeRendererFeature>();
        feature.name = nameof(PixelEdgeRendererFeature);

        SerializedObject serializedFeature = new(feature);
        serializedFeature.FindProperty("edgeShader").objectReferenceValue = shader;
        serializedFeature.ApplyModifiedPropertiesWithoutUndo();

        // 목록에 넣기 전에 저장해야 서브에셋의 로컬 파일 ID가 생기고, 그 값을 맵에 넣을 수 있다.
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        AssetDatabase.SaveAssets();

        SerializedObject serializedData = new(rendererData);
        SerializedProperty features = serializedData.FindProperty("m_RendererFeatures");
        SerializedProperty featureMap = serializedData.FindProperty("m_RendererFeatureMap");

        features.arraySize++;
        features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

        featureMap.arraySize = features.arraySize;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
        featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;

        serializedData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rendererData);
    }
}
