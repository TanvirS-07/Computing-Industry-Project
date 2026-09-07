Shader "Unlit/ARPhoneScreen"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		LOD 100

		// THE CODE FOR THE EFFECT
		// -----------------------
		Tags { "RenderType"="Transparent" "Queue"="Transparent" } // Transparent rendering

		// Blend transparent objects by just using the colour of the other objects
		//   (makes  this shader invisible as we are only using it for stencil writing below)
		Blend Zero One
		ZWrite Off // Don't write to the depth buffer
		
		// Always write "1" into the stencil buffer
		Stencil
		{
			Ref 1 // Reference value of 1 (Comp is Always, so it doesn't use this to compare, only to write it into the buffer)
			Comp Always // Stencil test always passes, i.e., don't bother comparing
			Pass Replace // Write the reference number into the stencil buffer
		}
		// -----------------------
		// THE CODE FOR THE EFFECT

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
