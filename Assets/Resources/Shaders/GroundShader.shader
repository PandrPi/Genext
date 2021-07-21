// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/GroundShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
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
            float4 _Tint;
            float _CameraSize;
            static const float logOfTwo = log(2);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Gets the xy position of the vertex in world space.
                float2 worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                // Use the world space coords instead of the mesh's UVs.
                o.uv = TRANSFORM_TEX(worldXY, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float cameraSize = max(_CameraSize, 1.0);
                int currentPower = (int)(log(cameraSize) / logOfTwo);
                
                float prevTexScale = pow(2, currentPower);
                float nextTexScale = pow(2, currentPower + 1);

                float lerpFactor = (_CameraSize - prevTexScale) / (nextTexScale - prevTexScale);
                
                fixed4 prevTexLod = tex2D(_MainTex, i.uv / prevTexScale);
                fixed4 nextTexLod = tex2D(_MainTex, i.uv / nextTexScale);
                fixed4 col = lerp(prevTexLod, nextTexLod, lerpFactor) * _Tint;
                return col;
            }
            ENDCG
        }
    }
}
