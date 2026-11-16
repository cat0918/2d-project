Shader "Fortress/Checkpoint Gem"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _GemActive ("Gem Active", Range(0,1)) = 0
        _SpriteRect ("Sprite UV Rect", Vector) = (0,0,1,1)
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
            sampler2D _MainTex;
            float _GemActive;
            float4 _SpriteRect;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                float2 local = (i.uv - _SpriteRect.xy) / _SpriteRect.zw;
                // Restrict the recolor to cyan crystal pixels in the upper central recess.
                float region = step(0.35, local.x) * step(local.x, 0.65)
                    * step(0.48, local.y) * step(local.y, 0.85);
                float cyan = smoothstep(0.055, 0.15, min(color.g, color.b) - color.r);
                float brightness = max(color.g, color.b);
                fixed3 green = brightness * fixed3(0.32, 1, 0.48);
                color.rgb = lerp(color.rgb, green, region * cyan * _GemActive);
                return color;
            }
            ENDCG
        }
    }
}
