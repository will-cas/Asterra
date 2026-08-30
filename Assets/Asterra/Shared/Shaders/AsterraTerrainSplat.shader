Shader "Asterra/TerrainSplat"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _GrassTex ("Grass", 2D) = "white" {}
        _DirtTex ("Dirt", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        _SandTex ("Sand", 2D) = "white" {}
        _UvScale ("World UV Scale", Float) = 0.085
        _AmbientFloor ("Ambient Floor", Range(0,1)) = 0.32
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

            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);
            TEXTURE2D(_DirtTex);
            SAMPLER(sampler_DirtTex);
            TEXTURE2D(_RockTex);
            SAMPLER(sampler_RockTex);
            TEXTURE2D(_SandTex);
            SAMPLER(sampler_SandTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _UvScale;
                float _AmbientFloor;
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
                output.shadowCoord = TransformWorldToShadowCoord(pos.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float4 w = max(input.color, 0);
                float sum = w.r + w.g + w.b + w.a + 1e-5;
                w /= sum;

                float2 uv = input.positionWS.xz * _UvScale;
                float3 grass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uv).rgb;
                float3 dirt = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, uv).rgb;
                float3 rock = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, uv).rgb;
                float3 sand = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uv).rgb;
                float3 albedo = (grass * w.r + dirt * w.g + rock * w.b + sand * w.a) * _Color.rgb;

                float slope = 1.0 - saturate(normal.y);
                albedo = lerp(albedo, albedo * float3(0.72, 0.7, 0.68), saturate(slope * 1.35));

                Light mainLight = GetMainLight(input.shadowCoord);
                float ndotl = saturate(dot(normal, mainLight.direction));
                float3 ambient = max(SampleSH(normal), _AmbientFloor.xxx);
                float3 lighting = ambient + mainLight.color * mainLight.shadowAttenuation * (0.22 + 0.78 * ndotl);
                return half4(albedo * lighting, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Asterra/VertexColorLit"
}
