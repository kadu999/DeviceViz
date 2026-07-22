Shader "Hidden/MatrixHeatmap_Digits"
{
    Properties
    {
        _DigitAtlas ("Digit Atlas", 2D) = "white" {}
        _DataTex    ("Data Texture", 2D) = "black" {}
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

            sampler2D _MainTex;   // dummy — Graphics.Blit 会设这个
            sampler2D _DataTex;
            float4 _DataTex_TexelSize;
            float4 _TextColor;
            float _GridSizeX;
            float _GridSizeY;

            sampler2D _DigitAtlas;
            float4 _DigitAtlas_TexelSize;

            static const float ATLAS_W = 650.0;
            static const float DIGIT_W = 64.0;
            static const float GAP = 1.0;
            static const float SLOT_W = DIGIT_W + GAP;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 格子坐标
                float cx = i.uv.x * _GridSizeX;
                float cy = i.uv.y * _GridSizeY;
                int ix = (int)floor(cx);
                int iy = (int)floor(cy);
                float fx = cx - floor(cx);
                float fy = cy - floor(cy);

                // 读数据
                float2 duv = float2((ix + 0.5) / _GridSizeX, (iy + 0.5) / _GridSizeY);
                float raw = tex2D(_DataTex, duv).r;
                uint val = (uint)clamp((int)round(raw * 999.0), 0, 999);

                // 拆位
                uint d2 = val / 100;
                uint d1 = (val / 10) % 10;
                uint d0 = val % 10;

                // 居中
                int num = (int)(val >= 100 ? 3 : (val >= 10 ? 2 : 1));
                float slot = 0.25;
                float start = (1.0 - num * slot) / 2.0;

                float p = fx - start;
                int col = (int)(p / slot);

                bool show = (p >= 0.0 && col >= 0 && col < num);

                uint digit = 0;
                if (show) {
                    if (num == 3)      digit = col == 0 ? d2 : (col == 1 ? d1 : d0);
                    else if (num == 2) digit = col == 0 ? d1 : d0;
                    else               digit = d0;
                }

                float alpha = 0;
                if (show) {
                    float sx = clamp((p - col * slot) / slot, 0, 1);
                    float px = digit * SLOT_W + sx * DIGIT_W;
                    float a = tex2D(_DigitAtlas, float2(px / ATLAS_W, fy)).a;
                    alpha = a > 0.5;
                }

                return fixed4(_TextColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
