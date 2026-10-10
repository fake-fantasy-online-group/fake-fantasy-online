Shader "fake-fantasy-online/EffectGL"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SrcF ("Src Blend", Float) = 1
        _DstF ("Dst Blend", Float) = 1
        _Op ("Blend Op", Float) = 0
        _Add ("Additive Intensity", Float) = 1
        [HideInInspector] _ApprovedGlowB ("Approved Glow B", Float) = 0
        _TintMode ("Glow Tint Enabled", Float) = 0
        _Tint ("Glow Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        BlendOp [_Op]
        Blend [_SrcF] [_DstF]

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Add;
            float _ApprovedGlowB;
            float _TintMode;
            float4 _Tint;

            VertexOutput Vertex(VertexInput input)
            {
                VertexOutput output;
                float4 worldPosition = mul(unity_ObjectToWorld, float4(input.vertex.xyz, 1.0));
                output.position = mul(UNITY_MATRIX_VP, worldPosition);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_Target
            {
                float4 sampled = tex2D(_MainTex, input.uv);
                // APPROVED-DEVIATION: GLOW-B-001. User selected B on 2026-09-07.
                // Only GlowLayerMat opts in; generic effects retain the recovered shader path.
                float3 glow = sampled.rgb;
                if (_ApprovedGlowB > 0.5)
                {
                    if (_TintMode > 0.5)
                        glow = max(sampled.r, max(sampled.g, sampled.b)) * _Tint.rgb;
                    // Normal additive modes already apply texture alpha through SrcAlpha blend.
                    if (_Add > 0.5)
                        glow *= sampled.a;
                }
                if (_Add > 0.5)
                {
                    float3 tinted = glow * input.color.rgb;
                    return float4(tinted * input.color.a, sampled.a);
                }

                return float4(glow, sampled.a) * input.color;
            }
            ENDCG
        }
    }

    Fallback Off
}
