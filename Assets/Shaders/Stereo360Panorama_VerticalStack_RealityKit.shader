Shader "Stereoscopic/Stereo360Panorama_VerticalStack_RealityKit"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        [Toggle] _LeftEyeOnTop("Left Eye On Top", Float) = 1
        [Toggle] _DebugMode("Debug Mode (Show UV)", Float) = 1
    }
    
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _LeftEyeOnTop;
            float _DebugMode;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.positionOS.xyz;
                return output;
            }
            
            float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / PI, 1.0 / PI);
                return float2(0.5, 1.0) - sphereCoords;
            }
            
            // Improved texture sampling function to handle edge seams
            float4 SamplePanoramaWithEdgeFix(float2 uv)
            {
                // Add a small epsilon to prevent precision issues at the seam
                float epsilon = 0.0001;
                
                // Check if we're near the UV edges on the x-axis (the seam)
                if (uv.x < epsilon) {
                    // Near the left edge - blend with the right edge
                    float4 color1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    float4 color2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(1.0 - epsilon, uv.y));
                    return lerp(color2, color1, uv.x / epsilon);
                }
                else if (uv.x > 1.0 - epsilon) {
                    // Near the right edge - blend with the left edge
                    float4 color1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    float4 color2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(epsilon, uv.y));
                    return lerp(color1, color2, (uv.x - (1.0 - epsilon)) / epsilon);
                }
                else {
                    // Not near edges, regular sampling
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                }
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = ToRadialCoords(input.texcoord);
                
                // Get the current eye index (0 = left, 1 = right)
                uint eyeIndex = unity_StereoEyeIndex;
                
                // Map UV for vertical stack stereo
                float halfY = uv.y * 0.5;
                
                if (_LeftEyeOnTop > 0.5) {
                    // Left eye on top, right eye on bottom
                    uv.y = eyeIndex == 0 ? halfY + 0.5 : halfY;
                } else {
                    // Left eye on bottom, right eye on top
                    uv.y = eyeIndex == 0 ? halfY : halfY + 0.5;
                }
                
                // Debug visualization
                if (_DebugMode > 0.5) {
                    // Create grid
                    float2 grid = abs(frac(uv * 10) - 0.5);
                    float gridLines = step(0.48, max(grid.x, grid.y));
                    
                    // Create a color that shows clear eye differentiation
                    float3 eyeColor = eyeIndex == 0 ? 
                        float3(0.8, 0.2, 0.2) :  // Left eye: reddish
                        float3(0.2, 0.2, 0.8);   // Right eye: bluish
                        
                    float3 debugColor = float3(uv.x, uv.y, 0.5) + eyeColor * 0.5;
                    float4 texColor = SamplePanoramaWithEdgeFix(uv);
                    
                    return float4(
                        lerp(texColor.rgb, 
                            debugColor + float3(gridLines, gridLines, gridLines) * 0.3,
                            0.7),
                        1.0);
                }
                
                // Sample the texture with edge fix
                return SamplePanoramaWithEdgeFix(uv);
            }
            ENDHLSL
        }
    }
    
    // Fallback to ensure compatibility if needed
    FallBack "Universal Render Pipeline/Unlit"
    
    CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
} 