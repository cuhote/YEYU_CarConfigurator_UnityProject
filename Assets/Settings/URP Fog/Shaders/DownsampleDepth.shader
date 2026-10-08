Shader "Hidden/DownsampleDepth"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DownsampleDepth"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
        // Only output to the Red channel (R32_SFloat)
        ColorMask R

        HLSLPROGRAM
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        #pragma target 4.5
        #pragma editor_sync_compilation

        #pragma vertex Vert
        #pragma fragment Frag

        float Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // Use Gather to fetch 4 depth samples (2x2 block) in a single instruction
    float4 depths = GATHER_RED_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);

    // Find min and max depth within the 2x2 neighborhood
    float minDepth = min(min(depths.x, depths.y), min(depths.z, depths.w));
    float maxDepth = max(max(depths.x, depths.y), max(depths.z, depths.w));

    // Checkerboard pattern selection to minimize artifacts at depth edges
    // positionCS.xy provides the pixel coordinates on the current render target
    uint2 pixelPos = uint2(input.positionCS.xy);
    return ((pixelPos.x + pixelPos.y) & 1) > 0 ? minDepth : maxDepth;
}

ENDHLSL
}
    }

        Fallback Off
}
