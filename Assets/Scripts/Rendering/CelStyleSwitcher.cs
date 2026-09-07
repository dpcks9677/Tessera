using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 씬의 URP Lit 재질을 셀 재질로 바꾸고 되돌린다(M10.8-T4).
    ///
    /// 테이블·러너·소품의 재질 생성 지점은 <c>Assets/Scripts/Tabletop/</c> 아래에 흩어져 있다.
    /// 그 지점마다 분기를 넣으면 롤백 지점이 열 곳 넘게 생긴다. 대신 이미 만들어진 렌더러를 훑어
    /// 재질을 교체하고 원본을 들고 있다가 되돌린다. 채택하지 않기로 하면 이 파일과 호출 한 줄만
    /// 걷어내면 되고, 빌더 코드는 손대지 않은 상태로 남는다.
    ///
    /// 주사위는 <c>DiceVisualPool</c>이 직접 관리하므로 여기서 제외한다.
    /// </summary>
    public sealed class CelStyleSwitcher
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private readonly Dictionary<Material, Material> celByOriginal = new();

        private RenderStyle currentStyle = RenderStyle.Baseline;

        public RenderStyle CurrentStyle => currentStyle;

        /// <summary>변환한 재질 수. 검증 도구와 로그가 읽는다.</summary>
        public int ConvertedRendererCount => originalMaterials.Count;

        public void Apply(Transform root, RenderStyle style, int excludedLayerMask)
        {
            if (root == null || currentStyle == style) return;
            currentStyle = style;

            if (style == RenderStyle.Cel) ConvertToCel(root, excludedLayerMask);
            else RestoreOriginals();
        }

        private void ConvertToCel(Transform root, int excludedLayerMask)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (((1 << renderer.gameObject.layer) & excludedLayerMask) != 0) continue;

                Material[] source = renderer.sharedMaterials;
                if (source == null || source.Length == 0) continue;

                Material[] replaced = null;
                for (int index = 0; index < source.Length; index++)
                {
                    Material celMaterial = CelVariantOf(source[index]);
                    if (celMaterial == null) continue;

                    replaced ??= (Material[])source.Clone();
                    replaced[index] = celMaterial;
                }

                if (replaced == null) continue;

                originalMaterials[renderer] = source;
                renderer.sharedMaterials = replaced;
            }
        }

        private void RestoreOriginals()
        {
            foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
            {
                if (entry.Key == null) continue;
                entry.Key.sharedMaterials = entry.Value;
            }
            originalMaterials.Clear();
        }

        /// <summary>
        /// 만들어 둔 셀 재질을 파괴하고 원본 재질을 되돌린다. 컨트롤러의 OnDestroy에서 부른다.
        /// 부르지 않으면 도메인 리로드 전까지 재질이 남는다.
        /// </summary>
        public void Dispose()
        {
            RestoreOriginals();

            foreach (Material material in celByOriginal.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) Object.Destroy(material);
                else Object.DestroyImmediate(material);
            }
            celByOriginal.Clear();
            currentStyle = RenderStyle.Baseline;
        }

        /// <summary>
        /// Lit 재질 하나에 대응하는 셀 재질을 만들거나 캐시에서 준다. Lit이 아니면 null을 준다.
        /// 이미 Unlit인 재질(촛대·룬 슬레이트 채널 등)은 두 모드 공용이라 그대로 둔다.
        /// </summary>
        private Material CelVariantOf(Material original)
        {
            if (original == null || original.shader == null) return null;
            if (original.shader.name != LitShaderName) return null;
            if (celByOriginal.TryGetValue(original, out Material cached) && cached != null) return cached;

            Color baseColor = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
            Texture baseMap = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null;
            float metallic = original.HasProperty("_Metallic") ? original.GetFloat("_Metallic") : 0f;

            // 금속으로 읽히던 재질(금색 트림 등)은 밴드를 하나 더 줘서 스페큘러가 사라진 자리를 메운다.
            int bands = metallic > 0.5f ? CelMaterialFactory.MetallicBands : CelMaterialFactory.DiffuseBands;

            Material celMaterial = CelMaterialFactory.Create(
                $"{original.name}_Cel",
                baseColor,
                bands,
                snapNormal: false,
                receiveShadows: original.HasProperty("_ReceiveShadows") ? original.GetFloat("_ReceiveShadows") > 0.5f : true,
                baseMap);

            if (celMaterial == null) return null;

            celByOriginal[original] = celMaterial;
            return celMaterial;
        }
    }
}
