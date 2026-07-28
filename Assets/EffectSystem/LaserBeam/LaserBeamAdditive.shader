/*━━━━━━━━━*
*@file LaserBeamAdditive.shader*
*@brief 透過加算合成でレーザーライトを描画する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Universal Render Pipeline用*
*━━━━━━━━━*/

Shader "Muscle/Effects/Laser Beam Additive"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.0, 0.05, 0.02, 1.0)
        _Intensity ("Emission Intensity", Range(0, 20)) = 4.0
        _Opacity ("Opacity", Range(0, 1)) = 0.8
        _CoreSharpness ("Core Sharpness", Range(0.25, 8)) = 2.5
        _StartFade ("Start Fade", Range(0.001, 0.5)) = 0.03
        _EndFade ("End Fade", Range(0.001, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2.0
        _PulseScale ("Pulse Scale", Range(1, 40)) = 12.0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LaserBeam"
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
                half _CoreSharpness;
                half _StartFade;
                half _EndFade;
                half _PulseSpeed;
                half _PulseScale;
                half _PulseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half centerDistance = abs(input.uv.x * 2.0h - 1.0h);
                half core = pow(saturate(1.0h - centerDistance), _CoreSharpness);
                half startFade = smoothstep(0.0h, max(_StartFade, 0.001h), input.uv.y);
                half endFade =
                    1.0h - smoothstep(1.0h - max(_EndFade, 0.001h), 1.0h, input.uv.y);
                half pulseWave =
                    sin(input.uv.y * _PulseScale - _Time.y * _PulseSpeed) * 0.5h + 0.5h;
                half pulse = lerp(1.0h, pulseWave, _PulseStrength);
                half alpha = _Opacity * core * startFade * endFade * pulse;
                return half4(_Color.rgb * _Intensity, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
