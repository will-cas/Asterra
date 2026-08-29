Shader "Asterra/SoftFogOfWar"
{
    Properties
    {
        _ExploredTex ("Explored", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0.22, 0.28, 0.36, 1)
        _UnexploredAlpha ("Unexplored Alpha", Range(0,1)) = 0.52
        _ExploredAlpha ("Explored Alpha", Range(0,1)) = 0.22
        _MapOrigin ("Map Origin XZ", Vector) = (-450, -450, 0, 0)
        _MapSize ("Map Size", Float) = 900
        _EdgeSoftness ("Edge Softness", Range(0.05, 0.6)) = 0.4
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SoftFog"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_VISION 64

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            TEXTURE2D(_ExploredTex);
            SAMPLER(sampler_ExploredTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _UnexploredAlpha;
                float _ExploredAlpha;
                float4 _MapOrigin;
                float _MapSize;
                float _EdgeSoftness;
            CBUFFER_END

            int _VisionCount;
            float4 _VisionData[MAX_VISION];

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                return output;
            }

            float SoftVision(float2 xz)
            {
                float best = 0;
                int count = min(_VisionCount, MAX_VISION);
                for (int i = 0; i < count; i++)
                {
                    float2 center = _VisionData[i].xy;
                    float radius = max(0.001, _VisionData[i].z);
                    float d = distance(xz, center);
                    float inner = radius * (1.0 - _EdgeSoftness);
                    float v = 1.0 - smoothstep(inner, radius, d);
                    best = max(best, v);
                }
                return best;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 xz = input.positionWS.xz;
                float2 uv = (xz - _MapOrigin.xy) / max(0.001, _MapSize);
                if (uv.x < 0 || uv.y < 0 || uv.x > 1 || uv.y > 1)
                    return half4(0, 0, 0, 0);

                float explored = SAMPLE_TEXTURE2D(_ExploredTex, sampler_ExploredTex, uv).r;
                float vision = SoftVision(xz);
                float shroud = 1.0 - vision;
                float baseAlpha = lerp(_UnexploredAlpha, _ExploredAlpha, saturate(explored));
                float alpha = baseAlpha * shroud;
                alpha *= smoothstep(0.0, 0.25, shroud);

                // Slate mist — never opaque black.
                float3 mist = _FogColor.rgb;
                return half4(mist, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
