Shader "DicePoC/CosmicCore"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (0.12, 1.35, 2.65, 1.0)
        [HDR] _CoreHotColor ("Core Hot Color", Color) = (0.90, 2.40, 3.20, 1.0)
        _CoreIntensity ("Core Intensity", Range(0.0, 10.0)) = 2.70
        _PulseSpeed ("Pulse Speed", Range(0.0, 6.0)) = 1.35
        _PulseAmount ("Pulse Amount", Range(0.0, 0.5)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-5"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "EnergyCore"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
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
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _CoreHotColor;
                float _CoreIntensity;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
                float facing = saturate(dot(normalWS, viewDirectionWS));
                float center = pow(facing, 1.35);
                float hotCenter = pow(facing, 5.0);
                float rim = pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), 3.0);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 glow = _CoreColor.rgb * (center * 0.72 + rim * 0.14);
                glow += _CoreHotColor.rgb * hotCenter * 1.10;
                glow *= _CoreIntensity * pulse;
                return float4(glow, saturate(center * 0.62 + hotCenter * 0.30 + rim * 0.10));
            }
            ENDHLSL
        }
    }
}
