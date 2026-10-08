Shader "Hidden/VolumetricFog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
        #include "./DeclareDownsampledDepthTexture.hlsl"

        // Uniforms
        int _FrameCount;
        float _Distance;
        float _BaseHeight;
        float _MaximumHeight;
        float _Density;
        float _Absortion;
        float3 _Tint;
        float _MainLightAnisotropy;
        float _MainLightScattering;
        int _MaxSteps;
        float _UseLocalBox;
        float3 _LocalBoxCenter;
        float3 _LocalBoxSize;
        float _LocalBoxFeather;

        TEXTURE2D_X(_VolumetricFogTexture);
        SAMPLER(sampler_VolumetricFogTexture);

        // SDF Based Fog Density
        float GetFogDensity(float3 posWS)
        {
            float heightRange = max(0.001, (_MaximumHeight - _BaseHeight));
            float hFactor = saturate((posWS.y - _BaseHeight) / heightRange);
            float density = _Density * (1.0 - hFactor * hFactor);

            if (_UseLocalBox > 0.5)
            {
                float3 halfSize = _LocalBoxSize * 0.5;
                float3 d = abs(posWS - _LocalBoxCenter) - halfSize;
                float distOutside = length(max(d, 0.0));

                // Hard cut outside the feather zone
                if (distOutside > _LocalBoxFeather) return 0.0;
                float mask = saturate(1.0 - distOutside / max(_LocalBoxFeather, 0.001));
                density *= (mask * mask);
            }
            return density;
        }

        // Enhanced Lighting with Shadow Attenuation
        float3 GetStepLightColor(float3 posWS, float3 rd, float phase, float dens)
        {
            // Explicit Shadow Coordination for Unity 6
            float4 shadowCoord = TransformWorldToShadowCoord(posWS);
            Light mainLight = GetMainLight(shadowCoord);

            // Apply Shadow Attenuation to create light shafts
            float3 lightCol = (mainLight.color * _Tint) * (mainLight.shadowAttenuation * phase * dens * _MainLightScattering);
            return lightCol;
        }
        ENDHLSL

        Pass
        {
            Name "VolumetricFogRender"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFog

            // Essential Keywords for Shadows in Unity 6.4 Render Graph
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            float4 FragFog(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float depth = SampleDownsampledSceneDepthConsiderReversedZ(input.texcoord);
                float3 posWS = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP);
                float3 ro = GetCameraPositionWS();
                float3 viewVec = posWS - ro;
                float maxLen = min(_Distance, length(viewVec));

                if (maxLen <= 0.001) return float4(0,0,0,1);

                float3 rd = viewVec / length(viewVec);
                float stepLen = maxLen / (float)_MaxSteps;
                float jitter = stepLen * InterleavedGradientNoise(input.positionCS.xy, _FrameCount);

                float3 accumColor = 0;
                float transmittance = 1.0;
                float phase = CornetteShanksPhaseFunction(_MainLightAnisotropy, dot(rd, GetMainLight().direction));

                for (int i = 0; i < _MaxSteps; ++i) {
                    float d = jitter + (float)i * stepLen;
                    if (d >= maxLen) break;

                    float3 currPos = ro + rd * d;
                    float dens = GetFogDensity(currPos);

                    if (dens > 0.001) {
                        float3 stepLight = GetStepLightColor(currPos, rd, phase, dens);
                        // Apply Beer-Lambert Law for scattering and absorption
                        float extinction = exp(-dens * _Absortion * stepLen);
                        accumColor += stepLight * (transmittance * (1.0 - extinction) / max(0.001, dens * _Absortion));
                        transmittance *= extinction;
                    }
                    if (transmittance <= 0.01) break;
                }
                return float4(accumColor, transmittance);
            }
            ENDHLSL
        }

        Pass
        {
            Name "VolumetricFogComposition"
            ZTest Always ZWrite Off Cull Off
            Blend One SrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            float4 FragComposite(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_VolumetricFogTexture, sampler_VolumetricFogTexture, input.texcoord);
            }
            ENDHLSL
        }
    }
}
