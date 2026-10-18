using System;
using System.Collections.Generic;
using Game.Gameplay.Vehicles.Rover.Config;
using Game.Gameplay.Vehicles.Rover.Physics;
using Game.Gameplay.Vehicles.Rover.States;
using Game.Gameplay.Vehicles.Rover.Telemetry;
using UnityEngine;

namespace Game.Gameplay.Vehicles.Rover.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [Header("Mode Configs")]
        [SerializeField] private HoverModeConfig _hoverConfig;
        [SerializeField] private FlightModeConfig _flightConfig;
        [SerializeField] private WaterModeConfig _waterConfig;
        [SerializeField] private UnderWaterModeConfig _underWaterConfig;

        [Header("Probes")]
        [SerializeField] private Transform[] _suspensionProbePoints;
        [SerializeField] private Transform[] _buoyancySamplePoints;
        [SerializeField] private LayerMask _terrainMask;
        [SerializeField] private LayerMask _liquidMask;

        public VehicleStateId CurrentStateId => _stateMachine.CurrentStateId;
        public VehicleTelemetrySnapshot LatestTelemetry => _telemetry.Snapshot;
        public IReadOnlyList<Transform> SuspensionProbePoints => _suspensionProbePoints;
        public IReadOnlyList<Transform> BuoyancySamplePoints => _buoyancySamplePoints;

        public event Action<VehicleStateId> StateChanged
        {
            add => _stateMachine.StateChanged += value;
            remove => _stateMachine.StateChanged -= value;
        }

        public event Action<VehicleTelemetrySnapshot> TelemetryChanged
        {
            add => _telemetry.Changed += value;
            remove => _telemetry.Changed -= value;
        }

        private Rigidbody _rigidbody;
        private VehicleRuntimeContext _context;
        private VehicleStateMachine _stateMachine;
        private VehicleTelemetry _telemetry;

        private int _fixedFrameCount;

        private void Awake()
        {
            Debug.Log($"[VehicleController] Awake start on '{name}'.");

            _rigidbody = GetComponent<Rigidbody>();
            Debug.Log($"[VehicleController] Rigidbody found: mass={_rigidbody.mass}, useGravity={_rigidbody.useGravity}, isKinematic={_rigidbody.isKinematic}.");

            LogConfigAssigned("_hoverConfig", _hoverConfig);
            LogConfigAssigned("_flightConfig", _flightConfig);
            LogConfigAssigned("_waterConfig", _waterConfig);
            LogConfigAssigned("_underWaterConfig", _underWaterConfig);

            int probeCount = _suspensionProbePoints?.Length ?? -1;
            int buoyancyCount = _buoyancySamplePoints?.Length ?? -1;
            Debug.Log($"[VehicleController] SuspensionProbePoints count={probeCount}, BuoyancySamplePoints count={buoyancyCount}.");
            if (_suspensionProbePoints != null)
            {
                for (int i = 0; i < _suspensionProbePoints.Length; i++)
                    Debug.Log($"[VehicleController]   probe[{i}] = {(_suspensionProbePoints[i] == null ? "NULL" : _suspensionProbePoints[i].name)}");
            }

            Debug.Log($"[VehicleController] _terrainMask bits={_terrainMask.value}, _liquidMask bits={_liquidMask.value}.");

            _telemetry = new VehicleTelemetry();

            IEnvironmentProbe probe = new RaycastEnvironmentProbe(_terrainMask, _liquidMask);
            IOceanHeightProvider oceanHeight = FindOceanHeightProvider();
            Debug.Log($"[VehicleController] Ocean height provider: {(oceanHeight == null ? "NONE" : oceanHeight.ToString())}");

            _context = new VehicleRuntimeContext(
                _rigidbody, transform, probe, oceanHeight, _telemetry,
                _suspensionProbePoints, _buoyancySamplePoints);

            IVehicleState[] states =
            {
                new HoverState(_hoverConfig, new HoverSuspensionProcessor()),
                new FlightState(_flightConfig, new FlightAerodynamicsProcessor()),
                new WaterState(_waterConfig, new BuoyancyProcessor()),
                new UnderWaterState(_underWaterConfig, new UnderwaterPropulsionProcessor(oceanHeight)),
            };

            _stateMachine = new VehicleStateMachine(_context, states, VehicleStateId.Hover);
            _stateMachine.StateChanged += state => Debug.Log($"[VehicleController] StateChanged -> {state}");

            Debug.Log($"[VehicleController] Awake complete. CurrentStateId={_stateMachine.CurrentStateId}.");
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _fixedFrameCount++;
            if (_fixedFrameCount % 50 == 0)
            {
                VehicleTelemetrySnapshot snap = _telemetry.Snapshot;
                Debug.Log($"[VehicleController] heartbeat frame={_fixedFrameCount} state={_stateMachine.CurrentStateId} speed={snap.Speed:F2} compression={snap.SuspensionCompression01:F2} pos={transform.position} input={_context.Input.Throttle:F2}/{_context.Input.Steer:F2}");
            }

            _stateMachine.FixedTick(Time.fixedDeltaTime);
        }

        public void SetInput(VehicleInputSample input)
        {
            _context.SetInput(input);
        }

        public bool EnterHoverMode()
        {
            Debug.Log("[VehicleController] EnterHoverMode requested.");
            return _stateMachine.RequestTransition(VehicleStateId.Hover);
        }

        public bool EnterFlightMode()
        {
            Debug.Log("[VehicleController] EnterFlightMode requested.");
            return _stateMachine.RequestTransition(VehicleStateId.Flight);
        }

        public bool EnterWaterMode()
        {
            Debug.Log("[VehicleController] EnterWaterMode requested.");
            return _stateMachine.RequestTransition(VehicleStateId.Water);
        }

        public bool EnterUnderWaterMode()
        {
            Debug.Log("[VehicleController] EnterUnderWaterMode requested.");
            return _stateMachine.RequestTransition(VehicleStateId.UnderWater);
        }

        private static void LogConfigAssigned(string fieldName, VehicleModeConfig config)
        {
            if (config == null)
                Debug.LogError($"[VehicleController] {fieldName} is NOT ASSIGNED (null)! That mode will throw a NullReferenceException on FixedTick.");
            else
                Debug.Log($"[VehicleController] {fieldName} = '{config.name}' (ModeId={config.ModeId}).");
        }

        private static IOceanHeightProvider FindOceanHeightProvider()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IOceanHeightProvider provider)
                    return provider;
            }

            Debug.LogWarning("[VehicleController] No IOceanHeightProvider found in the scene — UnderWater depth will read as 0.");
            return null;
        }
    }
}
