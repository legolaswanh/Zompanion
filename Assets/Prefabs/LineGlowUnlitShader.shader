Shader "Custom/LineGlowUnlit"
{
    Properties
    {
        [HDR]_Color ("Base Color", Color) = (0.1, 0.8, 1, 1)
        [HDR]_EdgeColor ("Edge Glow Color", Color) = (0.3, 1, 2, 1)
        _EdgeWidth ("Edge Width", Range(0.01, 0.5)) = 0.2
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        _Alpha ("Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            half4 _Color;
            half4 _EdgeColor;
            float _EdgeWidth;
            float _GlowIntensity;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // ?? LineRenderer ?????? UV ? 0~1
                float d = abs(i.uv.y - 0.5);        // ???????
                // ??? 0 ? 1??????? edgeMask ??
                float edgeMask = saturate((_EdgeWidth - d) / _EdgeWidth);

                half4 col = _Color;
                col.rgb += _EdgeColor.rgb * edgeMask * _GlowIntensity;
                col.a = _Alpha;

                return col;
            }
            ENDCG
        }
    }
}