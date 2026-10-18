using Game.Gameplay.Vehicles.Rover.Core;
using UnityEngine;

namespace Game.Debugging
{
    [DisallowMultipleComponent]
    public sealed class RoverDebugGizmos : MonoBehaviour
    {
        [SerializeField] private VehicleController _controller;
        [SerializeField] private float _probeRayLength = 2f;

        private void OnDrawGizmosSelected()
        {
            if (_controller == null)
                return;

            Gizmos.color = Color.yellow;
            foreach (Transform probe in _controller.SuspensionProbePoints)
            {
                if (probe == null)
                    continue;

                Gizmos.DrawLine(probe.position, probe.position - probe.up * _probeRayLength);
                Gizmos.DrawSphere(probe.position, 0.08f);
            }

            Gizmos.color = Color.blue;
            foreach (Transform buoyancyPoint in _controller.BuoyancySamplePoints)
            {
                if (buoyancyPoint == null)
                    continue;

                Gizmos.DrawSphere(buoyancyPoint.position, 0.1f);
            }
        }
    }
}
