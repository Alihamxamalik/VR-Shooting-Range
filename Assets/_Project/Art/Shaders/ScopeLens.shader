// The eyepiece of a rifle scope: a round lens that shows the scope camera's texture with a crosshair drawn
// over it and a dark edge, as a real scope has. Unlit and single-pass-instanced, so it is cheap on a headset.
Shader "RangeControl/Scope Lens"
{
    Properties
    {
        _BaseMap ("Scope Image", 2D) = "black" {}
        _ReticleColor ("Reticle Colour", Color) = (0, 0, 0, 1)
        _ReticleWidth ("Reticle Width", Range(0.001, 0.02)) = 0.003
        _EdgeStart ("Edge Darkening Start", Range(0.5, 1)) = 0.86
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ScopeLens"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _ReticleColor;
                half _ReticleWidth;
                half _EdgeStart;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centred = input.uv - 0.5;
                float radius = length(centred) * 2.0;
                clip(1.0 - radius);

                half3 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half onReticle = saturate(step(abs(centred.x), _ReticleWidth) + step(abs(centred.y), _ReticleWidth));
                colour = lerp(colour, _ReticleColor.rgb, onReticle * _ReticleColor.a);
                colour *= 1.0 - smoothstep(_EdgeStart, 1.0, radius);
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
