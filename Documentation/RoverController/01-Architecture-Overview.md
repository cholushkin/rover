# 01 — Architecture Overview

## Folder & namespace layout

Follows `DevConventions/ProjectConvention.md` (feature-first under `Gameplay/`, strict runtime/editor split). No new asmdef is created initially — the module lives inside the existing `Game.Runtime` assembly. If the rover ever needs to be reused in another project, promote the `Rover/` folder wholesale into `Assets/Libs/RoverController/` with its own `asmdef`, mirroring how `Gamelib`/`CVB` are isolated — don't pre-build that isolation now.

```text
Assets/Game/
├── Runtime/
│   └── Gameplay/
│       └── Vehicles/
│           └── Rover/
│               ├── Core/            # VehicleController, VehicleRuntimeContext, VehicleStateId
│               ├── States/          # HoverState, FlightState, WaterState, IVehicleState
│               ├── Physics/         # IEnvironmentProbe + probes, *Processor solvers, force result structs
│               ├── Config/          # VehicleModeConfig and concrete SOs
│               ├── Telemetry/       # VehicleTelemetry, VehicleTelemetrySnapshot
│               └── Presentation/    # VehicleVisualController, WingDeploymentView, VFX/audio views
│   └── Debug/
│       └── RoverDebugGizmos.cs      # scene-view suspension/buoyancy/lift visualization
└── DataAndSettings/
    └── Vehicles/
        └── Rover/                   # .asset instances of the Config SOs (see doc 04)
```

Namespace root: `Game.Gameplay.Vehicles.Rover`, with sub-namespaces matching the folders above (`Game.Gameplay.Vehicles.Rover.States`, `...Physics`, `...Config`, `...Telemetry`, `...Presentation`). All files use file-scoped namespace declarations per `CodingConvention.md`.

## Class decomposition

### Core

| Class | Kind | Responsibility |
| --- | --- | --- |
| `VehicleController` | `MonoBehaviour` | Composition root. Owns the `Rigidbody`, builds `VehicleRuntimeContext`, owns `VehicleStateMachine`, exposes explicit `EnterHoverMode()` / `EnterFlightMode()` / `EnterWaterMode()` methods, drives `Update`/`FixedUpdate`. |
| `VehicleRuntimeContext` | plain class | Dependency bag passed to every state: `Rigidbody`, `Transform`, current `VehicleInputSample`, `IEnvironmentProbe`, `VehicleTelemetry` sink. States read/write only through this — never reach back into `VehicleController`. |
| `VehicleStateId` | `enum` | `Hover`, `Flight`, `Water`, `UnderWater`. |
| `VehicleInputSample` | `readonly struct` | Normalized input for the current frame: `Throttle`, `Steer`, `Pitch`, `Roll`, `Yaw`, `Boost`, plus mode-switch intents. Produced upstream by an input adapter (out of scope for this plan — the controller only consumes the struct). |

### States (`States/`)

| Class | Implements | Depends on |
| --- | --- | --- |
| `HoverState` | `IVehicleState` | `HoverModeConfig`, `HoverSuspensionProcessor` |
| `FlightState` | `IVehicleState` | `FlightModeConfig`, `FlightAerodynamicsProcessor` |
| `WaterState` | `IVehicleState` | `WaterModeConfig`, `BuoyancyProcessor` |
| `UnderWaterState` | `IVehicleState` | `UnderWaterModeConfig`, `UnderwaterPropulsionProcessor` |

Each state is constructed once by `VehicleController` and reused for the object's lifetime (no per-transition allocation). A state never references another state or the state machine directly — it only knows `VehicleRuntimeContext` and its own config/processor.

