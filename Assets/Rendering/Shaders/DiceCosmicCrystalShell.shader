Shader "DicePoC/CosmicCrystalShell"
{
    Properties
    {
        [Header(Crystal Surface)]
        [HDR] _CrystalColor ("Crystal Color", Color) = (0.00, 0.55, 1.65, 1.0)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.20, 1.85, 2.80, 1.0)
        [HDR] _WarmReflectionColor ("Warm Reflection", Color) = (1.00, 0.42, 0.10, 1.0)
        [HDR] _ThicknessColor ("Crystal Thickness", Color) = (0.00, 0.18, 1.10, 1.0)
        _SurfaceAlpha ("Surface Alpha", Range(0.0, 0.5)) = 0.065
        _ThicknessIntensity ("Thickness Intensity", Range(0.0, 3.0)) = 0.70
        _ThicknessWidth ("Thickness Width", Range(0.01, 0.20)) = 0.075
        _RefractionStrength ("Fake Refraction", Range(0.0, 1.0)) = 0.18
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 6.0)) = 1.15
        _EdgeIntensity ("Edge Intensity", Range(0.0, 10.0)) = 2.35
        _EdgeWidth ("Edge Width", Range(0.005, 0.15)) = 0.043
        _ShellExpansion ("Shell Expansion", Range(0.0, 0.08)) = 0.018
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "CrystalShell"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CrystalColor;
                float4 _EdgeColor;
                float4 _WarmReflectionColor;
                float4 _ThicknessColor;
                float _SurfaceAlpha;
                float _ThicknessIntensity;
                float _ThicknessWidth;
                float _RefractionStrength;
                float _FresnelIntensity;
                float _EdgeIntensity;
                float _EdgeWidth;
                float _ShellExpansion;
                float _Cull;
            CBUFFER_END

            float CubeEdgeDistance(float3 positionOS)
            {
                float3 distanceToEdge = 0.5 - abs(positionOS);
                float minimum = min(distanceToEdge.x, min(distanceToEdge.y, distanceToEdge.z));
                float maximum = max(distanceToEdge.x, max(distanceToEdge.y, distanceToEdge.z));
                return distanceToEdge.x + distanceToEdge.y + distanceToEdge.z - minimum - maximum;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 expandedPositionOS = input.positionOS.xyz * (1.0 + _ShellExpansion);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(expandedPositionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInputs.normalWS;
                output.normalOS = normalize(input.normalOS);
                return output;
            }

            float4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float faceSign = IS_FRONT_VFACE(isFrontFace, 1.0, -1.0);
                float3 normalWS = normalize(input.normalWS) * faceSign;
                float3 viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
                float NdotV = saturate(abs(dot(normalWS, viewDirectionWS)));
                float fresnel = pow(1.0 - NdotV, 2.6);
                float edgeDistance = CubeEdgeDistance(input.positionOS);
                float edge = pow(1.0 - saturate(edgeDistance / max(_EdgeWidth, 0.001)), 3.0);
                float broadEdge = 1.0 - smoothstep(
                    _EdgeWidth * 0.35,
                    _EdgeWidth + _ThicknessWidth,
                    edgeDistance);
                float thicknessBand = saturate(broadEdge - edge * 0.72);

                float3 coolLightDirection = normalize(float3(-0.35, 0.72, -0.55));
                float3 warmLightDirection = normalize(float3(0.72, 0.45, 0.28));
                float coolSpecular = pow(saturate(dot(reflect(-viewDirectionWS, normalWS), coolLightDirection)), 24.0);
                float warmSpecular = pow(saturate(dot(reflect(-viewDirectionWS, normalWS), warmLightDirection)), 38.0);

                float3 surfaceColor = _CrystalColor.rgb * 0.46;
                float3 finalColor = surfaceColor * (_SurfaceAlpha + fresnel * _FresnelIntensity * 0.42);
                float refractionBlend = saturate(0.28 + NdotV * 0.42);
                float3 refractionColor = lerp(_ThicknessColor.rgb, _EdgeColor.rgb, refractionBlend);
                finalColor += refractionColor * fresnel * _RefractionStrength;
                finalColor += _ThicknessColor.rgb * thicknessBand * _ThicknessIntensity;
                finalColor += _EdgeColor.rgb * edge * _EdgeIntensity;
                finalColor += _EdgeColor.rgb * coolSpecular * 1.55;
                finalColor += _WarmReflectionColor.rgb * warmSpecular * 0.08;

                float alpha = saturate(
                    _SurfaceAlpha
                    + fresnel * 0.20
                    + thicknessBand * 0.16
                    + edge * 0.68
                    + coolSpecular * 0.24);
                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
