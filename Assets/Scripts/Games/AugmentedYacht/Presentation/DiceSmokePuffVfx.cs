using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// `dice-alchemy`(56) 발동 시 킵 안 된 주사위 위로 연기를 터뜨려 눈이 바뀌는 순간을 가린다(M17-T9-1-4).
    ///
    /// 절차적 파티클 대신 외부 에셋팩(`msVFX_Free Smoke Effects Pack`) 프리팹을 위치마다 인스턴스화한다.
    /// 게임당 1회 연출이라 풀링하지 않는다.
    /// </summary>
    public sealed class DiceSmokePuffVfx : MonoBehaviour
    {
        private const string PrefabResourcePath = "Vfx/DiceSmokeBurst";
        private const float LifetimeMargin = 0.2f;

        private static GameObject prefab;
        private static bool prefabLoadAttempted;

        private Camera viewCamera;

        /// <summary>지정한 월드 위치마다 연기 프리팹을 하나씩 터뜨린다.</summary>
        public void Burst(IReadOnlyList<Vector3> worldPositions)
        {
            if (worldPositions == null || worldPositions.Count == 0) return;

            if (!prefabLoadAttempted)
            {
                prefabLoadAttempted = true;
                prefab = Resources.Load<GameObject>(PrefabResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[DiceSmokePuffVfx] '{PrefabResourcePath}' 프리팹을 찾지 못해 연기 연출을 건너뛴다.");
                }
            }

            if (prefab == null) return;

            Quaternion rotation = ResolveBurstRotation();

            foreach (Vector3 position in worldPositions)
            {
                // 부모를 두지 않고 월드 공간에 그대로 둔다. `DiceVisualPool`은 풀이 아니라
                // 눈이 바뀔 때마다 주사위 오브젝트를 새로 만들고 이전 것을 Destroy하므로,
                // 주사위 자식으로 붙이면 연기가 주사위와 함께 사라져 가림 연출이 깨진다.
                GameObject instance = Instantiate(prefab, position, rotation);

                // 프리팹이 이미 Dice 레이어여도 코드에서 한 번 더 맞춘다. 프리팹이 교체되거나
                // 변형(Prefab Variant) 오버라이드가 풀려도 연기가 픽셀 필터를 그대로 타야 한다.
                SetLayerRecursive(instance.transform, TesseraLayers.Dice);

                ParticleSystem particles = instance.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    // playOnAwake로 보통 자동 재생되지만, 프리팹 값이 바뀌어도 재생을 보장한다.
                    particles.Play();

                    // 수명을 상수로 박지 않고 파티클 값에서 계산한다. 프리팹 지속시간을 바꿔도
                    // 인스턴스가 잘리거나 남지 않게 한다.
                    ParticleSystem.MainModule main = particles.main;
                    float lifetime = main.duration + main.startLifetime.constantMax + LifetimeMargin;
                    Destroy(instance, lifetime);
                }
                else
                {
                    Destroy(instance, LifetimeMargin);
                }
            }
        }

        /// <summary>
        /// 연기 방출면을 화면과 나란히 세우는 회전이다.
        ///
        /// 프리팹의 Shape는 원뿔이고 방출면은 그 축과 직각이다. 회전을 주지 않으면 방출면이 월드 축을
        /// 따라 서므로, 테이블을 비스듬히 내려보는 이 게임의 카메라에서는 그 면이 옆에서 보여 연기가
        /// 선 하나로 납작해진다. 원뿔 축을 카메라 정면 벡터의 반대로 돌리면 방출면이 화면과 나란해져
        /// 주사위를 덮는 원으로 퍼진다.
        ///
        /// 주사위마다 카메라 쪽을 개별로 바라보게 하지 않고 정면 벡터 하나를 공유한다. 개별로 바라보면
        /// 화면 가장자리 주사위의 연기가 안쪽으로 기울어 같은 연출이 주사위마다 다르게 보인다.
        /// </summary>
        private Quaternion ResolveBurstRotation()
        {
            if (viewCamera == null) viewCamera = ResolveDiceCamera();
            if (viewCamera == null) return Quaternion.identity;

            return Quaternion.LookRotation(-viewCamera.transform.forward, Vector3.up);
        }

        /// <summary>
        /// 주사위를 그리는 카메라를 찾는다. 태그가 아니라 컬링 마스크로 고르는 이유는, 이 씬이
        /// 월드·프레젠테이션·Crisp UI 카메라를 함께 쓰고 그중 주사위 레이어를 보는 것만이 연기의
        /// 기준이기 때문이다.
        /// </summary>
        private static Camera ResolveDiceCamera()
        {
            int diceMask = TesseraLayers.Mask(TesseraLayers.Dice);
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && (cameras[i].cullingMask & diceMask) != 0) return cameras[i];
            }

            return Camera.main;
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
            {
                SetLayerRecursive(t.GetChild(i), layer);
            }
        }
    }
}
