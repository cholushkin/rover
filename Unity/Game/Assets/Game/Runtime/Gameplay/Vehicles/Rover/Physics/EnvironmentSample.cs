using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Physics
{
    public readonly struct EnvironmentSample
    {
        public readonly bool Hit;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly float Distance;
        public readonly bool IsLiquid;

        public EnvironmentSample(bool hit, Vector3 point, Vector3 normal, float distance, bool isLiquid)
        {
            Hit = hit;
            Point = point;
            Normal = normal;
            Distance = distance;
            IsLiquid = isLiquid;
        }

        public static EnvironmentSample Miss => default;
    }
}
