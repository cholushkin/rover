# `rover-controller` — Implementation Plan

Multi-modal vehicle controller for the planetary rover: **Hover** (default surface traversal), **Flight** (aerial), **Water** (surface buoyancy/aquatic), and **UnderWater** (a submarine transformation, entered from Water mode, for full submersion). Designed for maximum extensibility, clean separation of physics from presentation, and real-time data-driven tuning via `ScriptableObject` profiles.

This directory breaks the system down into five focused documents:

| # | Document | Covers |
| - | -------- | ------ |
| 1 | [01-Architecture-Overview.md](01-Architecture-Overview.md) | Folder/namespace layout, class list, interfaces, dependency direction, per-frame data flow. |
| 2 | [02-State-Machine-And-Context.md](02-State-Machine-And-Context.md) | `IVehicleState`, `VehicleStateMachine`, `VehicleController` lifecycle, transition rules & safety — including the Water → UnderWater submarine transformation gate. |
| 3 | [03-Physics-Blueprint.md](03-Physics-Blueprint.md) | Environment probing abstraction, hover suspension math, flight aerodynamics, buoyancy math, submarine propulsion & depth math. |
| 4 | [04-Data-Assets-And-Configuration.md](04-Data-Assets-And-Configuration.md) | `VehicleModeConfig` SO schema for all four modes (Hover/Flight/Water/UnderWater), asset layout, editor validation. |
| 5 | [05-Scene-Assembly.md](05-Scene-Assembly.md) | GameObject hierarchy, colliders, probe/buoyancy point placement, component attachment table. |

## Reading order

Start at **01** for the mental model, then **02** and **03** for the runtime logic, then **04** and **05** when you're ready to author assets and wire up the scene. Each document is self-contained enough to reference independently once the architecture in 01 is understood.

## Design principles carried through every document

- **State pattern, not a switch statement.** Each mode is an isolated `IVehicleState` implementation; the controller only orchestrates lifecycle and transitions.
- **Physics math is pure and Unity-agnostic.** Suspension/aerodynamics/buoyancy solvers take structs in, return structs out — no `MonoBehaviour`, no hidden state — so they're unit-testable and easy to tune outside Play mode.
- **Config lives in assets, not code.** Every tunable constant is a serialized field on a `ScriptableObject`, editable live in the Inspector while the game is running.
- **Presentation never touches physics.** Visuals (wing meshes, VFX, audio) subscribe to a read-only telemetry stream; they never read or write the `Rigidbody` directly.
- **The submarine transformation is a deliberate, gated action, not a free-floating 4th mode.** `UnderWaterState` is only reachable from `WaterState` — the rover must be floating before it can transform and dive. Hover and Flight never transition directly into or out of `UnderWater`.
- **Follows this repo's existing conventions** (`DevConventions/CodingConvention.md`, `DevConventions/ProjectConvention.md`): file-scoped namespaces, `_camelCase` private fields, `[field: SerializeField]` properties, `Controller`/`Processor`/`Service` suffixes, feature-first folders under `Assets/Game/Runtime/Gameplay/`.
