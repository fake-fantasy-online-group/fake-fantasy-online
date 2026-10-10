Shader "Custom/IsoDepthY"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35
        _Bias ("Depth Bias", Float) = 0
        [PerRendererData] _DepthYOverride ("Depth Y Override", Float) = 99999
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("Z Test", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite On
        ZTest [_ZTestMode]

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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Cutoff;
            float _Bias;
            float _DepthYOverride;
            float _IsoYMin;
            float _IsoYMax;
            float _IsoReversed;

            VertexOutput Vertex(VertexInput input)
            {
                VertexOutput output;
                float4 worldPosition = mul(unity_ObjectToWorld, input.vertex);
                float depthY =
                    (_DepthYOverride < 9000.0 ? _DepthYOverride : worldPosition.y) + _Bias;
                float depth01 = saturate(
                    (_IsoYMax - depthY) / max(_IsoYMax - _IsoYMin, 0.001));
                if (_IsoReversed <= 0.5)
                {
                    depth01 = 1.0 - depth01;
                }

                output.position = mul(UNITY_MATRIX_VP, worldPosition);
                output.position.z *= depth01;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv) * input.color;
                clip(color.a - _Cutoff);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
