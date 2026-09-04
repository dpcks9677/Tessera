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
    /// </summary>
    public static class RuntimeAssetGuard
    {
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
            return clone;
        }

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
