Shader "UI/Navigation HUD Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Glow Color", Color) = (1,0.75,0.05,1)
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 2
        _GlowRadius ("Glow Radius", Range(0, 0.08)) = 0.018
        _GlowSoftness ("Glow Softness", Range(0, 2)) = 1

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
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowRadius;
            float _GlowSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 SampleSprite(float2 uv)
            {
                return tex2D(_MainTex, uv) + _TextureSampleAdd;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 center = SampleSprite(IN.texcoord) * IN.color;
                float radius = _GlowRadius;

                float outerAlpha = 0;
                outerAlpha += SampleSprite(IN.texcoord + float2( radius, 0)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2(-radius, 0)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2(0,  radius)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2(0, -radius)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2( radius,  radius)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2(-radius,  radius)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2( radius, -radius)).a;
                outerAlpha += SampleSprite(IN.texcoord + float2(-radius, -radius)).a;
                outerAlpha *= 0.125;

                float glowAlpha = saturate((outerAlpha - center.a) * _GlowSoftness);
                fixed4 glow = _GlowColor * (_GlowIntensity * glowAlpha);
                glow.a = glowAlpha * _GlowColor.a;

                fixed4 color = center + glow * (1 - center.a);
                color.a = saturate(center.a + glow.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
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
