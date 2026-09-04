Shader "DicePoC/PixelEdge"
{
    // 픽셀 격자 위에서 뎁스·노멀 이웃 차이를 읽어 외곽선과 모서리 하이라이트를 만든다(M10.5-T1).
    //
    // 원본은 KodyJKing/hello-threejs 의 RenderPixelatedPass 이다. 공식은 그대로 옮기되
    // 두 가지를 URP에 맞춰 바꿨다.
    //   1. 월드 카메라가 직교 투영이라 LinearEyeDepth(원근용) 대신 near~far 선형 보간을 쓴다.
    //   2. URP가 주는 씬 노멀은 월드 공간이라 원본이 전제한 뷰 공간으로 변환한 뒤 계산한다.
    //
    // 뎁스 임계값은 원본의 정규화 뎁스가 아니라 월드 유닛 기준이므로 프로퍼티로 노출한다.
    Properties
    {
        _DepthEdgeStrength ("Depth Edge Strength", Range(0, 1)) = 0.4
        _NormalEdgeStrength ("Normal Edge Strength", Range(0, 1)) = 0.3
        _DepthEdgeThreshold ("Depth Edge Threshold (Min, Max)", Vector) = (0.05, 0.12, 0, 0)
        _NormalEdgeDepthBias ("Normal Edge Depth Bias", Range(0, 0.2)) = 0.01
        _NormalEdgeBias ("Normal Edge Bias", Vector) = (1, 1, 1, 0)
        _EdgeLuminanceSuppression ("Bright Pixel Outline Suppression", Range(0, 4)) = 0
        _PixelEdgeVirtualResolution ("Virtual Resolution", Vector) = (640, 360, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "PixelEdge"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _DepthEdgeStrength;
            float  _NormalEdgeStrength;
            float4 _DepthEdgeThreshold;
            float  _NormalEdgeDepthBias;
            float4 _NormalEdgeBias;
            float  _EdgeLuminanceSuppression;
            float4 _PixelEdgeVirtualResolution;

            float2 VirtualResolution()
            {
                return max(_PixelEdgeVirtualResolution.xy, float2(1.0, 1.0));
            }

            // 가상 격자 칸의 중심으로 UV를 스냅한다. 업스케일 셰이더와 같은 격자를 써야
            // 두 격자가 어긋나 생기는 떨림이 없다.
            float2 SnapUV(float2 uv)
            {
                float2 resolution = VirtualResolution();
                return (floor(saturate(uv) * resolution) + 0.5) / resolution;
            }

            // 직교 투영에서는 뎁스가 near~far 사이의 선형 값이다. 원근용 변환을 쓰면 안 된다.
            float SampleLinearDepth(float2 snappedUV)
            {
                float rawDepth = SampleSceneDepth(snappedUV);
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                        rawDepth = 1.0 - rawDepth;
                    #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            // 원본은 뷰 공간 노멀을 전제한다. URP의 씬 노멀은 월드 공간이므로 변환한다.
            float3 SampleViewNormal(float2 snappedUV)
            {
                float3 normalWS = SampleSceneNormals(snappedUV);
                return normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
            }

            float2 NeighborUV(float2 snappedUV, float2 offset)
            {
                return saturate(snappedUV + offset / VirtualResolution());
            }

            // 이웃이 더 멀면 이 픽셀이 실루엣 안쪽이라는 뜻이다. 그 차이를 모아 외곽선을 만든다.
            float DepthEdgeIndicator(float2 snappedUV, float depth)
            {
                float diff = 0.0;
                diff += clamp(SampleLinearDepth(NeighborUV(snappedUV, float2( 1,  0))) - depth, 0.0, 1.0);
                diff += clamp(SampleLinearDepth(NeighborUV(snappedUV, float2(-1,  0))) - depth, 0.0, 1.0);
                diff += clamp(SampleLinearDepth(NeighborUV(snappedUV, float2( 0,  1))) - depth, 0.0, 1.0);
                diff += clamp(SampleLinearDepth(NeighborUV(snappedUV, float2( 0, -1))) - depth, 0.0, 1.0);
                float threshold = smoothstep(_DepthEdgeThreshold.x, _DepthEdgeThreshold.y, diff);
                return floor(threshold * 2.0) / 2.0;
            }

            // 노멀이 꺾이면서 이웃이 더 먼 쪽, 즉 볼록한 모서리에서만 값이 선다.
            float NeighborNormalEdgeIndicator(float2 snappedUV, float2 offset, float depth, float3 normalVS)
            {
                float2 neighborUV = NeighborUV(snappedUV, offset);
                float depthDiff = SampleLinearDepth(neighborUV) - depth;
                float3 neighborNormal = SampleViewNormal(neighborUV);

                float normalDiff = dot(normalVS - neighborNormal, _NormalEdgeBias.xyz);
                float normalIndicator = clamp(smoothstep(-0.01, 0.01, normalDiff), 0.0, 1.0);
                float depthIndicator = clamp(sign(depthDiff + _NormalEdgeDepthBias), 0.0, 1.0);
                return (1.0 - dot(normalVS, neighborNormal)) * depthIndicator * normalIndicator;
            }

            float NormalEdgeIndicator(float2 snappedUV, float depth, float3 normalVS)
            {
                float indicator = 0.0;
                indicator += NeighborNormalEdgeIndicator(snappedUV, float2( 1,  0), depth, normalVS);
                indicator += NeighborNormalEdgeIndicator(snappedUV, float2(-1,  0), depth, normalVS);
                indicator += NeighborNormalEdgeIndicator(snappedUV, float2( 0,  1), depth, normalVS);
                indicator += NeighborNormalEdgeIndicator(snappedUV, float2( 0, -1), depth, normalVS);
                return step(0.1, indicator);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 snappedUV = SnapUV(input.texcoord);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, snappedUV);

                float depth = SampleLinearDepth(snappedUV);
                float3 normalVS = SampleViewNormal(snappedUV);

                float depthEdge = _DepthEdgeStrength > 0.0 ? DepthEdgeIndicator(snappedUV, depth) : 0.0;

                // 발광체에 검은 테를 두르지 않아야 할 때만 쓴다. 기본값 0이면 아무 영향이 없다.
                float luminance = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                depthEdge *= saturate(1.0 - luminance * _EdgeLuminanceSuppression);

                float normalEdge = _NormalEdgeStrength > 0.0 ? NormalEdgeIndicator(snappedUV, depth, normalVS) : 0.0;

                float coefficient = depthEdge > 0.0
                    ? (1.0 - _DepthEdgeStrength * depthEdge)
                    : (1.0 + _NormalEdgeStrength * normalEdge);

                return half4(color.rgb * coefficient, color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
