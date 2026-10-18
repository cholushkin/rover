using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    // note: calls into UnityEngine.Physics are fully qualified throughout this file —
    // this namespace is itself named "Physics", which would otherwise shadow the engine type.
    public sealed class RaycastEnvironmentProbe : IEnvironmentProbe
    {
        private readonly LayerMask _terrainMask;
        private readonly LayerMask _liquidMask;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[1];

        public RaycastEnvironmentProbe(LayerMask terrainMask, LayerMask liquidMask)
        {
            _terrainMask = terrainMask;
            _liquidMask = liquidMask;
        }

        public EnvironmentSample SampleGround(Vector3 origin, Vector3 direction, float maxDistance)
        {
            int hitCount = UnityEngine.Physics.RaycastNonAlloc(origin, direction, _hitBuffer, maxDistance, _terrainMask);
            if (hitCount <= 0)
                return EnvironmentSample.Miss;

            RaycastHit hit = _hitBuffer[0];
            return new EnvironmentSample(true, hit.point, hit.normal, hit.distance, false);
        }

        public EnvironmentSample SampleLiquid(Vector3 origin, float maxDistance)
        {
            int hitCount = UnityEngine.Physics.RaycastNonAlloc(origin, Vector3.down, _hitBuffer, maxDistance, _liquidMask);
            if (hitCount <= 0)
                hitCount = UnityEngine.Physics.RaycastNonAlloc(origin, Vector3.up, _hitBuffer, maxDistance, _liquidMask);
            if (hitCount <= 0)
                return EnvironmentSample.Miss;

            RaycastHit hit = _hitBuffer[0];
            return new EnvironmentSample(true, hit.point, hit.normal, hit.distance, true);
        }
    }
}
