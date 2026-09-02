Shader "Asterra/TerrainSplat"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _GrassTex ("Grass", 2D) = "white" {}
        _DirtTex ("Dirt", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        _SandTex ("Sand", 2D) = "white" {}
        _IceTex ("Ice", 2D) = "white" {}
        _GrassN ("Grass Normal", 2D) = "bump" {}
        _DirtN ("Dirt Normal", 2D) = "bump" {}
        _RockN ("Rock Normal", 2D) = "bump" {}
        _SandN ("Sand Normal", 2D) = "bump" {}
        _IceN ("Ice Normal", 2D) = "bump" {}
        _GrassR ("Grass Rough", 2D) = "white" {}
        _DirtR ("Dirt Rough", 2D) = "white" {}
        _RockR ("Rock Rough", 2D) = "white" {}
        _SandR ("Sand Rough", 2D) = "white" {}
        _UvScale ("World UV Scale", Float) = 0.07
        _MacroScale ("Macro Scale", Float) = 0.012
        _MacroBlend ("Macro Blend", Range(0,1)) = 0.38
        _HeightBlend ("Height Blend", Range(0.04,0.6)) = 0.22
        _BumpScale ("Bump Scale", Range(0,2)) = 1.15
        _SnowLine ("Snow Line", Float) = 7.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "AsterraLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                float4 biome : TEXCOORD2;
            };

            TEXTURE2D(_GrassTex); TEXTURE2D(_DirtTex); TEXTURE2D(_RockTex); TEXTURE2D(_SandTex); TEXTURE2D(_IceTex);
            TEXTURE2D(_GrassN); TEXTURE2D(_DirtN); TEXTURE2D(_RockN); TEXTURE2D(_SandN); TEXTURE2D(_IceN);
            TEXTURE2D(_GrassR); TEXTURE2D(_DirtR); TEXTURE2D(_RockR); TEXTURE2D(_SandR);
            SAMPLER(sampler_GrassTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _UvScale;
                float _MacroScale;
                float _MacroBlend;
                float _HeightBlend;
                float _BumpScale;
                float _SnowLine;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.color = input.color;
                output.biome = float4(input.uv, input.uv2);
                return output;
            }

            void SampleLayer(
                TEXTURE2D_PARAM(albedoTex, albedoSamp),
                TEXTURE2D(nTex),
                TEXTURE2D(rTex),
                float2 uv, float2 uvM,
                out float3 albedo, out float3 nTS, out float rough, out float height)
            {
                float3 a0 = SAMPLE_TEXTURE2D(albedoTex, albedoSamp, uv).rgb;
                float3 a1 = SAMPLE_TEXTURE2D(albedoTex, albedoSamp, uvM).rgb;
                albedo = lerp(a0, a1, _MacroBlend);
                nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(nTex, albedoSamp, uv), _BumpScale);
                rough = SAMPLE_TEXTURE2D(rTex, albedoSamp, uv).r;
                height = albedo.g * 0.55 + (1.0 - rough) * 0.45;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 geom = normalize(input.normalWS);
                float4 w = max(input.color, 0);
                float slope = 1.0 - saturate(geom.y);

                w.b += slope * 0.7 * (1.0 - w.a);
                w.r *= lerp(1.0, 0.22, saturate(slope * 1.6));
                w.g += slope * 0.12 * w.r;
                float sum = w.r + w.g + w.b + w.a + 1e-5;
                w /= sum;

                float2 uv = input.positionWS.xz * _UvScale;
                float2 uvM = input.positionWS.xz * _MacroScale;
                float3 aG, aD, aR, aS, nG, nD, nR, nS;
                float rG, rD, rR, rS, hG, hD, hR, hS;
                SampleLayer(TEXTURE2D_ARGS(_GrassTex, sampler_GrassTex), _GrassN, _GrassR, uv, uvM, aG, nG, rG, hG);
                SampleLayer(TEXTURE2D_ARGS(_DirtTex, sampler_GrassTex), _DirtN, _DirtR, uv, uvM, aD, nD, rD, hD);
                SampleLayer(TEXTURE2D_ARGS(_RockTex, sampler_GrassTex), _RockN, _RockR, uv, uvM, aR, nR, rR, hR);
                SampleLayer(TEXTURE2D_ARGS(_SandTex, sampler_GrassTex), _SandN, _SandR, uv, uvM, aS, nS, rS, hS);

                if (slope > 0.28)
                {
                    float3 tp = AsterraTriplanarAlbedo(TEXTURE2D_ARGS(_RockTex, sampler_GrassTex), input.positionWS, geom, _UvScale * 0.85);
                    aR = lerp(aR, tp, saturate((slope - 0.28) * 2.2));
                    hR += slope * 0.12;
                }

                w = AsterraHeightBlend(w, float4(hG, hD, hR, hS), _HeightBlend);

                float3 albedo = aG * w.r + aD * w.g + aR * w.b + aS * w.a;
                float3 nTS = nG * w.r + nD * w.g + nR * w.b + nS * w.a;
                float rough = rG * w.r + rD * w.g + rR * w.b + rS * w.a;

                float wet = saturate(input.biome.x);
                float snow = saturate(input.biome.y);
                float shore = saturate(input.biome.z);
                float scorched = saturate(input.biome.w);
                float rain = saturate(_AsterraWind.y);
                wet = saturate(wet + rain * 0.4 * (w.g + w.r * 0.35) + shore * 0.55);

                float altSnow = saturate((input.positionWS.y - _SnowLine) / 3.5) * saturate(geom.y);
                snow = max(snow, altSnow * 0.85);

                if (snow > 0.02)
                {
                    float3 iceA = SAMPLE_TEXTURE2D(_IceTex, sampler_GrassTex, uv).rgb;
                    float3 iceN = UnpackNormalScale(SAMPLE_TEXTURE2D(_IceN, sampler_GrassTex, uv), 0.6);
                    albedo = lerp(albedo, iceA * 1.05, snow);
                    nTS = lerp(nTS, iceN, snow * 0.65);
                    rough = lerp(rough, 0.18, snow);
                }

                albedo *= _Color.rgb;
                albedo = lerp(albedo, albedo * float3(0.52, 0.58, 0.5), wet);
                albedo = lerp(albedo, albedo * float3(0.28, 0.24, 0.2), scorched);
                albedo += shore * (1.0 - wet) * float3(0.08, 0.07, 0.05);
                albedo = lerp(albedo, albedo * float3(0.78, 0.76, 0.74), saturate(slope * 0.9) * (1.0 - snow));

                float3 nWS = AsterraPerturbGroundNormal(geom, nTS, 1.0);
                half smoothness = saturate((1.0 - rough) * 0.85 + wet * 0.32 + snow * 0.28);
                half cavity = lerp(AsterraCavityAO(nWS), 0.55, saturate(slope * 1.4));
                cavity *= lerp(1.0, 0.72, scorched);
                cavity = lerp(cavity, 0.92, wet * 0.5);
                return AsterraShadePBR(
                    input.positionWS,
                    input.positionCS,
                    nWS,
                    albedo,
                    lerp(0.03, 0.06, wet),
                    smoothness,
                    cavity);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
