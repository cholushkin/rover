# 02 — State Machine & Context Setup

## `IVehicleState`

```csharp
namespace Game.Gameplay.Vehicles.Rover.States;

public interface IVehicleState
{
    VehicleStateId Id { get; }

    void Enter(VehicleRuntimeContext context);
    void Exit(VehicleRuntimeContext context);

    void Tick(VehicleRuntimeContext context, float deltaTime);
    void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime);

    bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context);
}
```

- `Enter`/`Exit` toggle mode-specific `Rigidbody` settings (`useGravity`, `drag`, `angularDrag`, collider trigger flags) and reset per-mode accumulators (e.g. wing deploy timer). They must be idempotent and cheap — no allocation.
- `Tick` runs from `VehicleController.Update()` for non-physics work (animation-adjacent timers, input-dependent auto-transition checks specific to that mode).
- `FixedTick` runs from `VehicleController.FixedUpdate()` and is where probing + processor + force application happens (doc 03).
- `CanTransitionTo` lets a state veto leaving itself in an invalid moment (e.g. `WaterState` refuses to hand off to `FlightState` while still mostly submerged — see the transition table below).

## `VehicleStateMachine`

A plain class, not a `MonoBehaviour` — owned and driven by `VehicleController`.

```csharp
namespace Game.Gameplay.Vehicles.Rover.Core;

public sealed class VehicleStateMachine
{
    public VehicleStateId CurrentStateId => _current.Id;
    public ReadOnlyReactiveProperty<VehicleStateId> CurrentState => _currentState;

    private readonly Dictionary<VehicleStateId, IVehicleState> _states;
    private readonly VehicleRuntimeContext _context;
    private readonly ReactiveProperty<VehicleStateId> _currentState;

    private IVehicleState _current;
    private VehicleStateId? _pendingTransition;

    public VehicleStateMachine(VehicleRuntimeContext context, IVehicleState[] states, VehicleStateId initial)
    {
        _context = context;
        _states = states.ToDictionary(s => s.Id);
        _current = _states[initial];
        _currentState = new ReactiveProperty<VehicleStateId>(initial);
        _current.Enter(_context);
    }

    public bool RequestTransition(VehicleStateId next)
    {
        if (next == _current.Id)
            return false;
        if (!_current.CanTransitionTo(next, _context))
            return false;

        _pendingTransition = next;
        return true;
    }

    public void Tick(float deltaTime) => _current.Tick(_context, deltaTime);

    public void FixedTick(float fixedDeltaTime)
    {
        ApplyPendingTransition();
        _current.FixedTick(_context, fixedDeltaTime);
    }

    private void ApplyPendingTransition()
    {
        if (_pendingTransition is not { } next)
            return;

        _current.Exit(_context);
        _current = _states[next];
        _current.Enter(_context);
        _currentState.Value = next;
        _pendingTransition = null;
    }
}
```

**Why transitions apply at the top of `FixedTick` and not immediately on request:** a request can arrive mid-`Update()` (player input) or mid-`FixedUpdate()` (an automatic condition evaluated by the current state). Deferring the swap to a single, well-defined point guarantees `Exit`/`Enter` never run in the middle of a physics step and that exactly one transition is ever "in flight" — a second `RequestTransition` call before the pending one applies simply overwrites `_pendingTransition`, it never queues or stacks.

## `VehicleController`

