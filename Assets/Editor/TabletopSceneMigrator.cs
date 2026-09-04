using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 씬에 코드로 구워져 있는 테이블 프롭을 프리팹 인스턴스로 교체한다(M9-T4).
    ///
    /// 현재 배치를 그대로 보존하는 것이 목적이다. 교체 전에 각 프롭의 로컬 포즈를 기록하고,
    /// 프리팹 인스턴스를 만든 뒤 같은 포즈를 복원한다. 따라서 화면상 결과는 변하지 않고
    /// 소유권만 "코드"에서 "씬"으로 옮겨간다.
    ///
    /// 한 번만 실행하는 도구다. 실행 전 씬 사본을 남긴다.
    /// </summary>
    public static class TabletopSceneMigrator
    {
        private const string PrefabFolder = "Assets/Prefabs/Tabletop";
        private const string LayoutRootName = "Graphics Layout";

        private sealed class PropPose
        {
            public string Name;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public int SiblingIndex;
            public bool ActiveSelf;
            public int Layer;
        }

        [MenuItem("Tessera/Tabletop/Migrate Scene To Prefab Instances")]
        public static void Migrate()
        {
            Scene scene = SceneManager.GetActiveScene();
            Transform layoutRoot = FindLayoutRoot();
            if (layoutRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "씬 프리팹 전환",
                    "씬에서 '" + LayoutRootName + "' 오브젝트를 찾지 못했습니다.",
                    "확인");
                return;
            }

            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            if (prefabPaths.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "씬 프리팹 전환",
                    PrefabFolder + " 에 프리팹이 없습니다. 먼저 'Bake Tabletop Prefabs'를 실행하십시오.",
                    "확인");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                "씬 프리팹 전환",
                $"'{scene.name}' 씬의 테이블 프롭을 프리팹 인스턴스로 교체합니다.\n" +
                "실행 전에 씬 사본이 저장됩니다. 계속하시겠습니까?",
                "실행",
                "취소");
            if (!proceed) return;

            if (!TrySaveBackup(scene)) return;

            List<string> replaced = new();
            List<string> notFound = new();

            foreach (string guid in prefabPaths)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                Transform existing = layoutRoot.Find(prefab.name);
                if (existing == null)
                {
                    notFound.Add(prefab.name);
                    continue;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(existing.gameObject))
                {
                    // 이미 전환된 프롭은 건너뛴다. 이 도구는 여러 번 실행해도 안전해야 한다.
                    continue;
                }

                PropPose pose = Capture(existing);
                Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layoutRoot);
                Undo.RegisterCreatedObjectUndo(instance, "테이블 프롭 프리팹 전환");
                Apply(instance.transform, pose);
                replaced.Add(prefab.name);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[TabletopSceneMigrator] 프롭 {replaced.Count}종을 프리팹 인스턴스로 교체했습니다.\n{string.Join("\n", replaced)}");
            if (notFound.Count > 0)
            {
                Debug.LogWarning($"[TabletopSceneMigrator] 씬에서 대응 오브젝트를 찾지 못한 프리팹: {string.Join(", ", notFound)}");
            }
        }

        private static PropPose Capture(Transform source)
        {
            return new PropPose
            {
                Name = source.name,
                LocalPosition = source.localPosition,
                LocalRotation = source.localRotation,
                LocalScale = source.localScale,
                SiblingIndex = source.GetSiblingIndex(),
                ActiveSelf = source.gameObject.activeSelf,
                Layer = source.gameObject.layer
            };
        }

        private static void Apply(Transform target, PropPose pose)
        {
            target.name = pose.Name;
            target.localPosition = pose.LocalPosition;
            target.localRotation = pose.LocalRotation;
            target.localScale = pose.LocalScale;
            target.SetSiblingIndex(pose.SiblingIndex);
            target.gameObject.SetActive(pose.ActiveSelf);
            target.gameObject.layer = pose.Layer;
        }

        private static bool TrySaveBackup(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("씬 프리팹 전환", "저장되지 않은 씬입니다. 먼저 씬을 저장하십시오.", "확인");
                return false;
            }

            string backupPath = scene.path.Replace(".unity", "_pre-prefab.unity");
            if (!EditorSceneManager.SaveScene(scene, backupPath, true))
            {
                EditorUtility.DisplayDialog("씬 프리팹 전환", "씬 사본 저장에 실패했습니다. 중단합니다.", "확인");
                return false;
            }

            Debug.Log($"[TabletopSceneMigrator] 씬 사본을 저장했습니다: {backupPath}");
            return true;
        }

        private static Transform FindLayoutRoot()
        {
            GameObject root = GameObject.Find(LayoutRootName);
            return root != null ? root.transform : null;
        }
    }
}
