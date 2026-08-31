#ifndef ASTERRA_LIGHTING_INCLUDED
#define ASTERRA_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

float _AsterraCloudStrength;
float4 _AsterraCloudParams;

float AsterraHash21(float2 p)
{
    p = frac(p * float2(123.34, 345.45));
    p += dot(p, p + 34.345);
    return frac(p.x * p.y);
}

float AsterraValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = AsterraHash21(i);
    float b = AsterraHash21(i + float2(1, 0));
    float c = AsterraHash21(i + float2(0, 1));
    float d = AsterraHash21(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float AsterraCloudShadow(float3 positionWS)
{
    float2 uv = positionWS.xz * _AsterraCloudParams.xy + _AsterraCloudParams.zw;
    float n = AsterraValueNoise(uv) * 0.55 + AsterraValueNoise(uv * 2.13 + 19.1) * 0.45;
    n = smoothstep(0.32, 0.78, n);
    return lerp(1.0, lerp(0.42, 1.0, n), saturate(_AsterraCloudStrength));
}

half AsterraCavityAO(float3 normalWS)
{
    float up = saturate(normalWS.y * 0.5 + 0.5);
    return lerp(0.62, 1.0, up);
}

void AsterraFillInputData(float3 positionWS, float4 positionCS, float3 normalWS, inout InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.positionCS = positionCS;
    inputData.normalWS = NormalizeNormalPerPixel(normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), 0);
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);
}

half4 AsterraShadePBR(float3 positionWS, float4 positionCS, float3 normalWS, half3 albedo, half metallic, half smoothness, half occlusion)
{
    InputData inputData;
    AsterraFillInputData(positionWS, positionCS, normalWS, inputData);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.metallic = metallic;
    surfaceData.specular = 0;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = 0;
    surfaceData.occlusion = occlusion * AsterraCloudShadow(positionWS);
    surfaceData.alpha = 1;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 0;

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

#endif
