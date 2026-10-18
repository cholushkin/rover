using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.States
{
    public sealed class FlightState : IVehicleState
    {
        public VehicleStateId Id => VehicleStateId.Flight;

        private readonly FlightModeConfig _config;
        private readonly FlightAerodynamicsProcessor _processor;

        public FlightState(FlightModeConfig config, FlightAerodynamicsProcessor processor)
        {
            _config = config;
            _processor = processor;
        }

        public void Enter(VehicleRuntimeContext context)
        {
            Debug.Log("[FlightState] Enter");
            context.Rigidbody.useGravity = false;
        }

        public void Exit(VehicleRuntimeContext context) => Debug.Log("[FlightState] Exit");

        public void Tick(VehicleRuntimeContext context, float deltaTime) { }

        public void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime)
        {
            if (_config == null)
            {
                Debug.LogError("[FlightState] _config (FlightModeConfig) is null — skipping physics this frame. Assign it on VehicleController.");
                return;
            }

            AerodynamicForceResult result = _processor.Solve(context.Rigidbody, context.Transform, context.Input, _config);

            context.Rigidbody.AddForce(result.Gravity, ForceMode.Acceleration);
            context.Rigidbody.AddForce(result.Lift);
            context.Rigidbody.AddTorque(result.ControlTorque);
            context.Rigidbody.AddTorque(result.AutoLevelTorque);

            context.Telemetry.Publish(new VehicleTelemetrySnapshot(
                Id,
                context.Rigidbody.linearVelocity.magnitude,
                0f,
                result.LiftFactor01,
                0f,
                context.Transform.up,
                0f,
                false));
        }

        public bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context)
        {
            return next switch
            {
                VehicleStateId.Hover => context.Probe.SampleGround(context.Transform.position, Vector3.down, _config.GroundSnapDistance).Hit,
                VehicleStateId.Water => context.Probe.SampleLiquid(context.Transform.position, _config.EntrySplashHeight).Hit,
                _ => false,
            };
        }
    }
}
