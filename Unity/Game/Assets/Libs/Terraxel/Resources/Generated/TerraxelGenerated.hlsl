// Hand-authored by Game.Editor.IslandExploration.RoverIslandDensityGenerator — see that file for why.
// Regenerate via Rover/Island/Regenerate Density. Do not hand-edit; edit RoverIslandSettings.asset instead.
#pragma kernel CSMain
#include "Assets/Libs/Terraxel/Shaders/FastNoiseLite/FastNoiseLite.hlsl"

int seed;

float noise(float2 worldPos, float amplitude, fnl_state props){
    return fnlGetNoise2D(props, worldPos.x, worldPos.y) * amplitude;
}

static const fnl_state props0 = fnlCreateState(seed, 0.00238f, 4, 2.10000f, 0.45000f);
static const fnl_state props1 = fnlCreateState(seed, 0.00714f, 3, 2.10000f, 0.45000f);
static const fnl_state props2 = fnlCreateState(seed, 0.01905f, 2, 2.00000f, 0.50000f);
static const fnl_state props3 = fnlCreateState(seed, 0.00163f, 2, 2.00000f, 0.50000f);

float finalNoise(int3 pos){
    float2 worldPos2D = pos.xz;
    float op0 = noise(worldPos2D, 220.00000f, props0);
    float op1 = noise(worldPos2D + float2(1000.0f, 1000.0f), 154.00000f, props1);
    float op2 = noise(worldPos2D + float2(2000.0f, 2000.0f), 6.00000f, props2);
    float op3 = noise(worldPos2D + float2(3000.0f, 3000.0f), 1.0f, props3);

    float terrainHeight = op0 + op1 + op2;

    float dx = (worldPos2D.x - 0.00000f) / 1024.00000f;
    float dz = (worldPos2D.y - 0.00000f) / 1024.00000f;
    float ellipseDist = sqrt(dx * dx + dz * dz);
    float warpedDist = ellipseDist + op3 * 0.22000f;

    float islandMask = 1.0f - smoothstep(0.82000f, 1.18000f, warpedDist);
    islandMask = saturate(islandMask);
    islandMask = pow(islandMask, 1.40000f);

    float value = 0.00000f + islandMask * terrainHeight - (1.0f - islandMask) * 300.00000f;
    return value;
}
