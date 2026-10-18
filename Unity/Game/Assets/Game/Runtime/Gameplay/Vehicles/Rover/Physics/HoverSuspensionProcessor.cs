using System;
using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public readonly struct ProbeForce
    {
        public readonly Vector3 WorldPosition;
        public readonly Vector3 Force;

        public ProbeForce(Vector3 worldPosition, Vector3 force)
        {
            WorldPosition = worldPosition;
            Force = force;
        }
    }

    public readonly struct SuspensionForceResult
    {
        public readonly Vector3 AverageNormal;
        public readonly float AverageCompression01;
        public readonly ProbeForce[] ProbeForces;
        public readonly Vector3 AlignTorque;
        public readonly Vector3 DriveForce;
        public readonly Vector3 SteerTorque;
        public readonly bool AnyGrounded;

        public SuspensionForceResult(
            Vector3 averageNormal, float averageCompression01, ProbeForce[] probeForces,
            Vector3 alignTorque, Vector3 driveForce, Vector3 steerTorque, bool anyGrounded)
        {
            AverageNormal = averageNormal;
            AverageCompression01 = averageCompression01;
            ProbeForces = probeForces;
            AlignTorque = alignTorque;
            DriveForce = driveForce;
            SteerTorque = steerTorque;
            AnyGrounded = anyGrounded;
        }
    }

    public sealed class HoverSuspensionProcessor
    {
        public SuspensionForceResult Solve(
            ReadOnlySpan<EnvironmentSample> probeSamples,
            ReadOnlySpan<Vector3> probeWorldPositions,
            Rigidbody rigidbody,
            Transform vehicleTransform,
            VehicleInputSample input,
            HoverModeConfig config)
        {
            var probeForces = new ProbeForce[probeSamples.Length];
            Vector3 normalSum = Vector3.zero;
            float compressionSum = 0f;
            int groundedCount = 0;

            for (int i = 0; i < probeSamples.Length; i++)
            {
                EnvironmentSample sample = probeSamples[i];
                if (!sample.Hit)
                {
                    probeForces[i] = new ProbeForce(probeWorldPositions[i], Vector3.zero);
                    continue;
                }

                float compression01 = Mathf.Clamp01((config.RestLength - sample.Distance) / config.RestLength);
                float springForce = compression01 * config.SpringStrength;

                Vector3 pointVelocity = rigidbody.GetPointVelocity(probeWorldPositions[i]);
                float relativeVelocity = Vector3.Dot(pointVelocity, sample.Normal);
                float damperForce = relativeVelocity * config.DamperStrength;

                float forceMagnitude = Mathf.Max(0f, springForce - damperForce);
                probeForces[i] = new ProbeForce(probeWorldPositions[i], sample.Normal * forceMagnitude);

                normalSum += sample.Normal;
                compressionSum += compression01;
                groundedCount++;
            }

            if (groundedCount == 0)
            {
                return new SuspensionForceResult(vehicleTransform.up, 0f, probeForces, Vector3.zero, Vector3.zero, Vector3.zero, false);
            }

            Vector3 averageNormal = (normalSum / groundedCount).normalized;
            float averageCompression01 = compressionSum / groundedCount;

            float slopeAngle = Vector3.Angle(averageNormal, Vector3.up);
            float grip01 = config.GripBySlopeAngle.Evaluate(slopeAngle);

            Vector3 rotationError = Vector3.Cross(vehicleTransform.up, averageNormal);
            Vector3 alignTorque = rotationError * config.AlignStrength - rigidbody.angularVelocity * config.AlignDamping;

            Vector3 tangentForward = Vector3.ProjectOnPlane(vehicleTransform.forward, averageNormal).normalized;
            Vector3 driveForce = tangentForward * (input.Throttle * config.DriveForce * grip01);
            Vector3 steerTorque = averageNormal * (input.Steer * config.TurnTorque * grip01);

            return new SuspensionForceResult(averageNormal, averageCompression01, probeForces, alignTorque, driveForce, steerTorque, true);
        }
    }
}
