Shader "Fortress/Upper Background Fade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FadeStartY ("Fade Start World Y", Float) = 0
        _FadeEndY ("Fade End World Y", Float) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            float _FadeStartY, _FadeEndY;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float y : TEXCOORD1; fixed4 color : COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                o.color = v.color;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;
                color.a *= 1 - smoothstep(_FadeStartY, _FadeEndY, i.y);
                return color;
            }
            ENDCG
        }
    }
}
