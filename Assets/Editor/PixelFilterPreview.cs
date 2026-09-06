using Tessera.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tessera.Editor
{
    /// <summary>
    /// 플레이를 누르기 전 에디터 프리뷰를 게임 시작 상태와 맞춘다.
    ///
    /// 픽셀 필터 사슬(월드 카메라 → 렌더 타깃 → 업스케일 RawImage)은 씬에 구워져 있어 편집 모드에서도
    /// 돈다. 그런데 그 값을 정하는 <see cref="AugmentedYachtController"/>는 플레이 모드에서만 살아나므로,
    /// 시작 해상도를 640x360에서 480x270으로 바꾼 뒤에도 씬에 구워진 업스케일 재질은 640x360으로 남아
    /// 있었다. 엣지 필터를 받을 <c>PixelEdgeCamera</c> 표시도 런타임에만 붙어 편집 모드에서는 외곽선이
    /// 아예 없었다. 결과적으로 플레이 전 화면과 플레이 직후 화면이 서로 달랐다.
    ///
    /// 이 도구가 그 차이를 없앤다. 값의 출처는 <see cref="PixelFilterSettings"/> 하나다.
    /// 바꿀 것이 있을 때만 씬을 더럽히므로, 이미 맞아 있으면 씬을 건드리지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class PixelFilterPreview
    {
        private const string UpscaleObjectName = "Point Upscale";
        private const string WorldCameraName = "Full Field World Camera";
        private const string LegacyWorldCameraName = "Low Resolution World Camera";

        private static readonly int PixelEdgeVirtualResolutionId = Shader.PropertyToID("_PixelEdgeVirtualResolution");

        static PixelFilterPreview()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            // 도메인 리로드 직후에도 한 번 맞춘다. 전역 유니폼은 리로드로 초기화되기 때문이다.
            EditorApplication.delayCall += () => Sync(logWhenAlreadyInSync: false);
        }

        [MenuItem("Tools/Tessera/Sync Pixel Filter Preview")]
        public static void SyncFromMenu()
        {
            Sync(logWhenAlreadyInSync: true);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Sync(logWhenAlreadyInSync: false);
        }

        /// <summary>
        /// 편집 모드 프리뷰를 게임 시작값으로 맞춘다. 플레이 중에는 컨트롤러가 주인이므로 아무것도 하지 않는다.
        /// </summary>
        private static void Sync(bool logWhenAlreadyInSync)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Vector2Int resolution = PixelFilterSettings.StartResolution;
            Vector4 resolutionVector = new(resolution.x, resolution.y, 0f, 0f);

            // 엣지 패스는 재질이 아니라 전역 값에서 격자를 읽는다. 씬을 더럽히지 않으므로 항상 갱신한다.
            Shader.SetGlobalVector(PixelEdgeVirtualResolutionId, resolutionVector);

            bool materialChanged = SyncUpscaleMaterial(resolutionVector);
            bool markerChanged = SyncPixelEdgeMarker();

            if (!materialChanged && !markerChanged)
            {
                if (logWhenAlreadyInSync)
                {
                    Debug.Log($"[PixelFilterPreview] 이미 게임 시작값과 같습니다. {resolution.x}x{resolution.y}, Quantize {PixelFilterSettings.StartQuantizeMode}.");
                }
                return;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[PixelFilterPreview] 프리뷰를 게임 시작값으로 맞췄습니다. {resolution.x}x{resolution.y}, Quantize {PixelFilterSettings.StartQuantizeMode}. 씬을 저장하십시오.");
        }

        /// <summary>씬에 구워진 업스케일 재질을 게임 시작값으로 맞춘다. 바뀐 것이 있으면 true를 준다.</summary>
        private static bool SyncUpscaleMaterial(Vector4 resolutionVector)
        {
            Material material = FindUpscaleMaterial();
            if (material == null) return false;

            bool changed = false;

            if (material.HasProperty("_VirtualResolution")
                && material.GetVector("_VirtualResolution") != resolutionVector)
            {
                material.SetVector("_VirtualResolution", resolutionVector);
                changed = true;
            }

            if (material.HasProperty("_Quantize")
                && !Mathf.Approximately(material.GetFloat("_Quantize"), PixelFilterSettings.StartQuantizeMode))
            {
                material.SetFloat("_Quantize", PixelFilterSettings.StartQuantizeMode);
                changed = true;
            }

            // 팔레트는 배열이라 값 비교가 번거롭다. 씬에 저장되지 않는 유니폼이므로 항상 넣고 변경으로 세지 않는다.
            Vector4[] palette = TesseraPixelPalette.BuildShaderArray(out int paletteCount);
            material.SetVectorArray("_PaletteColors", palette);
            material.SetFloat("_PaletteCount", paletteCount);

            if (changed) EditorUtility.SetDirty(material);
            return changed;
        }

        /// <summary>
        /// 월드 카메라에 엣지 패스 표시를 붙인다. 런타임에만 붙으면 편집 모드에서는 외곽선이 없다.
        /// </summary>
        private static bool SyncPixelEdgeMarker()
        {
            Camera worldCamera = FindWorldCamera();
            if (worldCamera == null) return false;
            if (worldCamera.GetComponent<PixelEdgeCamera>() != null) return false;

            Undo.AddComponent<PixelEdgeCamera>(worldCamera.gameObject);
            return true;
        }

        private static Material FindUpscaleMaterial()
        {
            GameObject upscaleObject = GameObject.Find(UpscaleObjectName);
            RawImage image = upscaleObject != null ? upscaleObject.GetComponent<RawImage>() : null;
            return image != null ? image.material : null;
        }

        private static Camera FindWorldCamera()
        {
            GameObject found = GameObject.Find(WorldCameraName) ?? GameObject.Find(LegacyWorldCameraName);
            return found != null ? found.GetComponent<Camera>() : null;
        }
    }
}
