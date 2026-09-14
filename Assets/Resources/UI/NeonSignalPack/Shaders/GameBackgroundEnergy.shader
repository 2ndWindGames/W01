Shader "UI/GameBackgroundEnergy"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Range(0.02, 1)) = 0.35
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.75
        _ArcActive ("Arc Active", Range(0, 2)) = 0
        _ArcX ("Arc X", Range(0, 1)) = 0.05
        _ArcY ("Arc Y", Range(0, 1)) = 0.5
        _ArcLength ("Arc Length", Range(0, 0.5)) = 0.18
        _ArcSeed ("Arc Seed", Float) = 0
        _ArcColor ("Arc Color", Color) = (0.35,0.85,1.0,1)
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
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _FlowSpeed;
            float _FlowStrength;
            float _ArcActive;
            float _ArcX;
            float _ArcY;
            float _ArcLength;
            float _ArcSeed;
            fixed4 _ArcColor;

            float Hash(float value)
            {
                return frac(sin(value * 127.1 + _ArcSeed * 31.7) * 43758.5453);
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float2 uv = input.texcoord;
                float maximum = max(color.r, max(color.g, color.b));
                float minimum = min(color.r, min(color.g, color.b));
                float railLine = smoothstep(0.17, 0.42, maximum) * smoothstep(0.07, 0.29, maximum - minimum);
                float floorLine = smoothstep(0.10, 0.25, maximum) * smoothstep(0.04, 0.14, maximum - minimum);
                float travel = frac(_Time.y * _FlowSpeed);
                float railBand = exp(-pow((uv.y - (1.08 - travel * 1.16)) / 0.035, 2.0));
                float floorBand = exp(-pow((uv.y - (0.34 - travel * 0.39)) / 0.030, 2.0));
                float sideArea = smoothstep(0.28, 0.42, abs(uv.x - 0.5));
                float floorArea = 1.0 - smoothstep(0.17, 0.29, uv.y);
                float flow = (railLine * railBand * sideArea + floorLine * floorBand * floorArea) * _FlowStrength;
                color.rgb += flow * float3(0.24, 0.68, 0.9);

                if (_ArcActive > 0.001)
                {
                    float segment = (uv.y - _ArcY) * 45.0;
                    float index = floor(segment);
                    float bend = lerp(Hash(index), Hash(index + 1.0), frac(segment));
                    float boltX = _ArcX + (bend - 0.5) * 0.035;
                    float distance = abs(uv.x - boltX);
                    float lengthMask = step(_ArcY, uv.y) * step(uv.y, _ArcY + _ArcLength);
                    float core = exp(-distance * 1100.0);
                    float halo = exp(-distance * 160.0);
                    float arc = lengthMask * (core * 0.9 + halo * 0.18) * _ArcActive;
                    color.rgb += arc * _ArcColor.rgb;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
