Shader "UI/IntroTitleCurrent"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EffectTime ("Unscaled Effect Time", Float) = 0
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.78
        _SparkStrength ("Spark Strength", Range(0, 1)) = 0.76
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
            float _EffectTime;
            float _FlowStrength;
            float _SparkStrength;

            float Hash(float value)
            {
                return frac(sin(value * 127.1) * 43758.5453);
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
                float2 uv = input.texcoord;
                fixed4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * input.color;
                float originalAlpha = color.a;
                float letter = smoothstep(0.12, 0.5, color.a);
                float luminous = smoothstep(0.26, 0.75, dot(color.rgb, float3(0.3, 0.55, 0.15)));

                // A narrow current travels through the existing logo. The soft shoulder
                // gives its printed neon tube a little light without flooding the screen.
                float path = uv.x + uv.y * 0.16;
                float travel = frac(_EffectTime * 0.17) * 1.38 - 0.13;
                float distanceToCurrent = abs(path - travel);
                float currentCore = exp(-pow(distanceToCurrent / 0.016, 2.0));
                float currentHalo = exp(-pow(distanceToCurrent / 0.055, 2.0));
                float current = (currentCore * 0.7 + currentHalo * 0.3) * letter * _FlowStrength;

                // A second, fainter wave moves in the opposite direction. The waves
                // tint only source pixels, so the logo silhouette and text remain intact.
                float reverseTravel = 1.22 - frac(_EffectTime * 0.105 + 0.41) * 1.38;
                float reverseCurrent = exp(-pow((path - reverseTravel) / 0.028, 2.0)) *
                                       letter * _FlowStrength * 0.31;
                float voltage = 0.96 + sin(_EffectTime * 4.7 + uv.x * 24.0) * 0.035;
                color.rgb *= voltage;
                color.rgb = lerp(color.rgb, float3(0.48, 0.94, 1.0), saturate(current * 0.65 * luminous));
                color.rgb += (current + reverseCurrent) * float3(0.08, 0.20, 0.31);

                // Rare, short zigzag discharges run across a small part of a letter.
                // The alpha mask keeps all sparks on the artwork instead of spraying
                // rectangular particles into the intro UI.
                float sparkClock = _EffectTime * 1.38;
                float sparkStep = floor(sparkClock);
                float sparkPhase = frac(sparkClock);
                float sparkGate = step(0.57, Hash(sparkStep + 2.7));
                float sparkEnvelope = smoothstep(0.02, 0.10, sparkPhase) *
                                      (1.0 - smoothstep(0.30, 0.46, sparkPhase));
                float2 sparkCenter = float2(lerp(0.17, 0.83, Hash(sparkStep + 7.1)),
                                            Hash(sparkStep + 13.3) > 0.5 ? 0.67 : 0.34);
                float2 local = uv - sparkCenter;
                float jag = sin(local.x * 160.0 + sparkStep * 2.3) * 0.006 +
                            sin(local.x * 370.0 - sparkStep) * 0.0025;
                float arc = exp(-pow((local.y - jag) / 0.0038, 2.0)) *
                            (1.0 - smoothstep(0.035, 0.13, abs(local.x)));
                float sparkHotspot = exp(-pow(length(local * float2(1.0, 1.5)) / 0.021, 2.0));
                float spark = (arc * 0.72 + sparkHotspot * 0.48) *
                              sparkGate * sparkEnvelope * letter * _SparkStrength;
                color.rgb = lerp(color.rgb, float3(0.86, 0.99, 1.0), saturate(spark));
                color.rgb += spark * float3(0.12, 0.25, 0.37);
                color.a = saturate(originalAlpha * (1.0 + current * 0.10 + spark * 0.15));

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
