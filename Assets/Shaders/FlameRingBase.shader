Shader "UI/FlameRingBase"
{
    Properties
    {
        [PerRendererData] _MainTex ("Ring Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseSpeed ("Noise Speed", Float) = 1.5
        _NoiseIntensity ("Noise Intensity", Range(0, 2)) = 0.6

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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;
            float _NoiseSpeed;
            float _NoiseIntensity;

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
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Animated noise — two octaves on circular path (seamless tiling)
                float2 c = i.uv - 0.5;
                float rawAngle = atan2(c.y, c.x);
                float ca = cos(rawAngle);
                float sa = sin(rawAngle);
                float t = _Time.y;
                float n1 = vnoise(float2(ca * 1.5 + t * _NoiseSpeed, sa * 1.5 + t * 0.4));
                float n2 = vnoise(float2(ca * 3.0 - t * _NoiseSpeed * 0.7, sa * 3.0 + t * 0.6 + 5.0));
                float noise = (n1 + n2 * 0.5) / 1.5;

                // Noise shifts the edge threshold — flame tongues at boundary
                // Core always opaque, hard edge, no gradient
                float threshold = 0.3 - (noise - 0.5) * _NoiseIntensity;
                col.a = saturate((col.a - threshold) * 6.0);

                // UI clipping (only when RectMask2D is active)
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
