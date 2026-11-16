Shader "Fortress/Damp Stone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _StoneTex ("Stone", 2D) = "white" {}
        _TileWorldSize ("Tile Size In World Units", Float) = 4
        _StoneTint ("Damp Stone Tint", Color) = (0.78, 0.88, 0.80, 1)
        _MossAmount ("Moss Amount", Range(0,1)) = 0.35
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
            float _TileWorldSize, _MossAmount;
            fixed4 _StoneTint;
            struct Input { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; fixed4 color : COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 stone = tex2D(_StoneTex, i.world / max(_TileWorldSize, 0.001));
                // World-space pixel clusters add moss without new geometry or animation.
                float2 cell = floor(i.world * 24);
                float patch = hash(floor(cell / 10));
                float grain = hash(cell);
                float moss = step(0.55, patch) * step(0.35, grain) * _MossAmount;
                fixed3 damp = stone.rgb * _StoneTint.rgb;
                fixed3 mossColor = stone.rgb * fixed3(0.58, 0.83, 0.38);
                return fixed4(lerp(damp, mossColor, moss), stone.a) * i.color;
            }
            ENDCG
        }
    }
}
