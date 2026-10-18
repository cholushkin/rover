using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Telemetry
{
    public readonly struct VehicleTelemetrySnapshot
    {
        public readonly VehicleStateId StateId;
        public readonly float Speed;
        public readonly float SuspensionCompression01;
        public readonly float LiftFactor01;
        public readonly float SubmersionFraction01;
        public readonly Vector3 SurfaceNormal;
        public readonly float DepthBelowSurface;
        public readonly bool IsTransforming;

        public VehicleTelemetrySnapshot(
            VehicleStateId stateId,
            float speed,
            float suspensionCompression01,
            float liftFactor01,
            float submersionFraction01,
            Vector3 surfaceNormal,
            float depthBelowSurface,
            bool isTransforming)
        {
            StateId = stateId;
            Speed = speed;
            SuspensionCompression01 = suspensionCompression01;
            LiftFactor01 = liftFactor01;
            SubmersionFraction01 = submersionFraction01;
            SurfaceNormal = surfaceNormal;
            DepthBelowSurface = depthBelowSurface;
            IsTransforming = isTransforming;
        }

        public static VehicleTelemetrySnapshot Empty => new(VehicleStateId.Hover, 0f, 0f, 0f, 0f, Vector3.up, 0f, false);
    }
}
