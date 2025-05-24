Shader "Stereoscopic/Stereo360Panorama_VerticalStack"
{
	Properties{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
		[Toggle] _LeftEyeOnTop("Left Eye On Top", Float) = 1
		[Toggle] _RespectObjectRotation("Respect Object Rotation", Float) = 1
		[Toggle] _DebugMode("Debug Mode (Show UV)", Float) = 1
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
			
			// Improved texture sampling function to handle edge seams
			fixed4 SamplePanoramaWithEdgeFix(sampler2D tex, float2 uv)
			{
				// Add a small epsilon to prevent precision issues at the seam
				float epsilon = 0.0001;
				
				// Check if we're near the UV edges on the x-axis (the seam)
				if (uv.x < epsilon) {
					// Near the left edge - blend with the right edge
					fixed4 color1 = tex2D(tex, uv);
					fixed4 color2 = tex2D(tex, float2(1.0 - epsilon, uv.y));
					return lerp(color2, color1, uv.x / epsilon);
				}
				else if (uv.x > 1.0 - epsilon) {
					// Near the right edge - blend with the left edge
					fixed4 color1 = tex2D(tex, uv);
					fixed4 color2 = tex2D(tex, float2(epsilon, uv.y));
					return lerp(color1, color2, (uv.x - (1.0 - epsilon)) / epsilon);
				}
				else {
					// Not near edges, regular sampling
					return tex2D(tex, uv);
				}
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
				col.a *= _Opacity;
				return col;
			}
			ENDCG
		}
	}
}