Shader "Custom/URP/FrostedGlass" {
    Properties {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Distortion ("Distortion Strength", Range(0,1)) = 0.1
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Pass {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Distortion;
                float _NoiseScale;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 screenPos   : TEXCOORD3;
            };

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float2 noiseUV   = IN.worldPos.xz * _NoiseScale + IN.uv * 10.0;
                float2 rand      = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).rg * 2 - 1) * _Distortion;
                float2 frostedUV = screenUV + rand;
                half4 bg         = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, frostedUV);

                float3 viewDir   = normalize(_WorldSpaceCameraPos.xyz - IN.worldPos);
                float fresnel    = pow(1 - saturate(dot(IN.normalWS, viewDir)), 5);
                half alpha       = _BaseColor.a;
                half3 blended    = lerp(bg.rgb, _BaseColor.rgb, alpha);
                half4 col        = half4(blended, alpha * (1 - fresnel));
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
} 