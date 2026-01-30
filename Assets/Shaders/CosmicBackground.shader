Shader "StellarVanguard/CosmicBackground"
{
    Properties
    {
        _Speed ("Animation Speed", Range(0.1, 5.0)) = 1.0
        _Intensity ("Color Intensity", Range(0.5, 3.0)) = 1.5
        _Scale ("Pattern Scale", Range(0.1, 2.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CosmicBackground"

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

            CBUFFER_START(UnityPerMaterial)
                float _Speed;
                float _Intensity;
                float _Scale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _Speed;
                float2 uv = IN.uv;

                // Centered coordinates
                float2 p = (uv - 0.5) * 2.0 / _Scale;

                // XorDev-style fractal
                float4 col = float4(0.02, 0.01, 0.05, 1.0); // Deep space base

                for (float i = 1.0; i < 7.0; i += 1.0)
                {
                    // Fractal warping
                    float2 v = p;
                    for (float j = 1.0; j < 5.0; j += 1.0)
                    {
                        v += sin(v.yx * j + i * 0.5 + time * 0.2) / (j + 1.0);
                    }

                    // Distance field
                    float d = length(p) - 2.0 - 1.0 / (abs(v.y) + 0.5);
                    float glow = 0.08 / (abs(d) + 0.02);

                    // Rainbow colors
                    float3 rgb = 0.5 + 0.5 * cos(i * 0.7 + float3(0.0, 2.1, 4.2) + time * 0.1);
                    col.rgb += rgb * glow * _Intensity * 0.2;
                }

                // Stars
                float2 starUV = uv * 100.0;
                float star = frac(sin(dot(floor(starUV), float2(12.9898, 78.233))) * 43758.5453);
                star = smoothstep(0.995, 1.0, star);
                col.rgb += star * 0.8;

                // Tone mapping
                col.rgb = 1.0 - exp(-col.rgb * 1.5);
                col.a = 1.0;

                return col;
            }
            ENDHLSL
        }
    }
}
