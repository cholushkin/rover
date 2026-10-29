using Unity.Collections;
using Unity.Mathematics;
using Terraxel.DataStructures;
using System.Runtime.CompilerServices;

// Hand-authored by Game.Editor.IslandExploration.RoverIslandDensityGenerator — see that file for why.
// Regenerate via Rover/Island/Regenerate Density. Do not hand-edit; edit RoverIslandSettings.asset instead.
public class TerraxelGenerated
{
    static readonly FastNoiseLite props0 = new FastNoiseLite(1337, 0.00238f, 4, 2.10000f, 0.45000f);
    static readonly FastNoiseLite props1 = new FastNoiseLite(1337, 0.00714f, 3, 2.10000f, 0.45000f);
    static readonly FastNoiseLite props2 = new FastNoiseLite(1337, 0.01905f, 2, 2.00000f, 0.50000f);
    static readonly FastNoiseLite props3 = new FastNoiseLite(1337, 0.00163f, 2, 2.00000f, 0.50000f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GenerateDensity(float3 pos, int seed = 1337){
        float2 pos2D = new float2(pos.x, pos.z);
        return DensityGenerator.HeightMapToIsosurface(pos, TerraxelGenerated.GenerateDensity(pos2D, seed));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GenerateDensity(float2 pos2D, int seed = 1337){
        float op0 = DensityGenerator.SurfaceNoise2D(pos2D, 220.00000f, props0, seed);
        float op1 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(1000.0f, 1000.0f), 154.00000f, props1, seed);
        float op2 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(2000.0f, 2000.0f), 6.00000f, props2, seed);
        float op3 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(3000.0f, 3000.0f), 1.0f, props3, seed);

        float terrainHeight = op0 + op1 + op2;

        float dx = (pos2D.x - 0.00000f) / 1024.00000f;
        float dz = (pos2D.y - 0.00000f) / 1024.00000f;
        float ellipseDist = math.sqrt(dx * dx + dz * dz);
        float warpedDist = ellipseDist + op3 * 0.22000f;

        float islandMask = 1.0f - math.smoothstep(0.82000f, 1.18000f, warpedDist);
        islandMask = math.saturate(islandMask);
        islandMask = math.pow(islandMask, 1.40000f);

        float value = 0.00000f + islandMask * terrainHeight - (1.0f - islandMask) * 300.00000f;
        return value;
    }
}