```csharp
namespace Game.Gameplay.Vehicles.Rover.Core;

[RequireComponent(typeof(Rigidbody))]
public sealed class VehicleController : MonoBehaviour
{
    [SerializeField] private HoverModeConfig _hoverConfig;
    [SerializeField] private FlightModeConfig _flightConfig;
    [SerializeField] private WaterModeConfig _waterConfig;
    [SerializeField] private UnderWaterModeConfig _underWaterConfig;
    [SerializeField] private Transform[] _suspensionProbePoints;
    [SerializeField] private Transform[] _buoyancySamplePoints;
    [SerializeField] private LayerMask _terrainMask;
    [SerializeField] private LayerMask _liquidMask;

    public ReadOnlyReactiveProperty<VehicleTelemetrySnapshot> Telemetry => _telemetry.Snapshot;
    public ReadOnlyReactiveProperty<VehicleStateId> CurrentState => _stateMachine.CurrentState;

    private Rigidbody _rigidbody;
    private VehicleRuntimeContext _context;
    private VehicleStateMachine _stateMachine;
    private VehicleTelemetry _telemetry;
    private VehicleInputSample _pendingInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _telemetry = new VehicleTelemetry();

        IEnvironmentProbe probe = new RaycastEnvironmentProbe(_terrainMask, _liquidMask);
        IOceanHeightProvider oceanHeight = GetComponentInParent<IOceanHeightProvider>(); // provided by the ocean/planet system
        _context = new VehicleRuntimeContext(_rigidbody, transform, probe, _telemetry,
            _suspensionProbePoints, _buoyancySamplePoints);

        IVehicleState[] states =
        {
            new HoverState(_hoverConfig, new HoverSuspensionProcessor()),
            new FlightState(_flightConfig, new FlightAerodynamicsProcessor()),
            new WaterState(_waterConfig, new BuoyancyProcessor()),
            new UnderWaterState(_underWaterConfig, new UnderwaterPropulsionProcessor(oceanHeight)),
        };

        _stateMachine = new VehicleStateMachine(_context, states, VehicleStateId.Hover);
    }

    private void Update()
    {
        _context.SetInput(_pendingInput);
        _stateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate() => _stateMachine.FixedTick(Time.fixedDeltaTime);

    public void SetInput(VehicleInputSample input) => _pendingInput = input;

    public bool EnterHoverMode() => _stateMachine.RequestTransition(VehicleStateId.Hover);
    public bool EnterFlightMode() => _stateMachine.RequestTransition(VehicleStateId.Flight);
    public bool EnterWaterMode() => _stateMachine.RequestTransition(VehicleStateId.Water);
    public bool EnterUnderWaterMode() => _stateMachine.RequestTransition(VehicleStateId.UnderWater);
}
```

`VehicleController` is intentionally thin: it wires dependencies once in `Awake` and otherwise just forwards `Update`/`FixedUpdate` ticks and input. All branching logic lives in the states and the state machine, not here.

## Transition rules

| From → To | Condition to allow | Rationale |
| --- | --- | --- |
| Hover → Flight | Explicit input (`Boost`/fly button) always allowed | Flight is pilot-initiated; no implicit auto-entry from Hover. |
| Hover → Water | `IEnvironmentProbe` reports a liquid sample under the vehicle | Driving onto the ocean surface should seamlessly become buoyant. |
| Flight → Hover | A terrain probe hits within `HoverModeConfig.MaxGroundSnapDistance` | Prevents snapping to ground from altitude; must be a controlled descent. |
| Flight → Water | Vehicle altitude above liquid surface ≤ `WaterModeConfig.EntrySplashHeight` | Avoids treating a high flyover of the ocean as a water entry. |
| Water → Hover | `SubmersionFraction01 < WaterModeConfig.SurfacedThreshold` **and** a terrain probe hits nearby | Must be surfaced and near shore/ground, not mid-ocean. |
| Water → Flight | `SubmersionFraction01 < WaterModeConfig.SurfacedThreshold` | Must be surfaced before thrusting into flight; prevents launching while still submerged. |
| Water → UnderWater | Explicit `Dive` input while in `WaterState` | The submarine transformation is a deliberate player action (like Hover → Flight), never automatic — see "Submarine transformation sequence" below. |
| UnderWater → Water | `DepthBelowSurface ≤ UnderWaterModeConfig.SurfaceEmergeThreshold` | Must have ascended shallow enough to safely transform back and float before leaving UnderWater. |

