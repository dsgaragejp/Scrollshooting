Shader "StellarVanguard/DiagonalGlow"
{
    Properties
    {
        _Intensity ("Intensity", Range(0.01, 0.1)) = 0.03
        _CircleRadius ("Circle Radius", Range(0.1, 1.0)) = 0.5
        _DiagonalWidth ("Diagonal Width", Range(0.05, 0.5)) = 0.1
        _ColorR ("Red Channel", Range(0.5, 4.0)) = 2.0
        _ColorG ("Green Channel", Range(0.5, 4.0)) = 1.0
        _ColorB ("Blue Base", Range(0.5, 4.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Intensity;
            float _CircleRadius;
            float _DiagonalWidth;
            float _ColorR;
            float _ColorG;
            float _ColorB;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // XorDev's diagonal glow shader
                // Original: vec2 p=(FC.xy*2.-r)/r.y,v;
                // o=tanh(.03*vec4(2,1,1.+p)/(.05+max(v+=length(p)-.5,-v/.1)).x/(.1+abs(p.x-p.y)));

                // Normalized coordinates (-1 to 1, aspect corrected)
                float2 p = IN.uv * 2.0 - 1.0;
                p.x *= 16.0 / 9.0; // Aspect ratio correction

                // v = length(p) - 0.5 (distance from center minus radius)
                float v = length(p) - _CircleRadius;

                // max(v, -v/0.1) creates smooth falloff
                float falloff = 0.05 + max(v, -v / 0.1);

                // Diagonal line effect
                float diagonal = _DiagonalWidth + abs(p.x - p.y);

                // Color with position variation
                float4 color = float4(_ColorR, _ColorG, _ColorB + p.x, _ColorB + p.y);

                // Final calculation with tone mapping
                float4 o = tanh(_Intensity * color / falloff / diagonal);

                return float4(o.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