`UnderWaterState` is the rover's "submarine mode" — full submersion with powered 3D movement, as opposed to `WaterState`'s surface buoyancy. It is reachable only from `WaterState` (see doc 02's transition table): the rover must be floating before it can transform into a submarine and dive, and must ascend back to the surface before transforming back. Hover and Flight never connect to it directly.

### Physics (`Physics/`) — see [03-Physics-Blueprint.md](03-Physics-Blueprint.md) for the math

| Class | Kind | Responsibility |
| --- | --- | --- |
| `IEnvironmentProbe` | interface | Unified spatial query surface: cast rays/spheres against terrain and liquid volumes without states knowing about `Physics.*` directly. |
| `RaycastEnvironmentProbe` | plain class | Default implementation using `Physics.RaycastNonAlloc` / `Physics.SphereCast` with explicit terrain/liquid `LayerMask`s. |
| `EnvironmentSample` | `readonly struct` | `Hit`, `Point`, `Normal`, `Distance`, `IsLiquid`, `SurfaceLayer`. |
| `IOceanHeightProvider` | interface | Analytic (non-raycast) global ocean surface height query, used for submarine depth tracking where the surface may be far beyond any sane raycast range. |
| `HoverSuspensionProcessor` | plain class | Pure function: probes + rigidbody state + `HoverModeConfig` → `SuspensionForceResult`. |
| `FlightAerodynamicsProcessor` | plain class | Pure function: rigidbody state + input + `FlightModeConfig` → `AerodynamicForceResult`. |
| `BuoyancyProcessor` | plain class | Pure function: liquid samples + rigidbody state + `WaterModeConfig` → `BuoyancyForceResult`. |
| `UnderwaterPropulsionProcessor` | plain class | Pure function: ocean height + seafloor probe + rigidbody state + input + `UnderWaterModeConfig` → `SubmarinePropulsionResult`. |

These processors never touch `Rigidbody.AddForce` themselves — they return a force/torque result struct, and the owning state applies it. This keeps the math testable with the Unity Test Framework (already a project dependency, see `Documentation/Dependencies.md`) without a running physics scene.

### Config (`Config/`) — full schema in [04-Data-Assets-And-Configuration.md](04-Data-Assets-And-Configuration.md)

`VehicleModeConfig` (abstract `ScriptableObject`) ← `HoverModeConfig`, `FlightModeConfig`, `WaterModeConfig`, `UnderWaterModeConfig`.

### Telemetry (`Telemetry/`)

| Class | Kind | Responsibility |
| --- | --- | --- |
| `VehicleTelemetry` | plain class | Owns an R3 `ReactiveProperty<VehicleTelemetrySnapshot>`, exposed read-only (`ReadOnlyReactiveProperty<T>`) to presentation code. Written once per `FixedUpdate` by the active state. |
| `VehicleTelemetrySnapshot` | `readonly struct` | `StateId`, `Speed`, `SuspensionCompression01`, `LiftFactor01`, `SubmersionFraction01`, `SurfaceNormal`, `DepthBelowSurface`, `IsTransforming`. |

### Presentation (`Presentation/`)

| Class | Kind | Responsibility |
| --- | --- | --- |
| `VehicleVisualController` | `MonoBehaviour` | Subscribes to `VehicleTelemetry`, drives mesh/animator parameters. Holds zero physics references. |
| `WingDeploymentView` | `MonoBehaviour` | Deploys/retracts wing geometry based on `LiftFactor01` crossing a threshold. |
| `SubmarineTransformView` | `MonoBehaviour` | Plays the hull reconfiguration animation while `IsTransforming` is true (Water ↔ UnderWater), driven purely by telemetry — never gates or delays the physics transition itself. |
| `HoverThrusterVfxView`, `WakeTrailView`, `BubbleTrailView` | `MonoBehaviour` | Per-mode particle/trail effects, each independently subscribed to the same telemetry stream. |

## Dependency direction (one-way, enforced by convention)

```text
Presentation  ──reads──▶  Telemetry
VehicleController  ──owns──▶  StateMachine, Context, Telemetry, Config references
States  ──use──▶  Context, their own Config, their own Processor
Processors  ──pure functions, no outward dependencies──
```

Nothing points backward: a `Processor` doesn't know about `IVehicleState`, a `State` doesn't know about `VehicleController`, and `Presentation` never reaches into `Rigidbody`. This is what makes it safe to add a fourth mode later (e.g. a future "Magnetic Grip" or "Space" mode) without touching existing states or presentation code.

## Per-`FixedUpdate` data flow

1. `VehicleController.FixedUpdate()` samples the latest `VehicleInputSample` into `VehicleRuntimeContext`.
2. `VehicleStateMachine.FixedTick(dt)` forwards to the current `IVehicleState.FixedTick(dt)`.
3. The active state asks `IEnvironmentProbe`/`IOceanHeightProvider` for fresh samples: ground probes for Hover, liquid height for Water, a forward/downward seafloor probe plus global ocean height for UnderWater; Flight mode may skip probing entirely when airborne.
4. The state passes samples + context + its config into its `Processor`, receiving a force/torque result struct back.
5. The state applies the result to `Rigidbody` (`AddForceAtPosition` / `AddTorque`) and writes a new `VehicleTelemetrySnapshot` into `VehicleTelemetry`.
6. `VehicleController.Update()` (variable-rate) evaluates automatic transition conditions (e.g. "liquid detected and no ground within range → request `Water`") and calls `VehicleStateMachine.RequestTransition(...)` if needed; the actual `Exit`/`Enter` swap happens at the start of the *next* `FixedTick` (see doc 02) so it never interrupts an in-progress physics step.
7. Presentation components react to the updated `VehicleTelemetry` value on their own `Update`, fully decoupled from steps 1–5.
