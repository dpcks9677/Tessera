Shader "DicePoC/PixelUpscale"
{
    // 저해상도 렌더 타깃을 픽셀 격자로 스냅해 확대하고, 선택적으로 색을 양자화한다.
    //
    // 양자화는 반드시 sRGB 공간에서 한다. 프로젝트가 Linear 색공간이라 셰이더가 받는 값은
    // 선형인데, 선형 값을 그대로 6단계로 자르면 한 단계가 sRGB 0.45에 해당한다. 어두운 웜톤
    // 화면에서는 R만 살아남고 G·B가 0으로 떨어져 화면 전체가 순수 빨강이 된다(M10.6).
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _ColorSteps ("Color Steps", Range(2, 12)) = 6
        _Quantize ("Quantize Mode (0 Off, 1 Steps, 2 Palette)", Float) = 0
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
        _VirtualResolution ("Virtual Resolution", Vector) = (640, 360, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // LinearToSRGB / SRGBToLinear 은 Core.hlsl 이 가져오지 않는다.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #define PIXEL_PALETTE_MAX 32

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _ColorSteps;
            float _Quantize;
            float _DitherStrength;
            float2 _VirtualResolution;

            // 팔레트는 Properties에 둘 수 없다. C# 쪽에서 SetVectorArray로 채운다.
            float4 _PaletteColors[PIXEL_PALETTE_MAX];
            float _PaletteCount;

            // 팔레트 모드의 디더 폭. 인접한 팔레트 색 사이 거리에 맞춘 경험값이다.
            static const float PaletteDitherSpread = 0.12;

            static const float BayerMatrix[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return output;
            }

            // 임계 행렬은 화면 픽셀이 아니라 가상 격자 칸으로 인덱싱한다. 화면 좌표를 쓰면
            // 디더 무늬가 픽셀 격자보다 촘촘해져 한 칸 안에서 무늬가 깨진다.
            float BayerThreshold(float2 gridCell)
            {
                int x = (int)fmod(abs(gridCell.x), 4.0);
                int y = (int)fmod(abs(gridCell.y), 4.0);
                return (BayerMatrix[y * 4 + x] + 0.5) / 16.0;
            }

            float3 NearestPaletteColor(float3 srgb)
            {
                int count = clamp((int)_PaletteCount, 1, PIXEL_PALETTE_MAX);
                float3 nearest = _PaletteColors[0].rgb;
                float nearestDistance = 1e9;
                for (int i = 0; i < count; i++)
                {
                    float3 delta = srgb - _PaletteColors[i].rgb;
                    float squaredDistance = dot(delta, delta);
                    if (squaredDistance < nearestDistance)
                    {
                        nearestDistance = squaredDistance;
                        nearest = _PaletteColors[i].rgb;
                    }
                }
                return nearest;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 virtualResolution = max(_VirtualResolution, float2(1.0, 1.0));
                float2 gridCell = floor(saturate(input.uv) * virtualResolution);
                float2 pixelUV = (gridCell + 0.5) / virtualResolution;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelUV);

                int mode = (int)round(_Quantize);
                if (mode > 0)
                {
                    float3 srgb = LinearToSRGB(color.rgb);
                    float dither = (BayerThreshold(gridCell) - 0.5) * _DitherStrength;

                    if (mode == 1)
                    {
                        float steps = max(2.0, _ColorSteps);
                        srgb = saturate(srgb + dither / steps);
                        srgb = floor(srgb * steps + 0.5) / steps;
                    }
                    else
                    {
                        srgb = saturate(srgb + dither * PaletteDitherSpread);
                        srgb = NearestPaletteColor(srgb);
                    }

                    color.rgb = (half3)SRGBToLinear(srgb);
                }

                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }
    }
}
