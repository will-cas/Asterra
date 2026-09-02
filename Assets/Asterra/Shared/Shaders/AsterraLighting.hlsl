#ifndef ASTERRA_LIGHTING_INCLUDED
#define ASTERRA_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

float _AsterraCloudStrength;
float4 _AsterraCloudParams;
float4 _AsterraWind;

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

float3 AsterraPerturbGroundNormal(float3 geomWS, float3 nTS, float scale)
{
    return normalize(geomWS + float3(nTS.x, 0.0, nTS.y) * scale);
}

float4 AsterraHeightBlend(float4 weights, float4 height, float sharpness)
{
    float4 wh = max(weights, 0.0) * (height + 0.03);
    float mx = max(max(wh.r, wh.g), max(wh.b, wh.a));
    float4 hw = saturate(wh - (mx - max(0.02, sharpness)));
    float s = hw.r + hw.g + hw.b + hw.a + 1e-5;
    return hw / s;
}

float3 AsterraTriplanarAlbedo(TEXTURE2D_PARAM(tex, samp), float3 posWS, float3 nWS, float scale)
{
    float3 n = abs(nWS);
    n = saturate(n - 0.22);
    n /= max(1e-4, n.x + n.y + n.z);
    float3 cx = SAMPLE_TEXTURE2D(tex, samp, posWS.yz * scale).rgb;
    float3 cy = SAMPLE_TEXTURE2D(tex, samp, posWS.xz * scale).rgb;
    float3 cz = SAMPLE_TEXTURE2D(tex, samp, posWS.xy * scale).rgb;
    return cx * n.x + cy * n.y + cz * n.z;
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

float3 AsterraRgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 AsterraHsvToRgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

float AsterraTeamPieceMask(float yOS, float yMin, float yMax, float building)
{
    float t = saturate((yOS - yMin) / max(0.001, yMax - yMin));
    if (building > 0.5)
    {
        float roof = smoothstep(0.58, 0.76, t);
        float belt = smoothstep(0.22, 0.32, t) * (1.0 - smoothstep(0.42, 0.52, t)) * 0.4;
        return saturate(roof + belt);
    }

    float tabard = smoothstep(0.36, 0.46, t) * (1.0 - smoothstep(0.68, 0.80, t));
    float sash = smoothstep(0.30, 0.36, t) * (1.0 - smoothstep(0.42, 0.48, t)) * 0.75;
    float plume = smoothstep(0.88, 0.94, t);
    return saturate(tabard + sash + plume * 0.9);
}

half3 AsterraApplyTeamColor(half3 tex, half3 team, float pieceMask, float clothWeight)
{
    float luma = dot(tex, half3(0.299, 0.587, 0.114));
    float avg = (tex.r + tex.g + tex.b) * 0.3333;
    float chroma = length(tex - avg);
    float dyeable = smoothstep(0.035, 0.14, chroma) * smoothstep(0.05, 0.14, luma);
    float mask = saturate(pieceMask * lerp(0.12, 1.0, saturate(clothWeight)) * lerp(0.25, 1.0, dyeable));
    mask *= saturate(clothWeight * 1.15);

    float3 thsv = AsterraRgbToHsv(saturate(team));
    float3 hsv = AsterraRgbToHsv(tex);
    hsv.x = thsv.x;
    hsv.y = lerp(hsv.y, saturate(thsv.y * 0.85 + 0.12), 0.8);
    hsv.z = luma * 1.05;
    half3 dyed = AsterraHsvToRgb(hsv);
    return lerp(tex, dyed, mask);
}

float3 AsterraRotAxis(float3 p, float3 pivot, float3 axis, float ang)
{
    float3 v = p - pivot;
    float s, c;
    sincos(ang, s, c);
    return pivot + v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
}

float3 AsterraAnimateVertex(float3 os, float4 anim, float4 anim2, float4 bounds, float4 anim3)
{
    float move = saturate(anim.y);
    float attack = saturate(anim.z);
    float gather = saturate(anim.w);
    float idle = saturate(anim2.x);
    float hit = saturate(anim2.y);
    float role = anim2.z;
    float gait = anim.x;
    float death = saturate(anim3.x);
    float run = saturate(anim3.y);
    float wade = saturate(anim3.z);
    float carry = saturate(anim3.w);
    float yMin = bounds.x;
    float yMax = max(bounds.y, bounds.x + 0.01);
    float ny = saturate((os.y - yMin) / (yMax - yMin));
    float span = yMax - yMin;
    float side = os.x >= 0.0 ? 1.0 : -1.0;
    float hipY = lerp(yMin, yMax, 0.40);
    float shY = lerp(yMin, yMax, 0.70);
    float headY = lerp(yMin, yMax, 0.84);
    move = saturate(move + run * 0.35);

    if (death > 0.001)
    {
        os = AsterraRotAxis(os, float3(0, yMin, 0), float3(0, 0, 1), death * 1.35);
        os.y = lerp(os.y, yMin + span * 0.12, death);
        os.z += death * span * 0.2;
        return os;
    }

    if (move + attack + gather + idle + hit + wade + carry < 0.001)
        return os;

    if (role > 4.5)
    {
        os.y += sin(gait) * 0.04 * span + idle * sin(anim2.w * 1.6) * 0.02 * span;
        os = AsterraRotAxis(os, float3(0, lerp(yMin, yMax, 0.2), 0), float3(0, 1, 0), sin(gait * 0.5) * 0.18 * move);
        os = AsterraRotAxis(os, float3(0, lerp(yMin, yMax, 0.35), 0), float3(1, 0, 0), sin(gait) * 0.08 * move);
        return os;
    }

    // --- Biped / default ---
    if (role < 1.5)
    {
        float step = sin(gait);
        float step2 = sin(gait * 2.0);
        float armLag = sin(gait - 0.28);

        os.y += (-min(0.0, step2) * 0.035 + abs(step) * 0.012) * move * span;

        if (ny < 0.44)
        {
            float hip = hipY;
            float swing = side * step * lerp(0.42, 0.62, move);
            os = AsterraRotAxis(os, float3(os.x, hip, 0.0), float3(1, 0, 0), swing * move);
            float plant = saturate(-side * step);
            os.y += plant * 0.05 * move * span * (1.0 - ny / 0.44);
        }
        else if (ny < 0.82 && abs(os.x) > span * 0.04)
        {
            float draw = role > 0.5 ? attack * 0.85 : 0.0;
            float chop = (role > 3.5 ? max(attack, gather) : gather) * side * -1.05;
            float swing = -side * armLag * 0.48 * move + chop + draw * (ny > 0.55 ? -0.9 : 0.0);
            os = AsterraRotAxis(os, float3(os.x, shY, 0.0), float3(1, 0, 0), swing);
        }

        if (ny > 0.80)
            os = AsterraRotAxis(os, float3(0, headY, 0), float3(1, 0, 0), -step * 0.1 * move + idle * sin(anim2.w * 1.1) * 0.04);

        os.z += attack * ny * span * 0.16;
        os.y += idle * sin(anim2.w * 1.45) * span * 0.012 * saturate(ny * 1.2);
        float shift = idle * sin(anim2.w * 0.55) * 0.06;
        os = AsterraRotAxis(os, float3(0, hipY, 0), float3(0, 0, 1), shift);
    }
    else if (role < 2.5)
    {
        // Cavalry: horse bounce + rider counter.
        float gallop = sin(gait);
        float beat = abs(sin(gait * 0.5));
        if (ny < 0.55)
        {
            os.y += beat * 0.07 * move * span;
            os.z += gallop * 0.04 * move * span;
            if (ny < 0.32)
            {
                float diag = sign(os.x) * sign(os.z + 0.001);
                os = AsterraRotAxis(os, float3(os.x, lerp(yMin, yMax, 0.28), 0), float3(1, 0, 0), diag * gallop * 0.55 * move);
            }
        }
        else
        {
            os.y += -beat * 0.03 * move * span + idle * sin(anim2.w) * span * 0.01;
            os = AsterraRotAxis(os, float3(0, lerp(yMin, yMax, 0.62), 0), float3(1, 0, 0), -gallop * 0.12 * move + attack * -0.25);
        }
    }
    else if (role < 3.5)
    {
        // Siege: rumble + rolling contact.
        float rumble = sin(gait * 3.1) * 0.012 + sin(gait * 5.7) * 0.008;
        os.y += rumble * move * span;
        os.x += sin(gait * 4.0) * 0.01 * move * span;
        if (ny < 0.28)
            os.y += abs(sin(gait * 2.0)) * 0.03 * move * span;
        os.z += attack * 0.1 * span;
    }
    else
    {
        // Builder: walk plus chop.
        float step = sin(gait);
        os.y += abs(step) * 0.03 * move * span;
        if (ny < 0.42)
            os = AsterraRotAxis(os, float3(os.x, hipY, 0), float3(1, 0, 0), side * step * 0.5 * move);
        if (ny > 0.48)
        {
            float chop = sin(gait * 1.35 + 0.4);
            float work = max(gather, attack);
            os = AsterraRotAxis(os, float3(abs(os.x) > 0.01 ? os.x : span * 0.08, shY, 0), float3(1, 0, 0),
                -side * step * 0.35 * move + work * chop * -0.9);
        }
        os.y += idle * sin(anim2.w * 1.2) * span * 0.01;
    }

    os.x += hit * sin(anim2.w * 40.0) * span * 0.03;
    os.z += attack * saturate(ny) * span * 0.04;
    os.y += wade * (0.03 + abs(sin(gait * 1.7)) * 0.04) * span;
    os = AsterraRotAxis(os, float3(0, hipY, 0), float3(1, 0, 0), carry * 0.18 + wade * 0.08);
    if (ny > 0.45)
        os.z -= carry * span * 0.04;
    return os;
}

#endif
