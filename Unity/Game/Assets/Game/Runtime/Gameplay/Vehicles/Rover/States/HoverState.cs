using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.States
{
    public sealed class HoverState : IVehicleState
    {
        public VehicleStateId Id => VehicleStateId.Hover;

        private readonly HoverModeConfig _config;
        private readonly HoverSuspensionProcessor _processor;

        private EnvironmentSample[] _probeSamples;
        private Vector3[] _probePositions;
        private int _frameCount;

        public HoverState(HoverModeConfig config, HoverSuspensionProcessor processor)
        {
            _config = config;
            _processor = processor;
        }

        public void Enter(VehicleRuntimeContext context)
        {
            Debug.Log("[HoverState] Enter");
            context.Rigidbody.useGravity = true;

            int probeCount = context.SuspensionProbePoints?.Length ?? 0;
            _probeSamples = new EnvironmentSample[probeCount];
            _probePositions = new Vector3[probeCount];

            if (probeCount == 0)
                Debug.LogError("[HoverState] SuspensionProbePoints is empty — no suspension force can ever be produced.");
        }

        public void Exit(VehicleRuntimeContext context) => Debug.Log("[HoverState] Exit");

        public void Tick(VehicleRuntimeContext context, float deltaTime) { }

        public void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime)
        {
            if (_config == null)
            {
                Debug.LogError("[HoverState] _config (HoverModeConfig) is null — skipping physics this frame. Assign it on VehicleController.");
                return;
            }

            _frameCount++;
            Transform vehicleTransform = context.Transform;
            float castDistance = _config.RestLength * 2f;

            for (int i = 0; i < context.SuspensionProbePoints.Length; i++)
            {
                Vector3 probePosition = context.SuspensionProbePoints[i].position;
                _probePositions[i] = probePosition;
                _probeSamples[i] = context.Probe.SampleGround(probePosition, -vehicleTransform.up, castDistance);
            }

            SuspensionForceResult result = _processor.Solve(_probeSamples, _probePositions, context.Rigidbody, vehicleTransform, context.Input, _config);

            if (_frameCount % 50 == 0)
            {
                int hitCount = 0;
                for (int i = 0; i < _probeSamples.Length; i++)
                {
                    if (_probeSamples[i].Hit)
                        hitCount++;
                }
                Debug.Log($"[HoverState] frame={_frameCount} grounded={result.AnyGrounded} hitProbes={hitCount}/{_probeSamples.Length} compression={result.AverageCompression01:F2} driveForce={result.DriveForce} input.Throttle={context.Input.Throttle}");
            }

            foreach (ProbeForce probeForce in result.ProbeForces)
            {
                if (probeForce.Force != Vector3.zero)
                    context.Rigidbody.AddForceAtPosition(probeForce.Force, probeForce.WorldPosition);
            }

            context.Rigidbody.AddTorque(result.AlignTorque);
            context.Rigidbody.AddForce(result.DriveForce);
            context.Rigidbody.AddTorque(result.SteerTorque);

            ClampSpeed(context.Rigidbody, _config.MaxSpeed);

            context.Telemetry.Publish(new VehicleTelemetrySnapshot(
                Id,
                context.Rigidbody.linearVelocity.magnitude,
                result.AverageCompression01,
                0f,
                0f,
                result.AverageNormal,
                0f,
                false));
        }

        public bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context)
        {
            return next switch
            {
                VehicleStateId.Flight => true,
                VehicleStateId.Water => context.Probe.SampleLiquid(context.Transform.position, 5f).Hit,
                _ => false,
            };
        }

        private static void ClampSpeed(Rigidbody rigidbody, float maxSpeed)
        {
            if (rigidbody.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
                rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * maxSpeed;
        }
    }
}
