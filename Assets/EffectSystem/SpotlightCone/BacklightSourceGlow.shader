Shader "EffectSystem/BacklightSourceGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (1, 0.58, 0.2, 1)
        _Intensity ("Intensity", Range(0, 8)) = 3
        _Opacity ("Opacity", Range(0, 1)) = 0.74
        _CoreWhiteness ("Core Whiteness", Range(0, 1)) = 0.96
        _CoreIntensityMultiplier ("Core Intensity Multiplier", Range(1, 8)) = 5
        _RingIntensity ("Ring Intensity", Range(0, 2)) = 0.65
        _RingRadius ("Ring Radius", Range(0.05, 0.9)) = 0.42
        _RingWidth ("Ring Width", Range(0.01, 0.4)) = 0.12
        _Shape ("Shape", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
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
            Name "Backlight Source Glow"
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

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
                half _Opacity;
                half _CoreWhiteness;
                half _CoreIntensityMultiplier;
                half _RingIntensity;
                half _RingRadius;
                half _RingWidth;
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
                float2 centeredUv = input.uv * 2.0 - 1.0;
                float circularRadius = length(centeredUv);
                float rectangularRadius = max(abs(centeredUv.x), abs(centeredUv.y));
                float radius = lerp(circularRadius, rectangularRadius, saturate(_Shape));
                float radiusSquared = radius * radius;

                //外・中・中心を別々の帯で作らず、同じ距離から連続した指数減衰を作ります。
                //これにより複数のsmoothstep境界が重なって見えていた三段階の切れ目をなくします。
                half continuousHalo = exp2(-radiusSquared * 2.7h);
                half hotCore = exp2(-radiusSquared * 34.0h);
                half whiteCore = exp2(-radiusSquared * 13.0h);
                // Quadの外周より十分内側から透明化し、四角い描画境界を見せません。
                // 二乗カーブにすることで中心の明るさを保ちながら外側だけを柔らかく落とします。
                half quadEdgeFade = 1.0h - smoothstep(0.38h, 0.98h, radius);
                quadEdgeFade *= quadEdgeFade;

                //周期の異なる角度Patternを重ね、均一過ぎない放射状の光条を作ります。
                half ringDistance = (radius - _RingRadius) / max(_RingWidth, 0.01h);
                half haloRing = exp2(-ringDistance * ringDistance * 3.0h)
                    * _RingIntensity;

                half glowAlpha = saturate(
                    continuousHalo * 0.76h
                    + haloRing * 0.55h
                    + hotCore * 0.44h)
                    * quadEdgeFade;
                half3 coreColor = half3(1.0h, 0.97h, 0.9h);
                half3 glowColor = lerp(
                    _GlowColor.rgb,
                    coreColor,
                    whiteCore * _CoreWhiteness);
                half coreIntensity = lerp(
                    1.0h,
                    _CoreIntensityMultiplier,
                    whiteCore);
                half haloIntensity = 0.28h
                    + continuousHalo * 0.72h
                    + haloRing * 0.8h;
                glowColor *= _Intensity * coreIntensity * haloIntensity;
                return half4(glowColor, glowAlpha * _Opacity * _GlowColor.a);
            }
            ENDHLSL
        }
    }
}
