// used by a URP "Full Screen Pass Renderer Feature" (After Rendering Transparents) to hard-cap
// the composited frame brightness at 1.0 per channel, so no amount of stacked/overlapping 2D
// lights can blow a region out past a single intensity-1 light's worth of brightness
Shader "Hidden/GardenDefense/LightIntensityClamp"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "LightIntensityClamp"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, 0);
                color.rgb = saturate(color.rgb);
                return color;
            }
            ENDHLSL
        }
    }
}
