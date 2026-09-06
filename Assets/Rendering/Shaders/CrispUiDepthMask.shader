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

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
