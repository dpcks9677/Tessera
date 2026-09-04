using System;
using UnityEngine;

namespace Tessera.Tabletop
{
    /// <summary>
    /// 룬 석판과 족보 종이 사이의 빈 공간에 3종 장식 오브젝트를 정삼각형 구도로 배치 및 관리하는 클러스터
    /// - 상단 중앙: 절삭 마나 크리스탈 & 샤프 받침대
    /// - 하단 좌측: 스털링 실버 룬 반지
    /// - 하단 우측: 타원형 토파즈 브로치 & 짧은 골드 체인
    /// </summary>
    [ExecuteAlways]
    public sealed class TabletopTrinketCluster : MonoBehaviour
    {
        private const int DecorationLayer = 11;

        [Header("Trinket References")]
        [SerializeField] private TabletopTrinketRing ring;
        [SerializeField] private TabletopTrinketBrooch brooch;
        [SerializeField] private TabletopTrinketManaCrystal manaCrystal;

        public TabletopTrinketRing Ring => ring;
        public TabletopTrinketBrooch Brooch => brooch;
        public TabletopTrinketManaCrystal ManaCrystal => manaCrystal;

        private void Awake()
        {
            EnsureCluster();
        }

        private void OnEnable()
        {
            EnsureCluster();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= DelayEnsureCluster;
                UnityEditor.EditorApplication.delayCall += DelayEnsureCluster;
            }
        }

        private void DelayEnsureCluster()
        {
            if (this == null || gameObject == null) return;
            EnsureCluster();
        }
#endif

        public void EnsureCluster()
        {
            BindTrinkets();
            if (transform.childCount == 0 || IsClusterMissing())
            {
                BuildCluster();
            }
        }

        private void BindTrinkets()
        {
            if (ring == null) ring = GetComponentInChildren<TabletopTrinketRing>(true);
            if (brooch == null) brooch = GetComponentInChildren<TabletopTrinketBrooch>(true);
            if (manaCrystal == null) manaCrystal = GetComponentInChildren<TabletopTrinketManaCrystal>(true);
        }

        // 자식 존재 여부만 본다. 포즈로 판정하면 씬에서 옮긴 배치가 재생성으로 되돌아간다.
        private bool IsClusterMissing()
        {
            return transform.Find("Trinket_SilverRing") == null
                || transform.Find("Trinket_OvalBrooch") == null
                || transform.Find("Trinket_ManaCrystal") == null;
        }

        public static TabletopTrinketCluster Create(Transform parent, Vector3? worldPosition = null)
        {
            GameObject root = new("3D Trinket Cluster (Ring, Brooch, Crystal)");
            root.layer = DecorationLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = worldPosition ?? Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            TabletopTrinketCluster comp = root.AddComponent<TabletopTrinketCluster>();
            comp.BuildCluster();
            return comp;
        }

        [ContextMenu("Rebuild Trinket Cluster")]
        public void BuildCluster()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying)
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // 1. 하단 좌측 - 밴드와 보석 측면이 함께 보이도록 비스듬히 눕힌 은반지
            Vector3 ringPos = new(2.15f, 0.42f, 5.20f);
            ring = TabletopTrinketRing.Create(transform, ringPos, Quaternion.Euler(62f, -25f, 0f), Vector3.one * 1.20f);

            // 2. 하단 우측 - 독자적인 타원형 브로치와 짧은 체인
            Vector3 broochPos = new(3.45f, 0.09f, 5.25f);
            brooch = TabletopTrinketBrooch.Create(transform, broochPos, Quaternion.Euler(0f, 12f, 0f), Vector3.one * 1.10f);

            // 3. 상단 중앙 - 룬 석판과 족보 사이의 쿨 컬러 포인트
            Vector3 crystalPos = new(2.75f, 0.10f, 6.45f);
            manaCrystal = TabletopTrinketManaCrystal.Create(transform, crystalPos, Quaternion.Euler(0f, -12f, 0f), Vector3.one * 1.10f);
        }
    }
}
