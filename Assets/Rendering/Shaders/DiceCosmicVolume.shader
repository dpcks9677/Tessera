Shader "DicePoC/CosmicVolume"
{
    Properties
    {
        [Header(Volume Color)]
        [HDR] _AbyssColor ("Abyss Color", Color) = (0.003, 0.015, 0.09, 1.0)
        [HDR] _NebulaColor ("Nebula Color", Color) = (0.00, 0.20, 1.25, 1.0)
        [HDR] _CloudColor ("Cloud Color", Color) = (0.00, 1.55, 2.40, 1.0)
        [HDR] _CoreColor ("Core Color", Color) = (0.55, 1.80, 2.80, 1.0)
        [HDR] _StarColor ("Star Color", Color) = (0.65, 2.00, 3.00, 1.0)

        [Header(Volume Shape)]
        _Brightness ("Brightness", Range(0.1, 3.0)) = 1.18
        _Density ("Nebula Density", Range(0.1, 2.0)) = 0.94
        _Opacity ("Volume Opacity", Range(0.1, 1.0)) = 0.84
        _NoiseScale ("Noise Scale", Range(0.5, 8.0)) = 3.2
        _NoiseSpeed ("Noise Speed", Range(0.0, 0.5)) = 0.06

        [Header(Internal Light)]
        _CoreIntensity ("Core Intensity", Range(0.0, 8.0)) = 2.45
        _CoreRadius ("Core Radius", Range(0.1, 0.6)) = 0.39
        _StarIntensity ("Star Intensity", Range(0.0, 8.0)) = 2.65
        _TwinkleSpeed ("Twinkle Speed", Range(0.0, 8.0)) = 2.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "CosmicVolume"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 viewDirOS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AbyssColor;
                float4 _NebulaColor;
                float4 _CloudColor;
                float4 _CoreColor;
                float4 _StarColor;
                float _Brightness;
                float _Density;
                float _Opacity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _CoreIntensity;
                float _CoreRadius;
                float _StarIntensity;
                float _TwinkleSpeed;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 cell = floor(p);
                float3 local = frac(p);
                float3 blend = local * local * (3.0 - 2.0 * local);

                float x00 = lerp(Hash31(cell + float3(0, 0, 0)), Hash31(cell + float3(1, 0, 0)), blend.x);
                float x10 = lerp(Hash31(cell + float3(0, 1, 0)), Hash31(cell + float3(1, 1, 0)), blend.x);
                float x01 = lerp(Hash31(cell + float3(0, 0, 1)), Hash31(cell + float3(1, 0, 1)), blend.x);
                float x11 = lerp(Hash31(cell + float3(0, 1, 1)), Hash31(cell + float3(1, 1, 1)), blend.x);
                float y0 = lerp(x00, x10, blend.y);
                float y1 = lerp(x01, x11, blend.y);
                return lerp(y0, y1, blend.z);
            }

            float NebulaNoise(float3 p)
            {
                float low = ValueNoise(p);
                float high = ValueNoise(p * 2.03 + 4.17);
                return low * 0.72 + high * 0.28;
            }

            float StarField(float3 p, float time)
            {
                float3 gridPosition = p * 17.0 + 8.5;
                float3 cell = floor(gridPosition);
                float3 local = frac(gridPosition);
                float3 randomPosition = float3(
                    Hash31(cell + 3.1),
                    Hash31(cell + 7.7),
                    Hash31(cell + 13.9));
                float distanceToStar = length(local - randomPosition);
                float existence = step(0.82, Hash31(cell + 21.4));
                float starPoint = (1.0 - smoothstep(0.018, 0.105, distanceToStar)) * existence;
                float twinkle = 0.55 + 0.45 * sin(time * _TwinkleSpeed + Hash31(cell) * 6.28318);
                return starPoint * twinkle;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;

                float3 viewDirWS = normalize(GetCameraPositionWS() - positionInputs.positionWS);
                output.viewDirOS = normalize(mul((float3x3)unity_WorldToObject, viewDirWS));
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                const int sampleCount = 8;
                float time = _Time.y;
                float3 rayOrigin = input.positionOS;
                float3 rayDirection = -normalize(input.viewDirOS);
                float3 directionSign = step(0.0, rayDirection) * 2.0 - 1.0;
                float3 safeDirection = directionSign * max(abs(rayDirection), 0.0001);
                float3 exitBounds = lerp(float3(-0.5, -0.5, -0.5), float3(0.5, 0.5, 0.5), step(0.0, safeDirection));
                float3 exitDistances = (exitBounds - rayOrigin) / safeDirection;
                float travelDistance = max(min(exitDistances.x, min(exitDistances.y, exitDistances.z)), 0.001);
                float stepLength = travelDistance / sampleCount;

                float3 accumulatedColor = 0.0;
                float accumulatedAlpha = 0.0;
                float3 additiveLight = 0.0;

                [unroll]
                for (int index = 0; index < sampleCount; index++)
                {
                    float samplePosition = (index + 0.5) / sampleCount;
                    float3 p = rayOrigin + rayDirection * travelDistance * samplePosition;
                    float3 drift = float3(time * _NoiseSpeed, -time * _NoiseSpeed * 0.63, time * _NoiseSpeed * 0.38);

                    float noise = NebulaNoise(p * _NoiseScale + drift);
                    float cloud = smoothstep(0.38, 0.72, noise);
                    float filament = smoothstep(0.58, 0.82, noise);
                    float boundaryFade = saturate((0.5 - max(abs(p.x), max(abs(p.y), abs(p.z)))) * 8.0);
                    float density = (0.10 + cloud * 0.62 + filament * 0.35) * boundaryFade * _Density;
                    float sampleAlpha = saturate(density * stepLength * 1.65);

                    float3 cloudColor = lerp(_AbyssColor.rgb, _NebulaColor.rgb, cloud);
                    cloudColor = lerp(cloudColor, _CloudColor.rgb, filament * 0.72);
                    accumulatedColor += (1.0 - accumulatedAlpha) * cloudColor * sampleAlpha;
                    accumulatedAlpha += (1.0 - accumulatedAlpha) * sampleAlpha;

                    float coreDistance = length(p) / max(_CoreRadius, 0.001);
                    float core = exp2(-coreDistance * coreDistance * 1.65);
                    float hotCore = exp2(-coreDistance * coreDistance * 7.5);
                    float3 hotCoreColor = float3(0.82, 1.90, 2.55);
                    additiveLight += (_CoreColor.rgb * core * 0.72 + hotCoreColor * hotCore * 1.35)
                        * _CoreIntensity * stepLength;

                    float star = StarField(p, time) * _StarIntensity;
                    additiveLight += _StarColor.rgb * star * stepLength * 1.8;
                }

                float3 finalColor = accumulatedColor * _Brightness + additiveLight;
                finalColor += _NebulaColor.rgb * accumulatedAlpha * 0.18;
                float thicknessGlow = saturate(travelDistance * 1.35);
                finalColor += _NebulaColor.rgb * thicknessGlow * 0.10;
                float finalAlpha = saturate(0.30 + accumulatedAlpha * _Opacity);
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
