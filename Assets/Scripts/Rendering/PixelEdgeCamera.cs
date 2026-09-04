using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 이 카메라만 픽셀 엣지 패스를 받는다는 표시다(M10.5-T2).
    ///
    /// 렌더러 피처는 렌더러 에셋에 등록되므로 그 에셋을 쓰는 모든 카메라에서 후보가 된다.
    /// 월드 카메라 외에 Crisp UI 카메라와 Display 1 카메라도 같은 에셋을 쓰기 때문에,
    /// 붙어 있는 카메라에서만 패스를 돌리도록 이 표시를 기준으로 삼는다.
    ///
    /// <see cref="EdgeFilterEnabled"/>를 끄면 패스 자체가 등록되지 않는다. 블릿 비용까지
    /// 사라지므로 엣지 이전의 픽셀 필터와 완전히 같은 경로가 되고, 그래야 A/B 비교가 정직해진다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PixelEdgeCamera : MonoBehaviour
    {
        [SerializeField] private bool edgeFilterEnabled = true;

        public bool EdgeFilterEnabled
        {
            get => edgeFilterEnabled;
            set => edgeFilterEnabled = value;
        }
    }
}
