Shader "DicePoC/CrispUiText"
{
    // 깊이를 존중하는 글리프 셰이더.
    //
    // Unity 내장 폰트 재질(`GUI/Text Shader`)은 `ZTest Always`에 `Cull Off`라 깊이 버퍼를 통째로
    // 무시한다. UI 캔버스에는 맞지만, 8면 주사위처럼 입체에 붙은 글자에는 뒷면 글자가 몸체를 뚫고
    // 나온다. `CrispUiDepthMask`가 깊이를 남겨도 글자가 그 검사를 받지 않기 때문이다.
    //
    // 이 셰이더는 같은 글리프 아틀라스를 그리되 깊이 검사를 켜고 뒷면을 잘라낸다. 색은 TextMesh가
    // 정점 색으로 실어 보내므로 종류별 팔레트도 재질 하나로 처리된다.
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "CrispUiText"
            // TextMesh가 굽는 사각형의 감김 방향은 보장되지 않는다. 실제로 Cull Back이면 보이는 쪽이
            // 잘려 글자가 통째로 사라진다. 뒷면 글자는 감김이 아니라 깊이 검사로 가린다.
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 동적 폰트 아틀라스는 글리프 덮임을 알파에만 담는다.
                half4 color = input.color;
                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
