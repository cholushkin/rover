using System;
using Game.Gameplay.Vehicles.Rover.Config;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public readonly struct PointForce
    {
        public readonly Vector3 WorldPosition;
        public readonly Vector3 Force;

        public PointForce(Vector3 worldPosition, Vector3 force)
        {
            WorldPosition = worldPosition;
            Force = force;
        }
    }

    public readonly struct BuoyancyForceResult
    {
        public readonly PointForce[] BuoyantForces;
        public readonly float AverageSubmersion01;
        public readonly float ResolvedDrag;
        public readonly float ResolvedAngularDrag;
        public readonly float ThrustMultiplier;

        public BuoyancyForceResult(PointForce[] buoyantForces, float averageSubmersion01, float resolvedDrag, float resolvedAngularDrag, float thrustMultiplier)
        {
            BuoyantForces = buoyantForces;
            AverageSubmersion01 = averageSubmersion01;
            ResolvedDrag = resolvedDrag;
            ResolvedAngularDrag = resolvedAngularDrag;
            ThrustMultiplier = thrustMultiplier;
        }
    }

    public sealed class BuoyancyProcessor
    {
        public BuoyancyForceResult Solve(
            ReadOnlySpan<Vector3> samplePoints,
            IOceanHeightProvider oceanHeight,
            WaterModeConfig config)
        {
            var forces = new PointForce[samplePoints.Length];
            float submersionSum = 0f;

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 point = samplePoints[i];
                float surfaceHeight = oceanHeight?.GetSurfaceHeight(point) ?? point.y;

                float submersionDepth = surfaceHeight - point.y;
                float submersion01 = Mathf.Clamp01(submersionDepth / config.FullySubmergedDepth);

                float buoyantMagnitude = submersion01 * config.WaterDensity * -UnityEngine.Physics.gravity.y * config.VolumePerSamplePoint;
                forces[i] = new PointForce(point, Vector3.up * buoyantMagnitude);

                submersionSum += submersion01;
            }

            float averageSubmersion01 = samplePoints.Length > 0 ? submersionSum / samplePoints.Length : 0f;

            float resolvedDrag = Mathf.Lerp(config.SurfaceDrag, config.UnderwaterDrag, averageSubmersion01);
            float thrustMultiplier = Mathf.Lerp(1f, config.UnderwaterThrustMultiplier, averageSubmersion01);

            return new BuoyancyForceResult(forces, averageSubmersion01, resolvedDrag, resolvedDrag, thrustMultiplier);
        }
    }
}
