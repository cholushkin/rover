using Game.Gameplay.Vehicles.Rover.Core;

namespace Game.Gameplay.Vehicles.Rover.States
{
    public interface IVehicleState
    {
        VehicleStateId Id { get; }

        void Enter(VehicleRuntimeContext context);
        void Exit(VehicleRuntimeContext context);

        void Tick(VehicleRuntimeContext context, float deltaTime);
        void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime);

        bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context);
    }
}
