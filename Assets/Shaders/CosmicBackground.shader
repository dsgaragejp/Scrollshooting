Shader "StellarVanguard/CosmicBackground"
{
    Properties
    {
        _MainTex ("Feedback Texture", 2D) = "black" {}
        _Speed ("Animation Speed", Range(0.1, 5.0)) = 1.0
        _Intensity ("Color Intensity", Range(0.1, 2.0)) = 1.0
        _FractalScale ("Fractal Scale", Range(0.1, 1.0)) = 0.3
        _FeedbackStrength ("Feedback Strength", Range(0.0, 1.0)) = 0.5
        _DistortAmount ("Distortion Amount", Range(0.0, 0.1)) = 0.04
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Speed;
                float _Intensity;
                float _FractalScale;
                float _FeedbackStrength;
                float _DistortAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // XorDev-inspired fractal noise function
            float4 cosmicFractal(float2 uv, float time, float2 resolution)
            {
                float2 p = (uv * 2.0 - 1.0) / _FractalScale;
                float4 o = float4(0, 0, 0, 0);
                float2 v = p;

                // Outer loop - color accumulation
                for (float i = 1.0; i < 9.0; i += 1.0)
                {
                    // Inner loop - fractal iteration
                    v = p;
                    for (float f = 1.0; f < 9.0; f += 1.0)
                    {
                        float2 offset = sin(ceil(v.yx * f + i * 0.3) + resolution - time * _Speed * 0.5) / f;
                        v += offset;
                    }

                    // Distance-based coloring
                    float l = dot(p, p) - 5.0 - 2.0 / v.y;
                    float invL = 0.1 / abs(l);

                    // Rainbow color based on iteration
                    float4 rainbow = cos(i / 3.0 + 0.1 / l + float4(1, 2, 3, 4)) + 1.0;
                    o += invL * rainbow * _Intensity;
                }

                return o;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                float2 resolution = float2(1920, 1080); // Approximate resolution

                // Generate fractal pattern
                float4 fractal = cosmicFractal(IN.uv, time, resolution);

                // Feedback distortion
                float2 distortedUV = IN.uv + _DistortAmount * sin(IN.uv * 10.0 + IN.uv.yx * 1.66);
                float4 feedback = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Combine with feedback
                float4 color = fractal + feedback * fractal * _FeedbackStrength;

                // Apply tanh for smooth clamping (like XorDev's original)
                color = max(tanh(color), 0.0);
                color.a = 1.0;

                return color;
            }
            ENDHLSL
        }
    }
}
