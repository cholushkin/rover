using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Config
{
    [CreateAssetMenu(menuName = "Rover/Configs/Water Mode Config", fileName = "RoverWaterConfig")]
    public sealed class WaterModeConfig : VehicleModeConfig
    {
        public override VehicleStateId ModeId => VehicleStateId.Water;

        [Header("Buoyancy")]
        [field: SerializeField] public float WaterDensity { get; private set; } = 1000f;
        [field: SerializeField] public float VolumePerSamplePoint { get; private set; } = 0.02f;
        [field: SerializeField] public float FullySubmergedDepth { get; private set; } = 0.6f;

        [Header("Drag")]
        [field: SerializeField] public float SurfaceDrag { get; private set; } = 0.2f;
        [field: SerializeField] public float UnderwaterDrag { get; private set; } = 3f;
        [field: SerializeField, Range(0f, 1f)] public float UnderwaterThrustMultiplier { get; private set; } = 0.5f;

        [Header("Drive")]
        [field: SerializeField] public float DriveForce { get; private set; } = 150f;
        [field: SerializeField] public float TurnTorque { get; private set; } = 100f;

        [Header("Transitions")]
        [field: SerializeField, Range(0f, 1f)] public float SurfacedThreshold { get; private set; } = 0.1f;
        [field: SerializeField] public float MaxGroundSnapDistance { get; private set; } = 3f;
    }
}
