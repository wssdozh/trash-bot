Shader "UI/OldNetDarkFractal_URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask (A)", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0.02,0.02,0.03,1)
        _AccentColorA ("Accent A", Color) = (0.10,0.90,0.70,1)
        _AccentColorB ("Accent B", Color) = (0.75,0.25,0.95,1)

        _Scale ("Scale", Range(0.5, 10)) = 2.7
        _Speed ("Speed", Range(0, 2)) = 0.35
        _Rotation ("Rotation", Range(0, 2)) = 0.25

        _Iterations ("Iterations", Range(8, 80)) = 36
        _EscapeRadius ("Escape Radius", Range(2, 10)) = 4.0
        _TrapSharpness ("Trap Sharpness", Range(0.5, 12)) = 5.5
        _StripeDensity ("Stripe Density", Range(0, 50)) = 14
        _Glow ("Glow", Range(0, 3)) = 1.0

        _PixelResolution ("Pixel Resolution", Range(48, 720)) = 200
        _PaletteSteps ("Palette Steps", Range(2, 64)) = 12
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.55

        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35
        _ScanlineDensity ("Scanline Density", Range(60, 1600)) = 520

        _GlitchStrength ("Glitch Strength", Range(0, 1)) = 0.16
        _Curvature ("CRT Curvature", Range(0, 1)) = 0.12
        _Vignette ("Vignette", Range(0, 1)) = 0.45

        _GradientStart ("Left->Right Start (uv.x)", Range(0, 1)) = 0.0
        _GradientEnd ("Left->Right End (uv.x)", Range(0, 1)) = 1.0
        _GradientPower ("Left->Right Power", Range(0.2, 6)) = 1.6

        _Alpha ("Alpha", Range(0, 1)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "OldNetDarkFractal"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BackgroundColor;
            float4 _AccentColorA;
            float4 _AccentColorB;

            float _Scale;
            float _Speed;
            float _Rotation;

            float _Iterations;
            float _EscapeRadius;
            float _TrapSharpness;
            float _StripeDensity;
            float _Glow;

            float _PixelResolution;
            float _PaletteSteps;
            float _DitherStrength;

            float _ScanlineStrength;
            float _ScanlineDensity;

            float _GlitchStrength;
            float _Curvature;
            float _Vignette;

            float _GradientStart;
            float _GradientEnd;
            float _GradientPower;

            float _Alpha;
            float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _ClipRect;
            float _UseUIAlphaClip;

            float _UiUnscaledTime;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 positionOSXY : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float UiGet2DClipping(float2 position, float4 clipRect)
            {
                float2 inside01 = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside01.x * inside01.y;
            }

            float Hash21(float2 p)
            {
                float n = sin(dot(p, float2(12.9898, 78.233)));
                return frac(n * 43758.5453);
            }

            float2 Rotate2(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            void ComputeTricornFractal(float2 c, int iterationCount, float escapeRadius, out float normalizedIteration, out float orbitTrap)
            {
                float2 z = float2(0.0, 0.0);
                float escapeRadiusSquared = escapeRadius * escapeRadius;

                orbitTrap = 100000.0;
                int escapedAt = iterationCount;
                float2 zAtEscape = z;

                [loop]
                for (int index = 0; index < 96; index++)
                {
                    if (index >= iterationCount)
                    {
                        break;
                    }

                    float2 conjugateZ = float2(z.x, -z.y);

                    float x = conjugateZ.x;
                    float y = conjugateZ.y;

                    float2 zSquared = float2(x * x - y * y, 2.0 * x * y);
                    z = zSquared + c;

                    float trapValue = abs(z.x) + abs(z.y);
                    orbitTrap = min(orbitTrap, trapValue);

                    float r2 = dot(z, z);
                    if (r2 > escapeRadiusSquared)
                    {
                        escapedAt = index;
                        zAtEscape = z;
                        break;
                    }
                }

                if (escapedAt >= iterationCount)
                {
                    normalizedIteration = 0.0;
                    return;
                }

                float r = max(1e-6, length(zAtEscape));
                float smoothIteration = (float)escapedAt + 1.0 - log2(max(1e-6, log2(r)));
                normalizedIteration = saturate(smoothIteration / max(1.0, (float)iterationCount));
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                float2 transformedUv;
                transformedUv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                output.uv = transformedUv;
                output.color = input.color;
                output.positionOSXY = input.positionOS.xy;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 maskSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float leftToRight = smoothstep(_GradientStart, _GradientEnd, input.uv.x);
                leftToRight = pow(saturate(leftToRight), _GradientPower);

                float timeValue = _UiUnscaledTime * _Speed;

                float2 pixelGrid = float2(_PixelResolution, _PixelResolution);
                float2 pixelUv = floor(input.uv * pixelGrid) / pixelGrid;

                float2 centered = pixelUv * 2.0 - 1.0;
                float curvatureAmount = _Curvature * (0.25 + 0.75 * leftToRight);
                centered = centered * (1.0 + curvatureAmount * dot(centered, centered));
                float2 crtUv = centered * 0.5 + 0.5;

                float glitchBandIndex = floor(crtUv.y * 260.0);
                float glitchTimeIndex = floor(timeValue * 10.0);

                float glitchGate = step(0.965, Hash21(float2(glitchBandIndex, glitchTimeIndex)));
                float glitchJitter = (Hash21(float2(glitchBandIndex + 19.0, glitchTimeIndex + 7.0)) - 0.5);
                glitchJitter = glitchJitter * _GlitchStrength * glitchGate * (0.25 + 0.75 * leftToRight);

                crtUv.x = crtUv.x + glitchJitter;

                float2 p = crtUv * 2.0 - 1.0;
                float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                p.x = p.x * aspect;

                p = Rotate2(p, timeValue * _Rotation);
                p = p * _Scale;

                float2 drift = float2(sin(timeValue * 0.17), cos(timeValue * 0.13)) * (0.12 + 0.18 * leftToRight);
                float2 c = p + drift;

                int iterationCount = (int)round(_Iterations);
                float normalizedIteration;
                float orbitTrap;

                ComputeTricornFractal(c, iterationCount, _EscapeRadius, normalizedIteration, orbitTrap);

                float trapLine = exp2(-orbitTrap * _TrapSharpness);
                float stripe = 0.5 + 0.5 * sin((normalizedIteration * _StripeDensity + timeValue * (0.55 + 0.35 * leftToRight)) * 6.28318530718);

                float3 accent = lerp(_AccentColorA.rgb, _AccentColorB.rgb, stripe);
                float3 background = _BackgroundColor.rgb;

                float intensity = (trapLine * (0.35 + 0.65 * stripe)) * _Glow;
                intensity = intensity * leftToRight;

                float3 colorOut = background + accent * intensity;

                float paletteSteps = max(2.0, _PaletteSteps);
                colorOut = floor(saturate(colorOut) * paletteSteps) / paletteSteps;

                float2 ditherCoordinate = floor(crtUv * pixelGrid);
                float dither = Hash21(ditherCoordinate) - 0.5;
                colorOut = colorOut + (dither / paletteSteps) * _DitherStrength;

                float scan = sin((crtUv.y * _ScanlineDensity + timeValue * 0.35) * 6.28318530718);
                float scanFactor = 1.0 - _ScanlineStrength * (0.35 - 0.35 * scan);
                colorOut = colorOut * scanFactor;

                float vignetteValue = 1.0 - smoothstep(0.15, 1.10, dot(centered, centered));
                colorOut = lerp(colorOut, colorOut * vignetteValue, _Vignette);

                float alphaOut = maskSample.a * input.color.a * _Alpha;

                #ifdef UNITY_UI_CLIP_RECT
                    alphaOut = alphaOut * UiGet2DClipping(input.positionOSXY, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(alphaOut - 0.001);
                #endif

                if (_UseUIAlphaClip == 1.0)
                {
                    clip(alphaOut - 0.001);
                }

                return half4(colorOut, alphaOut);
            }
            ENDHLSL
        }
    }
}
