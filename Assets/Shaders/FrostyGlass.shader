Shader "Custom/URP/FrostedGlassModern"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,0.4)
        _BlurStrength ("Blur Strength", Range(0, 2)) = 1.0
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.5
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _Darken ("Background Darken", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "FrostedGlass"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                float2 screenUV : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _BlurStrength;
                float _FresnelStrength;
                float4 _FresnelColor;
                float _Darken;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(IN.positionOS.xyz));
                float4 screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.screenUV = screenPos.xy / screenPos.w;
                return OUT;
            }

            // Multi-tap blur (hexagonal pattern)
            float3 SampleBlur(float2 uv, float blur)
            {
                float2 texel = _ScreenParams.zw; // 1/width, 1/height
                float2 offsets[7] = {
                    float2(0, 0),
                    float2(1, 0), float2(-1, 0),
                    float2(0, 1), float2(0, -1),
                    float2(0.707, 0.707), float2(-0.707, -0.707)
                };
                float3 col = 0;
                float total = 0;
                for (int i = 0; i < 7; i++)
                {
                    float2 o = offsets[i] * blur * 2.5;
                    col += SampleSceneColor(uv + o * texel);
                    total += 1;
                }
                return col / total;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // Blur background
                float3 blurred = SampleBlur(IN.screenUV, _BlurStrength);

                // Tint and darken
                blurred = lerp(blurred, blurred * _Color.rgb, _Color.a);
                blurred = lerp(blurred, float3(0,0,0), _Darken);

                // Fresnel highlight
                float fresnel = pow(1.0 - saturate(dot(N, V)), 2.0) * _FresnelStrength;
                float3 finalCol = blurred + _FresnelColor.rgb * fresnel;

                // Final alpha: use tint alpha
                return float4(finalCol, _Color.a);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
} 