namespace Game.Gameplay.Vehicles.Rover.Core
{
    public readonly struct VehicleInputSample
    {
        public readonly float Throttle;
        public readonly float Steer;
        public readonly float Pitch;
        public readonly float Roll;
        public readonly float Yaw;
        public readonly float Ascend;
        public readonly float Boost;

        public VehicleInputSample(float throttle, float steer, float pitch, float roll, float yaw, float ascend, float boost)
        {
            Throttle = throttle;
            Steer = steer;
            Pitch = pitch;
            Roll = roll;
            Yaw = yaw;
            Ascend = ascend;
            Boost = boost;
        }

        public static VehicleInputSample None => default;
    }
}
