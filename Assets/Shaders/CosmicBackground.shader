Shader "StellarVanguard/CosmicBackground"
{
    Properties
    {
        _Speed ("Animation Speed", Range(0.1, 3.0)) = 1.0
        _Scale ("Pattern Scale", Range(10.0, 1000.0)) = 80.0
        _ColorIntensity ("Color Intensity", Range(0.05, 0.5)) = 0.1
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

            float _Speed;
            float _Scale;
            float _ColorIntensity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // XorDev's fractal cosmic shader
                // Original: vec2 p=(FC.xy*2.-r)/r.y/.3,v;
                // for(float i,l,f;i++<9.;o+=.1/abs(l=dot(p,p)-5.-2./v.y)*(cos(i/3.+.1/l+vec4(1,2,3,4))+1.))
                //   for(v=p,f=0.;f++<9.;v+=sin(ceil(v.yx*f+i*.3)+r-t/2.)/f);

                // Use UV directly, normalized to -1 to 1 range, then scaled
                float2 uv = IN.uv * 2.0 - 1.0;  // -1 to 1
                uv.x *= 16.0 / 9.0;              // Aspect ratio correction

                float2 p = uv * _Scale;          // Scale the pattern
                float t = _Time.y * _Speed;

                float4 o = float4(0.0, 0.0, 0.0, 0.0);
                float2 v = float2(0.0, 0.0);

                // XorDev's double loop
                for (float i = 1.0; i < 10.0; i += 1.0)
                {
                    // Inner loop: compute v
                    v = p;
                    for (float f = 1.0; f < 10.0; f += 1.0)
                    {
                        // v += sin(ceil(v.yx*f+i*.3)+r-t/2.)/f
                        // r is large (resolution), we use a constant offset instead
                        float2 cellPos = ceil(v.yx * f + i * 0.3);
                        v += sin(cellPos + 500.0 - t * 0.5) / f;
                    }

                    // l = dot(p,p) - 5. - 2./v.y
                    float vy = v.y;
                    if (abs(vy) < 0.01) vy = 0.01;
                    float l = dot(p, p) - 5.0 - 2.0 / vy;

                    // o += .1/abs(l) * (cos(i/3.+.1/l+vec4(1,2,3,4))+1.)
                    float absL = abs(l);
                    if (absL < 0.001) absL = 0.001;

                    float brightness = _ColorIntensity / absL;
                    float4 colorPhase = float4(1.0, 2.0, 3.0, 4.0);
                    float4 color = cos(i / 3.0 + 0.1 / absL + colorPhase) + 1.0;
                    o += brightness * color;
                }

                // Tone mapping with tanh
                o = tanh(o);
                o = max(o, 0.0);

                return float4(o.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
