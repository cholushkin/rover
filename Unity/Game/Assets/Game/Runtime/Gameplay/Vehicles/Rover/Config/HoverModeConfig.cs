using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Config
{
    [CreateAssetMenu(menuName = "Rover/Configs/Hover Mode Config", fileName = "RoverHoverConfig")]
    public sealed class HoverModeConfig : VehicleModeConfig
    {
        public override VehicleStateId ModeId => VehicleStateId.Hover;

        [Header("Suspension")]
        [field: SerializeField, Range(0.1f, 5f)] public float RestLength { get; private set; } = 1.2f;
        [field: SerializeField] public float SpringStrength { get; private set; } = 500f;
        [field: SerializeField] public float DamperStrength { get; private set; } = 80f;

        [Header("Orientation")]
        [field: SerializeField] public float AlignStrength { get; private set; } = 150f;
        [field: SerializeField] public float AlignDamping { get; private set; } = 20f;

        [Header("Drive")]
        [field: SerializeField] public float DriveForce { get; private set; } = 220f;
        [field: SerializeField] public float TurnTorque { get; private set; } = 150f;
        [field: SerializeField] public float MaxSpeed { get; private set; } = 30f;

        [Header("Surface Grip")]
        [field: SerializeField] public AnimationCurve GripBySlopeAngle { get; private set; }
            = AnimationCurve.Linear(0f, 1f, 85f, 0f);

        [Header("Transitions")]
        [field: SerializeField] public float MaxGroundSnapDistance { get; private set; } = 3f;
    }
}
