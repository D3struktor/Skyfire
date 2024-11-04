Shader "UI/PulsingOutlineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 1.0
        _PulseSpeed ("Pulse Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 outlinePos1 : TEXCOORD1;
                float4 outlinePos2 : TEXCOORD2;
                float4 outlinePos3 : TEXCOORD3;
                float4 outlinePos4 : TEXCOORD4;
                float timeFactor : TEXCOORD5;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float outlineOffset = _OutlineWidth / _ScreenParams.y; // Skalowanie outline

                // Przesunięcia konturu w cztery kierunki z czasowym czynnikiem pulsacji
                o.outlinePos1 = o.pos + float4(outlineOffset, 0, 0, 0);
                o.outlinePos2 = o.pos + float4(-outlineOffset, 0, 0, 0);
                o.outlinePos3 = o.pos + float4(0, outlineOffset, 0, 0);
                o.outlinePos4 = o.pos + float4(0, -outlineOffset, 0, 0);

                // Dodajemy dynamiczny czasowy czynnik
                o.timeFactor = _Time.y * _PulseSpeed;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 mainColor = tex2D(_MainTex, i.uv);

                // Zmienna przeźroczystości zależna od czasu dla efektu pulsacji
                float outlineAlpha = abs(sin(i.timeFactor)) * _OutlineColor.a;

                // Kontur kolorowy z dynamiczną przeźroczystością
                float4 outlineColor = float4(_OutlineColor.rgb, outlineAlpha);

                if (tex2Dproj(_MainTex, i.outlinePos1).a < 0.1 ||
                    tex2Dproj(_MainTex, i.outlinePos2).a < 0.1 ||
                    tex2Dproj(_MainTex, i.outlinePos3).a < 0.1 ||
                    tex2Dproj(_MainTex, i.outlinePos4).a < 0.1)
                {
                    return outlineColor;
                }

                return mainColor;
            }
            ENDCG
        }
    }
}
