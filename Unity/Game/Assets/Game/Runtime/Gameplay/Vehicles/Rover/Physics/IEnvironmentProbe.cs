using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public interface IEnvironmentProbe
    {
        EnvironmentSample SampleGround(Vector3 origin, Vector3 direction, float maxDistance);
        EnvironmentSample SampleLiquid(Vector3 origin, float maxDistance);
    }
}
