/*━━━━━━━━━*
*@file LiveParticleSoft.shader*
*@brief 煙用の柔らかい透過Particleを描画する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Universal Render Pipeline用*
*━━━━━━━━━*/

Shader "Muscle/Particles/Live Soft"
{
    Properties
    {
        _Softness ("Edge Softness", Range(0.5, 8)) = 2.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Softness;
                half _NoiseStrength;
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

            half Hash(float2 value)
            {
                return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half distanceFromCenter = length(input.uv - 0.5h) * 2.0h;
                half radialAlpha = pow(saturate(1.0h - distanceFromCenter), _Softness);
                half noise = lerp(1.0h, Hash(floor(input.uv * 12.0h)), _NoiseStrength);
                return half4(input.color.rgb, input.color.a * radialAlpha * noise);
            }
            ENDHLSL
        }
    }
}
