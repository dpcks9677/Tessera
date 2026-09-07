using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Tessera.Games.AugmentedYacht
{
    /// <summary>
    /// 주사위 효과음을 StreamingAssets에서 읽어 재생 준비까지 마친다(M10-T4).
    ///
    /// 파일이 없거나 읽기에 실패해도 게임은 그대로 진행된다. 소리는 있으면 좋은 것이지
    /// 없으면 멈춰야 하는 것이 아니다.
    /// </summary>
    public sealed class YachtAudioService : MonoBehaviour
    {
        private static readonly string[] RollFileNames =
        {
            "dice_roll.mp3", "dice-throw-1.ogg", "dice-throw-2.ogg", "dice-throw-3.ogg"
        };

        private static readonly string[] ImpactFileNames =
        {
            "die-throw-1.ogg", "die-throw-2.ogg", "die-throw-3.ogg", "die-throw-4.ogg"
        };

        private readonly List<AudioClip> rollClips = new();
        private readonly List<AudioClip> impactClips = new();

        private AudioSource source;

        public AudioSource Source => source;
        public AudioClip[] RollClips => rollClips.ToArray();
        public AudioClip[] ImpactClips => impactClips.ToArray();

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

        /// <summary>효과음을 비동기로 읽어들인다. 호출한 쪽이 코루틴으로 돌린다.</summary>
        public IEnumerator LoadClipsAsync()
        {
            EnsureSource();

            string soundsPath = Path.Combine(Application.streamingAssetsPath, "WebSource", "sounds");
            if (!Directory.Exists(soundsPath)) yield break;

            yield return LoadInto(soundsPath, RollFileNames, rollClips);
            yield return LoadInto(soundsPath, ImpactFileNames, impactClips);

            ClipsReady?.Invoke(source, rollClips.ToArray(), impactClips.ToArray());
        }

        private static IEnumerator LoadInto(string folder, string[] fileNames, List<AudioClip> target)
        {
            foreach (string fileName in fileNames)
            {
                string path = Path.Combine(folder, fileName);
                if (!File.Exists(path)) continue;

                yield return LoadAudioClip(path, clip => target.Add(clip));
            }
        }

        private static IEnumerator LoadAudioClip(string filePath, Action<AudioClip> onLoaded)
        {
            string uri = "file://" + filePath.Replace("\\", "/");
            AudioType audioType = filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                ? AudioType.MPEG
                : AudioType.OGGVORBIS;

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) yield break;

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null) yield break;

            clip.name = Path.GetFileNameWithoutExtension(filePath);
            onLoaded?.Invoke(clip);
        }
    }
}
