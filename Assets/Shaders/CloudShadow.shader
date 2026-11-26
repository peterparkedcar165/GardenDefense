// Tommy's cloud shadows (2026-11-26, CloudShadows): black shapes from the sheet, see through at the material
// colour's alpha. two shadows that overlap don't add up (Tommy): every pixel a shadow draws marks the stencil
// buffer (bit 128), and a shadow skips the pixels already marked, so each pixel is darkened once. the shapes
// are hard edged, so a texel is either in a shape (drawn and marked) or not (clipped, neither)
Shader "Custom/CloudShadow"
{
    Properties
    {
        _MainTex ("Shapes", 2D) = "white" {}
        _Color ("Colour", Color) = (0, 0, 0, 0.5)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Stencil
        {
            Ref 128
            ReadMask 128
            WriteMask 128
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 shape = tex2D(_MainTex, i.uv);
                // outside the shape: not drawn and not marked
                clip(shape.a - 0.5);
                return fixed4(_Color.rgb * i.color.rgb, _Color.a * i.color.a);
            }
            ENDCG
        }
    }
}
