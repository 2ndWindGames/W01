Shader "UI/IntroBackgroundLaser"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LaserSpeed ("Laser Speed", Range(0.05, 2)) = 0.24
        _LaserStrength ("Laser Strength", Range(0, 2)) = 0.55
        _LaserWidth ("Laser Width", Range(0.005, 0.2)) = 0.025
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
            float _LaserSpeed;
            float _LaserStrength;
            float _LaserWidth;

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
                float railLine = smoothstep(0.17, 0.42, maximum) * smoothstep(0.08, 0.30, maximum - minimum);
                float floorLine = smoothstep(0.10, 0.25, maximum) * smoothstep(0.04, 0.14, maximum - minimum);

                float travel = frac(_Time.y * _LaserSpeed);
                float railPosition = 1.10 - travel * 1.20;
                float floorPosition = 0.55 - travel * 0.62;
                float railBand = exp(-pow((uv.y - railPosition) / _LaserWidth, 2.0));
                float floorBand = exp(-pow((uv.y - floorPosition) / (_LaserWidth * 1.15), 2.0));
                float edge = smoothstep(0.27, 0.40, abs(uv.x - 0.5));
                float floorArea = 1.0 - smoothstep(0.37, 0.53, uv.y);
                float laser = (railLine * railBand * edge + floorLine * floorBand * floorArea) * _LaserStrength;
                color.rgb += laser * float3(0.35, 0.85, 1.15);

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
