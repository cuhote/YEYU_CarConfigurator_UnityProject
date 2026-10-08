#ifndef VOLUMETRIC_FOG_BLUR_INCLUDED
#define VOLUMETRIC_FOG_BLUR_INCLUDED

#include "./DeclareDownsampledDepthTexture.hlsl"

#define KERNEL_RADIUS 4
#define BLUR_DEPTH_FALLOFF 0.5
static const float KernelWeights[5] = { 0.2026, 0.1790, 0.1240, 0.0672, 0.0285 };

float4 DepthAwareGaussianBlur(float2 uv, float2 dir, TEXTURE2D_X(textureToBlur), SAMPLER(sampler_TextureToBlur), float2 texelSize)
{
    float4 centerSample = SAMPLE_TEXTURE2D_X_LOD(textureToBlur, sampler_TextureToBlur, UnityStereoTransformScreenSpaceTex(uv), 0);
    float centerRawDepth = SampleDownsampledSceneDepthConsiderReversedZ(uv);
    float centerLinearDepth = LinearEyeDepth(centerRawDepth, _ZBufferParams);

    float3 rgbAccum = centerSample.rgb * KernelWeights[0];
    float weightAccum = KernelWeights[0];
    float2 stepSize = texelSize * dir;

    for (int i = 1; i <= KERNEL_RADIUS; ++i) {
        float weight = KernelWeights[i];
        float2 offsets[2] = { (float)i * stepSize, (float)-i * stepSize };
        for (int j = 0; j < 2; ++j) {
            float2 sampleUV = uv + offsets[j];
            float neighborRawDepth = SampleDownsampledSceneDepthConsiderReversedZ(sampleUV);
            float neighborLinearDepth = LinearEyeDepth(neighborRawDepth, _ZBufferParams);
            float r2 = BLUR_DEPTH_FALLOFF * abs(centerLinearDepth - neighborLinearDepth);
            float bilateralWeight = exp(-r2 * r2) * weight;
            rgbAccum += SAMPLE_TEXTURE2D_X_LOD(textureToBlur, sampler_TextureToBlur, UnityStereoTransformScreenSpaceTex(sampleUV), 0).rgb * bilateralWeight;
            weightAccum += bilateralWeight;
        }
    }
    return float4(rgbAccum / max(0.0001, weightAccum), centerSample.a);
}
#endif
