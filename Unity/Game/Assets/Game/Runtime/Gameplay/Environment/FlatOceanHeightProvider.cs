using Game.Gameplay.Vehicles.Rover.Physics;
using UnityEngine;

namespace Game.Gameplay.Environment
{
    // todo: swap for a Gerstner-wave height query once the real ocean shader/system exists
    [DisallowMultipleComponent]
    public sealed class FlatOceanHeightProvider : MonoBehaviour, IOceanHeightProvider
    {
        public float GetSurfaceHeight(Vector3 worldPosition) => transform.position.y;
    }
}
