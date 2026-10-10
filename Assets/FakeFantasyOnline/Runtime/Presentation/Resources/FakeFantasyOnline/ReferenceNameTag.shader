Shader "Overlay/NameTag"
{
    Properties
    {
        [PerRendererData] _MainTex ("Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
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

            VertexOutput Vertex(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 Fragment(VertexOutput input) : SV_Target
            {
                return tex2D(_MainTex, input.uv) * input.color;
            }
            ENDCG
        }
    }

    Fallback Off
}
