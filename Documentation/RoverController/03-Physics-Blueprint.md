# 03 — Physics Implementation Blueprint

All solvers below are plain classes with no `MonoBehaviour`/`Rigidbody` dependency — they take immutable snapshots in and return an immutable force/torque result out. States are the only place `Rigidbody.AddForce`/`AddTorque` is actually called. This split is what makes the math unit-testable via the Unity Test Framework without a running physics scene.

## Environment probing abstraction

```csharp
namespace Game.Gameplay.Vehicles.Rover.Physics;

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

public interface IEnvironmentProbe
{
    EnvironmentSample SampleGround(Vector3 origin, Vector3 direction, float maxDistance);
    EnvironmentSample SampleLiquid(Vector3 origin, float maxDistance);
}
```

`RaycastEnvironmentProbe` implements this with `Physics.RaycastNonAlloc` against a terrain `LayerMask` for `SampleGround`, and a liquid `LayerMask` (trigger volumes marking ocean bounds, owned by the planet/ocean system — not by the rover) for `SampleLiquid`. Both methods use a single reusable `RaycastHit[1]` buffer field on the probe instance — zero per-call allocation, safe because probing is single-threaded on the main thread inside `FixedUpdate`.

States never call `Physics.*` directly; they only ever go through `IEnvironmentProbe`. This is what lets terrain/ocean implementation change (procedural mesh colliders today, SDF-based collision later) without touching any vehicle state.

### `IOceanHeightProvider` — global surface height for the submarine

`SampleLiquid`'s raycast has a bounded `maxDistance`, which is fine for `WaterState`'s per-point buoyancy sampling (the surface is always within a meter or two of a floating hull) but wrong for `UnderWaterState`: a submarine cruising deep below the surface still needs to know "how far up is the surface" to compute depth, and a raycast long enough to always reach it would be wasteful and would also hit the seafloor/terrain first if any is in the way. So this is deliberately a *second*, non-raycast interface, owned by the same planet/ocean system that owns the liquid volumes:

```csharp
namespace Game.Gameplay.Vehicles.Rover.Physics;

public interface IOceanHeightProvider
{
    float GetSurfaceHeight(Vector3 worldPositionXZ);
}
```

An analytic query (flat plane today, a Gerstner-wave height function later) rather than a physics cast — it never misses, never cares what's in between, and costs nothing per-call. `UnderWaterState` is the only consumer; `WaterState` keeps using `IEnvironmentProbe.SampleLiquid` for its short-range per-point sampling since that already gives it a surface normal for pitch, which `IOceanHeightProvider` deliberately does not (it only answers "how high," not "which way is up here").

## Hover suspension

### Per-probe spring-damper

For each configured probe point (local-space offsets from `VehicleController._suspensionProbePoints`, typically the 4 corners — see doc 05), cast straight down (`-transform.up`) up to `RestLength + MaxTravel`:

```text
compression = (RestLength - sample.Distance) / RestLength   // 0 at rest length, 1 fully compressed
springForce = compression * SpringStrength
relativeVel = Vector3.Dot(rigidbody.GetPointVelocity(probe.Point), -direction)
damperForce = relativeVel * DamperStrength
forceAlongRay = (springForce - damperForce) * direction   // direction = sample.Normal (or -ray dir), clamped ≥ 0
```

Each probe's `forceAlongRay` is applied independently via `Rigidbody.AddForceAtPosition(forceAlongRay, probe.Point)`. Using 4 independent probes (rather than one average) is what gives slope/roll stability for free — a probe that loses ground contact (`sample.Hit == false`) simply contributes zero force that frame.

### Orientation alignment

Average the hit normals of all probes that reported a hit into `averageNormal`. Compute a corrective torque that rotates `transform.up` toward `averageNormal`:

```text
rotationError = Vector3.Cross(transform.up, averageNormal)
alignTorque = rotationError * AlignStrength - rigidbody.angularVelocity * AlignDamping
```

This is a proportional-derivative (spring-damper) torque, same shape as the suspension force — tune `AlignStrength`/`AlignDamping` on `HoverModeConfig` independently from the linear spring constants.

### Drive & steering

Project the forward input onto the plane defined by `averageNormal` (so throttle pushes *along* a slope/wall, not into it):

```text
tangentForward = Vector3.ProjectOnPlane(transform.forward, averageNormal).normalized
driveForce = tangentForward * (input.Throttle * DriveForce)
```

Steering is a yaw torque around `averageNormal` scaled by `input.Steer * TurnTorque`, clamped by current speed so the vehicle doesn't spin in place at a standstill if that's not the desired feel (expose a `MinSteerSpeed` on the config if so).

