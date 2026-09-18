Shader "MuscleBeat/RealtimeCameraSurface"
{
    Properties
    {
        _MainTex ("Camera Texture", 2D) = "black" {}
        _FlipX ("Horizontal Mirror", Float) = 0
        _FlipY ("Vertical Mirror", Float) = 0
        _RotationRadians ("UV Rotation", Float) = 0
        _ViewRect ("Visible Area", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _FlipX;
                float _FlipY;
                float _RotationRadians;
                float4 _ViewRect;
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
                float2 uv = input.uv;
                uv.x = lerp(uv.x, 1.0 - uv.x, _FlipX);
                uv = uv * _ViewRect.xy + _ViewRect.zw;
                float sine;
                float cosine;
                sincos(_RotationRadians, sine, cosine);
                uv -= 0.5;
                uv = float2(cosine * uv.x - sine * uv.y, sine * uv.x + cosine * uv.y) + 0.5;
                uv.y = lerp(uv.y, 1.0 - uv.y, _FlipY);
                return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
