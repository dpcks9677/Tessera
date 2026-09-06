using UnityEngine;

namespace Tessera.Rendering
{
    /// <summary>
    /// 셀 재질을 만드는 유일한 지점(M10.8). 셰이더 이름과 프로퍼티 규약을 여기 한 곳에 두어
    /// 채택하지 않기로 하면 이 파일과 호출부만 걷어내면 되게 한다.
    /// </summary>
    public static class CelMaterialFactory
    {
        public const string ShaderName = "Tessera/CelSurface";

        /// <summary>기본 밴드 수. 확산 재질은 세 값(그림자·중간·광원면)이면 형태가 읽힌다.</summary>
        public const int DiffuseBands = 3;

        /// <summary>금속으로 읽히길 원하는 재질용. 하이라이트 밴드를 하나 더 준다.</summary>
        public const int MetallicBands = 4;

        private static Shader celShader;

        public static Shader CelShader
        {
            get
            {
                if (celShader == null) celShader = Shader.Find(ShaderName);
                return celShader;
            }
        }

        public static bool IsAvailable => CelShader != null;

        /// <summary>
        /// 셀 재질을 만든다. <paramref name="snapNormal"/>은 면이 오브젝트 축에 정렬된 메시에만 켠다.
        /// 주사위가 여기 해당하고, 곡면이 있는 소품은 꺼야 실루엣이 뭉개지지 않는다.
        ///
        /// <paramref name="receiveShadows"/>는 <c>Renderer.receiveShadows</c>를 대신한다. 그 플래그는
        /// URP Lit 전용이라 커스텀 라이팅인 셀 셰이더에는 통하지 않는다.
        /// </summary>
        public static Material Create(string name, Color baseColor, int bands, bool snapNormal, bool receiveShadows = true, Texture baseMap = null)
        {
            if (!IsAvailable) return null;

            Material material = new(CelShader)
            {
                name = name,
                enableInstancing = true
            };

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Bands", Mathf.Clamp(bands, 2, 4));
            material.SetVector("_RampValues", TesseraPixelPalette.RampVector);
            material.SetFloat("_NormalSnap", snapNormal ? 1f : 0f);
            material.SetFloat("_ReceiveShadows", receiveShadows ? 1f : 0f);
            if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return material;
        }
    }
}
