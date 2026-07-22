Shader "Hidden/MatrixHeatmap_Color"
{
    Properties
    {
        _DataTex ("Data Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f    { float2 uv:TEXCOORD0; float4 vertex:SV_POSITION; };

            sampler2D _DataTex;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float raw = tex2D(_DataTex, i.uv).r;
                float v = 1.0 - raw;
                return fixed4(v, v, v, 1);
            }
            ENDCG
        }
    }
}
