Shader "DicePoC/CosmicCubeHoverOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0.02, 1.80, 3.60, 0.45)
        _OutlineWidth ("Outline Width", Range(0.0, 0.15)) = 0.045
        _OutlineIntensity ("Outline Intensity", Range(0.0, 4.0)) = 0.0

        [Header(Augment State)]
        _AugmentTint ("Augment Tint (a = strength)", Color) = (0.62, 0.24, 0.92, 0.0)
        _AugmentDrain ("Augment Drain", Range(0.0, 1.0)) = 0.0
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
            #include "CosmicAugmentState.hlsl"

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
                float4 _AugmentTint;
                float _OutlineWidth;
                float _OutlineIntensity;
                float _AugmentDrain;
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
                // 일렁임은 C#이 _OutlineIntensity를 흔들어 만든다. 여기서 알파까지 건드리면 그 연출과 싸운다.
                float3 outline = ApplyCosmicAugmentState(_OutlineColor.rgb * intensity, _AugmentTint, _AugmentDrain);
                return float4(outline, saturate(_OutlineColor.a * intensity));
            }
            ENDHLSL
        }
    }
}
