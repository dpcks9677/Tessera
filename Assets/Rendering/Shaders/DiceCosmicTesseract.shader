Shader "DicePoC/CosmicTesseract"
{
    Properties
    {
        [HDR] _LineColor ("Line Color", Color) = (0.04, 1.55, 3.20, 1.0)
        [HDR] _HotColor ("Hot Color", Color) = (0.72, 2.35, 3.40, 1.0)
        _Intensity ("Intensity", Range(0.0, 5.0)) = 0.90
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.62
        _FlowSpeed ("Flow Speed", Range(0.0, 6.0)) = 1.15
        _FlowScale ("Flow Scale", Range(1.0, 20.0)) = 8.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-8"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "TesseractLines"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float4 _HotColor;
                float _Intensity;
                float _Opacity;
                float _FlowSpeed;
                float _FlowScale;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float phase = input.uv.x * _FlowScale
                    + dot(input.positionOS, float3(2.1, 1.4, 1.7))
                    - _Time.y * _FlowSpeed;
                float wave = sin(phase) * 0.5 + 0.5;
                float hotPulse = pow(wave, 7.0);
                float steadyGlow = 0.78 + wave * 0.22;
                float3 lineColor = lerp(_LineColor.rgb, _HotColor.rgb, hotPulse * 0.72);
                float alpha = saturate(_Opacity * input.color.a * (0.82 + hotPulse * 0.18));
                return float4(lineColor * _Intensity * steadyGlow, alpha);
            }
            ENDHLSL
        }
    }
}
