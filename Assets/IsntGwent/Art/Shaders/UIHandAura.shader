Shader "IsntGwent/UI/HandAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Halftone Tint", Color) = (0.85, 0.66, 0.34, 0.55)
        _LineColor ("Ink Line", Color) = (0.94, 0.88, 0.72, 0.8)
        _Width ("Aura Width", Float) = 46
        _Corner ("Corner Radius", Float) = 8
        _DotCell ("Halftone Cell", Float) = 7.5
        _LineOffset ("Line Offset", Float) = 7
        _LineWidth ("Line Width", Float) = 2.4
        _Wobble ("Wobble", Float) = 3.4
        _DashPhase ("Dash Phase", Float) = 0
        _DashCount ("Dash Count", Float) = 16
        _Under ("Underlight", Float) = 0.7
        _Cutout ("Cut Inside", Float) = 0
        _Pulse ("Pulse", Float) = 0
        _HalfSize ("Half Size", Vector) = (1, 1, 0, 0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            #define AURA_CAPACITY 16

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 local : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _LineColor;
            float _Width;
            float _Corner;
            float _DotCell;
            float _LineOffset;
            float _LineWidth;
            float _Wobble;
            float _DashPhase;
            float _DashCount;
            float _Under;
            float _Cutout;
            float _Pulse;
            float4 _HalfSize;

            float4 _AuraRects[AURA_CAPACITY];
            float4 _AuraRot[AURA_CAPACITY];
            int _AuraCount;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.local = v.texcoord;
                o.color = v.color;

                return o;
            }

            float SilhouetteDistance(float2 p)
            {
                float d = 100000.0;

                for (int k = 0; k < AURA_CAPACITY; k++)
                {
                    if (k >= _AuraCount) break;

                    float4 r = _AuraRects[k];
                    float4 q = _AuraRot[k];

                    float2 lp = p - r.xy;
                    float2 rp = float2(lp.x * q.x + lp.y * q.y, lp.y * q.x - lp.x * q.y);
                    float2 e = abs(rp) - max(r.zw - _Corner, 0.0);
                    float sd = length(max(e, 0.0)) + min(max(e.x, e.y), 0.0) - _Corner;

                    d = min(d, sd);
                }

                return d;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.local;

                float wobble = sin(p.x * 0.052 + p.y * 0.023) + sin(p.y * 0.041 - p.x * 0.017 + 2.1);
                float d = SilhouetteDistance(p) + wobble * _Wobble;

                float width = max(_Width * (1.0 + _Pulse), 1.0);
                float body = 1.0 - saturate(d / width);
                body *= body;
                body *= lerp(1.0, smoothstep(0.0, 2.0, d), _Cutout);

                float2 grid = p / max(_DotCell, 1.0);
                float2 cell = floor(grid);
                float2 f = frac(grid) - 0.5;
                float jitter = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                float radius = body * 0.70 * (0.75 + jitter * 0.5);
                float dots = 1.0 - smoothstep(radius - 0.10, radius + 0.10, length(f));

                float edge = abs(d - _LineOffset);
                float ink = 1.0 - smoothstep(_LineWidth * 0.5, _LineWidth * 0.5 + 1.2, edge);
                float2 nrm = float2(p.x / max(_HalfSize.x, 1.0), p.y / max(_HalfSize.y, 1.0));
                float dash = 0.5 + 0.5 * sin((atan2(nrm.y, nrm.x) - _DashPhase) * _DashCount);
                ink *= lerp(0.12, 1.0, smoothstep(0.15, 0.75, dash));

                float under = saturate(-p.y / max(_HalfSize.y, 1.0));
                float halftone = dots * body * _Color.a * (1.0 + under * _Under);
                float inkLine = ink * _LineColor.a;

                float alpha = saturate(halftone + inkLine);
                float3 rgb = _Color.rgb * halftone + _LineColor.rgb * inkLine;

                return float4(rgb * i.color.a, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
