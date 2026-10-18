using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public readonly struct SubmarinePropulsionResult
    {
        public readonly Vector3 Thrust;
        public readonly Vector3 ControlTorque;
        public readonly float DepthBelowSurface;
        public readonly float CrushRatio01;
        public readonly bool ObstacleAhead;

        public SubmarinePropulsionResult(Vector3 thrust, Vector3 controlTorque, float depthBelowSurface, float crushRatio01, bool obstacleAhead)
        {
            Thrust = thrust;
            ControlTorque = controlTorque;
            DepthBelowSurface = depthBelowSurface;
            CrushRatio01 = crushRatio01;
            ObstacleAhead = obstacleAhead;
        }
    }

    public sealed class UnderwaterPropulsionProcessor
    {
        private readonly IOceanHeightProvider _oceanHeight;

        public UnderwaterPropulsionProcessor(IOceanHeightProvider oceanHeight)
        {
            _oceanHeight = oceanHeight;
        }

        public SubmarinePropulsionResult Solve(
            Rigidbody rigidbody,
            Transform vehicleTransform,
            IEnvironmentProbe probe,
            VehicleInputSample input,
            float controlAuthority01,
            UnderWaterModeConfig config)
        {
            float surfaceHeight = _oceanHeight?.GetSurfaceHeight(vehicleTransform.position) ?? vehicleTransform.position.y;
            float depthBelowSurface = Mathf.Max(0f, surfaceHeight - vehicleTransform.position.y);
            float crushRatio01 = Mathf.Clamp01(depthBelowSurface / config.MaxDiveDepth);

            Vector3 forwardThrust = vehicleTransform.forward * (input.Throttle * config.ThrustForce * controlAuthority01);
            Vector3 verticalThrust = vehicleTransform.up * (input.Ascend * config.AscendThrust * controlAuthority01);
            Vector3 thrust = forwardThrust + verticalThrust;

            Vector3 localControlTorque = new Vector3(input.Pitch, input.Yaw, -input.Roll) * (config.ControlTorqueStrength * controlAuthority01);
            Vector3 controlTorque = vehicleTransform.TransformDirection(localControlTorque);

            EnvironmentSample obstacleSample = probe.SampleGround(vehicleTransform.position, vehicleTransform.forward, config.ObstacleWarningDistance);
            bool obstacleAhead = obstacleSample.Hit;

            return new SubmarinePropulsionResult(thrust, controlTorque, depthBelowSurface, crushRatio01, obstacleAhead);
        }
    }
}
