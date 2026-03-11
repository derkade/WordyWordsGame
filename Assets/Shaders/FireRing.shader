Shader "UI/FireRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _NoiseSpeed ("Noise Speed", Float) = 3.0
        _FlameHeight ("Flame Height", Range(0, 0.3)) = 0.12
        _RingRadius ("Ring Center Radius", Range(0.1, 0.5)) = 0.40
        _RingWidth ("Ring Base Width", Range(0.01, 0.15)) = 0.05

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
        Blend SrcAlpha One
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
            float _NoiseSpeed;
            float _FlameHeight;
            float _RingRadius;
            float _RingWidth;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i), hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x),
                    f.y);
            }

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
                float angle = atan2(c.y, c.x);

                // Seamless noise on circular path (no seam at ±π)
                float ca = cos(angle);
                float sa = sin(angle);
                float t = _Time.y;

                // Four noise octaves — large swooshes + medium detail + fine flicker + micro turbulence
                float n1 = vnoise(float2(ca * 2.5 + t * _NoiseSpeed * 0.4, sa * 2.5 + t * 0.5));
                float n2 = vnoise(float2(ca * 5.0 - t * _NoiseSpeed * 0.6, sa * 5.0 + t * 0.7 + 5.0));
                float n3 = vnoise(float2(ca * 10.0 + t * _NoiseSpeed * 1.1, sa * 10.0 - t * 0.3 + 10.0));
                float n4 = vnoise(float2(ca * 20.0 + t * _NoiseSpeed * 1.8, sa * 20.0 + t * 1.2 + 20.0));
                float noise = n1 * 0.35 + n2 * 0.3 + n3 * 0.2 + n4 * 0.15;

                // Flame displacement — tongues extend outward from ring
                float flameDisp = noise * _FlameHeight;

                float halfW = _RingWidth * 0.5;
                float innerEdge = _RingRadius - halfW;
                float outerEdge = _RingRadius + halfW + flameDisp;

                // Hard inner edge (crisp boundary against wheel), soft outer (flame tips fade)
                float innerFade = saturate((dist - innerEdge) * 50.0);
                float outerSoftness = max(flameDisp * 0.7, 0.005);
                float outerFade = saturate((outerEdge - dist) / outerSoftness);
                float ringAlpha = innerFade * outerFade;

                // Boost alpha in the core region for solidity
                float coreDist = abs(dist - _RingRadius) / max(halfW, 0.001);
                float coreBoost = saturate(1.0 - coreDist);
                ringAlpha = max(ringAlpha, coreBoost * innerFade);

                // Fire color gradient derived from combo color
                // Inner (core): bright, tending toward white
                // Mid: the combo color as-is
                // Outer (tips): darkened
                float flameFrac = saturate((dist - innerEdge) / max(outerEdge - innerEdge, 0.001));
                fixed3 brightCore = lerp(i.color.rgb, fixed3(1, 1, 1), 0.65);
                fixed3 midColor = i.color.rgb;
                fixed3 darkTip = i.color.rgb * 1.8; // saturated hot glow

                fixed3 fireColor = lerp(brightCore, midColor, saturate(flameFrac * 2.0));
                fireColor = lerp(fireColor, darkTip, saturate(flameFrac * 2.0 - 0.8));

                fixed4 col;
                col.rgb = fireColor;
                col.a = ringAlpha * i.color.a;

                // Radial fill from top, clockwise — with noisy edge for organic burn look
                float fillAngle = frac(atan2(c.x, c.y) / 6.28318530);
                float fillNoise = (vnoise(float2(fillAngle * 8.0, t * 2.0)) - 0.5) * 0.04;
                col.a *= saturate((_FillAmount + fillNoise - fillAngle) * 40.0);

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
