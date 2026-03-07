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
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0)
        _ShadowOffset ("Shadow Offset (px)", Vector) = (0,0,0,0)
        _ShadowBlur ("Shadow Blur (px)", Float) = 0
        _ShadowExpand ("Shadow Expand (px)", Float) = 0
        _BevelSize ("Bevel Size (px)", Float) = 0
        _BevelStrength ("Bevel Strength", Float) = 0
        _GlossStrength ("Gloss Strength", Range(0, 1)) = 0
        _GlossSize ("Gloss Size (how far down)", Range(0, 1)) = 0.5
        _GlossCurve ("Gloss Curve (inset amount)", Range(0, 2)) = 0.3

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
            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowBlur;
            float _ShadowExpand;
            float _BevelSize;
            float _BevelStrength;
            float _GlossStrength;
            float _GlossSize;
            float _GlossCurve;

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

                // Compute SDF rounded rect (inset by shadow expand)
                float2 halfSize = _RectSize.xy * 0.5;
                float expand = max(_ShadowExpand, 0.0);
                float2 shapeHalf = halfSize - expand;
                float radius = min(_Radius, min(shapeHalf.x, shapeHalf.y));
                float2 p = (i.uv - 0.5) * _RectSize.xy;
                float dist = sdRoundedBox(p, shapeHalf, radius);

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

                // Inner bevel (raised pillow effect)
                float bevelSz = max(_BevelSize, 0.1);
                float bevelDepth = max(-dist, 0.0);
                float bevelMask = 1.0 - saturate(bevelDepth / bevelSz);
                bevelMask = smoothstep(0.0, 1.0, bevelMask); // soft S-curve
                // Light from top with slight left bias for 3D feel
                float maxHalf = max(shapeHalf.x, shapeHalf.y);
                float lightDir = (p.y + p.x * 0.25) / max(maxHalf, 1.0);
                // Asymmetric: strong darken on bottom/right, mild brighten on top/left
                float bevelHighlight = max(lightDir, 0.0) * 0.35;
                float bevelShadow = min(lightDir, 0.0);
                float bevel = bevelMask * _BevelStrength * (bevelHighlight + bevelShadow);

                // Glossy highlight — curved inset on upper half
                float glossY = p.y / max(shapeHalf.y, 1.0);  // -1 bottom, +1 top
                float glossX = p.x / max(shapeHalf.x, 1.0);  // -1 left, +1 right
                // Curved bottom edge: parabola cuts across top half
                float glossCutoff = glossY - _GlossCurve * glossX * glossX;
                float glossFade = smoothstep(0.0, _GlossSize, glossCutoff);
                // Soften near shape edges using SDF distance
                float glossEdge = saturate(-dist / max(edgeSoftness * 3.0, 1.0));
                float gloss = glossFade * glossEdge * _GlossStrength;

                // Composite fill over border
                fixed4 fillCol = color;
                fillCol.rgb = saturate(fillCol.rgb + bevel + gloss);
                fillCol.a *= innerAlpha;

                fixed4 borderCol = _BorderColor;
                borderCol.a *= (outerAlpha - innerAlpha);

                float shapeAlpha = fillCol.a + borderCol.a * (1.0 - fillCol.a);
                float3 shapeRGB = (shapeAlpha > 0.001)
                    ? (fillCol.rgb * fillCol.a + borderCol.rgb * borderCol.a * (1.0 - fillCol.a)) / shapeAlpha
                    : float3(0, 0, 0);

                // Blurred drop shadow
                float2 shadowP = p - _ShadowOffset.xy;
                float shadowDist = sdRoundedBox(shadowP, shapeHalf, radius);
                float shadowSoft = max(_ShadowBlur, 0.5);
                float shadowA = (1.0 - smoothstep(-1.0, shadowSoft, shadowDist)) * _ShadowColor.a;

                // Composite: shape over shadow
                float finalAlpha = shapeAlpha + shadowA * (1.0 - shapeAlpha);
                float3 finalRGB = (finalAlpha > 0.001)
                    ? (shapeRGB * shapeAlpha + _ShadowColor.rgb * shadowA * (1.0 - shapeAlpha)) / finalAlpha
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
