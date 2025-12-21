Shader "UI/URP/GlitchEdgesOnlyV2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Intensity ("Edge Glitch Intensity", Range(0,1)) = 0.85

        _EdgeThreshold ("Alpha Edge Threshold", Range(0,1)) = 0.5
        _EdgeWidth ("Alpha Edge Width", Range(0.25, 8)) = 2.0

        _RectEdgePixels ("Rect Edge Pixels (fallback)", Range(1,32)) = 3

        _BuzzSegmentCount ("Buzz Segment Count", Range(1,256)) = 120
        _BuzzSpeed ("Buzz Speed", Range(0,60)) = 28
        _BuzzDensity ("Buzz Density", Range(0,1)) = 0.85

        _EdgeShift ("Edge Shift", Range(0,0.12)) = 0.045
        _MicroShift ("Micro Shift", Range(0,0.04)) = 0.010

        _EdgeBite ("Edge Bite (alpha tears)", Range(0,1)) = 0.25
        _EdgeBiteSpeed ("Edge Bite Speed", Range(0,60)) = 22

        _GlitchColor ("Glitch Color", Color) = (0.2, 1.0, 0.7, 1.0)
        _GlitchColorAmount ("Glitch Color Amount", Range(0,1)) = 0.75

        _Bulge ("Bulge (Lens/CRT)", Range(-0.75,0.75)) = 0.28
        _BulgeCenter ("Bulge Center (0..1)", Vector) = (0.5,0.5,0,0)

        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
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
            Name "UI"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;

                float2 uvAtlas : TEXCOORD0;
                float2 uvLocal01 : TEXCOORD1;
                float4 uvMinMax : TEXCOORD2;

                float4 localPosition : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;

                float _Intensity;

                float _EdgeThreshold;
                float _EdgeWidth;

                float _RectEdgePixels;

                float _BuzzSegmentCount;
                float _BuzzSpeed;
                float _BuzzDensity;

                float _EdgeShift;
                float _MicroShift;

                float _EdgeBite;
                float _EdgeBiteSpeed;

                half4 _GlitchColor;
                float _GlitchColorAmount;

                float _Bulge;
                float4 _BulgeCenter;

                half4 _TextureSampleAdd;
                float4 _ClipRect;
            CBUFFER_END

            float Hash21(float2 value)
            {
                float2 p = frac(value * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 SampleUI(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) + _TextureSampleAdd;
            }

            float UnityGet2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            half4 ApplyClipAndAlpha(half4 colorValue, float2 localPosition)
            {
                #ifdef UNITY_UI_CLIP_RECT
                    colorValue.a *= (half)UnityGet2DClipping(localPosition, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(colorValue.a - 0.001);
                #endif

                return colorValue;
            }

            float GetAlphaEdgeMask(float alphaValue, float thresholdValue, float widthValue)
            {
                float alphaFwidthValue = max(fwidth(alphaValue), 1e-4);
                float normalizedDistance = abs(alphaValue - thresholdValue) / (alphaFwidthValue * max(0.25, widthValue));
                return saturate(1.0 - saturate(normalizedDistance));
            }

            float GetRectEdgeMask(float2 local01, float rectEdgePixels)
            {
                float distToRectEdge = min(min(local01.x, 1.0 - local01.x), min(local01.y, 1.0 - local01.y));
                float pixelStep = max(max(fwidth(local01.x), fwidth(local01.y)), 1e-5);
                float edgeWidth01 = pixelStep * max(1.0, rectEdgePixels);
                return saturate(1.0 - (distToRectEdge / max(edgeWidth01, 1e-5)));
            }

            float2 Bulge01(float2 uv01, float2 center01, float bulgeValue)
            {
                float2 delta = uv01 - center01;
                float r2 = dot(delta, delta);
                float factor = 1.0 + bulgeValue * r2 * 2.0;
                return center01 + delta * factor;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.localPosition = input.positionOS;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                output.uvAtlas = input.uv0.xy;
                output.uvLocal01 = input.uv1.xy;
                output.uvMinMax = input.uv2;

                output.color = (half4)input.color * _Color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float intensityValue = saturate(_Intensity);

                half4 baseSample = SampleUI(input.uvAtlas);
                half4 baseColor = baseSample * input.color;

                if (intensityValue <= 0.0001)
                {
                    return ApplyClipAndAlpha(baseColor, input.localPosition.xy);
                }

                float alphaEdgeMask = GetAlphaEdgeMask(baseSample.a, _EdgeThreshold, _EdgeWidth);

                float rectEdgeMask = GetRectEdgeMask(input.uvLocal01, _RectEdgePixels);
                rectEdgeMask = rectEdgeMask * step(0.001, baseSample.a);

                float edgeMask = max(alphaEdgeMask, rectEdgeMask);
                if (edgeMask <= 0.0001)
                {
                    return ApplyClipAndAlpha(baseColor, input.localPosition.xy);
                }

                float edgeBlend = saturate(edgeMask * intensityValue);

                float timeValue = _Time.y;

                float segmentCountValue = max(1.0, _BuzzSegmentCount);
                float segmentIndexValue = floor(input.uvLocal01.y * segmentCountValue);

                float tickValue = floor(timeValue * max(0.0, _BuzzSpeed));
                float2 keyA = float2(segmentIndexValue, tickValue);
                float2 keyB = float2(segmentIndexValue + 31.7, tickValue + 9.3);

                float randomA = Hash21(keyA);
                float randomB = Hash21(keyB);

                float enabledValue = step(1.0 - saturate(_BuzzDensity), randomA);
                float signedValue = (randomB - 0.5) * 2.0;

                float microTickValue = floor(timeValue * 60.0);
                float microRandom = Hash21(float2(segmentIndexValue + 113.1, microTickValue));
                float microSigned = (microRandom - 0.5) * 2.0;

                float shiftLocalX = (signedValue * _EdgeShift * enabledValue + microSigned * _MicroShift) * edgeBlend;

                float2 uvMin = input.uvMinMax.xy;
                float2 uvMax = input.uvMinMax.zw;
                float2 uvRange = max(uvMax - uvMin, 1e-6);

                float2 local01 = input.uvLocal01;
                local01.x = local01.x + shiftLocalX;

                float2 bulgeCenter = _BulgeCenter.xy;
                float bulgeValue = _Bulge * edgeBlend;
                float2 localBulged = Bulge01(local01, bulgeCenter, bulgeValue);

                localBulged = clamp(localBulged, 0.0, 1.0);

                float2 uvEdge = uvMin + localBulged * uvRange;
                uvEdge = clamp(uvEdge, uvMin, uvMax);

                half4 edgeSample = SampleUI(uvEdge);

                float biteTickValue = floor(timeValue * max(0.0, _EdgeBiteSpeed));
                float biteRandom = Hash21(float2(segmentIndexValue + 7.9, biteTickValue + 3.1));
                float biteThreshold = saturate(_EdgeBite * edgeBlend);
                float biteKeep = step(biteThreshold, biteRandom);
                edgeSample.a *= (half)lerp(1.0, biteKeep, biteThreshold);

                half4 mixedSample = lerp(baseSample, edgeSample, (half)edgeBlend);

                half4 outColor = mixedSample * input.color;

                float colorBlend = saturate(_GlitchColorAmount * edgeBlend) * _GlitchColor.a;
                outColor.rgb = lerp(outColor.rgb, _GlitchColor.rgb, (half)colorBlend);

                return ApplyClipAndAlpha(outColor, input.localPosition.xy);
            }
            ENDHLSL
        }
    }
}
