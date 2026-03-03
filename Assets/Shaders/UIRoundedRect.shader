Shader "UI/RoundedRect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Radius ("Corner Radius (px)", Float) = 8
        _Softness ("Edge Softness (px)", Float) = 1
        _RectSize ("Rect Size (px)", Vector) = (100, 100, 0, 0)
        _BorderWidth ("Border Width (px)", Float) = 0
        _BorderColor ("Border Color", Color) = (0,0,0,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            float _Radius;
            float _Softness;
            float4 _RectSize;
            float _BorderWidth;
            fixed4 _BorderColor;

            // SDF for a rounded rectangle centered at origin
            // p = point, b = half-size, r = corner radius
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.vertex = vPosition;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                o.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample base texture (for compatibility)
                half4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                // Compute SDF rounded rect
                float2 halfSize = _RectSize.xy * 0.5;
                float radius = min(_Radius, min(halfSize.x, halfSize.y));
                float2 p = (i.uv - 0.5) * _RectSize.xy;
                float dist = sdRoundedBox(p, halfSize, radius);

                // Anti-aliased edge using screen-space derivatives
                float fw = fwidth(dist);
                float edgeSoftness = max(_Softness, fw);

                // Outer edge alpha (full shape boundary)
                float outerAlpha = 1.0 - smoothstep(-edgeSoftness, edgeSoftness, dist);

                // Inner edge alpha (inset by border width)
                float borderW = max(_BorderWidth, 0.0);
                float innerAlpha = (borderW > 0.001)
                    ? 1.0 - smoothstep(-borderW - edgeSoftness, -borderW + edgeSoftness, dist)
                    : outerAlpha;

                // Composite fill over border
                fixed4 fillCol = color;
                fillCol.a *= innerAlpha;

                fixed4 borderCol = _BorderColor;
                borderCol.a *= (outerAlpha - innerAlpha);

                float finalAlpha = fillCol.a + borderCol.a * (1.0 - fillCol.a);
                float3 finalRGB = (finalAlpha > 0.001)
                    ? (fillCol.rgb * fillCol.a + borderCol.rgb * borderCol.a * (1.0 - fillCol.a)) / finalAlpha
                    : float3(0, 0, 0);

                color = fixed4(finalRGB, finalAlpha);

                // UI clipping
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= m.x * m.y;

                // Premultiply alpha
                color.rgb *= color.a;

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
