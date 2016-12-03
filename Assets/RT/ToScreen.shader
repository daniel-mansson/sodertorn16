Shader "Unlit/ToScreen"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
			float4 res = float4(0, 0, 0, 0);
			if (col.r < 0.2)
				res = float4(0, 0, 0, 0);
			else if (col.r < 0.4)
				res = float4(1, 1, 1, 1);
			else if (col.r < 0.6)
				res = float4(0, 0, 0, 0);
			else if (col.r < 0.8)
				res = float4(1, 1, 1, 1);

			if (col.b > 0.7)
				res.b = 0.5;
			else if (col.b > 0.5)
				res.b = 1;

			if (col.g > 0.7)
				res.g = 0.5;
			else if (col.g > 0.5)
				res.g = 1;

			return res;
			}
			ENDCG
		}
	}
}
