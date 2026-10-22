Shader "DicePoC/CrispUiDepthMask"
{
    // Crisp UI 카메라에만 "여기 앞에 물체가 있다"는 깊이를 남기는 판.
    //
    // 족보 표와 카드 글자는 픽셀 필터를 피하려고 CrispUI 레이어의 월드 스페이스 캔버스에 있고,
    // 그 레이어는 월드 카메라의 컬링 마스크에서 빠져 있다(M9.5). 그래서 전용 Crisp 카메라의
    // 깊이 버퍼에는 월드 물체가 하나도 없고, 앞을 막는 것이 있어도 글자가 그대로 그려진다.
    //
    // 이 셰이더를 입힌 판을 CrispUI 레이어에 두면 색은 한 픽셀도 쓰지 않고 깊이만 남긴다.
    // 월드 스페이스 캔버스는 ZTest LEqual로 그려지므로, 판보다 뒤에 있는 글자가 그 부분에서 잘린다.
    // 월드 카메라는 CrispUI를 찍지 않으므로 이 판이 월드 화면의 깊이를 건드릴 일은 없다.
    //
    // 원본이 알파로 실루엣을 만드는 경우(깃펜 깃털) 판도 같은 알파로 잘라야 한다. 그러지
    // 않으면 뚫린 자리까지 깊이가 남아, 화면에 보이지도 않는 영역에서 글자가 사라진다.
    // _BaseMap이 비어 있으면 알파 1로 읽히므로 통짜 판으로도 그대로 쓸 수 있다.
    Properties
    {
        _BaseMap ("Alpha Source", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-100" }

        Pass
        {
            Name "CrispUiDepthMask"
            Cull Off
            ColorMask 0
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Cutoff;
            CBUFFER_END

            // YachtCameraRig.ApplyRenderSettings가 채우는 전역값. 업스케일 셰이더가 쓰는
            // 가상 격자와 같은 값이라, 여기서 같은 격자로 스냅하면 두 실루엣이 어긋나지 않는다.
            float4 _PixelEdgeVirtualResolution;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // 화면에 실제로 보이는 깃펜은 월드 카메라가 1920x1080에 그린 뒤 업스케일 셰이더가
            // 가상 격자의 셀 중심 한 점만 뽑은 결과다. 반면 이 판은 Crisp 카메라가 풀해상도로
            // 그린다. 그대로 두면 판정 지점이 서로 달라 실루엣이 최대 한 셀만큼 어긋난다.
            // 그래서 알파를 읽을 자리를 같은 격자의 셀 중심으로 옮긴다. UV는 보간값이라 직접
            // 스냅할 수 없으므로, 화면에서 옮긴 거리만큼 UV 미분을 더해 같은 자리를 찾아간다.
            float2 SnapUvToPixelGrid(float2 uv, float4 positionCS)
            {
                float2 resolution = _PixelEdgeVirtualResolution.xy;
                if (resolution.x < 1.0 || resolution.y < 1.0) return uv;

                float2 screenUV = positionCS.xy / _ScreenParams.xy;
                float2 snappedUV = (floor(screenUV * resolution) + 0.5) / resolution;
                float2 pixelDelta = (snappedUV - screenUV) * _ScreenParams.xy;

                return uv + ddx(uv) * pixelDelta.x + ddy(uv) * pixelDelta.y;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = SnapUvToPixelGrid(input.uv, input.positionCS);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
