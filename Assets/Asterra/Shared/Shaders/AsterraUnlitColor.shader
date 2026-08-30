Shader "Asterra/UnlitColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _UvScale ("World UV Scale", Float) = 0.18
        _TexBlend ("Texture Blend", Range(0,1)) = 0.72
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _UvScale;
                float _TexBlend;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = world;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(world);
                return output;
            }

            half4 SampleTriplanar(float3 posWS, float3 nWS)
            {
                float3 n = abs(nWS);
                n = pow(n, 4);
                float sum = n.x + n.y + n.z;
                n = sum > 0.0001 ? n / sum : float3(0, 1, 0);
                float s = _UvScale;
                half4 cx = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, posWS.zy * s);
                half4 cy = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, posWS.xz * s);
                half4 cz = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, posWS.xy * s);
                return cx * n.x + cy * n.y + cz * n.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SampleTriplanar(input.positionWS, normalize(input.normalWS));
                half4 tint = (half4)_Color;
                return lerp(tint, tex * tint, _TexBlend);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Color"
}
