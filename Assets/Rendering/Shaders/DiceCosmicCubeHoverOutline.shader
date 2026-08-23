Shader "DicePoC/CosmicCubeHoverOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0.02, 1.80, 3.60, 0.45)
        _OutlineWidth ("Outline Width", Range(0.0, 0.15)) = 0.045
        _OutlineIntensity ("Outline Intensity", Range(0.0, 4.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+1"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "HoverSilhouette"
            Tags { "LightMode"="UniversalForward" }

            Cull Front
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineIntensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 expandedPositionOS = input.positionOS.xyz * (1.0 + _OutlineWidth);
                output.positionCS = TransformObjectToHClip(expandedPositionOS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float intensity = max(_OutlineIntensity, 0.0);
                return float4(
                    _OutlineColor.rgb * intensity,
                    saturate(_OutlineColor.a * intensity));
            }
            ENDHLSL
        }
    }
}