Each row above is exactly what that state's `CanTransitionTo` implementation checks — the table is the spec for those methods, not a separate runtime construct. Keep the thresholds themselves on the configs (doc 04), not hard-coded in the state, so designers can tune "how deep is too deep to fly off" without a code change.

Note what is deliberately **absent**: there is no Hover ↔ UnderWater or Flight ↔ UnderWater row. `WaterState` is the only gate into and out of `UnderWaterState` — the rover must already be floating to transform into a submarine, and must resurface and transform back before it can drive onto land or take off. This keeps `UnderWaterState`'s own `CanTransitionTo` trivial (it only ever needs to validate the single path back to `Water`) and avoids every other state having to know the submarine mode exists.

## Submarine transformation sequence

Transforming between `WaterState` and `UnderWaterState` plays a short hull-reconfiguration animation (fins/ballast fairings extending or retracting). This is handled entirely inside `UnderWaterState.Enter`/`Exit` and `UnderWaterModeConfig.TransformDuration` — **not** by freezing the rigidbody or blocking the state machine:

```csharp
public sealed class UnderWaterState : IVehicleState
{
    public VehicleStateId Id => VehicleStateId.UnderWater;

    private readonly UnderWaterModeConfig _config;
    private readonly UnderwaterPropulsionProcessor _processor;
    private float _transformTimeRemaining;

    public UnderWaterState(UnderWaterModeConfig config, UnderwaterPropulsionProcessor processor)
    {
        _config = config;
        _processor = processor;
    }

    public void Enter(VehicleRuntimeContext context)
    {
        context.Rigidbody.useGravity = false;
        _transformTimeRemaining = _config.TransformDuration;
        context.Telemetry.SetTransforming(true);
    }

    public void Exit(VehicleRuntimeContext context) => context.Telemetry.SetTransforming(false);

    public void Tick(VehicleRuntimeContext context, float deltaTime) { }

    public void FixedTick(VehicleRuntimeContext context, float fixedDeltaTime)
    {
        if (_transformTimeRemaining > 0f)
        {
            _transformTimeRemaining -= fixedDeltaTime;
            context.Telemetry.SetTransforming(_transformTimeRemaining > 0f);
        }

        float controlAuthority01 = _transformTimeRemaining > 0f ? 0.1f : 1f;
        SubmarinePropulsionResult result = _processor.Solve(context, controlAuthority01, _config);
        context.Rigidbody.AddForce(result.Thrust);
        context.Rigidbody.AddTorque(result.ControlTorque);
    }

    public bool CanTransitionTo(VehicleStateId next, VehicleRuntimeContext context) =>
        next == VehicleStateId.Water && context.Telemetry.Snapshot.CurrentValue.DepthBelowSurface <= _config.SurfaceEmergeThreshold;
}
```

The rigidbody keeps receiving (heavily damped) propulsion forces throughout the transform — momentum is never reset, consistent with every other transition in this document — but `controlAuthority01` scales input responsiveness near zero while the hull is mid-reconfiguration, so the player can't steer sharply until the animation finishes. `SubmarineTransformView` (doc 01) only ever *watches* `IsTransforming`; it has no authority over when the transition starts, ends, or how long it takes.

## Why no velocity reset on transition

`Enter`/`Exit` only flip `Rigidbody` flags (`useGravity`, `drag`) and reset per-mode bookkeeping — they never zero or rewrite `velocity`/`angularVelocity`. Momentum carries continuously across modes: driving off a cliff into Flight keeps the exit speed as forward airspeed; splashing into Water keeps the impact velocity for the buoyancy damping to absorb. This is what the spec means by "no permanent magnetic attachment" — contact is re-evaluated every `FixedTick` from fresh probe samples, so losing surface contact in `HoverState` doesn't need a separate "falling" state; it simply coasts under gravity until a probe hits again or the player switches modes explicitly.
