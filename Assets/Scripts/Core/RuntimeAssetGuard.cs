using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Core
{
    /// <summary>
    /// 런타임 코드가 구워진 에셋을 직접 고치는 것을 막는다.
    ///
    /// M9에서 테이블 프롭을 프리팹으로 구운 뒤, 애니메이션이 <c>sharedMesh</c>와
    /// <c>sharedMaterial</c>을 통해 에셋 파일을 계속 덮어쓰는 문제가 반복해서 나왔다.
    /// 코스믹 큐브의 테서랙트 메시, 모래시계의 모래 메시와 발광 색이 그 예다.
    ///
    /// 원인은 하나다. 굽기 전에는 이 참조들이 런타임 생성물이라 마음대로 써도 됐지만,
    /// 굽기 후에는 디스크의 에셋을 가리킨다. 쓰기 전에 이 헬퍼로 한 번 걸러야 한다.
    ///
    /// 플레이 모드에서는 Unity가 <c>Renderer.material</c>에서 사본을 만들어 준다.
    /// 에디터에서는 그 경로가 없으므로 <c>DontSave</c> 사본을 직접 만들어 끼운다.
    ///
    /// 그 사본은 씬에 직렬화되지 않으므로, 끼워 둔 채로 씬을 저장하면 참조가 null로 기록된다.
    /// 프리팹 인스턴스에서는 그 null이 오버라이드로 남아 씬을 다시 열어도 머티리얼이 비어 있다.
    /// 그래서 사본과 원본 에셋을 짝지어 기억해 두고, 저장 직전에 구운 에셋으로 되돌린 뒤
    /// 저장이 끝나면 같은 사본을 다시 끼운다. 그 시점을 잡아 주는 것은
    /// <c>Assets/Editor/RuntimeAssetGuardSceneHook.cs</c>다.
    /// </summary>
    public static class RuntimeAssetGuard
    {
#if UNITY_EDITOR
        private static readonly Dictionary<Renderer, Material> materialAssets = new();
        private static readonly Dictionary<Renderer, Material> materialClones = new();
        private static readonly Dictionary<MeshFilter, Mesh> meshAssets = new();
        private static readonly Dictionary<MeshFilter, Mesh> meshClones = new();
#endif

        /// <summary>
        /// 값을 바꿔도 되는 머티리얼을 돌려준다.
        /// 에셋을 가리키고 있으면 씬에도 저장되지 않는 사본으로 갈아 끼운다.
        /// </summary>
        public static Material GetWritableMaterial(Renderer renderer)
        {
            if (renderer == null) return null;
            if (Application.isPlaying) return renderer.material;

            Material shared = renderer.sharedMaterial;
            if (!IsAsset(shared)) return shared;

            Material clone = new(shared) { name = shared.name, hideFlags = HideFlags.DontSave };
            renderer.sharedMaterial = clone;
#if UNITY_EDITOR
            materialAssets[renderer] = shared;
            materialClones[renderer] = clone;
#endif
            return clone;
        }

        /// <summary>
        /// 정점을 다시 써도 되는 메시를 돌려준다.
        /// 에셋을 가리키고 있으면 씬에도 저장되지 않는 사본으로 갈아 끼운다.
        ///
        /// 메시는 머티리얼과 달리 플레이 모드에서도 자동 사본이 생기지 않으므로
        /// <c>MeshFilter.mesh</c>에 기대지 않고 두 경우 모두 여기서 처리한다.
        /// </summary>
        public static Mesh GetWritableMesh(MeshFilter filter)
        {
            if (filter == null) return null;

            Mesh shared = filter.sharedMesh;
            if (!IsAsset(shared)) return shared;

            Mesh clone = Object.Instantiate(shared);
            clone.name = shared.name;
            clone.hideFlags = HideFlags.DontSave;
            filter.sharedMesh = clone;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                meshAssets[filter] = shared;
                meshClones[filter] = clone;
            }
#endif
            return clone;
        }

#if UNITY_EDITOR
        /// <summary>씬 저장 직전에 사본을 구운 에셋으로 되돌린다. 저장된 씬에는 에셋 참조가 남는다.</summary>
        public static void RestoreBakedAssets()
        {
            Swap(materialAssets, meshAssets);
        }

        /// <summary>저장이 끝난 뒤 원래 사본을 다시 끼운다. 저장 때문에 에디터 화면의 연출이 끊기지 않게 한다.</summary>
        public static void ReapplyEditorClones()
        {
            Swap(materialClones, meshClones);
        }

        private static void Swap(
            Dictionary<Renderer, Material> materials,
            Dictionary<MeshFilter, Mesh> meshes)
        {
            Prune();
            foreach (KeyValuePair<Renderer, Material> entry in materials)
            {
                entry.Key.sharedMaterial = entry.Value;
            }
            foreach (KeyValuePair<MeshFilter, Mesh> entry in meshes)
            {
                entry.Key.sharedMesh = entry.Value;
            }
        }

        /// <summary>파괴된 렌더러·필터와 참조를 잃은 항목을 지운다. 도메인 리로드 뒤 남은 껍데기를 정리한다.</summary>
        private static void Prune()
        {
            List<Renderer> deadRenderers = new();
            foreach (KeyValuePair<Renderer, Material> entry in materialAssets)
            {
                if (entry.Key == null || entry.Value == null ||
                    !materialClones.TryGetValue(entry.Key, out Material clone) || clone == null)
                {
                    deadRenderers.Add(entry.Key);
                }
            }
            foreach (Renderer renderer in deadRenderers)
            {
                materialAssets.Remove(renderer);
                materialClones.Remove(renderer);
            }

            List<MeshFilter> deadFilters = new();
            foreach (KeyValuePair<MeshFilter, Mesh> entry in meshAssets)
            {
                if (entry.Key == null || entry.Value == null ||
                    !meshClones.TryGetValue(entry.Key, out Mesh clone) || clone == null)
                {
                    deadFilters.Add(entry.Key);
                }
            }
            foreach (MeshFilter filter in deadFilters)
            {
                meshAssets.Remove(filter);
                meshClones.Remove(filter);
            }
        }
#endif

        private static bool IsAsset(Object candidate)
        {
            if (candidate == null) return false;
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(candidate);
#else
            return false;
#endif
        }
    }
}
