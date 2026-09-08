#ifndef TESSERA_COSMIC_AUGMENT_STATE_INCLUDED
#define TESSERA_COSMIC_AUGMENT_STATE_INCLUDED

// 코스믹 큐브의 굴림 예산 3상태(기본 / 등가교환 대기 / 소진)를 다섯 셰이더가 같은 식으로 처리한다.
// HDR 색 쌍을 열다섯 개 손으로 맞추는 대신, 각 셰이더의 최종 컬러 한 지점에서 채도와 명도만 건드린다.
//
// 제약: 모든 카메라가 allowHDR = false이고(YachtCameraRig.cs) 화면은 480x270 격자로 깎인다
// (DicePixelUpscale.shader). 따라서 상태 차이는 반드시 0~1 안에서 색상과 명도 차로 읽혀야 한다.
// 1.0을 넘는 값은 전부 흰색으로 뭉개져 "탈색"이 "발광"으로 보인다.
//
// 휘도 가중치는 DiceCosmicCube.shader의 luminanceWeights와 같은 Rec.709 값이다.
// 두 곳이 어긋나면 같은 큐브의 회색 톤이 셰이더마다 달라진다.
static const float3 kCosmicLuminanceWeights = float3(0.2126, 0.7152, 0.0722);

// color : 각 셰이더가 만든 최종 RGB. 알파는 넘기지 않는다.
// tint  : rgb = 틴트 색(선형 공간), a = 틴트 강도(일렁임 포함)
// drain : 0 = 그대로, 1 = 채도 완전 제거 + 65% 밝기
//
// 알파를 함께 줄이면 안 된다. 가산 블렌딩(Blend SrcAlpha One) 레이어는 화면 기여가 rgb * alpha라
// 두 번 어두워지고(0.65^2 = 0.42), 알파 블렌딩 레이어는 "회색"이 아니라 "투명"해진다.
// 상태 표현은 RGB에서만 한다.
float3 ApplyCosmicAugmentState(float3 color, float4 tint, float drain)
{
    // 원본 휘도는 1을 넘을 수 있다. 성운과 코어가 가산으로 쌓이기 때문이다.
    // 목표색은 반드시 saturate한 값으로 만들어야 회색이 흰색으로 클리핑되지 않는다.
    float value = saturate(dot(color, kCosmicLuminanceWeights));

    // 1. 소진: 채도를 걷어내고 약간 어둡게 한다.
    float d = saturate(drain);
    color = lerp(color, value.xxx, d);
    color *= lerp(1.0, 0.65, d);

    // 2. 등가교환 대기: 휘도는 유지한 채 색상만 보라로 민다. 틴트 자신의 휘도로 나눠야
    //    원래의 밝고 어두운 무늬가 살아남아 저해상도에서 형태가 뭉개지지 않는다.
    float t = saturate(tint.a);
    float tintLuminance = max(dot(tint.rgb, kCosmicLuminanceWeights), 1e-4);
    float3 tinted = min(tint.rgb * (value / tintLuminance), 1.0);
    color = lerp(color, tinted, t);

    return color;
}

#endif
