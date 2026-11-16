Shader "Fortress/Boss Backdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FadeStartX ("Entrance Fade Start", Float) = 0
        _FadeEndX ("Entrance Fade End", Float) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _FadeStartX, _FadeEndX;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float x : TEXCOORD1; fixed4 color : COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.x = mul(unity_ObjectToWorld, v.vertex).x;
                o.color = v.color;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;
                color.a *= 1 - smoothstep(_FadeStartX, _FadeEndX, i.x);
                return color;
            }
            ENDCG
        }
    }
}