### Climbing walls without magnetic attachment

Grip (how much of the drive/align force actually applies) is read from an `AnimationCurve` on `HoverModeConfig` keyed by slope angle (`Vector3.Angle(averageNormal, Vector3.up)`), e.g. full grip at 0°–60°, tapering to near-zero past 85°. There is **no explicit "attached" boolean and no joint** — a vertical surface is driven onto simply because the probes keep hitting it and the spring keeps pushing outward from it. The vehicle detaches the instant probes stop hitting (crests the wall, or player applies enough opposing throttle/gravity wins) and falls ballistically — the same `HoverState.FixedTick` keeps running, just with every probe reporting a miss that frame, which is a valid, intentional, zero-force state rather than an error case.

### `HoverSuspensionProcessor` shape

```csharp
public readonly struct SuspensionForceResult
{
    public readonly Vector3 AverageNormal;
    public readonly float AverageCompression01;
    public readonly ProbeForce[] ProbeForces;   // one AddForceAtPosition call per entry
    public readonly Vector3 AlignTorque;
    public readonly Vector3 DriveForce;
    public readonly Vector3 SteerTorque;
}

public sealed class HoverSuspensionProcessor
{
    public SuspensionForceResult Solve(
        ReadOnlySpan<EnvironmentSample> probeSamples,
        ReadOnlySpan<Vector3> probeWorldPositions,
        Rigidbody rigidbody,
        VehicleInputSample input,
        HoverModeConfig config);
}
```

## Flight aerodynamics

Entered with `Rigidbody.useGravity = false`; the state applies its own scaled gravity so it can be lighter than real gravity without touching the global `Physics.gravity`:

```text
customGravity = Physics.gravity * config.GravityScale
```

### Lift

Simple arcade lift model — proportional to forward speed squared, scaled by a single tunable coefficient (no need for a real air-density term in an arcade controller; fold it into `LiftCoefficient`):

```text
forwardSpeed = Vector3.Dot(rigidbody.velocity, transform.forward)
liftFactor01 = Saturate((forwardSpeed - MinLiftSpeed) / (MaxLiftSpeed - MinLiftSpeed))
liftForce = transform.up * (forwardSpeed * forwardSpeed * LiftCoefficient * liftFactor01)
```

`liftFactor01` is written straight into `VehicleTelemetrySnapshot.LiftFactor01` — this is the single value `WingDeploymentView` watches to animate wings open/closed, with no separate "is flying fast enough" logic duplicated in presentation code.

### Pitch / roll / yaw

Arcade-style direct torque from input, not a true control-surface simulation:

```text
controlTorque = new Vector3(input.Pitch, input.Yaw, -input.Roll) * config.ControlTorqueStrength
```

