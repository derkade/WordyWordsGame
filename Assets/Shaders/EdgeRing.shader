Shader "UI/EdgeRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _RingRadius ("Ring Center Radius", Range(0.1, 0.5)) = 0.45
        _RingWidth ("Ring Width", Range(0.001, 0.1)) = 0.02
        _EdgeSoftness ("Fill Edge Softness", Range(0.001, 0.1)) = 0.02

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;
            float _FillAmount;
            float _RingRadius;
            float _RingWidth;
            float _EdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float dist = length(c);

                // Ring shape with antialiased edges
                float halfW = _RingWidth * 0.5;
                float innerEdge = _RingRadius - halfW;
                float outerEdge = _RingRadius + halfW;
                float ringAA = 0.003; // 1-pixel antialias
                float ringAlpha = smoothstep(innerEdge - ringAA, innerEdge + ringAA, dist)
                                * smoothstep(outerEdge + ringAA, outerEdge - ringAA, dist);

                // Radial fill from top, counter-clockwise (matching Image fillClockwise=false)
                // atan2(x, y) gives 0 at top, increasing CW. Negate for CCW.
                float fillAngle = frac(-atan2(c.x, c.y) / 6.28318530);

                // Soft edges at both fill start (stationary, 12 o'clock) and fill end (moving)
                float startFade = smoothstep(0.0, _EdgeSoftness, fillAngle);
                float endFade = smoothstep(_FillAmount + _EdgeSoftness, _FillAmount, fillAngle);
                float fillAlpha = startFade * endFade;

                fixed4 col;
                col.rgb = i.color.rgb;
                col.a = ringAlpha * fillAlpha * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.worldPosition.xy * 2 - _ClipRect.xy - _ClipRect.zw)) * 200.0);
                col.a *= m.x * m.y;
                #endif

                return col;
            }
            ENDCG
        }
    }
}
