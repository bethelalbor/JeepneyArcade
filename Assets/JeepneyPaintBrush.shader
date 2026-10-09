Shader "Jeepney/Paint Brush"
{
    Properties
    {
        _MainTex ("Current Paint Texture", 2D) = "black" {}
        _BrushFromUV ("Brush From UV", Vector) = (0, 0, 0, 0)
        _BrushToUV ("Brush To UV", Vector) = (0, 0, 0, 0)
        _BrushColor ("Brush Color", Color) = (1, 0, 0, 1)
        _BrushSize ("Brush Radius", Float) = 0.025
        _BrushOpacity ("Brush Opacity", Range(0, 1)) = 1
        _EraseMode ("Erase Mode", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "PaintStroke"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BrushFromUV;
                float4 _BrushToUV;
                float4 _BrushColor;
                float _BrushSize;
                float _BrushOpacity;
                float _EraseMode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float DistanceToSegment(float2 sampleUv, float2 segmentStart, float2 segmentEnd)
            {
                float2 segment = segmentEnd - segmentStart;
                float lengthSquared = dot(segment, segment);
                float t = lengthSquared > 0.0000001
                    ? saturate(dot(sampleUv - segmentStart, segment) / lengthSquared)
                    : 0.0;
                return length(sampleUv - (segmentStart + segment * t));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 destination = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float distanceToStroke = DistanceToSegment(input.uv, _BrushFromUV.xy, _BrushToUV.xy);
                float antialiasWidth = max(fwidth(distanceToStroke), 0.00001);
                float coverage = 1.0 - smoothstep(_BrushSize - antialiasWidth, _BrushSize + antialiasWidth, distanceToStroke);

                float sourceAlpha = saturate(_BrushColor.a * _BrushOpacity * coverage);
                float eraseAlpha = saturate(_BrushOpacity * coverage);

                if (_EraseMode > 0.5)
                {
                    // Destination-out erasing: reduce only the existing paint
                    // layer alpha so the underlying jeepney atlas is revealed.
                    return half4(destination.rgb, destination.a * (1.0 - eraseAlpha));
                }

                if (sourceAlpha <= 0.00001)
                    return destination;

                float outputAlpha = sourceAlpha + destination.a * (1.0 - sourceAlpha);
                float3 sourceOverRgb = _BrushColor.rgb * sourceAlpha + destination.rgb * destination.a * (1.0 - sourceAlpha);
                float3 outputRgb = outputAlpha > 0.00001 ? sourceOverRgb / outputAlpha : 0.0;

                return half4(outputRgb, outputAlpha);
            }
            ENDHLSL
        }
    }
}
