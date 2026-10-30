#ifndef TERRAXEL_TRIPLANAR_URP_INCLUDED
#define TERRAXEL_TRIPLANAR_URP_INCLUDED

// Shared triplanar lighting/vertex/fragment code for Terraxel's terrain shaders
// (Triplanar.shader / SimpleVert.shader) on the Universal Render Pipeline.
//
// Each including .shader file must first include TriplanarURPCommon.hlsl, then
// define (in this order, since they depend on the cbuffer declared there):
//   - struct Attributes { ... }               (its own mesh vertex layout)
//   - float3 GetObjectPositionOS(Attributes)   object-space vertex position
//   - float3 GetObjectNormalOS(Attributes)     object-space vertex normal
//   - float  GetObjectTextureIndex(Attributes) raw per-vertex biome/texture index
// ...and only then include this file.

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float3 localCoord   : TEXCOORD2; // _WorldPos + object-space position: continuous world-ish coord used for triplanar UVs
    float textureIndex  : TEXCOORD3;
    float4 shadowCoord   : TEXCOORD4;
    half fogCoord        : TEXCOORD5;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings TriplanarVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = GetObjectPositionOS(input);
    float3 normalOS = GetObjectNormalOS(input);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
    output.localCoord = _WorldPos.xyz + positionOS;
    output.textureIndex = GetObjectTextureIndex(input) * _SliceRange;
    output.shadowCoord = GetShadowCoord(vertexInput);
    output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);

    return output;
}

// Triplanar albedo + world-space "whiteout" normal-map blending, shared by the forward pass.
void SampleTriplanar(float3 localCoord, float3 normalWS, out half3 albedo, out half3 blendedNormalWS, float textureIndex)
{
    float3 bf = abs(normalWS);
    bf /= max(dot(bf, float3(1, 1, 1)), 1e-5);

    float2 tx = localCoord.yz * _MapScale;
    float2 ty = localCoord.zx * _MapScale;
    float2 tz = localCoord.xy * _MapScale;

    half4 cx = SAMPLE_TEXTURE2D_ARRAY(_MainTextures, sampler_MainTextures, tx, textureIndex) * bf.x;
    half4 cy = SAMPLE_TEXTURE2D_ARRAY(_MainTextures, sampler_MainTextures, ty, textureIndex) * bf.y;
    half4 cz = SAMPLE_TEXTURE2D_ARRAY(_MainTextures, sampler_MainTextures, tz, textureIndex) * bf.z;
    albedo = ((cx + cy + cz) * _Color).rgb;

    half3 tnormalX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, tx), _BumpScale);
    half3 tnormalY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, ty), _BumpScale);
    half3 tnormalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, tz), _BumpScale);

    // Whiteout triplanar normal blending (world-space).
    tnormalX = half3(tnormalX.xy + normalWS.zy, abs(tnormalX.z) * normalWS.x);
    tnormalY = half3(tnormalY.xy + normalWS.xz, abs(tnormalY.z) * normalWS.y);
    tnormalZ = half3(tnormalZ.xy + normalWS.xy, abs(tnormalZ.z) * normalWS.z);

    blendedNormalWS = normalize(
        tnormalX.zyx * bf.x +
        tnormalY.xzy * bf.y +
        tnormalZ.xyz * bf.z);
}

half4 TriplanarForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    half3 normalWS = NormalizeNormalPerPixel(input.normalWS);

    half3 albedo;
    half3 blendedNormalWS;
    SampleTriplanar(input.localCoord, normalWS, albedo, blendedNormalWS, input.textureIndex);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Glossiness;
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.emission = half3(0, 0, 0);
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = NormalizeNormalPerPixel(blendedNormalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.shadowCoord = input.shadowCoord;
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogCoord);
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return half4(color.rgb, 1.0h);
}

// ---------------------------------------------------------------------------
// ShadowCaster pass
// ---------------------------------------------------------------------------

float3 _LightDirection;
float3 _LightPosition;

struct ShadowVaryings
{
    float4 positionCS : SV_POSITION;
};

float4 TriplanarGetShadowPositionHClip(Attributes input)
{
    float3 positionOS = GetObjectPositionOS(input);
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 normalWS = TransformObjectToWorldNormal(GetObjectNormalOS(input));

#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    positionCS = ApplyShadowClamping(positionCS);
    return positionCS;
}

ShadowVaryings TriplanarShadowVertex(Attributes input)
{
    ShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    output.positionCS = TriplanarGetShadowPositionHClip(input);
    return output;
}

half4 TriplanarShadowFragment(ShadowVaryings input) : SV_Target
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    return 0;
}

// ---------------------------------------------------------------------------
// DepthOnly pass
// ---------------------------------------------------------------------------

struct DepthOnlyVaryings
{
    float4 positionCS : SV_POSITION;
};

DepthOnlyVaryings TriplanarDepthOnlyVertex(Attributes input)
{
    DepthOnlyVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    output.positionCS = TransformObjectToHClip(GetObjectPositionOS(input));
    return output;
}

half TriplanarDepthOnlyFragment(DepthOnlyVaryings input) : SV_Target
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    return input.positionCS.z;
}

// ---------------------------------------------------------------------------
// DepthNormals pass (feeds the _CameraNormalsTexture used by SSAO, etc.)
// ---------------------------------------------------------------------------

struct DepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    half3 normalWS    : TEXCOORD0;
};

DepthNormalsVaryings TriplanarDepthNormalsVertex(Attributes input)
{
    DepthNormalsVaryings output = (DepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    output.positionCS = TransformObjectToHClip(GetObjectPositionOS(input));
    output.normalWS = half3(TransformObjectToWorldNormal(GetObjectNormalOS(input)));
    return output;
}

half4 TriplanarDepthNormalsFragment(DepthNormalsVaryings input) : SV_Target
{
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif

#if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(input.normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    return half4(packedNormalWS, 0.0);
#else
    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
#endif
}

#endif // TERRAXEL_TRIPLANAR_URP_INCLUDED
