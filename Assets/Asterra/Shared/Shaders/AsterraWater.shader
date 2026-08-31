Shader "Asterra/Water"
{
    Properties
    {
        _Color ("Tint", Color) = (0.12, 0.32, 0.48, 1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.92
        _WaveScale ("Wave Scale", Float) = 0.045
        _WaveSpeed ("Wave Speed", Float) = 0.12
        _Fresnel ("Fresnel", Range(0,2)) = 0.55
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry+10"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _WaveScale;
                float _WaveSpeed;
                float _Fresnel;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                return output;
            }

            float3 WaveNormal(float3 positionWS)
            {
                float t = _Time.y * _WaveSpeed;
                float2 uv = positionWS.xz * _WaveScale;
                float n0 = AsterraValueNoise(uv + t);
                float n1 = AsterraValueNoise(uv * 1.7 - t * 0.7 + 12.4);
                float e = 0.35;
                float nx = AsterraValueNoise(uv + float2(e, 0) + t) - n0;
                float nz = AsterraValueNoise(uv + float2(0, e) + t) - n0;
                float3 n = normalize(float3(-nx * 1.8, 1.0, -nz * 1.8));
                n = normalize(n + float3((n1 - 0.5) * 0.35, 0, (n0 - 0.5) * 0.35));
                return n;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 nWS = normalize(lerp(normalize(input.normalWS), WaveNormal(input.positionWS), 0.65));
                half3 albedo = _Color.rgb * 0.55;
                half4 lit = AsterraShadePBR(
                    input.positionWS,
                    input.positionCS,
                    nWS,
                    albedo,
                    0.04,
                    _Smoothness,
                    1.0);

                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 reflectVec = reflect(-viewDir, nWS);
                half3 env = GlossyEnvironmentReflection(reflectVec, input.positionWS, 1.0 - _Smoothness, 1.0);
                float fresnel = pow(1.0 - saturate(dot(nWS, viewDir)), 3.0) * _Fresnel;
                lit.rgb += env * fresnel;
                return lit;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
