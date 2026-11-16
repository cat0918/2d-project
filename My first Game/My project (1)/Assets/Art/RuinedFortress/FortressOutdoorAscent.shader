Shader "Fortress/Outdoor Ascent"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Outside ("Outside", 2D) = "white" {}
        _Inside ("Inside", 2D) = "white" {}
        _OutsideTint ("Outside Tint", Color) = (1,1,1,1)
        _InsideTint ("Inside Tint", Color) = (0.6,0.66,0.74,1)
        _OutsideRect ("Outside XY Size", Vector) = (0,0,1,1)
        _InsideRect ("Inside XY Size", Vector) = (0,0,1,1)
        _Entrance ("Interior X / Exterior X / Low Y / High Y", Vector) = (0,8,0,4)
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
            sampler2D _Outside, _Inside;
            float4 _OutsideRect, _InsideRect, _Entrance;
            fixed4 _OutsideTint, _InsideTint;
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }
            float mirror(float x) { return 1 - abs(frac(x * 0.5) * 2 - 1); }
            fixed4 frag(Output i) : SV_Target
            {
                float2 a = (i.world - _OutsideRect.xy) / _OutsideRect.zw;
                float2 b = (i.world - _InsideRect.xy) / _InsideRect.zw;
                fixed4 outside = tex2D(_Outside, float2(mirror(a.x), saturate(a.y)));
                // Extend only a cloud-only portion above the existing artwork,
                // instead of repeating the full wall vertically.
                float2 skyUV = float2(0.02 + mirror(a.x) * 0.28,
                    0.92 + 0.08 * mirror((a.y - 1) * 2));
                fixed4 sky = tex2D(_Outside, skyUV);
                outside = lerp(outside, sky, smoothstep(1, 1.2, a.y)) * _OutsideTint;
                fixed4 inside = tex2D(_Inside, saturate(b)) * _InsideTint;
                float enter = (1 - smoothstep(_Entrance.x, _Entrance.y, i.world.x))
                    * smoothstep(_Entrance.z, _Entrance.w, i.world.y);
                return lerp(outside, inside, enter);
            }
            ENDCG
        }
    }
}
