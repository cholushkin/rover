using System;

namespace Game.Gameplay.Vehicles.Rover.Telemetry
{
    // todo: swap for an R3 ReactiveProperty once the module adopts the project's reactive stack
    public sealed class VehicleTelemetry
    {
        public event Action<VehicleTelemetrySnapshot> Changed;

        public VehicleTelemetrySnapshot Snapshot { get; private set; } = VehicleTelemetrySnapshot.Empty;

        public void Publish(VehicleTelemetrySnapshot snapshot)
        {
            Snapshot = snapshot;
            Changed?.Invoke(snapshot);
        }
    }
}
