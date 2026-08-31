Shader "Asterra/TerrainLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Gloss ("Water Gloss", Range(0,1)) = 0.4
        _DetailTex ("Detail", 2D) = "white" {}
        _DetailScale ("Detail Scale", Float) = 0.08
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.45
        _MacroScale ("Macro Scale", Float) = 0.012
        _MacroStrength ("Macro Strength", Range(0,1)) = 0.22
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
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Gloss;
                float4 _DetailTex_ST;
                float _DetailScale;
                float _DetailStrength;
                float _MacroScale;
                float _MacroStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float2 xz = input.positionWS.xz;

                float detailSample = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, xz * _DetailScale).r;
                float macro = AsterraValueNoise(xz * _MacroScale);
                float mottling = lerp(1.0, detailSample * 1.35, _DetailStrength);
                mottling *= lerp(1.0, 0.82 + macro * 0.36, _MacroStrength);

                float wetMask = saturate((input.color.b - max(input.color.r, input.color.g)) * 3.0);
                float slope = 1.0 - saturate(normal.y);
                float3 albedo = input.color.rgb * lerp(mottling, 1.0, wetMask);
                albedo = lerp(albedo, albedo * float3(0.72, 0.7, 0.68), saturate(slope * 1.4) * (1.0 - wetMask));

                half smoothness = lerp(0.12, saturate(_Gloss), wetMask);
                half occlusion = lerp(AsterraCavityAO(normal), 0.9, wetMask);
                return AsterraShadePBR(input.positionWS, input.positionCS, normal, albedo, lerp(0.02, 0.08, wetMask), smoothness, occlusion);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
