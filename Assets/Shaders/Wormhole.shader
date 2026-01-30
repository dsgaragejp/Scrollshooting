Shader "StellarVanguard/Wormhole"
{
    Properties
    {
        _MainTex ("Current Space Texture", 2D) = "black" {}
        _DestinationTex ("Destination Space Texture", 2D) = "black" {}
        _WormholeRadius ("Wormhole Radius", Range(0.05, 0.5)) = 0.2
        _WormholeCenter ("Wormhole Center", Vector) = (0.5, 0.5, 0, 0)
        _EdgeGlow ("Edge Glow Intensity", Range(0, 5)) = 2.0
        _EdgeColor ("Edge Color", Color) = (0.5, 0.7, 1.0, 1)
        _DistortionStrength ("Distortion Strength", Range(0.1, 3.0)) = 1.5
        _TunnelDepth ("Tunnel Depth", Range(1, 10)) = 5
        _RotationSpeed ("Rotation Speed", Range(0, 5)) = 1.0
        _TransitionProgress ("Transition Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Wormhole"

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
            TEXTURE2D(_DestinationTex);
            SAMPLER(sampler_DestinationTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _WormholeRadius;
                float4 _WormholeCenter;
                float _EdgeGlow;
                float4 _EdgeColor;
                float _DistortionStrength;
                float _TunnelDepth;
                float _RotationSpeed;
                float _TransitionProgress;
            CBUFFER_END

            #define PI 3.14159265359

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Spherical wormhole visualization (like Interstellar)
            float4 frag(Varyings IN) : SV_Target
            {
                float2 center = _WormholeCenter.xy;
                float2 delta = IN.uv - center;
                float dist = length(delta);
                float angle = atan2(delta.y, delta.x);
                float time = _Time.y;

                // Outside wormhole - show current space with distortion
                if (dist > _WormholeRadius)
                {
                    // Gravitational distortion near edge
                    float distortFactor = _DistortionStrength * _WormholeRadius / dist;
                    distortFactor = min(distortFactor, 0.5);

                    float2 distortedUV = IN.uv + delta * distortFactor;
                    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                    // Edge glow
                    float edgeDist = dist - _WormholeRadius;
                    float glow = exp(-edgeDist * 15.0) * _EdgeGlow;

                    // Rainbow edge effect
                    float hue = angle / (2.0 * PI) + time * _RotationSpeed * 0.1;
                    float3 rainbow = 0.5 + 0.5 * cos(2.0 * PI * (hue + float3(0, 0.33, 0.67)));

                    color.rgb += rainbow * glow * 0.5 + _EdgeColor.rgb * glow;

                    return color;
                }

                // Inside wormhole - show destination space through spherical lens
                // Normalize distance within wormhole
                float normalizedDist = dist / _WormholeRadius;

                // Spherical lens effect - map to destination texture
                // The center shows the opposite side, edges show our side
                float sphereZ = sqrt(1.0 - normalizedDist * normalizedDist);

                // Tunnel rotation effect
                float rotatedAngle = angle + time * _RotationSpeed + sphereZ * _TunnelDepth;
                float2 tunnelUV;
                tunnelUV.x = 0.5 + cos(rotatedAngle) * normalizedDist * 0.5;
                tunnelUV.y = 0.5 + sin(rotatedAngle) * normalizedDist * 0.5;

                // Blend between current and destination based on depth
                float4 destinationColor = SAMPLE_TEXTURE2D(_DestinationTex, sampler_DestinationTex, tunnelUV);
                float4 currentColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tunnelUV);

                // Deeper = more destination visible
                float blendFactor = sphereZ * (1.0 - _TransitionProgress) + _TransitionProgress;
                float4 tunnelColor = lerp(currentColor, destinationColor, blendFactor);

                // Tunnel lighting - brighter at center
                float tunnelLight = sphereZ * 0.5 + 0.5;
                tunnelColor.rgb *= tunnelLight;

                // Rainbow tunnel rings
                float rings = sin(normalizedDist * 20.0 - time * 3.0) * 0.5 + 0.5;
                float hue = normalizedDist + time * 0.2;
                float3 ringColor = 0.5 + 0.5 * cos(2.0 * PI * (hue + float3(0, 0.33, 0.67)));
                tunnelColor.rgb += ringColor * rings * 0.3 * (1.0 - sphereZ);

                // Inner edge glow
                float innerGlow = exp(-(1.0 - normalizedDist) * 5.0) * _EdgeGlow * 0.5;
                tunnelColor.rgb += _EdgeColor.rgb * innerGlow;

                tunnelColor.a = 1.0;
                return tunnelColor;
            }
            ENDHLSL
        }
    }
}
