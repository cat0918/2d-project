Shader "Fortress/Background Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Exterior ("Courtyard", 2D) = "white" {}
        _Interior ("Gallery", 2D) = "white" {}
        _ExteriorTint ("Courtyard Tint", Color) = (1,1,1,1)
        _InteriorTint ("Gallery Tint", Color) = (0.78,0.85,0.82,1)
        _ExteriorRect ("Courtyard XY Size", Vector) = (0,0,1,1)
        _GalleryRect ("Gallery XY Size", Vector) = (0,0,1,1)
        _Transition ("Blend Start End / Fade Start End", Vector) = (0,10,0,2)
        _UpperFade ("Upper Fade Start End Enabled", Vector) = (0,8,0,0)
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
            sampler2D _Exterior, _Interior;
            float4 _ExteriorRect, _GalleryRect, _Transition, _UpperFade;
            fixed4 _ExteriorTint, _InteriorTint;
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
                float2 a = (i.world - _ExteriorRect.xy) / _ExteriorRect.zw;
                float2 b = (i.world - _GalleryRect.xy) / _GalleryRect.zw;
                a = float2(mirror(a.x), saturate(a.y));
                b = float2(mirror(b.x), saturate(b.y));
                fixed4 outside = tex2D(_Exterior, a) * _ExteriorTint;
                outside.a *= 1 - saturate(_UpperFade.z) * smoothstep(_UpperFade.x, _UpperFade.y, i.world.y);
                fixed4 inside = tex2D(_Interior, b) * _InteriorTint;
                fixed4 color = lerp(outside, inside, smoothstep(_Transition.x, _Transition.y, i.world.x));
                color.a *= smoothstep(_Transition.z, _Transition.w, i.world.x);
                return color;
            }
            ENDCG
        }
    }
}
