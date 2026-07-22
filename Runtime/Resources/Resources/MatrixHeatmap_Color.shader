Shader "Hidden/MatrixHeatmap_Color"
{
    Properties
    {
        _ColorRamp ("Color Ramp", 2D) = "white" {}
        _DataTex   ("Data Texture", 2D) = "black" {}
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
            sampler2D _ColorRamp;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float raw = tex2D(_DataTex, i.uv).r;
                return fixed4(tex2D(_ColorRamp, float2(raw, 0.5)).rgb, 1);
            }
            ENDCG
        }
    }
}
