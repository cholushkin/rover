using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Config
{
    public abstract class VehicleModeConfig : ScriptableObject
    {
        public abstract VehicleStateId ModeId { get; }

        [field: SerializeField] public string DisplayName { get; private set; } = string.Empty;
    }
}
