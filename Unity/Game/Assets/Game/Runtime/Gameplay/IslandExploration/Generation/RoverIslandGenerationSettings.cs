using UnityEngine;

namespace Game.Gameplay.IslandExploration.Generation
{
    // todo: expose CaveNoiseStrength once true 3D (non-heightmap-derived) density is needed for overhangs/caves
    // todo: IslandCenter is fixed at the world origin for this single-island prototype; keep it a real field (not a constant) so multi-island placement is a config change later, not a rewrite
    [CreateAssetMenu(menuName = "Rover/Island/Island Generation Settings", fileName = "RoverIslandSettings")]
    public sealed class RoverIslandGenerationSettings : ScriptableObject
    {
        [Header("Seed (live at runtime via TerraxelWorldSettings.seed)")]
        [field: SerializeField] public int Seed { get; private set; } = 12345;

        [Header("Island Footprint — baked by Rover/Island/Regenerate Density")]
        [field: SerializeField, Min(64f)] public float IslandWidth { get; private set; } = 2048f;
        [field: SerializeField, Min(64f)] public float IslandLength { get; private set; } = 2048f;
        [field: SerializeField] public Vector2 IslandCenter { get; private set; } = Vector2.zero;

        [Header("Vertical Shape")]
        [field: SerializeField] public float SeaLevel { get; private set; } = 0f;
        [field: SerializeField, Min(1f)] public float MaxHeight { get; private set; } = 220f;
        [field: SerializeField, Min(1f)] public float DepthBelowSea { get; private set; } = 300f;

        [Header("Coastline Falloff")]
        [field: SerializeField, Range(0.01f, 0.95f)] public float FalloffDistanceFraction { get; private set; } = 0.18f;
        [field: SerializeField, Range(0.25f, 4f)] public float FalloffSharpness { get; private set; } = 1.4f;
        [field: SerializeField, Range(0f, 0.5f)] public float CoastlineWarpStrength { get; private set; } = 0.22f;

        [Header("Terrain Noise")]
        [field: SerializeField, Min(16f)] public float NoiseScale { get; private set; } = 420f;
        [field: SerializeField, Range(1, 8)] public int NoiseOctaves { get; private set; } = 4;
        [field: SerializeField, Range(0.1f, 0.9f)] public float NoisePersistence { get; private set; } = 0.45f;
        [field: SerializeField, Range(1.5f, 3.5f)] public float NoiseLacunarity { get; private set; } = 2.1f;

        [Header("Mountains & Detail")]
        [field: SerializeField, Range(0f, 2f)] public float MountainStrength { get; private set; } = 0.7f;
        [field: SerializeField, Range(0f, 25f)] public float Roughness { get; private set; } = 6f;

        private void OnValidate()
        {
            if (IslandWidth < 64f)
                IslandWidth = 64f;
            if (IslandLength < 64f)
                IslandLength = 64f;
            if (MaxHeight < 1f)
                MaxHeight = 1f;
            if (DepthBelowSea < 1f)
                DepthBelowSea = 1f;
        }
    }
}
