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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float4 shadowCoord : TEXCOORD2;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.color = input.color * _Color;
                output.shadowCoord = TransformWorldToShadowCoord(pos.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float2 xz = input.positionWS.xz;

                float detailSample = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, xz * _DetailScale).r;
                float macro = ValueNoise(xz * _MacroScale);
                float mottling = lerp(1.0, detailSample * 1.35, _DetailStrength);
                mottling *= lerp(1.0, 0.82 + macro * 0.36, _MacroStrength);

                float wetMask = saturate((input.color.b - max(input.color.r, input.color.g)) * 3.0);

                // Slope darkening for rocky cliffs — skip water so basins stay blue.
                float slope = 1.0 - saturate(normal.y);
                float3 albedo = input.color.rgb * lerp(mottling, 1.05, wetMask);
                albedo = lerp(albedo, albedo * float3(0.72, 0.7, 0.68), saturate(slope * 1.4) * (1.0 - wetMask));
                // Guarantee readable water even under low key light / evening.
                float3 waterFloor = float3(0.12, 0.32, 0.52);
                albedo = lerp(albedo, max(albedo, waterFloor), wetMask * 0.85);

                Light mainLight = GetMainLight(input.shadowCoord);
                float ndotl = saturate(dot(normal, mainLight.direction));
                float3 ambient = SampleSH(normal) * 0.7 + float3(0.1, 0.12, 0.14);
                ambient = max(ambient, float3(0.16, 0.2, 0.24) * (0.55 + wetMask * 0.6));
                float3 lighting = ambient + mainLight.color * mainLight.shadowAttenuation * (0.22 + 0.78 * ndotl);
                float3 diffuse = albedo * lighting;

                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), lerp(24.0, 64.0, wetMask))
                             * _Gloss * lerp(0.08, 1.15, wetMask) * mainLight.shadowAttenuation;
                diffuse += mainLight.color * spec;
                // Soft fresnel sheen on water.
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 3.0) * wetMask;
                diffuse += float3(0.35, 0.55, 0.75) * fresnel * 0.35;

                return half4(diffuse, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
