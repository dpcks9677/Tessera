using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Tessera.Core;
using UnityEngine;

namespace Tessera.Dice
{
    /// <summary>동전 스핀 클립 한 프레임(로컬 위치·회전)(M17-T24).</summary>
    public readonly struct CoinSpinFrame
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public CoinSpinFrame(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// CoinSpinPresetBaker(Editor)가 구운 동전 스핀 궤적 하나(M17-T24).
    ///
    /// 원점은 착지 정지 지점이고, <see cref="LandFrame"/>은 공중 구간이 끝나는 프레임 인덱스다.
    /// </summary>
    public sealed class CoinSpinClip
    {
        public readonly float Fps;
        public readonly int LandFrame;
        public readonly CoinSpinFrame[] Frames;

        public CoinSpinClip(float fps, int landFrame, CoinSpinFrame[] frames)
        {
            Fps = fps;
            LandFrame = landFrame;
            Frames = frames;
        }
    }

    /// <summary>
    /// `coin-toss` 증강 발동 시 동전 3개를 던져 앞/뒤를 정하는 연출(M17-T24).
    ///
    /// 물리 시뮬 대신 에디터에서 구운 해석적 스핀 프리셋(<see cref="CoinSpinClip"/>) 5종을 재생한다.
    /// 동전마다 프리셋을 중복 없이 골라 스핀 속도를 다르게 보이게 한다. 게임당 1회 연출이라
    /// 풀링하지 않는다(Tessera.Games.AugmentedYacht.DiceSmokePuffVfx 선례).
    ///
    /// 코루틴 실행 주체는 호출 측 MonoBehaviour다. <see cref="Play"/>는 IEnumerator만 반환한다.
    /// </summary>
    public sealed class CoinTossVfx
    {
        public const int PresetCount = 5;
        public const int Fps = 30;

        /// <summary>런타임 프리팹 리소스 경로. unity-operator가 Coin.prefab Variant로 만든다.</summary>
        public const string PrefabResourcePath = "Vfx/CoinTossCoin";

        /// <summary>
        /// 스핀 프리셋 JSON 리소스 경로 규칙. CoinSpinPresetBaker(Editor)와 공유한다.
        /// 실제 자산 경로는 Assets/Resources/{ClipResourceFolder}/{ClipFileNamePrefix}{spinIndex}.json.
        /// </summary>
        public const string ClipResourceFolder = "Vfx/CoinSpin";
        public const string ClipFileNamePrefix = "coin_spin_";

        /// <summary>
        /// 동전 3개의 X축 배치 오프셋(centerSectionX 기준). 트레이 플레이 경계는 X [-3.0, 3.0]
        /// (<see cref="DiceBoardMetrics.PlayBoundsMinX"/>·<see cref="DiceBoardMetrics.PlayBoundsMaxX"/>)이고
        /// 동전 반경이 0.8이라 이 배치면 트레이 안에 다 들어간다.
        /// </summary>
        public static readonly float[] CoinOffsetsX = { -1.8f, 0f, 1.8f };

        /// <summary>트레이 비주얼의 고정 Z 오프셋과 같다. 주사위·동전이 같은 트레이 중심을 공유한다.</summary>
        public const float CoinRestZ = DiceBoardMetrics.TrayCenterZ;

        /// <summary>트레이 플레이 바닥(RollSurfaceY) + 동전 두께 절반 0.08.</summary>
        public const float CoinRestY = DiceBoardMetrics.RollSurfaceY + 0.08f;

        /// <summary>동전 i가 시작을 늦추는 간격(초).</summary>
        private const float StartStagger = 0.12f;

        /// <summary>모든 동전이 정지한 뒤 유지하는 시간(초). 사용자 확인: 착지 후 2초 대기.</summary>
        private const float HoldDuration = 2f;

        private static GameObject prefab;
        private static CoinSpinClip[] presetClips;
        private static bool resourcesLoadAttempted;
        private static bool resourcesValid;

        private readonly List<GameObject> spawnedCoins = new();

        /// <summary>동전 i의 배치 앵커(월드 좌표).</summary>
        public static Vector3 ResolveAnchor(int i, float centerSectionX)
        {
            return new Vector3(centerSectionX + CoinOffsetsX[i], CoinRestY, CoinRestZ);
        }

        /// <summary>faces 비트마스크에서 coinIndex번째 동전이 앞면인지.</summary>
        public static bool IsHeads(int faces, int coinIndex) => (faces & (1 << coinIndex)) != 0;

        /// <summary>
        /// 프레임 회전을 착지 면에 맞춰 뒤집는다. 원판이 로컬 X축 180°에 대칭이라 실루엣과 궤적은
        /// 그대로 두고 착지 면만 반전된다.
        /// </summary>
        public static Quaternion ApplyFace(Quaternion frameRotation, bool heads)
        {
            return heads ? frameRotation : frameRotation * Quaternion.Euler(180f, 0f, 0f);
        }

        /// <summary>클립에서 시간 time(초)에 해당하는 자세를 보간한다. 범위를 넘으면 마지막 프레임을 반환한다.</summary>
        public static Pose Sample(CoinSpinClip clip, float time)
        {
            float frameFloat = time * clip.Fps;
            int frameIndex = Mathf.FloorToInt(frameFloat);
            if (frameIndex < 0) frameIndex = 0;

            if (frameIndex >= clip.Frames.Length - 1)
            {
                CoinSpinFrame last = clip.Frames[^1];
                return new Pose(last.Position, last.Rotation);
            }

            float t = frameFloat - frameIndex;
            CoinSpinFrame a = clip.Frames[frameIndex];
            CoinSpinFrame b = clip.Frames[frameIndex + 1];
            return new Pose(Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
        }

        /// <summary>프리셋 PresetCount개 중 3개를 중복 없이 고른다.</summary>
        public static int[] PickPresets(System.Random random)
        {
            var pool = new List<int>(PresetCount);
            for (int i = 0; i < PresetCount; i++) pool.Add(i);

            var picked = new int[3];
            for (int i = 0; i < picked.Length; i++)
            {
                int index = random.Next(pool.Count);
                picked[i] = pool[index];
                pool.RemoveAt(index);
            }
            return picked;
        }

        /// <summary>베이커가 쓴 JSON을 클립으로 되돌린다. 프레임은 [px,py,pz,qx,qy,qz,qw] 평탄 배열이다.</summary>
        public static CoinSpinClip Parse(string json)
        {
            JObject root = JObject.Parse(json);
            float fps = (float?)root["fps"] ?? Fps;
            int landFrame = (int?)root["landFrame"] ?? 0;

            var frameTokens = (JArray)root["frames"];
            var frames = new CoinSpinFrame[frameTokens.Count];
            for (int i = 0; i < frameTokens.Count; i++)
            {
                var values = (JArray)frameTokens[i];
                frames[i] = new CoinSpinFrame(
                    new Vector3((float)values[0], (float)values[1], (float)values[2]),
                    new Quaternion((float)values[3], (float)values[4], (float)values[5], (float)values[6]));
            }

            return new CoinSpinClip(fps, landFrame, frames);
        }

        /// <summary>
        /// 동전 3개를 던져 정지 자세까지 재생한다. faces는 코인별 앞/뒤 비트마스크(<see cref="IsHeads"/>),
        /// centerSectionX는 배치 기준 X 좌표다. 프리팹이나 프리셋을 못 찾으면 경고만 남기고 종료한다.
        /// onCoinSpinStarted는 동전이 자기 스태거 지연을 지나 스핀을 시작하는 순간마다(동전당 1회, 총 3회) 호출된다.
        /// </summary>
        public System.Collections.IEnumerator Play(int faces, float centerSectionX, Action onCoinSpinStarted = null)
        {
            if (!EnsureResourcesLoaded()) yield break;

            int[] presetIndices = PickPresets(new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue)));

            int coinCount = CoinOffsetsX.Length;
            var coins = new GameObject[coinCount];
            var anchors = new Vector3[coinCount];
            var clipEnds = new float[coinCount];
            float maxClipEnd = 0f;

            for (int i = 0; i < coinCount; i++)
            {
                GameObject coin = UnityEngine.Object.Instantiate(prefab);
                coins[i] = coin;
                spawnedCoins.Add(coin);
                anchors[i] = ResolveAnchor(i, centerSectionX);

                CoinSpinClip clip = presetClips[presetIndices[i]];
                // 늦게 출발하는 동전이 대기 중 원점에 보이지 않도록 첫 프레임 자세로 둔다.
                CoinSpinFrame first = clip.Frames[0];
                coin.transform.SetPositionAndRotation(
                    anchors[i] + first.Position, ApplyFace(first.Rotation, IsHeads(faces, i)));
                float startDelay = i * StartStagger;
                clipEnds[i] = startDelay + (clip.Frames.Length - 1) / clip.Fps;
                maxClipEnd = Mathf.Max(maxClipEnd, clipEnds[i]);
            }

            var spinStarted = new bool[coinCount];
            float elapsed = 0f;
            while (elapsed < maxClipEnd)
            {
                for (int i = 0; i < coinCount; i++)
                {
                    if (coins[i] == null) continue;
                    float local = elapsed - i * StartStagger;
                    if (local < 0f) continue;

                    if (!spinStarted[i])
                    {
                        spinStarted[i] = true;
                        onCoinSpinStarted?.Invoke();
                    }

                    CoinSpinClip clip = presetClips[presetIndices[i]];
                    Pose pose = Sample(clip, local);
                    coins[i].transform.SetPositionAndRotation(
                        anchors[i] + pose.position, ApplyFace(pose.rotation, IsHeads(faces, i)));
                }
                yield return null;
                elapsed += Time.deltaTime;
            }

            // 마지막 자세를 확정한다. 프레임 스텝이 클립 길이를 살짝 넘거나 못 미칠 수 있어 정지 자세를 보장한다.
            for (int i = 0; i < coinCount; i++)
            {
                if (coins[i] == null) continue;
                CoinSpinClip clip = presetClips[presetIndices[i]];
                Pose pose = Sample(clip, clipEnds[i] - i * StartStagger);
                coins[i].transform.SetPositionAndRotation(
                    anchors[i] + pose.position, ApplyFace(pose.rotation, IsHeads(faces, i)));
            }

            yield return new WaitForSeconds(HoldDuration);
        }

        /// <summary>생성한 동전을 전부 파괴한다. 여러 번 호출해도 안전하다.</summary>
        public void Cleanup()
        {
            foreach (GameObject coin in spawnedCoins)
            {
                if (coin != null) UnityEngine.Object.Destroy(coin);
            }
            spawnedCoins.Clear();
        }

        private static bool EnsureResourcesLoaded()
        {
            if (resourcesLoadAttempted) return resourcesValid;
            resourcesLoadAttempted = true;

            prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CoinTossVfx] '{PrefabResourcePath}' 프리팹을 찾지 못해 동전 연출을 건너뛴다.");
                return false;
            }

            presetClips = new CoinSpinClip[PresetCount];
            for (int i = 0; i < PresetCount; i++)
            {
                string path = $"{ClipResourceFolder}/{ClipFileNamePrefix}{i}";
                TextAsset asset = Resources.Load<TextAsset>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"[CoinTossVfx] '{path}' 프리셋을 찾지 못해 동전 연출을 건너뛴다.");
                    return false;
                }
                presetClips[i] = Parse(asset.text);
            }

            resourcesValid = true;
            return true;
        }
    }
}
