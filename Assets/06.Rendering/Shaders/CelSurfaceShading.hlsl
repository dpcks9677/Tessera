#ifndef TESSERA_CEL_SURFACE_SHADING_INCLUDED
#define TESSERA_CEL_SURFACE_SHADING_INCLUDED

// CelSurface.shader의 세 패스가 공유하는 셰이딩 규약. 노멀 스냅을 ForwardLit과 DepthNormals가
// 똑같이 써야 엣지 필터의 노멀 경계와 색 경계가 같은 자리에 놓인다.

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float4 _RampValues;
    float4 _RimColor;
    float  _Bands;
    float  _NormalSnap;
    float  _RimThreshold;
    float  _RimStrength;
    float  _AmbientStrength;
    float  _ReceiveShadows;
CBUFFER_END

// 오브젝트 공간 노멀을 가장 큰 성분의 축으로 스냅한다. 주사위처럼 면이 축에 정렬된 메시에서
// 면 하나가 통째로 같은 값이 되게 하는 것이 목적이다.
float3 CelSnapNormalOS(float3 normalOS)
{
    float3 magnitude = abs(normalOS);
    if (magnitude.x >= magnitude.y && magnitude.x >= magnitude.z)
    {
        return float3(normalOS.x >= 0.0 ? 1.0 : -1.0, 0.0, 0.0);
    }
    if (magnitude.y >= magnitude.z)
    {
        return float3(0.0, normalOS.y >= 0.0 ? 1.0 : -1.0, 0.0);
    }
    return float3(0.0, 0.0, normalOS.z >= 0.0 ? 1.0 : -1.0);
}

float3 CelResolveNormal(float3 normalOS, float3 normalWS)
{
    if (_NormalSnap < 0.5)
    {
        return normalize(normalWS);
    }
    return normalize(TransformObjectToWorldNormal(CelSnapNormalOS(normalize(normalOS))));
}

// 램프 인덱스를 float4 성분으로 고르는 분기. 배열 인덱싱을 쓰면 상수 버퍼 성분 접근이
// 플랫폼마다 갈리므로 명시적으로 고른다.
float CelRampValue(int index)
{
    if (index <= 0) return _RampValues.x;
    if (index == 1) return _RampValues.y;
    if (index == 2) return _RampValues.z;
    return _RampValues.w;
}

float3 CelShade(float3 albedo, float3 normalWS, float3 viewDirWS, float3 lightDirWS, float3 lightColor, float shadowAttenuation)
{
    // 램버트 값을 0~1로 감싸 뒷면이 순흑으로 죽지 않게 한다. 계단화 대상은 이 값이다.
    float wrapped = saturate(dot(normalWS, lightDirWS) * 0.5 + 0.5);

    // 그림자도 계단이어야 한다. 반그림자가 남으면 밴드 경계가 흐려져 3D로 읽힌다.
    //
    // 다만 그림자를 받지 않아야 하는 재질이 있다. Renderer.receiveShadows는 URP Lit 전용이라
    // 커스텀 라이팅인 이 셰이더에는 적용되지 않는다. 주사위가 그랬다. 자기 ShadowProxy가 만든
    // 그림자를 자기 얼굴에 받고, 하드 step이 그 경계를 면 한가운데에 칼같이 그어 평면을 쪼갰다.
    // 측정에서 주사위 면 안쪽 최대 평면 영역이 Baseline 90%에서 Cel 50%로 떨어진 원인이다.
    float hardShadow = _ReceiveShadows < 0.5 ? 1.0 : step(0.5, shadowAttenuation);
    wrapped *= lerp(0.55, 1.0, hardShadow);

    int bands = (int)clamp(round(_Bands), 2.0, 4.0);
    int index = (int)clamp(floor(wrapped * bands), 0.0, (float)(bands - 1));
    float rampValue = CelRampValue(index);

    float3 ambient = unity_AmbientSky.rgb * _AmbientStrength;
    float3 color = albedo * (lightColor * rampValue + ambient);

    // 하드 1밴드 림. 감쇠가 아니라 계단이라 픽셀 격자에서 한 칸 굵기로 떨어진다.
    float facing = 1.0 - saturate(dot(normalWS, viewDirWS));
    float rim = step(_RimThreshold, facing) * _RimStrength;
    return lerp(color, _RimColor.rgb, rim);
}

#endif
