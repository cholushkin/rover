using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public interface IOceanHeightProvider
    {
        float GetSurfaceHeight(Vector3 worldPosition);
    }
}
