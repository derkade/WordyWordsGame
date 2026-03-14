Shader "UI/xBRFilter"
{
    // Posterize + Scale3x + smooth subpixel blending:
    // 1. Posterize source into flat color bands (creates pixel-art-like content)
    // 2. Scale3x rules determine which neighbor fills each 3x3 subpixel
    // 3. Smooth interpolation at subpixel boundaries creates flowing curves
    // Result: flat color regions with smooth diagonal edges — vector art look.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Strength ("Filter Strength", Range(0, 1)) = 1.0
        _PosterLevels ("Color Levels", Range(2, 32)) = 10
        _EdgeWidth ("Smooth Width", Range(0, 1)) = 0.5

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Scale3xSmooth"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Strength;
            float _PosterLevels;
            float _EdgeWidth;

            #ifdef UNITY_UI_CLIP_RECT
            float4 _ClipRect;
            #endif

            float3 poster(float3 c, float levels)
            {
                return floor(c * levels + 0.5) / levels;
            }

            bool ceq(float3 a, float3 b)
            {
                return all(abs(a - b) < 0.01);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float2 texSize = _MainTex_TexelSize.zw;
                float2 uv = i.texcoord;
                float levels = _PosterLevels;

                // Find source texel and sub-texel position
                float2 srcPos = uv * texSize - 0.5;
                float2 tc = (floor(srcPos) + 0.5) * texel;
                float2 f = frac(srcPos);

                // Read and posterize 3x3 neighborhood (9 texture reads)
                float3 A = poster(tex2D(_MainTex, tc + float2(-1,-1) * texel).rgb, levels);
                float3 B = poster(tex2D(_MainTex, tc + float2( 0,-1) * texel).rgb, levels);
                float3 C = poster(tex2D(_MainTex, tc + float2( 1,-1) * texel).rgb, levels);
                float3 D = poster(tex2D(_MainTex, tc + float2(-1, 0) * texel).rgb, levels);
                float3 E = poster(tex2D(_MainTex, tc                        ).rgb, levels);
                float3 F = poster(tex2D(_MainTex, tc + float2( 1, 0) * texel).rgb, levels);
                float3 G = poster(tex2D(_MainTex, tc + float2(-1, 1) * texel).rgb, levels);
                float3 H = poster(tex2D(_MainTex, tc + float2( 0, 1) * texel).rgb, levels);
                float3 II = poster(tex2D(_MainTex, tc + float2( 1, 1) * texel).rgb, levels);

                // Scale3x edge conditions
                bool bDB = ceq(D, B), bDH = ceq(D, H);
                bool bBF = ceq(B, F), bHF = ceq(H, F);
                bool bEA = ceq(E, A), bEC = ceq(E, C);
                bool bEG = ceq(E, G), bEI = ceq(E, II);

                // Scale3x: 9 subpixel colors
                float3 s00 = (bDB && !bDH && !bBF) ? D : E;
                float3 s20 = (bBF && !bDB && !bHF) ? F : E;
                float3 s02 = (bDH && !bDB && !bHF) ? D : E;
                float3 s22 = (bHF && !bDH && !bBF) ? F : E;
                float3 s10 = ((bDB && !bDH && !bBF && !bEC) || (bBF && !bDB && !bHF && !bEA)) ? B : E;
                float3 s01 = ((bDB && !bDH && !bBF && !bEG) || (bDH && !bDB && !bHF && !bEA)) ? D : E;
                float3 s21 = ((bBF && !bDB && !bHF && !bEI) || (bHF && !bDH && !bBF && !bEC)) ? F : E;
                float3 s12 = ((bDH && !bDB && !bHF && !bEI) || (bHF && !bDH && !bBF && !bEG)) ? H : E;
                float3 s11 = E;

                // Smooth blending at subpixel boundaries
                float2 sp = f * 3.0;
                float w = _EdgeWidth;

                float bx1 = smoothstep(1.0 - w, 1.0 + w, sp.x);
                float bx2 = smoothstep(2.0 - w, 2.0 + w, sp.x);
                float by1 = smoothstep(1.0 - w, 1.0 + w, sp.y);
                float by2 = smoothstep(2.0 - w, 2.0 + w, sp.y);

                float wx0 = 1.0 - bx1, wx1 = bx1 - bx2, wx2 = bx2;
                float3 row0 = wx0 * s00 + wx1 * s10 + wx2 * s20;
                float3 row1 = wx0 * s01 + wx1 * s11 + wx2 * s21;
                float3 row2 = wx0 * s02 + wx1 * s12 + wx2 * s22;

                float wy0 = 1.0 - by1, wy1 = by1 - by2, wy2 = by2;
                float3 filtered = wy0 * row0 + wy1 * row1 + wy2 * row2;

                // Blend with raw source based on strength
                float4 raw = tex2D(_MainTex, uv);
                float4 result;
                result.rgb = lerp(raw.rgb, filtered, _Strength);
                result.a = raw.a;
                result *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
