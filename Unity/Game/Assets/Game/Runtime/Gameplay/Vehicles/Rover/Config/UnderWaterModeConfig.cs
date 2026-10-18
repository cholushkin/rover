using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Config
{
    [CreateAssetMenu(menuName = "Rover/Configs/UnderWater Mode Config", fileName = "RoverUnderWaterConfig")]
    public sealed class UnderWaterModeConfig : VehicleModeConfig
    {
        public override VehicleStateId ModeId => VehicleStateId.UnderWater;

        [Header("Propulsion")]
        [field: SerializeField] public float ThrustForce { get; private set; } = 150f;
        [field: SerializeField] public float AscendThrust { get; private set; } = 120f;
        [field: SerializeField] public float ControlTorqueStrength { get; private set; } = 120f;
        [field: SerializeField] public float AngularDrag { get; private set; } = 4f;
        [field: SerializeField] public float LinearDrag { get; private set; } = 2.5f;

        [Header("Depth")]
        [field: SerializeField] public float MaxDiveDepth { get; private set; } = 60f;
        [field: SerializeField, Range(0f, 1f)] public float CrushWarningDepth01 { get; private set; } = 0.8f;
        [field: SerializeField] public float ObstacleWarningDistance { get; private set; } = 10f;

        [Header("Transformation")]
        [field: SerializeField] public float TransformDuration { get; private set; } = 1.2f;
        [field: SerializeField] public float SurfaceEmergeThreshold { get; private set; } = 0.5f;

        [Header("Presentation Thresholds")]
        [field: SerializeField] public float HeadlightActivationDepth { get; private set; } = 8f;
    }
}
