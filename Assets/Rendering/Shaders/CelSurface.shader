Shader "Tessera/CelSurface"
{
    // 조명 응답을 몇 단계로 계단화한 셀 셰이더. 주사위, 테이블, 러너, 소품이 함께 쓴다.
    //
    // 픽셀아트를 만드는 것은 적은 색 수가 아니라 적은 평면 영역 수다. 포스트 양자화는 합성된
    // 프레임에 걸려 색 경계를 밝기 그라데이션의 등치선에 만드는데, 그 등치선은 휜 노멀 위에서
    // 동심원 밴드가 되어 도형 경계와 어긋난다(M10.6에서 확인). 여기서는 재료 단계에서 계단화해
    // 경계가 지오메트리 위에 놓이게 한다.
    //
    // _NormalSnap을 켜면 노멀을 오브젝트 공간 지배 축으로 스냅한다. 주사위처럼 면이 축에
    // 정렬된 메시는 이걸 켜야 보이는 면마다 값이 하나로 떨어진다. 메시가 실제로 어떻게 스무딩
    // 되어 있든 결과가 같아지므로 FBX와 절차적 폴백 메시의 차이도 함께 사라진다.
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}

        // 밴드 수. 3이면 램프 앞 세 값, 4면 하이라이트까지 쓴다. 금속으로 읽히길 원하는
        // 재질에만 4를 준다.
        _Bands ("Band Count", Range(2, 4)) = 3

        // 아트 가이드 명도 램프(TesseraPixelPalette.ValueScales)를 C#에서 넣는다.
        _RampValues ("Ramp Values", Vector) = (0.35, 0.65, 1.0, 1.45)

        [Toggle] _NormalSnap ("Snap Normal To Object Axis", Float) = 0

        // 쿨 림. 프레넬 감쇠가 아니라 하드 1밴드다. 아트 가이드의 웜 키 + 쿨 림 균형을
        // 픽셀아트 규칙 안에서 유지하기 위한 것이다.
        _RimColor ("Rim Color", Color) = (0.212, 0.294, 0.431, 1)
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.72
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.55

        // 최저 밴드에 더하는 상수 앰비언트 비율. 씬의 Flat 앰비언트를 그대로 쓴다.
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1.0

        // 그림자를 받을지. Renderer.receiveShadows는 URP Lit 전용이라 이 셰이더에는 통하지 않으므로
        // 재질 쪽에서 정한다. 주사위는 꺼야 한다. 자기 ShadowProxy 그림자를 자기 얼굴에 받는다.
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CelSurfaceShading.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalOS = input.normalOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = CelResolveNormal(input.normalOS, input.normalWS);
                // 직교 카메라에서는 시선 벡터가 화면 전체에서 같다. 원근 방식으로 구하면 한 면
                // 안에서도 값이 변해, 하드 림의 step 경계가 면 한가운데를 가른다. 실제로 주사위 면
                // 안쪽 최대 평면 영역이 정확히 50%로 반토막 났다.
                // URP 헬퍼가 투영 방식을 보고 알맞은 벡터를 준다.
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                float3 color = CelShade(albedo, normalWS, viewDirWS, mainLight.direction, mainLight.color, mainLight.shadowAttenuation);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            // 재질 상수 버퍼 선언을 모든 패스가 똑같이 갖고 있어야 SRP Batcher가 묶는다.
            #include "CelSurfaceShading.hlsl"

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            // 엣지 필터가 노멀을 여기서 읽는다. Forward 경로에서는 GBuffer가 없으므로 이 패스가
            // 유일한 노멀 공급원이다. 스냅한 노멀을 그대로 내보내 노멀 엣지가 주사위 모서리에
            // 정확히 걸리게 한다.
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CelSurfaceShading.hlsl"

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalOS = input.normalOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings input) : SV_Target
            {
                float3 normalWS = CelResolveNormal(input.normalOS, input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
