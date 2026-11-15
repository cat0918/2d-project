Shader "Fortress/World Stone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _StoneTex ("Stone", 2D) = "white" {}
        _TileWorldSize ("Tile Size In World Units", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _StoneTex;
            float _TileWorldSize;
            struct Input
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };
            struct Output
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = mul(unity_ObjectToWorld, v.vertex).xy / max(_TileWorldSize, 0.001);
                o.color = v.color;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                return tex2D(_StoneTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
