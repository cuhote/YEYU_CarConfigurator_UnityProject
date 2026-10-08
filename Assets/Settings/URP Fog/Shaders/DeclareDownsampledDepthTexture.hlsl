#ifndef VOLUMETRIC_FOG_DEPTH_INCLUDED
#define VOLUMETRIC_FOG_DEPTH_INCLUDED

TEXTURE2D_X_FLOAT(_HalfResDepth);
SAMPLER(sampler_HalfResDepth);

float SampleDownsampledSceneDepthConsiderReversedZ(float2 uv)
{
    float2 sampledUV = UnityStereoTransformScreenSpaceTex(uv);
    float depth = SAMPLE_TEXTURE2D_X_LOD(_HalfResDepth, sampler_PointClamp, sampledUV, 0).r;

#if !UNITY_REVERSED_Z
    depth = depth * 2.0 - 1.0;
#endif

    return depth;
}

#endif
