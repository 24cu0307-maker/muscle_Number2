Shader "Muscle/Effects/Penlight Glow Sphere"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (0.2, 0.9, 1.0, 1.0)
        _Intensity ("Glow Intensity", Range(0, 20)) = 1.5
        _Opacity ("Glow Opacity", Range(0, 1)) = 0.3
        _FresnelPower ("Outer Fade", Range(0.25, 8)) = 1.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+25"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PenlightGlowSphere"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Opacity;
                half _FresnelPower;
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
                // LaserBeamMeshと同じUVを使い、中心から横方向の外周へFadeします。
                // 始点と終点も透明にすることで、太い板の輪郭を見せません。
                half centerDistance = abs(input.uv.x * 2.0h - 1.0h);
                half sideFade = pow(
                    saturate(1.0h - centerDistance),
                    max(0.25h, _FresnelPower));
                half startFade = smoothstep(0.0h, 0.12h, input.uv.y);
                half endFade = 1.0h - smoothstep(0.72h, 1.0h, input.uv.y);
                half glow = sideFade * startFade * endFade;
                return half4(
                    _Color.rgb * _Intensity * glow,
                    _Color.a * _Opacity * glow);
            }
            ENDHLSL
        }
    }
}
