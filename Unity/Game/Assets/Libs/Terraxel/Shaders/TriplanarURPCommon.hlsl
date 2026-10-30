#ifndef TERRAXEL_TRIPLANAR_URP_COMMON_INCLUDED
#define TERRAXEL_TRIPLANAR_URP_COMMON_INCLUDED

// Pipeline includes + material declarations shared by Triplanar.shader and
// SimpleVert.shader. Must be included BEFORE each shader's own `Attributes`
// struct and GetObjectPositionOS/GetObjectNormalOS/GetObjectTextureIndex
// accessors, since those reference the cbuffer values declared here
// (notably _DirectionMask). See TriplanarURP.hlsl for the actual
// vertex/fragment implementations that depend on those accessors.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half _SliceRange;
    half _MapScale;
    half _Glossiness;
    half _Metallic;
    half _BumpScale;
    float4 _WorldPos;
    int _DirectionMask;
CBUFFER_END

TEXTURE2D_ARRAY(_MainTextures);
SAMPLER(sampler_MainTextures);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

#endif // TERRAXEL_TRIPLANAR_URP_COMMON_INCLUDED
