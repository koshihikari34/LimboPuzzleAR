Shader "LimboPuzzleAR/UI/TitleLogoGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlitchAmount ("Glitch Amount", Range(0, 1)) = 0
        _GlitchJitter ("Glitch Jitter", Range(0, 1)) = 0
        _CyanColor ("Cyan", Color) = (0, 0.9, 1, 1)
        _MagentaColor ("Magenta", Color) = (1, 0, 0.8, 1)
        _LimeColor ("Lime", Color) = (0.184, 1, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _CyanColor;
            fixed4 _MagentaColor;
            fixed4 _LimeColor;
            float _GlitchAmount;
            float _GlitchJitter;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float band = step(0.45, frac(sin(floor(IN.texcoord.y * 42.0 + _Time.y * 34.0)) * 43758.5453));
                float pulse = saturate(_GlitchAmount);
                float offset = (0.008 + _GlitchJitter * 0.025) * pulse * lerp(0.45, 1.0, band);

                fixed4 baseCol = tex2D(_MainTex, IN.texcoord) * IN.color;
                fixed4 cyan = tex2D(_MainTex, IN.texcoord + float2(offset, 0)) * _CyanColor;
                fixed4 magenta = tex2D(_MainTex, IN.texcoord - float2(offset, 0)) * _MagentaColor;
                fixed4 lime = tex2D(_MainTex, IN.texcoord + float2(offset * 0.35, offset * 0.65)) * _LimeColor;

                float cyanEdge = abs(cyan.a - baseCol.a);
                float magentaEdge = abs(magenta.a - baseCol.a);
                float limeEdge = abs(lime.a - baseCol.a);
                float stripe = band * baseCol.a * pulse;

                fixed3 rgb = baseCol.rgb * (1.0 - pulse * 0.18);
                rgb += cyan.rgb * (cyanEdge + stripe * 0.2) * pulse * 1.4;
                rgb += magenta.rgb * (magentaEdge + stripe * 0.2) * pulse * 1.4;
                rgb += lime.rgb * (limeEdge + stripe * 0.25) * pulse * 1.15;

                return fixed4(saturate(rgb), baseCol.a);
            }
            ENDCG
        }
    }
}
