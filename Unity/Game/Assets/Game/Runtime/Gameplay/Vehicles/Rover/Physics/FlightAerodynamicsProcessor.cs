using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public readonly struct AerodynamicForceResult
    {
        public readonly Vector3 Gravity;
        public readonly Vector3 Lift;
        public readonly Vector3 ControlTorque;
        public readonly Vector3 AutoLevelTorque;
        public readonly float LiftFactor01;

        public AerodynamicForceResult(Vector3 gravity, Vector3 lift, Vector3 controlTorque, Vector3 autoLevelTorque, float liftFactor01)
        {
            Gravity = gravity;
            Lift = lift;
            ControlTorque = controlTorque;
            AutoLevelTorque = autoLevelTorque;
            LiftFactor01 = liftFactor01;
        }
    }

    public sealed class FlightAerodynamicsProcessor
    {
        public AerodynamicForceResult Solve(Rigidbody rigidbody, Transform vehicleTransform, VehicleInputSample input, FlightModeConfig config)
        {
            Vector3 gravity = UnityEngine.Physics.gravity * config.GravityScale;

            float forwardSpeed = Vector3.Dot(rigidbody.linearVelocity, vehicleTransform.forward);
            float liftFactor01 = Mathf.InverseLerp(config.MinLiftSpeed, config.MaxLiftSpeed, forwardSpeed);
            liftFactor01 = Mathf.Clamp01(liftFactor01);
            Vector3 lift = vehicleTransform.up * (forwardSpeed * forwardSpeed * config.LiftCoefficient * liftFactor01);

            Vector3 localControlTorque = new Vector3(input.Pitch, input.Yaw, -input.Roll) * config.ControlTorqueStrength;
            Vector3 controlTorque = vehicleTransform.TransformDirection(localControlTorque);

            Vector3 uprightError = Vector3.Cross(vehicleTransform.up, Vector3.up);
            Vector3 autoLevelTorque = uprightError * config.AutoLevelStrength - rigidbody.angularVelocity * config.AngularDamping;

            return new AerodynamicForceResult(gravity, lift, controlTorque, autoLevelTorque, liftFactor01);
        }
    }
}
