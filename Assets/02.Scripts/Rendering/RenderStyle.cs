namespace Tessera.Rendering
{
    /// <summary>
    /// 화면 연출 방식. 채택 여부가 정해지지 않은 동안 두 경로를 모두 살려 두고 런타임에 전환한다(M10.8).
    /// </summary>
    public enum RenderStyle
    {
        /// <summary>M10.7까지의 화면. URP Lit, 소프트 그림자, SSAO, 1920x1080 렌더 후 점 샘플링.</summary>
        Baseline,

        /// <summary>재료 단계 셀 램프, 하드 그림자, SSAO 없음, 내부 해상도로 직접 렌더.</summary>
        Cel
    }
}
