Shader "UI/ComboAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mirror ("Mirror", Float) = 0
        _Phase ("Phase", Float) = 0
        _Burst ("Entry Pulse", Range(0, 1)) = 0
        _Gold ("Gold Tier", Float) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            float _Mirror;
            float _Phase;
            float _Burst;
            float _Gold;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float x = lerp(input.uv.x, 1.0 - input.uv.x, _Mirror);
                float y = input.uv.y;
                // The strip itself only occupies the outer 180 canvas units. Keep the
                // middle, HUD and floor entirely clear, even during an entry pulse.
                float gate = smoothstep(0.125, 0.19, y) * (1.0 - smoothstep(0.81, 0.875, y));
                // The main colored trace sits inside the existing cyan frame, over
                // the dark field, so the tier color can actually be read.
                float halo = exp(-pow((x - 0.67) / 0.18, 2.0));
                float hairline = exp(-pow((x - 0.67) / 0.010, 2.0));
                float satellite = exp(-pow((x - 0.34) / 0.008, 2.0));
                float segment = smoothstep(-0.1, 0.62, sin(y * 56.0 + _Mirror * 1.8));
                float secondarySegment = smoothstep(0.1, 0.75, sin(y * 37.0 + 1.4));
                float breath = 0.82 + 0.18 * sin(_Phase * (2.5 + _Gold * 0.7));

                float moving = 0.17 + frac(_Phase * (0.17 + _Gold * 0.06) + _Mirror * 0.48) * 0.66;
                float glint = exp(-pow((y - moving) / 0.026, 2.0))
                    * exp(-pow((x - 0.67) / 0.075, 2.0));
                float whiteCore = exp(-pow((y - moving) / 0.008, 2.0))
                    * exp(-pow((x - 0.67) / 0.018, 2.0));

                // Small angled circuit terminals land on the two playfield rails.
                float terminalY = min(abs(y - 0.16), abs(y - 0.84));
                float terminal = exp(-pow(terminalY / 0.003, 2.0))
                    * smoothstep(0.53, 0.61, x) * (1.0 - smoothstep(0.77, 0.84, x));
                float angled = exp(-pow((x - (0.64 + terminalY * 4.0)) / 0.012, 2.0))
                    * (1.0 - smoothstep(0.02, 0.035, terminalY));

                float energy = gate * breath * (halo * 0.26
                    + hairline * (0.18 + 0.68 * segment)
                    + satellite * secondarySegment * 0.10
                    + glint * 0.98 + whiteCore * 0.35)
                    + (terminal * 0.45 + angled * 0.29) * (0.7 + 0.3 * breath);
                energy += _Burst * gate * (halo * 0.26 + glint * 0.4 + hairline * 0.18);
                float hot = saturate(whiteCore * 0.6 + terminal * 0.2 + glint * 0.05);
                float3 tint = lerp(input.color.rgb, float3(1.0, 1.0, 1.0), hot);
                // Gold keeps a bright lemon core; it does not retain green from the prior tier.
                tint = lerp(tint, float3(1.0, 0.98, 0.78), hot * _Gold * 0.35);
                fixed4 result = fixed4(tint, saturate(energy * input.color.a));
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
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
