/*━━━━━━━━━*
*@file LiveParticleAdditive.shader*
*@brief 火花やきらめき用の加算Particleを描画する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Universal Render Pipeline用*
*━━━━━━━━━*/

Shader "Muscle/Particles/Live Additive"
{
    Properties
    {
        _Softness ("Edge Softness", Range(0.5, 12)) = 3.0
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
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half distanceFromCenter = length(input.uv - 0.5h) * 2.0h;
                half radialAlpha = pow(saturate(1.0h - distanceFromCenter), _Softness);
                return half4(input.color.rgb, input.color.a * radialAlpha);
            }
            ENDHLSL
        }
    }
}
