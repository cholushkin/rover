using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Config
{
    [CreateAssetMenu(menuName = "Rover/Configs/Flight Mode Config", fileName = "RoverFlightConfig")]
    public sealed class FlightModeConfig : VehicleModeConfig
    {
        public override VehicleStateId ModeId => VehicleStateId.Flight;

        [Header("Gravity & Lift")]
        [field: SerializeField, Range(0f, 1f)] public float GravityScale { get; private set; } = 0.3f;
        [field: SerializeField] public float LiftCoefficient { get; private set; } = 0.4f;
        [field: SerializeField] public float MinLiftSpeed { get; private set; } = 5f;
        [field: SerializeField] public float MaxLiftSpeed { get; private set; } = 25f;

        [Header("Control")]
        [field: SerializeField] public float ControlTorqueStrength { get; private set; } = 150f;
        [field: SerializeField] public float AngularDamping { get; private set; } = 3f;
        [field: SerializeField] public float AutoLevelStrength { get; private set; } = 80f;

        [Header("Presentation Thresholds")]
        [field: SerializeField, Range(0f, 1f)] public float WingDeployLiftThreshold { get; private set; } = 0.15f;

        [Header("Transitions")]
        [field: SerializeField] public float EntrySplashHeight { get; private set; } = 2f;
        [field: SerializeField] public float GroundSnapDistance { get; private set; } = 3f;
    }
}
