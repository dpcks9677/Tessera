Shader "DicePoC/CosmicCube"
{
    Properties
    {
        [Header(Color and Lighting)]
        _AbyssColor ("Deep Space Abyss", Color) = (0.02, 0.06, 0.18, 0.95)
        _NebulaMidColor ("Nebula Mid Sapphire", Color) = (0.08, 0.38, 0.75, 1.0)
        _NebulaCyanColor ("Nebula Vivid Cyan Gas", Color) = (0.18, 0.88, 1.00, 1.0)
        _NebulaCoreColor ("Nebula Core White Glint", Color) = (0.85, 0.96, 1.00, 1.0)
        _Brightness ("Overall Brightness", Range(0.1, 2.5)) = 0.88
        _UniformFaceGlow ("Uniform Face Glow", Color) = (0.18, 0.88, 1.00, 1.0)
        _MinFaceBrightness ("Minimum Face Brightness", Range(0.0, 1.0)) = 0.42

        [Header(Chunky Macro Nebula for Pixel Filter)]
        _NebulaScale ("Macro Nebula Scale", Range(0.5, 6.0)) = 2.20
        _NebulaDetailScale ("Nebula Cloud Scale", Range(1.0, 12.0)) = 4.2
        _NebulaRoughness ("Nebula Roughness", Range(0.1, 2.0)) = 0.75
        _NebulaContrast ("Nebula Contrast Sharpness", Range(0.5, 5.0)) = 1.8
        _NebulaSpeed ("Nebula Drift Speed", Range(0.01, 1.0)) = 0.12
        _NebulaTurbulence ("Nebula Warp Turbulence", Range(0.1, 4.0)) = 1.4

        [Header(Starfield and Glitter Clusters)]
        _StarColor ("Micro Star Color", Color) = (0.90, 0.98, 1.00, 1.0)
        _StarDensity ("Starfield Density Scale", Range(5.0, 40.0)) = 16.0
        _StarIntensity ("Starfield Intensity", Range(0.0, 8.0)) = 3.2
        _StarSharpness ("Star Point Sharpness", Range(2.0, 24.0)) = 8.5
        _StarTwinkleSpeed ("Star Twinkle Speed", Range(0.5, 8.0)) = 2.5

        [Header(3D Parallax Internal Depth)]
        _ParallaxDepth ("Internal Parallax Depth", Range(0.0, 0.6)) = 0.18
        _ParallaxLayers ("Parallax Layer Weight", Range(0.2, 1.0)) = 0.65

        [Header(Edge Wireframe and Silhouette Highlight)]
        _EdgeColor ("Edge Neon Color", Color) = (0.40, 0.95, 1.00, 1.0)
        _EdgeWidth ("Edge Width", Range(0.01, 0.25)) = 0.085
        _EdgeIntensity ("Edge Intensity", Range(0.5, 12.0)) = 2.60
        _EdgeFalloff ("Edge Falloff Sharpness", Range(1.0, 16.0)) = 4.0

        [Header(Internal Cosmic Core)]
        _CoreColor ("Internal Center Core Color", Color) = (0.65, 0.90, 1.00, 1.0)
        _CoreIntensity ("Core Intensity", Range(0.0, 4.0)) = 1.10
        _CoreRadius ("Core Radius", Range(0.1, 1.5)) = 0.50

        [Header(Glass Surface and Fresnel Rim)]
        _RimColor ("Rim Halo Color", Color) = (0.35, 0.92, 1.00, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.5
        _RimIntensity ("Rim Intensity", Range(0.1, 8.0)) = 0.85
        _Smoothness ("Glass Smoothness", Range(0.0, 1.0)) = 0.95

    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True" 
        }

        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 viewDirOS  : TEXCOORD3;
                float2 uv         : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AbyssColor;
                float4 _NebulaMidColor;
                float4 _NebulaCyanColor;
                float4 _NebulaCoreColor;
                float  _Brightness;
                float4 _UniformFaceGlow;
                float  _MinFaceBrightness;

                float  _NebulaScale;
                float  _NebulaDetailScale;
                float  _NebulaRoughness;
                float  _NebulaContrast;
                float  _NebulaSpeed;
                float  _NebulaTurbulence;

                float4 _StarColor;
                float  _StarDensity;
                float  _StarIntensity;
                float  _StarSharpness;
                float  _StarTwinkleSpeed;

                float  _ParallaxDepth;
                float  _ParallaxLayers;

                float4 _EdgeColor;
                float  _EdgeWidth;
                float  _EdgeIntensity;
                float  _EdgeFalloff;

                float4 _CoreColor;
                float  _CoreIntensity;
                float  _CoreRadius;

                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _Smoothness;
            CBUFFER_END

            // 의사 난수 3D 해시 함수
            float3 Hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            // 부드러운 3D 그라디언트 노이즈
            float Noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = dot(Hash33(i + float3(0,0,0)) * 2.0 - 1.0, f - float3(0,0,0));
                float n100 = dot(Hash33(i + float3(1,0,0)) * 2.0 - 1.0, f - float3(1,0,0));
                float n010 = dot(Hash33(i + float3(0,1,0)) * 2.0 - 1.0, f - float3(0,1,0));
                float n110 = dot(Hash33(i + float3(1,1,0)) * 2.0 - 1.0, f - float3(1,1,0));
                float n001 = dot(Hash33(i + float3(0,0,1)) * 2.0 - 1.0, f - float3(0,0,1));
                float n101 = dot(Hash33(i + float3(1,0,1)) * 2.0 - 1.0, f - float3(1,0,1));
                float n011 = dot(Hash33(i + float3(0,1,1)) * 2.0 - 1.0, f - float3(0,1,1));
                float n111 = dot(Hash33(i + float3(1,1,1)) * 2.0 - 1.0, f - float3(1,1,1));

                float nx0 = lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y);
                float nx1 = lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y);
                return lerp(nx0, nx1, u.z) * 0.5 + 0.5;
            }

            // 4옥타브 3D FBM
            float Fbm3D(float3 p)
            {
                float val = 0.0;
                float amp = 0.55;
                float freq = 1.0;
                for (int i = 0; i < 4; i++)
                {
                    val += Noise3D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.50 * _NebulaRoughness;
                }
                return val;
            }

            // [방안 A] 면별 로컬 2D 평면 좌표(Face UV) 및 로컬 시선 벡터 추출 (6개 면 완벽 대칭)
            void GetCubeFaceCoords(float3 posOS, float3 viewOS, out float2 faceUV, out float2 faceViewDir)
            {
                float3 a = abs(posOS);
                if (a.x >= a.y && a.x >= a.z)
                {
                    // X축 면 (Right / Left)
                    faceUV = float2(posOS.y, posOS.z);
                    faceViewDir = float2(viewOS.y, viewOS.z);
                }
                else if (a.y >= a.x && a.y >= a.z)
                {
                    // Y축 면 (Top / Bottom)
                    faceUV = float2(posOS.x, posOS.z);
                    faceViewDir = float2(viewOS.x, viewOS.z);
                }
                else
                {
                    // Z축 면 (Front / Back)
                    faceUV = float2(posOS.x, posOS.y);
                    faceViewDir = float2(viewOS.x, viewOS.y);
                }
            }

            // [방안 A & B] 면별 대칭 성운 가스 구름 연산 (모든 면 완벽히 균등한 톤과 흐름)
            float EvaluateFaceNebula(float2 faceUV, float depth, float time)
            {
                float3 p = float3(faceUV * _NebulaScale, depth * 0.8);
                float t = time * _NebulaSpeed;

                float3 q = float3(
                    Fbm3D(p + float3(0.0, 0.0, t)),
                    Fbm3D(p + float3(5.2, 1.3, t * 0.8)),
                    Fbm3D(p + float3(2.8, 8.4, -t * 0.6))
                );

                float3 r = float3(
                    Fbm3D(p + 2.2 * q + float3(1.7, 9.2, t * 0.5)),
                    Fbm3D(p + 2.2 * q + float3(8.3, 2.8, -t * 0.7)),
                    Fbm3D(p + 2.5 * q + float3(4.1, 6.5, t * 0.4))
                );

                float mainCloud = Fbm3D(p + 2.0 * r * _NebulaTurbulence);
                float detailCloud = Fbm3D(float3(faceUV * _NebulaDetailScale, depth * 1.5) + 1.2 * r);

                float combined = lerp(mainCloud, detailCloud, 0.25);
                // 부드러운 스무스 대비 보정 (극단적인 흑백 편차 방지)
                combined = smoothstep(0.12, 0.88, combined);
                combined = pow(saturate(combined), _NebulaContrast);
                return combined;
            }

            // [방안 A] 면별 대칭 별무리 연산
            float EvaluateFaceStarfield(float2 faceUV, float depth, float time)
            {
                float3 p = float3(faceUV * _StarDensity, depth * 12.0);
                float3 i = floor(p);
                float3 f = frac(p);

                float starGlow = 0.0;
                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 randVal = Hash33(i + neighbor);
                            float3 starPos = neighbor + randVal;
                            float dist = length(f - starPos);

                            float twinkle = sin(time * _StarTwinkleSpeed + randVal.x * 62.83) * 0.5 + 0.5;
                            twinkle = pow(twinkle, 1.8);

                            float star = exp(-dist * _StarSharpness);
                            starGlow += star * (0.35 + 0.65 * twinkle) * randVal.y;
                        }
                    }
                }
                return saturate(starGlow) * _StarIntensity;
            }

            // 정육면체 로컬 좌표([-0.5, 0.5]) 기반 12개 모서리 에지 팩터 계산 (3면 경계선 균등)
            float EvaluateCubeEdges(float3 posOS)
            {
                float3 d = 0.5 - abs(posOS);
                float min1 = min(d.x, min(d.y, d.z));
                float max1 = max(d.x, max(d.y, d.z));
                float mid1 = d.x + d.y + d.z - min1 - max1;

                float edgeFactor = 1.0 - saturate(mid1 / max(_EdgeWidth, 0.001));
                return pow(edgeFactor, _EdgeFalloff);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.uv         = input.uv;

                float3 viewDirWS = normalize(GetCameraPositionWS() - vertexInput.positionWS);
                output.viewDirOS = normalize(mul((float3x3)unity_WorldToObject, viewDirWS));

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 viewDirOS = normalize(input.viewDirOS);
                float time = _Time.y;

                // 1. [방안 A] 면별 로컬 좌표 및 시선 벡터 추출 (3개 면 100% 대칭)
                float2 faceUV, faceViewDir;
                GetCubeFaceCoords(input.positionOS, viewDirOS, faceUV, faceViewDir);

                // 2. 3단계 대칭 체적 패럴랙스 샘플링 (모든 면 균등한 깊이감)
                float depth1 = _ParallaxDepth * 0.5;
                float depth2 = _ParallaxDepth;

                float nebula0 = EvaluateFaceNebula(faceUV, 0.0, time);
                float stars0 = EvaluateFaceStarfield(faceUV, 0.0, time);

                float nebula1 = EvaluateFaceNebula(faceUV - faceViewDir * depth1, depth1, time * 0.9 + 1.5);
                float stars1 = EvaluateFaceStarfield(faceUV - faceViewDir * depth1, depth1, time + 2.0);

                float nebula2 = EvaluateFaceNebula(faceUV - faceViewDir * depth2, depth2, time * 0.8 + 3.0);
                float stars2 = EvaluateFaceStarfield(faceUV - faceViewDir * depth2, depth2, time + 4.5);

                float totalNebula = nebula0 * 0.52 + nebula1 * 0.30 + nebula2 * 0.18;
                float totalStars = stars0 * 0.55 + stars1 * 0.30 + stars2 * 0.15;

                // 3. [방안 B] 부드러운 4톤 컬러 램프 밸런싱 (일정한 평균 조도 유지)
                float3 nebulaTone;
                if (totalNebula < 0.35)
                {
                    float t = smoothstep(0.0, 0.35, totalNebula);
                    nebulaTone = lerp(_AbyssColor.rgb, _NebulaMidColor.rgb, t);
                }
                else if (totalNebula < 0.70)
                {
                    float t = smoothstep(0.35, 0.70, totalNebula);
                    nebulaTone = lerp(_NebulaMidColor.rgb, _NebulaCyanColor.rgb, t);
                }
                else
                {
                    float t = smoothstep(0.70, 1.0, totalNebula);
                    nebulaTone = lerp(_NebulaCyanColor.rgb, _NebulaCoreColor.rgb, pow(t, 1.5));
                }

                // 4. 미세 별무리 결합
                float3 starGlow = _StarColor.rgb * totalStars * 0.85;

                // 5. [방안 A] 면 중심 마나 코어 글로우 (3면 대칭 원형 부유감)
                float distFromFaceCenter = length(faceUV);
                float coreFactor = saturate(1.0 - (distFromFaceCenter / max(_CoreRadius, 0.01)));
                coreFactor = pow(coreFactor, 2.2);
                float3 coreTint = _CoreColor.rgb * _CoreIntensity;

                // 6. [방안 C] 큐브 12개 모서리 에지 네온 발광 (모든 면 경계 완벽 균등)
                float edgeFactor = EvaluateCubeEdges(input.positionOS);
                float3 edgeGlow = _EdgeColor.rgb * edgeFactor * _EdgeIntensity;

                // 7. 실루엣 프레넬을 실제 큐브 모서리로 제한한다.
                // 평면 전체가 동일한 법선을 갖는 큐브에서 프레넬만 사용하면 특정 각도의 면 전체가 밝아진다.
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                fresnel = smoothstep(0.15, 0.90, fresnel);
                float rimFactor = fresnel * edgeFactor;
                float3 rimGlow = _RimColor.rgb * rimFactor * _RimIntensity;

                // 8. 최종 종합 컬러 합성 (3면 100% 균등 조도 및 비비드 아쿠아 사파이어)
                float3 cosmicBody = lerp(_AbyssColor.rgb, nebulaTone, 0.88) * _Brightness;
                cosmicBody += starGlow * _Brightness;
                cosmicBody = lerp(cosmicBody, coreTint, coreFactor * 0.35);

                // 성운 무늬는 유지하면서 모든 면의 최소 발광 휘도를 동일하게 보장한다.
                const float3 luminanceWeights = float3(0.2126, 0.7152, 0.0722);
                float bodyLuminance = dot(cosmicBody, luminanceWeights);
                float glowLuminance = max(dot(_UniformFaceGlow.rgb, luminanceWeights), 0.001);
                float luminanceDeficit = max(_MinFaceBrightness - bodyLuminance, 0.0);
                cosmicBody += _UniformFaceGlow.rgb * (luminanceDeficit / glowLuminance);
                cosmicBody = saturate(cosmicBody);

                // 에지 및 실루엣 림 합성
                float3 finalColor = cosmicBody + edgeGlow * _Brightness * 1.15 + rimGlow * _Brightness * 0.80;
                float finalAlpha = saturate(_AbyssColor.a + totalNebula * 0.35 + totalStars * 0.25 + edgeFactor * 0.7 + rimFactor * 0.3);

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

    }
    FallBack "Universal Render Pipeline/Unlit"
}
