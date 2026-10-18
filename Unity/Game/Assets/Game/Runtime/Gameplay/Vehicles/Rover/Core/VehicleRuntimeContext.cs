using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Core
{
    public sealed class VehicleRuntimeContext
    {
        public Rigidbody Rigidbody { get; }
        public Transform Transform { get; }
        public IEnvironmentProbe Probe { get; }
        public IOceanHeightProvider OceanHeight { get; }
        public VehicleTelemetry Telemetry { get; }
        public Transform[] SuspensionProbePoints { get; }
        public Transform[] BuoyancySamplePoints { get; }

        public VehicleInputSample Input { get; private set; }

        public VehicleRuntimeContext(
            Rigidbody rigidbody,
            Transform transform,
            IEnvironmentProbe probe,
            IOceanHeightProvider oceanHeight,
            VehicleTelemetry telemetry,
            Transform[] suspensionProbePoints,
            Transform[] buoyancySamplePoints)
        {
            Rigidbody = rigidbody;
            Transform = transform;
            Probe = probe;
            OceanHeight = oceanHeight;
            Telemetry = telemetry;
            SuspensionProbePoints = suspensionProbePoints;
            BuoyancySamplePoints = buoyancySamplePoints;
        }

        public void SetInput(VehicleInputSample input) => Input = input;
    }
}
