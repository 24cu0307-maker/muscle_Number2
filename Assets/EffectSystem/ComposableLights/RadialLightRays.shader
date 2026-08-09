Shader "EffectSystem/RadialLightRays"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.58, 0.2, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.45
        _RayCount ("Ray Count", Range(2, 24)) = 9
        _RaySharpness ("Ray Sharpness", Range(1, 20)) = 4
        _InnerFade ("Inner Fade", Range(0, 0.8)) = 0.08
        _OuterFadeStart ("Outer Fade Start", Range(0.1, 1)) = 0.5
        _CenterBrightness ("Center Brightness", Range(0, 4)) = 1.5
        _CenterFalloff ("Center Falloff", Range(0.25, 8)) = 2
        _Shape ("Shape", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
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
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half _Intensity; half _Opacity; half _RayCount;
                half _RaySharpness; half _InnerFade; half _OuterFadeStart;
                half _CenterBrightness; half _CenterFalloff;
                half _Shape;
                half _ZTest;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float circularRadius = length(p);
                float rectangularRadius = max(abs(p.x), abs(p.y));
                float radius = lerp(circularRadius, rectangularRadius, saturate(_Shape));
                float angle = atan2(p.y, p.x);
                half primary = pow(saturate(0.5h + 0.5h * cos(angle * _RayCount)), _RaySharpness);
                half secondary = pow(saturate(0.5h + 0.5h * cos(angle * (_RayCount + 3.0h) + 1.7h)), _RaySharpness * 1.3h);
                half innerFade = smoothstep(_InnerFade, _InnerFade + 0.12h, radius);
                half outerFade = 1.0h - smoothstep(_OuterFadeStart, 1.0h, radius);
                outerFade *= outerFade;
                half centerBoost = 1.0h + _CenterBrightness
                    * pow(saturate(1.0h - radius), _CenterFalloff);
                half rays = (primary * 0.65h + secondary * 0.35h)
                    * innerFade
                    * outerFade
                    * centerBoost;
                half rayAlpha = saturate(rays * _Opacity * _Color.a);
                half3 rayColor = _Color.rgb * _Intensity * centerBoost;
                return half4(rayColor, rayAlpha);
            }
            ENDHLSL
        }
    }
}
