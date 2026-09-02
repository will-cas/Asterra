Shader "Asterra/LitPBR"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.04
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _TexBlend ("Texture Blend", Range(0,1)) = 1
        _UvScale ("World UV Scale (0 = mesh UV)", Float) = 0
        _TeamColor ("Team Color", Color) = (0.55, 0.22, 0.7, 1)
        _TeamCloth ("Team Cloth Weight", Range(0,1)) = 1
        _TeamBuilding ("Team Building Pieces", Range(0,1)) = 0
        _TeamBounds ("Team Height MinMax", Vector) = (0, 1, 0, 0)
        _AnimParams ("Anim Gait Move Attack Gather", Vector) = (0, 0, 0, 0)
        _AnimParams2 ("Anim Idle Hit Role Time", Vector) = (0, 0, 0, 0)
        _AnimBounds ("Anim Height MinMax", Vector) = (0, 1, 0, 0)
        _AnimParams3 ("Anim Death Run Wade Carry", Vector) = (0, 0, 0, 0)
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
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float positionOSY : TEXCOORD5;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseMap_ST;
                float _Metallic;
                float _BumpScale;
                float _TexBlend;
                float _UvScale;
                float4 _TeamColor;
                float _TeamCloth;
                float _TeamBuilding;
                float4 _TeamBounds;
                float4 _AnimParams;
                float4 _AnimParams2;
                float4 _AnimBounds;
                float4 _AnimParams3;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 os = AsterraAnimateVertex(input.positionOS.xyz, _AnimParams, _AnimParams2, _AnimBounds, _AnimParams3);
                VertexPositionInputs pos = GetVertexPositionInputs(os);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = nrm.normalWS;
                output.tangentWS = nrm.tangentWS;
                output.bitangentWS = nrm.bitangentWS;
                output.positionOSY = input.positionOS.y;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = _UvScale > 0.0001
                    ? input.positionWS.xz * _UvScale
                    : input.uv;
                half3 texA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half3 pieceColor = max(input.color.rgb, half3(0.04, 0.04, 0.04));
                half3 albedo = lerp(_Color.rgb, texA * _Color.rgb, saturate(_TexBlend));
                albedo *= lerp(half3(1, 1, 1), pieceColor, 0.18);
                float piece = AsterraTeamPieceMask(input.positionOSY, _TeamBounds.x, _TeamBounds.y, _TeamBuilding);
                albedo = AsterraApplyTeamColor(albedo, _TeamColor.rgb, piece, _TeamCloth);
                albedo = max(albedo, half3(0.02, 0.02, 0.02));

                half rough = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                float3 nWS = normalize(
                    nTS.x * input.tangentWS +
                    nTS.y * input.bitangentWS +
                    nTS.z * input.normalWS);

                half3 hsv = AsterraRgbToHsv(pieceColor);
                float gold = saturate(1.0 - abs(hsv.x - 0.12) * 9.0) * smoothstep(0.28, 0.55, hsv.y) * smoothstep(0.35, 0.7, hsv.z);
                float crystal = saturate(1.0 - abs(hsv.x - 0.72) * 6.0) * smoothstep(0.25, 0.55, hsv.y);
                float glass = saturate(1.0 - abs(hsv.x - 0.55) * 8.0) * smoothstep(0.2, 0.5, hsv.y);
                float steel = (1.0 - hsv.y) * smoothstep(0.28, 0.55, hsv.z);
                half metal = saturate(_Metallic + gold * 0.72 + steel * 0.62 + crystal * 0.38);
                half occlusion = AsterraCavityAO(nWS);
                half smoothness = saturate(1.0 - rough + gold * 0.35 + crystal * 0.45 + glass * 0.4);
                return AsterraShadePBR(input.positionWS, input.positionCS, nWS, albedo, metal, smoothness, occlusion);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
