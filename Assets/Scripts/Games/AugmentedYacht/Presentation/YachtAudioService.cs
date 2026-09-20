using System;
using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 주사위 효과음 재생 준비를 마친다(M10-T4).
    ///
    /// 효과음 클립은 씬에서 직렬화 필드로 할당한다. 비어 있어도 게임은 그대로 진행된다. 소리는
    /// 있으면 좋은 것이지 없으면 멈춰야 하는 것이 아니다.
    /// </summary>
    public sealed class YachtAudioService : MonoBehaviour
    {
        [SerializeField] private AudioClip[] rollClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] impactClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip coinTossClip;

        private AudioSource source;

        public AudioSource Source => source;
        public AudioClip[] RollClips => (AudioClip[])rollClips.Clone();
        public AudioClip[] ImpactClips => (AudioClip[])impactClips.Clone();
        public AudioClip CoinTossClip => coinTossClip;

        /// <summary>적재가 끝나면 알린다. 아직 한 개도 못 읽었을 수 있다.</summary>
        public event Action<AudioSource, AudioClip[], AudioClip[]> ClipsReady;

        /// <summary>재생에 쓸 AudioSource를 마련한다.</summary>
        public void EnsureSource()
        {
            if (source != null) return;

            source = GetComponent<AudioSource>();
            if (source != null) return;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        /// <summary>씬에서 할당된 클립을 구독자에게 알린다.</summary>
        public void PublishClips()
        {
            EnsureSource();
            ClipsReady?.Invoke(source, RollClips, ImpactClips);
        }

        /// <summary>코인 토스 효과음을 재생한다. 클립이 비어 있으면 조용히 넘어간다.</summary>
        public void PlayCoinToss()
        {
            EnsureSource();
            if (coinTossClip == null) return;
            source.PlayOneShot(coinTossClip);
        }
    }
}
