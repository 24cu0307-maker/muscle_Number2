/*━━━━━━━━━*
*@file SpotlightConeAdditive.shader*
*@brief 重なりによる白飛びを抑えた透過合成でスポットライトを描画する*
*@author 24CU0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Universal Render Pipeline用。複数Coneの重なりを無制限に加算しない*
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
            //通常の加算合成では、光線が重なるたびに色が足し算されて端だけ白飛びします。
            //背景色を透明度に応じて減衰させることで、単体の見え方を保ちながら重複光量を抑えます。
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            //表裏を同時描画すると同じCone自身が二重に合成されるため、Camera側の表面だけを描画します。
            Cull Back

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
                //端を強調するFresnelではなく、Cameraへ向いた面をわずかに濃くして立体感を作ります。
                //輪郭側は暗くなる方向だけに補正するため、以前のような端の白飛びは発生しません。
                half3 normal = normalize(input.normalWS)
                    * (isFrontFace ? 1.0h : -1.0h);
                half facing = abs(dot(normal, normalize(input.viewDirWS)));
                half volumeShading = lerp(0.35h, 1.0h, facing);

                //実Spot Lightを受けない軽量な透明Shader内で、照明器具の反射光だけを再現します。
                //固定方向との内積一回で円周方向に明暗を作り、Lit Shaderの追加Light計算を避けます。
                half3 reflectorDirectionWS = normalize(
                    TransformObjectToWorldDir(half3(-0.35h, 0.55h, -1.0h)));
                half reflectorFacing = saturate(
                    dot(normal, reflectorDirectionWS) * 0.5h + 0.5h);
                half reflectorShading = lerp(0.5h, 1.0h, reflectorFacing);

                //始点と終点は従来どおりFadeさせ、光が急に出現・消失して見えるのを防ぎます。
                half startFade = smoothstep(
                    0.0h,
                    max(_StartFade, 0.001h),
                    input.uv.y);
                half endFade = 1.0h - smoothstep(
                    1.0h - max(_EndFade, 0.001h),
                    1.0h,
                    input.uv.y);
                //光源から離れるほど35%まで暗くする単純な一次補間です。
                //Texture参照や追加Light計算を使わないため、Coneが多くても負荷を抑えられます。
                half distanceAttenuation = lerp(
                    1.0h,
                    0.35h,
                    saturate(input.uv.y));
                half alpha = _Opacity
                    * startFade
                    * endFade
                    * volumeShading
                    * reflectorShading
                    * distanceAttenuation;

                //HDR値を単純に乗算すると、強い部分のRGBがすべて飽和して白く見えます。
                //指数圧縮で明るさを1未満へ収め、Color本来の各成分比率を維持します。
                half maximumColorChannel = max(
                    max(_Color.r, _Color.g),
                    max(_Color.b, 0.001h));
                half3 huePreservedColor = _Color.rgb / maximumColorChannel;
                half compressedBrightness = 1.0h
                    - exp(-max(_Intensity, 0.0h) * 0.45h);
                half3 finalColor = huePreservedColor * compressedBrightness;
                return half4(finalColor, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
