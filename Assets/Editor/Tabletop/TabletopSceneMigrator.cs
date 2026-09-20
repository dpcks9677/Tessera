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
            // 모달 다이얼로그를 쓰지 않는다. 이 메뉴는 REST(unity-skills)로도 실행되며,
            // 모달이 뜨면 메인 스레드가 막혀 호출이 응답 없이 대기한다. 안전장치는 씬 사본이다.
            Scene scene = SceneManager.GetActiveScene();
            Transform layoutRoot = FindLayoutRoot();
            if (layoutRoot == null)
            {
                Debug.LogError($"[TabletopSceneMigrator] 씬에서 '{LayoutRootName}' 오브젝트를 찾지 못했습니다.");
                return;
            }

            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            if (prefabPaths.Length == 0)
            {
                Debug.LogError($"[TabletopSceneMigrator] {PrefabFolder} 에 프리팹이 없습니다. 먼저 'Bake Tabletop Prefabs'를 실행하십시오.");
                return;
            }

            if (!TrySaveBackup(scene)) return;

            List<string> replaced = new();
            List<string> notFound = new();

            foreach (string guid in prefabPaths)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                // 베이커가 프리팹 파일명을 씬 오브젝트 이름 그대로 쓰므로 루트 이름이 곧 조회 키다.
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

        /// <summary>
        /// 열려 있는 씬을 디스크 상태로 다시 읽어 메모리 변경을 버린다.
        /// 전환 작업 중 잘못된 재생성으로 씬이 오염됐을 때 복구용으로 쓴다.
        /// </summary>
        [MenuItem("Tessera/Tabletop/Reload Scene From Disk")]
        public static void ReloadSceneFromDisk()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[TabletopSceneMigrator] 저장된 적 없는 씬은 다시 읽을 수 없습니다.");
                return;
            }

            string path = scene.path;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log($"[TabletopSceneMigrator] 씬을 디스크에서 다시 읽었습니다: {path}");
        }

        /// <summary>
        /// 픽셀 필터를 거치지 않는 UI 경로를 씬에 굽는다(M9.5).
        ///
        /// 월드 카메라의 컬링 마스크에서 CrispUI 레이어를 빼고, 전용 카메라와 합성용 RawImage를
        /// 씬 오브젝트로 만든다. 렌더 타깃은 화면 해상도에 묶여 있어 런타임에 만들어진다.
        /// </summary>
        [MenuItem("Tessera/Tabletop/Setup Crisp UI Pipeline")]
        public static void SetupCrispUiPipeline()
        {
            var controller = Object.FindFirstObjectByType<Tessera.Games.AugmentedYacht.AugmentedYachtController>();
            if (controller == null)
            {
                Debug.LogError("[TabletopSceneMigrator] 씬에서 AugmentedYachtController를 찾지 못했습니다.");
                return;
            }

            if (!controller.SetupCrispUiSceneObjects())
            {
                Debug.LogError("[TabletopSceneMigrator] 월드 카메라를 찾지 못해 Crisp UI 경로를 구성하지 못했습니다.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            HidePresentationCanvasInSceneView();
            Debug.Log("[TabletopSceneMigrator] Crisp UI 카메라와 오버레이를 씬에 구성했습니다. 화면 UI 캔버스는 Scene View에서 숨겼습니다.");
        }

        private const string PresentationCanvasName = "Pixel Presentation";

        /// <summary>
        /// 화면 UI 캔버스를 Scene View에서 숨기거나 다시 보이게 한다.
        ///
        /// Screen Space Overlay 캔버스는 Scene View에서 화면 픽셀 크기(1920x1080)만 한
        /// 월드 평면으로 그려진다. 테이블이 40단위 남짓이라 작업 화면을 통째로 가린다.
        /// Scene Visibility는 에디터 표시에만 영향을 주고 플레이 모드와 빌드는 건드리지 않는다.
        /// 하이어라키의 눈 아이콘과 같은 기능이며, 이 메뉴는 그 토글을 한 번에 적용한다.
        /// </summary>
        [MenuItem("Tessera/Tabletop/Toggle Presentation Canvas In Scene View")]
        public static void TogglePresentationCanvasVisibility()
        {
            GameObject canvas = GameObject.Find(PresentationCanvasName);
            if (canvas == null)
            {
                Debug.LogError($"[TabletopSceneMigrator] 씬에서 '{PresentationCanvasName}'을 찾지 못했습니다.");
                return;
            }

            if (SceneVisibilityManager.instance.IsHidden(canvas))
            {
                SceneVisibilityManager.instance.Show(canvas, true);
                Debug.Log($"[TabletopSceneMigrator] '{PresentationCanvasName}'을 Scene View에 다시 표시합니다.");
            }
            else
            {
                SceneVisibilityManager.instance.Hide(canvas, true);
                Debug.Log($"[TabletopSceneMigrator] '{PresentationCanvasName}'을 Scene View에서 숨겼습니다. 플레이 모드와 빌드에는 영향이 없습니다.");
            }
        }

        private static void HidePresentationCanvasInSceneView()
        {
            GameObject canvas = GameObject.Find(PresentationCanvasName);
            if (canvas != null) SceneVisibilityManager.instance.Hide(canvas, true);
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
                Debug.LogError("[TabletopSceneMigrator] 저장되지 않은 씬입니다. 먼저 씬을 저장하십시오.");
                return false;
            }

            string backupPath = scene.path.Replace(".unity", "_pre-prefab.unity");
            if (!EditorSceneManager.SaveScene(scene, backupPath, true))
            {
                Debug.LogError("[TabletopSceneMigrator] 씬 사본 저장에 실패했습니다. 중단합니다.");
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
