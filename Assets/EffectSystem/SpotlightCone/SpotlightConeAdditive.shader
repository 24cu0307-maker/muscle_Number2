/*━━━━━━━━━*
*@file SpotlightConeAdditive.shader*
*@brief 透過加算合成でスポットライトの光量を描画する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Universal Render Pipeline用*
*━━━━━━━━━*/

Shader "Muscle/Effects/Spotlight Cone Additive"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.0, 0.85, 0.45, 1.0)
        _Intensity ("Emission Intensity", Range(0, 10)) = 2.0
        _Opacity ("Opacity", Range(0, 1)) = 0.18
        _EdgeSoftness ("Edge Softness", Range(0.1, 8)) = 2.0
        _StartFade ("Start Fade", Range(0.01, 1)) = 0.12
        _EndFade ("End Fade", Range(0.01, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SpotlightCone"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Opacity;
                half _EdgeSoftness;
                half _StartFade;
                half _EndFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positions.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half3 normal = normalize(input.normalWS) * (isFrontFace ? 1.0h : -1.0h);
                half fresnel = pow(saturate(1.0h - abs(dot(normal, normalize(input.viewDirWS)))), _EdgeSoftness);
                half startFade = smoothstep(0.0h, max(_StartFade, 0.001h), input.uv.y);
                half endFade = 1.0h - smoothstep(1.0h - max(_EndFade, 0.001h), 1.0h, input.uv.y);
                half alpha = _Opacity * fresnel * startFade * endFade;
                return half4(_Color.rgb * _Intensity, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
