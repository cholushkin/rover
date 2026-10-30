Shader "Custom/TriplanarSimple"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _SliceRange ("Slices", Range(0,16)) = 6
        _MainTextures ("Albedo Textures", 2DArray) = "" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MapScale("Tiling Scale", Float) = 1
        _BumpMap ("Normal Map", 2D) = "bump" {}

        _BumpScale("Normal Strength", Float) = 1
        [PerRendererData]_WorldPos("WorldPos", Vector) = (0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "DisableBatching"="True" }
        LOD 200

        HLSLINCLUDE
        #include "TriplanarURPCommon.hlsl"

        // Chunk2D vertex layout (see Chunk2D.cs `layout`): Position(float3), Normal(float3), TexCoord0(float).
        // No LOD-seam stitching here (Chunk2D chunks don't transition between LODs the way Chunk3D does).
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float textureIndex : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 GetObjectPositionOS(Attributes input)
        {
            return input.positionOS.xyz;
        }

        float3 GetObjectNormalOS(Attributes input)
        {
            return input.normalOS;
        }

        float GetObjectTextureIndex(Attributes input)
        {
            return input.textureIndex;
        }

        #include "TriplanarURP.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray

            #pragma vertex TriplanarVertex
            #pragma fragment TriplanarForwardFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray

            #pragma vertex TriplanarShadowVertex
            #pragma fragment TriplanarShadowFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray

            #pragma vertex TriplanarDepthOnlyVertex
            #pragma fragment TriplanarDepthOnlyFragment

            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require 2darray

            #pragma vertex TriplanarDepthNormalsVertex
            #pragma fragment TriplanarDepthNormalsFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
    FallBack Off
}
