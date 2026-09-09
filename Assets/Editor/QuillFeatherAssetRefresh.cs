using Tessera.Tabletop;
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 깃펜 깃털 메시·머티리얼·텍스처 4종만 같은 경로에 덮어써서 갱신한다(M17-T19).
    ///
    /// <see cref="TabletopPrefabBaker.Bake"/>를 다시 돌리지 않는 이유가 셋이다.
    /// 1) ExtractMeshes가 AssetDatabase.Contains(mesh)인 메시를 건너뛴다. 씬의 깃펜은 이미
    ///    베이크된 에셋을 물고 있는 프리팹 인스턴스라 재베이크해도 깃털이 안 바뀐다.
    /// 2) UniquePath가 덮어쓰지 않아 "..._01.asset" 중복이 쌓인다.
    /// 3) BakeProp의 AssetDatabase.DeleteAsset(prefabPath)가 .meta를 지워 프리팹 15개의
    ///    GUID를 전부 재발급한다. 씬 인스턴스 링크와 AugmentedYachtController의
    ///    quillHoverAnimator 직렬화 참조가 끊겨 M17-T18 깃펜 필기 연출이 조용히 죽는다.
    /// 그래서 깃털 에셋 4종만 같은 경로에 덮어써서 GUID를 보존한다.
    /// </summary>
    public static class QuillFeatherAssetRefresh
    {
        private const string MeshPath =
            "Assets/Art/Generated/Tabletop/Meshes/3D_Inkwell_and_Quill_Decoration_Quill_Feather_Blade.asset";
        private const string MaterialPath =
            "Assets/Art/Generated/Tabletop/Materials/3D_Inkwell_and_Quill_Decoration_Quill_Feather_Material.mat";
        private const string AlbedoPath =
            "Assets/Art/Generated/Tabletop/Textures/3D_Inkwell_and_Quill_Decoration_Stylized_Quill_Feather_Albedo.png";
        private const string NormalPath =
            "Assets/Art/Generated/Tabletop/Textures/3D_Inkwell_and_Quill_Decoration_Stylized_Quill_Feather_Normal.png";

        [MenuItem("Tessera/Tabletop/Refresh Quill Feather Assets")]
        public static void Refresh()
        {
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D existingAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            Texture2D existingNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            if (existingMesh == null || existingMaterial == null || existingAlbedo == null || existingNormal == null)
            {
                Debug.LogError(
                    "[QuillFeatherAssetRefresh] 갱신 대상 에셋을 찾지 못했습니다. " +
                    $"메시={MeshPath}, 머티리얼={MaterialPath}, 알베도={AlbedoPath}, 노멀={NormalPath}");
                return;
            }

            InkwellAndQuill instance = InkwellAndQuill.Create(null, Vector3.zero);
            try
            {
                Transform blade = instance.transform.Find("Quill Pen Root/Quill_Feather_Blade");
                if (blade == null)
                {
                    Debug.LogError("[QuillFeatherAssetRefresh] 'Quill Pen Root/Quill_Feather_Blade'를 찾지 못했습니다.");
                    return;
                }

                MeshFilter filter = blade.GetComponent<MeshFilter>();
                MeshRenderer renderer = blade.GetComponent<MeshRenderer>();
                Mesh newMesh = filter.sharedMesh;
                Material newMaterial = renderer.sharedMaterial;

                EditorUtility.CopySerialized(newMesh, existingMesh);
                EditorUtility.SetDirty(existingMesh);

                Texture2D newAlbedo = newMaterial.GetTexture("_BaseMap") as Texture2D ?? newMaterial.mainTexture as Texture2D;
                Texture2D newNormal = newMaterial.GetTexture("_BumpMap") as Texture2D;

                TabletopPrefabBaker.TryWritePng(newAlbedo, AlbedoPath);
                TabletopPrefabBaker.ConfigureTextureImporter(AlbedoPath);
                TabletopPrefabBaker.TryWritePng(newNormal, NormalPath);
                TabletopPrefabBaker.ConfigureTextureImporter(NormalPath);

                EditorUtility.CopySerialized(newMaterial, existingMaterial);
                // CopySerialized가 런타임 텍스처 참조를 그대로 끌고 오므로, 저장된 PNG 에셋으로 되돌린다.
                // 이 복구가 없으면 머티리얼이 저장 즉시 깨진 참조를 갖는다.
                existingMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath));
                existingMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
                existingMaterial.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
                EditorUtility.SetDirty(existingMaterial);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[QuillFeatherAssetRefresh] 깃털 에셋을 갱신했습니다.\n" +
                    $"{MeshPath}\n{MaterialPath}\n{AlbedoPath}\n{NormalPath}");
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