applied in local space (`transform.TransformDirection`). An optional auto-level term (`-rigidbody.angularVelocity * config.AngularDamping`, plus a small torque pulling `transform.up` back toward world-up when there's no roll input) gives the "flight assist" feel without a full 6DOF simulation — expose `AutoLevelStrength` on the config and allow `0` to disable it for a harder flight model later.

### `FlightAerodynamicsProcessor` shape

```csharp
public readonly struct AerodynamicForceResult
{
    public readonly Vector3 Gravity;
    public readonly Vector3 Lift;
    public readonly Vector3 ControlTorque;
    public readonly Vector3 AutoLevelTorque;
    public readonly float LiftFactor01;
}
```

## Water / buoyancy

`WaterState` reads liquid height via `IEnvironmentProbe.SampleLiquid`, independent of whatever the ocean's actual geometry/shader implementation is (planar now, Gerstner-wave-displaced later — the state never needs to know).

### Per-sample submersion

Using 2+ sample points (bow/stern, see doc 05 for placement) rather than a single center point gives pitch stability for free, the same way the 4 hover probes give roll stability:

```text
submersionDepth = liquidSample.Point.y - samplePoint.y   // positive when point is underwater
submergedFraction01 = Saturate(submersionDepth / config.FullySubmergedDepth)
buoyantForce = Vector3.up * (submergedFraction01 * config.WaterDensity * config.GravityMagnitude * config.VolumePerSamplePoint)
```

applied per-point via `AddForceAtPosition`, same pattern as hover springs. `VehicleTelemetrySnapshot.SubmersionFraction01` is the average across all sample points, used both by the `Water → Hover/Flight` transition guard (doc 02) and by `WakeTrailView`.

### Drag blending

Lerp drag/angular-drag between an "above surface" and "underwater" value by the average submersion fraction, rather than hard-switching, so entering/exiting the water doesn't pop:

```text
rigidbody.drag = Lerp(config.SurfaceDrag, config.UnderwaterDrag, averageSubmersion01)
```

Thrust (`input.Throttle`) is similarly scaled by `Lerp(1, config.UnderwaterThrustMultiplier, averageSubmersion01)` so submerged driving feels appropriately sluggish without a separate code path.

### `BuoyancyProcessor` shape

```csharp
public readonly struct BuoyancyForceResult
{
    public readonly PointForce[] BuoyantForces;   // one per sample point
    public readonly float AverageSubmersion01;
    public readonly float ResolvedDrag;
    public readonly float ResolvedAngularDrag;
    public readonly float ThrustMultiplier;
}
```

## Underwater / submarine propulsion

Entered only from `WaterState` (doc 02). Where `FlightState` fights gravity with speed-based lift, `UnderWaterState` cancels gravity outright and replaces it with fully player-directed thrust in all three axes — the vehicle is a submarine, not a glider, once transformed.

### Neutral buoyancy & depth

```text
rigidbody.useGravity = false   // no custom gravity term either — thrust alone controls vertical motion
surfaceHeight = oceanHeightProvider.GetSurfaceHeight(transform.position)
depthBelowSurface = surfaceHeight - transform.position.y   // 0 at the surface, grows positive going down
crushRatio01 = Saturate(depthBelowSurface / config.MaxDiveDepth)
```

`depthBelowSurface` is written into `VehicleTelemetrySnapshot.DepthBelowSurface` every `FixedTick` — it's the single value both the `UnderWater → Water` transition guard (doc 02) and any future "hull stress" UI/gameplay hook (`crushRatio01` crossing `config.CrushWarningDepth01`) read from. This plan treats exceeding `MaxDiveDepth` as a designer-facing extension point (slow forced ascent, hull damage, alarm VFX) rather than speccing specific punishment here — wire it up when the gameplay need is concrete.

### Thrust & control

```text
forwardThrust = transform.forward * (input.Throttle * config.ThrustForce)
verticalThrust = transform.up * (input.Ascend * config.AscendThrust)   // dedicated ascend/descend axis, not reused from Pitch
controlTorque = TransformDirection(new Vector3(input.Pitch, input.Yaw, -input.Roll)) * config.ControlTorqueStrength * controlAuthority01
```

`controlAuthority01` is the same transformation-lockout multiplier from doc 02's `UnderWaterState.FixedTick` (≈0.1 while mid-transform, 1 once settled) — the processor takes it as a parameter rather than reading transform state itself, keeping it a pure function. There is no lift term and no forward-speed dependency: ascend/descend is a direct thrust axis the player holds, which reads as "submarine" rather than "underwater plane."

### Drag & seafloor avoidance

Drag is constant and heavy while in this state (no blending needed — `UnderWaterState` is either fully entered or not, unlike `WaterState`'s partial-submersion blend):

```text
rigidbody.drag = config.LinearDrag
rigidbody.angularDrag = config.AngularDrag
```

For seafloor/cave-wall avoidance, reuse `IEnvironmentProbe.SampleGround` — cast along `transform.forward` and `-transform.up` against the same terrain `LayerMask` already used by `HoverState`. No new probe type is needed; the submarine just points existing ground probes forward/down instead of only down. A hit within a configurable `config.ObstacleWarningDistance` is exposed on telemetry for a sonar-ping VFX/SFX cue rather than an automatic avoidance force — steering around terrain stays the player's job.

### `UnderwaterPropulsionProcessor` shape

```csharp
public readonly struct SubmarinePropulsionResult
{
    public readonly Vector3 Thrust;
    public readonly Vector3 ControlTorque;
    public readonly float DepthBelowSurface;
    public readonly float CrushRatio01;
    public readonly bool ObstacleAhead;
}

public sealed class UnderwaterPropulsionProcessor
{
    private readonly IOceanHeightProvider _oceanHeight;

    public UnderwaterPropulsionProcessor(IOceanHeightProvider oceanHeight) => _oceanHeight = oceanHeight;

    public SubmarinePropulsionResult Solve(
        VehicleRuntimeContext context,
        float controlAuthority01,
        UnderWaterModeConfig config);
}
```

## Testing note

Because every `*Processor.Solve(...)` method takes plain structs/spans and returns a plain struct, write EditMode tests (`com.unity.test-framework`, already in `Documentation/Dependencies.md`) that construct a fake `EnvironmentSample[]` and a `HoverModeConfig` asset in-memory (`ScriptableObject.CreateInstance`) and assert on the returned `SuspensionForceResult` — no scene load, no Play mode, no `Rigidbody` required for the math itself.
