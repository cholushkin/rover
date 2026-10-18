using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.States
{
    public sealed class UnderWaterState : IVehicleState
    {
        public VehicleStateId Id => VehicleStateId.UnderWater;

        private readonly UnderWaterModeConfig _config;
        private readonly UnderwaterPropulsionProcessor _processor;

        private float _transformTimeRemaining;
        private float _lastDepthBelowSurface;

        public UnderWaterState(UnderWaterModeConfig config, UnderwaterPropulsionProcessor processor)
        {
            _config = config;
            _processor = processor;
        }

        public void Enter(VehicleRuntimeContext context)
        {
            Debug.Log("[UnderWaterState] Enter");
            context.Rigidbody.useGravity = false;
            context.Rigidbody.linearDamping = _config.LinearDrag;
            context.Rigidbody.angularDamping = _config.AngularDrag;
            _transformTimeRemaining = _config.TransformDuration;
        }

        public void Exit(VehicleRuntimeContext context) => Debug.Log("[UnderWaterState] Exit");

        public void Tick(VehicleRuntimeContext context, float deltaTime) { }

        public void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime)
        {
            if (_config == null)
            {
                Debug.LogError("[UnderWaterState] _config (UnderWaterModeConfig) is null — skipping physics this frame. Assign it on VehicleController.");
                return;
            }

            if (_transformTimeRemaining > 0f)
                _transformTimeRemaining -= fixedDeltaTime;

            bool isTransforming = _transformTimeRemaining > 0f;
            float controlAuthority01 = isTransforming ? 0.1f : 1f;

            SubmarinePropulsionResult result = _processor.Solve(
                context.Rigidbody, context.Transform, context.Probe, context.Input, controlAuthority01, _config);

            _lastDepthBelowSurface = result.DepthBelowSurface;

            context.Rigidbody.AddForce(result.Thrust);
            context.Rigidbody.AddTorque(result.ControlTorque);

            context.Telemetry.Publish(new VehicleTelemetrySnapshot(
                Id,
                context.Rigidbody.linearVelocity.magnitude,
                0f,
                0f,
                1f,
                Vector3.up,
                result.DepthBelowSurface,
                isTransforming));
        }

        public bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context)
        {
            return next == VehicleStateId.Water && _lastDepthBelowSurface <= _config.SurfaceEmergeThreshold;
        }
    }
}
