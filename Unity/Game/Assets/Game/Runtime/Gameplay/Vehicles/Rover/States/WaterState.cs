using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.States
{
    public sealed class WaterState : IVehicleState
    {
        public VehicleStateId Id => VehicleStateId.Water;

        private readonly WaterModeConfig _config;
        private readonly BuoyancyProcessor _processor;

        private Vector3[] _samplePositions;
        private float _lastAverageSubmersion01;

        public WaterState(WaterModeConfig config, BuoyancyProcessor processor)
        {
            _config = config;
            _processor = processor;
        }

        public void Enter(VehicleRuntimeContext context)
        {
            Debug.Log("[WaterState] Enter");
            context.Rigidbody.useGravity = true;
            _samplePositions = new Vector3[context.BuoyancySamplePoints?.Length ?? 0];
            _lastAverageSubmersion01 = 0f;

            if (_samplePositions.Length == 0)
                Debug.LogError("[WaterState] BuoyancySamplePoints is empty — no buoyancy force can ever be produced.");
        }

        public void Exit(VehicleRuntimeContext context) => Debug.Log("[WaterState] Exit");

        public void Tick(VehicleRuntimeContext context, float deltaTime) { }

        public void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime)
        {
            if (_config == null)
            {
                Debug.LogError("[WaterState] _config (WaterModeConfig) is null — skipping physics this frame. Assign it on VehicleController.");
                return;
            }

            for (int i = 0; i < context.BuoyancySamplePoints.Length; i++)
                _samplePositions[i] = context.BuoyancySamplePoints[i].position;

            BuoyancyForceResult result = _processor.Solve(_samplePositions, context.OceanHeight, _config);
            _lastAverageSubmersion01 = result.AverageSubmersion01;

            foreach (PointForce pointForce in result.BuoyantForces)
            {
                if (pointForce.Force != Vector3.zero)
                    context.Rigidbody.AddForceAtPosition(pointForce.Force, pointForce.WorldPosition);
            }

            context.Rigidbody.linearDamping = result.ResolvedDrag;
            context.Rigidbody.angularDamping = result.ResolvedAngularDrag;

            Vector3 tangentForward = Vector3.ProjectOnPlane(context.Transform.forward, Vector3.up).normalized;
            Vector3 driveForce = tangentForward * (context.Input.Throttle * _config.DriveForce * result.ThrustMultiplier);
            Vector3 steerTorque = Vector3.up * (context.Input.Steer * _config.TurnTorque * result.ThrustMultiplier);

            context.Rigidbody.AddForce(driveForce);
            context.Rigidbody.AddTorque(steerTorque);

            context.Telemetry.Publish(new VehicleTelemetrySnapshot(
                Id,
                context.Rigidbody.linearVelocity.magnitude,
                0f,
                0f,
                result.AverageSubmersion01,
                Vector3.up,
                0f,
                false));
        }

        public bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context)
        {
            bool isSurfaced = _lastAverageSubmersion01 < _config.SurfacedThreshold;

            return next switch
            {
                VehicleStateId.Hover => isSurfaced && context.Probe.SampleGround(context.Transform.position, Vector3.down, _config.MaxGroundSnapDistance).Hit,
                VehicleStateId.Flight => isSurfaced,
                VehicleStateId.UnderWater => true,
                _ => false,
            };
        }
    }
}
