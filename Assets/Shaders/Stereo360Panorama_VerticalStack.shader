Shader "Stereoscopic/Stereo360Panorama_VerticalStack"
{
	Properties{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
		[Toggle] _LeftEyeOnTop("Left Eye On Top", Float) = 1
		[Toggle] _RespectObjectRotation("Respect Object Rotation", Float) = 1
		[Toggle] _DebugMode("Debug Mode (Show UV)", Float) = 1
		[Toggle] _ShowEyeOverlay("Show Eye Color Overlay", Float) = 0
		[Range(0,1)] _Opacity("Opacity", Range(0,1)) = 1
	}
	SubShader{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "PreviewType" = "Skybox" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off ZWrite Off
		
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma target 3.0
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			float _LeftEyeOnTop;
			float _RespectObjectRotation;
			float _DebugMode;
			float _ShowEyeOverlay;
			float _Opacity;
			
			struct appdata {
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float3 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.vertex.xyz;
				return o;
			}
			
			inline float2 ToRadialCoords(float3 coords)
			{
				float3 normalizedCoords = normalize(coords);
				float latitude = acos(normalizedCoords.y);
				float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
				float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
				return float2(0.5, 1.0) - sphereCoords;
			}
			
			// Robust texture sampling function to handle UV wrapping with precision
			fixed4 SamplePanoramaWithEdgeFix(sampler2D tex, float2 uv)
			{
				// Handle precision issues at the seam boundary
				// Add a tiny offset to avoid exact boundary conditions
				float epsilon = 0.00001;
				
				// Ensure UV coordinates are in valid range with precision handling
				uv.x = frac(uv.x + epsilon) - epsilon;
				uv.x = clamp(uv.x, 0.0, 0.999999); // Prevent exact 1.0 to avoid seam
				uv.y = saturate(uv.y);
				
				// Use filtered sampling to reduce precision artifacts
				return tex2Dlod(tex, float4(uv, 0, 0));
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				float2 uv = ToRadialCoords(i.texcoord);
				
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
					fixed4 texColor = SamplePanoramaWithEdgeFix(_MainTex, uv);
					
					fixed4 col = fixed4(
						lerp(texColor.rgb, 
							debugColor + float3(gridLines, gridLines, gridLines) * 0.3,
							0.7),
						1.0);
					col.a *= _Opacity;
					return col;
				}
				
				// Sample the texture with edge fix
				fixed4 col = SamplePanoramaWithEdgeFix(_MainTex, uv);
				
				// Eye overlay debug mode
				if (_ShowEyeOverlay > 0.5) {
					// Apply colored overlay to differentiate eyes
					float3 eyeOverlay = eyeIndex == 0 ? 
						float3(0.3, 0.0, 0.0) :  // Left eye: red tint
						float3(0.0, 0.0, 0.3);   // Right eye: blue tint
					
					col.rgb += eyeOverlay;
				}
				
				col.a *= _Opacity;
				return col;
			}
			ENDCG
		}
	}
}