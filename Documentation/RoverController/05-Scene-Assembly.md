# 05 — Scene Assembly

## GameObject hierarchy

```text
RoverVehicle                           (Rigidbody, main collider, VehicleController)
├── Probes/                            (empty, local-space anchor only — no components)
│   ├── Probe_FrontLeft                (Transform only; position = probe origin)
│   ├── Probe_FrontRight
│   ├── Probe_RearLeft
│   └── Probe_RearRight
├── BuoyancyPoints/                     (empty, local-space anchor only)
│   ├── Buoyancy_Bow
│   └── Buoyancy_Stern
├── Visuals/                            (no Rigidbody, no colliders)
│   ├── Hull_Mesh                       (MeshRenderer/SkinnedMeshRenderer)
│   ├── Wing_Left                       (toggled by WingDeploymentView)
│   ├── Wing_Right
│   └── VehicleVisualController         (component on this node)
├── VFX/
│   ├── HoverThrusters                  (ParticleSystem × N, HoverThrusterVfxView)
│   └── WakeTrail                       (TrailRenderer/ParticleSystem, WakeTrailView)
├── Audio/
│   └── EngineAudioSource                (AudioSource, VehicleAudioController)
├── CameraTarget                         (empty Transform; follow-camera rig parents to THIS, never to the Rigidbody root)
└── Debug/
    └── RoverDebugGizmos                 (component; OnDrawGizmosSelected only, stripped from builds)
```

## Component attachment table

| GameObject | Components | Notes |
| --- | --- | --- |
| `RoverVehicle` (root) | `Rigidbody`, 1 primary `Collider` (convex), `VehicleController` | Single physics body for the whole vehicle. Keep the collider count minimal — one convex hull or a small compound of 2–3 primitives, never a mesh collider on the live rigidbody. |
| `Probes/Probe_*` | none (bare `Transform`) | Positions read by `VehicleController` into the `_suspensionProbePoints` array (doc 02). No `Collider`, no script — they're just raycast origins. Place at the four lowest/outer corners of the hull in local space. |
| `BuoyancyPoints/Buoyancy_*` | none (bare `Transform`) | Same pattern for `_buoyancySamplePoints`. Bow + stern is the minimum for pitch stability; add left/right pairs later only if roll-while-floating turns out to need it. |
| `Visuals/*` | `MeshRenderer`, `VehicleVisualController` | Entirely decorative. `VehicleVisualController` subscribes to `VehicleController.Telemetry`; it never reads `Rigidbody` or `Transform` of the physics root beyond the implicit parent-transform follow. |
| `VFX/HoverThrusters` | `ParticleSystem`, `HoverThrusterVfxView` | Emission rate driven by `SuspensionCompression01`/current state id from telemetry. |
| `VFX/WakeTrail` | `TrailRenderer` or `ParticleSystem`, `WakeTrailView` | Enabled only while `StateId == Water` and `SubmersionFraction01` is near the surface threshold. |
| `Audio/EngineAudioSource` | `AudioSource`, `VehicleAudioController` | Pitch/volume mapped from `Speed`/`StateId`. |
| `CameraTarget` | none (bare `Transform`) | A follow/orbit camera rig should target this, not the Rigidbody transform directly, so camera smoothing logic stays independent of physics-step jitter. |
| `Debug/RoverDebugGizmos` | `MonoBehaviour` with only `OnDrawGizmosSelected` | Draws suspension rays (color-coded by compression), buoyancy sample points, and the current lift vector. Lives in `Assets/Game/Debug/` per `ProjectConvention.md`'s `Debug/` folder, excluded from release builds via a `UNITY_EDITOR` guard or by living in an editor-only asmdef. |

## Collider guidance

- **Physics root:** one convex `Collider` (or a tiny compound of box/capsule primitives) sized to the hull's silhouette. This is what terrain/liquid actually collides against.
- **Visuals never carry colliders.** Keeping `Visuals/` fully collider-free is what guarantees "decoupled physics and presentation" isn't just a code-level claim — a designer can swap the entire visual mesh for a different rover skin without touching physics behavior at all.
- **Probe/buoyancy anchors are not colliders.** They're pure `Transform` markers consumed by `IEnvironmentProbe` calls originating from the root — giving them colliders would make them participate in physics queries themselves, which is never wanted.
- **Liquid detection is the ocean system's responsibility, not the rover's.** The planet/ocean system owns trigger volumes (or an analytic height function) on a dedicated liquid `LayerMask`; `RaycastEnvironmentProbe.SampleLiquid` queries *that*. The rover prefab should contain zero water-specific colliders.

## Probe count & placement rationale

Four hover probes (front-left/right, rear-left/right) at the hull's outer-lowest corners is the standard minimum for a vehicle that needs both roll stability (left/right pair) and pitch stability (front/rear pair) while climbing slopes and walls. Fewer than 4 loses one axis of stability; more than 4–6 is rarely worth the extra raycasts for a vehicle this size. Two buoyancy points (bow/stern) is the equivalent minimum for pitch-only floating stability; add a left/right pair only if playtesting shows roll instability while floating.

## Prefab strategy

Build one base `RoverVehicle.prefab` with the hierarchy above. Vehicle variants (different hull mesh, different `VehicleProfile` from doc 04) become prefab *variants* that override only the `Visuals/` mesh references and the `VehicleController`'s config fields — never fork the whole hierarchy. Keeps probe/buoyancy placement and the physics-root setup identical and bug-fixed-once across every variant.
