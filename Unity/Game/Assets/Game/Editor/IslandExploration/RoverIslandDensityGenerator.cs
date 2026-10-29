using System.Globalization;
using System.IO;
using Game.Gameplay.IslandExploration.Generation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.IslandExploration
{
    // todo: once a per-world density graph is actually needed, this is the seam to swap for driving Terraxel's
    // NodeGraphProcessor-based graph instead of hand-emitting these two files — see Documentation for why that
    // wasn't done for this prototype (interactive-GUI-only authoring risk vs. this file's testable determinism).
    //
    // Hand-authored replacement for Terraxel's visual density graph output. Terraxel normally code-generates
    // Assets/Libs/Terraxel/Resources/Generated/TerraxelGenerated.{hlsl,cs} from a NodeGraphProcessor graph edited
    // in DensityGraphWindow; that graph is a single project-wide artifact (Noise2D.compute/Noise3D.compute both
    // #include the same fixed path), so there can only ever be one compiled density formula per project. This
    // tool regenerates that exact pair of files directly from a RoverIslandGenerationSettings asset, matching
    // the shape Terraxel's own ComputeTemplate.txt/ScriptTemplate.txt expect (a `finalNoise`/`GenerateDensity`
    // height function consumed by Terraxel's own unmodified isosurface/packing code).
    public static class RoverIslandDensityGenerator
    {
        private const string DefaultSettingsPath = "Assets/Game/DataAndSettings/IslandExploration/RoverIslandSettings.asset";
        private const string HlslPath = "Assets/Libs/Terraxel/Resources/Generated/TerraxelGenerated.hlsl";
        private const string CsPath = "Assets/Libs/Terraxel/Resources/Generated/TerraxelGenerated.cs";

        [MenuItem("Rover/Island/Regenerate Density")]
        public static void RegenerateFromMenu()
        {
            RoverIslandGenerationSettings settings = Selection.activeObject as RoverIslandGenerationSettings;
            if (settings == null)
                settings = AssetDatabase.LoadAssetAtPath<RoverIslandGenerationSettings>(DefaultSettingsPath);

            if (settings == null)
            {
                UnityEngine.Debug.LogError($"[RoverIslandDensityGenerator] No RoverIslandGenerationSettings found at '{DefaultSettingsPath}' and none selected.");
                return;
            }

            Regenerate(settings);
        }

        public static void Regenerate(RoverIslandGenerationSettings settings)
        {
            IslandFormula formula = IslandFormula.Bake(settings);

            File.WriteAllText(ToAbsolutePath(HlslPath), formula.BuildHlsl());
            File.WriteAllText(ToAbsolutePath(CsPath), formula.BuildCSharp());

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[RoverIslandDensityGenerator] Regenerated island density from '{settings.name}' (seed {settings.Seed}).");
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }

        // Baked (editor-time only) numeric literals for the formula below — see RoverIslandGenerationSettings
        // for the authored fields this derives from. Kept as a separate struct so the HLSL/C# emitters can't
        // drift out of sync with each other.
        private readonly struct IslandFormula
        {
            private readonly float _halfWidth;
            private readonly float _halfLength;
            private readonly float _centerX;
            private readonly float _centerZ;
            private readonly float _seaLevel;
            private readonly float _maxHeight;
            private readonly float _depthBelowSea;
            private readonly float _falloffLower;
            private readonly float _falloffUpper;
            private readonly float _falloffSharpness;
            private readonly float _warpStrength;
            private readonly float _baseFreq;
            private readonly int _baseOctaves;
            private readonly float _baseLacunarity;
            private readonly float _baseGain;
            private readonly float _mountainFreq;
            private readonly int _mountainOctaves;
            private readonly float _mountainAmplitude;
            private readonly float _fineFreq;
            private readonly float _fineAmplitude;
            private readonly float _warpFreq;

            private IslandFormula(RoverIslandGenerationSettings s)
            {
                _halfWidth = s.IslandWidth * 0.5f;
                _halfLength = s.IslandLength * 0.5f;
                _centerX = s.IslandCenter.x;
                _centerZ = s.IslandCenter.y;
                _seaLevel = s.SeaLevel;
                _maxHeight = s.MaxHeight;
                _depthBelowSea = s.DepthBelowSea;
                _falloffLower = 1f - s.FalloffDistanceFraction;
                _falloffUpper = 1f + s.FalloffDistanceFraction;
                _falloffSharpness = s.FalloffSharpness;
                _warpStrength = s.CoastlineWarpStrength;

                _baseFreq = 1f / s.NoiseScale;
                _baseOctaves = s.NoiseOctaves;
                _baseLacunarity = s.NoiseLacunarity;
                _baseGain = s.NoisePersistence;

                _mountainFreq = _baseFreq * 3f;
                _mountainOctaves = Mathf.Max(s.NoiseOctaves - 1, 2);
                _mountainAmplitude = s.MaxHeight * s.MountainStrength;

                _fineFreq = _baseFreq * 8f;
                _fineAmplitude = s.Roughness;

                _warpFreq = 1f / (Mathf.Max(s.IslandWidth, s.IslandLength) * 0.3f);
            }

            public static IslandFormula Bake(RoverIslandGenerationSettings s) => new(s);

            public string BuildHlsl()
            {
                return
$@"// Hand-authored by Game.Editor.IslandExploration.RoverIslandDensityGenerator — see that file for why.
// Regenerate via Rover/Island/Regenerate Density. Do not hand-edit; edit RoverIslandSettings.asset instead.
#pragma kernel CSMain
#include ""Assets/Libs/Terraxel/Shaders/FastNoiseLite/FastNoiseLite.hlsl""

int seed;

float noise(float2 worldPos, float amplitude, fnl_state props){{
    return fnlGetNoise2D(props, worldPos.x, worldPos.y) * amplitude;
}}

static const fnl_state props0 = fnlCreateState(seed, {F(_baseFreq)}, {_baseOctaves}, {F(_baseLacunarity)}, {F(_baseGain)});
static const fnl_state props1 = fnlCreateState(seed, {F(_mountainFreq)}, {_mountainOctaves}, {F(_baseLacunarity)}, {F(_baseGain)});
static const fnl_state props2 = fnlCreateState(seed, {F(_fineFreq)}, 2, 2.00000f, 0.50000f);
static const fnl_state props3 = fnlCreateState(seed, {F(_warpFreq)}, 2, 2.00000f, 0.50000f);

float finalNoise(int3 pos){{
    float2 worldPos2D = pos.xz;
    float op0 = noise(worldPos2D, {F(_maxHeight)}, props0);
    float op1 = noise(worldPos2D + float2(1000.0f, 1000.0f), {F(_mountainAmplitude)}, props1);
    float op2 = noise(worldPos2D + float2(2000.0f, 2000.0f), {F(_fineAmplitude)}, props2);
    float op3 = noise(worldPos2D + float2(3000.0f, 3000.0f), 1.0f, props3);

    float terrainHeight = op0 + op1 + op2;

    float dx = (worldPos2D.x - {F(_centerX)}) / {F(_halfWidth)};
    float dz = (worldPos2D.y - {F(_centerZ)}) / {F(_halfLength)};
    float ellipseDist = sqrt(dx * dx + dz * dz);
    float warpedDist = ellipseDist + op3 * {F(_warpStrength)};

    float islandMask = 1.0f - smoothstep({F(_falloffLower)}, {F(_falloffUpper)}, warpedDist);
    islandMask = saturate(islandMask);
    islandMask = pow(islandMask, {F(_falloffSharpness)});

    float value = {F(_seaLevel)} + islandMask * terrainHeight - (1.0f - islandMask) * {F(_depthBelowSea)};
    return value;
}}
";
            }

            public string BuildCSharp()
            {
                return
$@"using Unity.Collections;
using Unity.Mathematics;
using Terraxel.DataStructures;
using System.Runtime.CompilerServices;

// Hand-authored by Game.Editor.IslandExploration.RoverIslandDensityGenerator — see that file for why.
// Regenerate via Rover/Island/Regenerate Density. Do not hand-edit; edit RoverIslandSettings.asset instead.
public class TerraxelGenerated
{{
    static readonly FastNoiseLite props0 = new FastNoiseLite(1337, {F(_baseFreq)}, {_baseOctaves}, {F(_baseLacunarity)}, {F(_baseGain)});
    static readonly FastNoiseLite props1 = new FastNoiseLite(1337, {F(_mountainFreq)}, {_mountainOctaves}, {F(_baseLacunarity)}, {F(_baseGain)});
    static readonly FastNoiseLite props2 = new FastNoiseLite(1337, {F(_fineFreq)}, 2, 2.00000f, 0.50000f);
    static readonly FastNoiseLite props3 = new FastNoiseLite(1337, {F(_warpFreq)}, 2, 2.00000f, 0.50000f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GenerateDensity(float3 pos, int seed = 1337){{
        float2 pos2D = new float2(pos.x, pos.z);
        return DensityGenerator.HeightMapToIsosurface(pos, TerraxelGenerated.GenerateDensity(pos2D, seed));
    }}
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GenerateDensity(float2 pos2D, int seed = 1337){{
        float op0 = DensityGenerator.SurfaceNoise2D(pos2D, {F(_maxHeight)}, props0, seed);
        float op1 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(1000.0f, 1000.0f), {F(_mountainAmplitude)}, props1, seed);
        float op2 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(2000.0f, 2000.0f), {F(_fineAmplitude)}, props2, seed);
        float op3 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(3000.0f, 3000.0f), 1.0f, props3, seed);

        float terrainHeight = op0 + op1 + op2;

        float dx = (pos2D.x - {F(_centerX)}) / {F(_halfWidth)};
        float dz = (pos2D.y - {F(_centerZ)}) / {F(_halfLength)};
        float ellipseDist = math.sqrt(dx * dx + dz * dz);
        float warpedDist = ellipseDist + op3 * {F(_warpStrength)};

        float islandMask = 1.0f - math.smoothstep({F(_falloffLower)}, {F(_falloffUpper)}, warpedDist);
        islandMask = math.saturate(islandMask);
        islandMask = math.pow(islandMask, {F(_falloffSharpness)});

        float value = {F(_seaLevel)} + islandMask * terrainHeight - (1.0f - islandMask) * {F(_depthBelowSea)};
        return value;
    }}
}}
";
            }

            private static string F(float value) => value.ToString("0.00000", CultureInfo.InvariantCulture) + "f";
        }
    }
}
