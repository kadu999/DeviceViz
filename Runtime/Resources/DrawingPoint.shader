Shader "Custom/DrawingPoint"
{
    Properties
    {
        _LineWidth("Line Width", Float) = 8
        _DrawColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One // additive — 颜色叠加
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            struct PointData { float2 position; float alpha; };
            StructuredBuffer<PointData> _PointBuffer;
            float  _LineWidth;
            float2 _CanvasSize;
            float4 _DrawColor;

            struct v2f { float4 pos:SV_POSITION; float alpha:TEXCOORD0; float2 uv:TEXCOORD1; };

            v2f vert(uint vertexID:SV_VertexID)
            {
                int pi = vertexID / 4;
                int c  = vertexID % 4;
                PointData pt = _PointBuffer[pi];
                float h = _LineWidth * 0.5;
                float2 off;
                off.x = (c == 1 || c == 2) ? h : -h;
                off.y = (c == 2 || c == 3) ? h : -h;
                float2 pos  = pt.position + off;
                float2 uv01 = pos / _CanvasSize;
                float2 clip = uv01 * 2.0 - 1.0;
                clip.y *= -1.0;
                v2f o;
                o.pos   = float4(clip, 0, 1);
                o.alpha = pt.alpha;
                o.uv    = off / h;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = length(i.uv);
                float edge = 1.0 - d;
                clip(edge);
                return _DrawColor * (i.alpha * edge);
            }
            ENDCG
        }
    }
}
