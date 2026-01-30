Shader "StellarVanguard/BlackHole"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "black" {}
        _BlackHoleRadius ("Black Hole Radius", Range(0.01, 0.5)) = 0.15
        _AccretionDiskWidth ("Accretion Disk Width", Range(0.01, 0.3)) = 0.1
        _GravityStrength ("Gravity Lensing Strength", Range(0.1, 5.0)) = 2.0
        _DiskColor1 ("Disk Color Inner", Color) = (1, 0.6, 0.1, 1)
        _DiskColor2 ("Disk Color Outer", Color) = (1, 0.9, 0.5, 1)
        _RotationSpeed ("Disk Rotation Speed", Range(0.1, 5.0)) = 1.0
        _BlackHoleCenter ("Black Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _PhotonRingIntensity ("Photon Ring Intensity", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BlackHole"

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
                float _BlackHoleRadius;
                float _AccretionDiskWidth;
                float _GravityStrength;
                float4 _DiskColor1;
                float4 _DiskColor2;
                float _RotationSpeed;
                float4 _BlackHoleCenter;
                float _PhotonRingIntensity;
            CBUFFER_END

            #define PI 3.14159265359

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Gravitational lensing effect
            float2 gravitationalLens(float2 uv, float2 center, float strength, float radius)
            {
                float2 delta = uv - center;
                float dist = length(delta);

                if (dist < 0.001) return uv;

                // Schwarzschild-like lensing approximation
                float deflection = strength * radius * radius / (dist * dist);
                deflection = min(deflection, 1.0);

                float2 lensedUV = uv + delta * deflection;
                return lensedUV;
            }

            // Accretion disk with relativistic beaming
            float4 accretionDisk(float2 uv, float2 center, float innerRadius, float outerRadius, float time)
            {
                float2 delta = uv - center;
                float dist = length(delta);
                float angle = atan2(delta.y, delta.x);

                // Disk boundaries
                if (dist < innerRadius || dist > outerRadius) return float4(0, 0, 0, 0);

                // Radial gradient
                float radialGradient = 1.0 - (dist - innerRadius) / (outerRadius - innerRadius);
                radialGradient = pow(radialGradient, 0.5);

                // Rotating spiral pattern
                float spiral = sin(angle * 8.0 + time * _RotationSpeed - dist * 20.0) * 0.5 + 0.5;
                float noise = sin(angle * 16.0 - time * _RotationSpeed * 2.0 + dist * 40.0) * 0.3 + 0.7;

                // Doppler beaming (brighter on approaching side)
                float doppler = cos(angle - time * _RotationSpeed * 0.5) * 0.5 + 1.0;

                // Color interpolation
                float4 color = lerp(_DiskColor2, _DiskColor1, radialGradient);
                float intensity = radialGradient * spiral * noise * doppler;

                return float4(color.rgb * intensity, intensity * 0.9);
            }

            // Photon ring (Einstein ring)
            float photonRing(float2 uv, float2 center, float radius)
            {
                float dist = length(uv - center);
                float ring = exp(-pow((dist - radius) * 50.0, 2.0));
                return ring * _PhotonRingIntensity;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 center = _BlackHoleCenter.xy;
                float time = _Time.y;

                // Distance from black hole center
                float dist = length(IN.uv - center);

                // Event horizon (completely black)
                if (dist < _BlackHoleRadius * 0.8)
                {
                    return float4(0, 0, 0, 1);
                }

                // Apply gravitational lensing to background
                float2 lensedUV = gravitationalLens(IN.uv, center, _GravityStrength, _BlackHoleRadius);
                float4 background = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, lensedUV);

                // Accretion disk
                float innerRadius = _BlackHoleRadius * 1.2;
                float outerRadius = innerRadius + _AccretionDiskWidth;
                float4 disk = accretionDisk(IN.uv, center, innerRadius, outerRadius, time);

                // Photon ring
                float ring = photonRing(IN.uv, center, _BlackHoleRadius);
                float4 ringColor = float4(1.0, 0.95, 0.8, 1.0) * ring;

                // Composite
                float4 color = background;
                color.rgb += ringColor.rgb;
                color = lerp(color, disk, disk.a);

                // Darken near event horizon
                float edgeDarkness = smoothstep(_BlackHoleRadius * 0.8, _BlackHoleRadius * 1.2, dist);
                color.rgb *= edgeDarkness;

                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }
    }
}
