# 04 — Data Assets & Configuration

## Base type

```csharp
namespace Game.Gameplay.Vehicles.Rover.Config;

public abstract class VehicleModeConfig : ScriptableObject
{
    [field: SerializeField] public VehicleStateId ModeId { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; } = string.Empty;
}
```

Each concrete config below inherits this and adds its own tunables. Keep shared *transition-threshold* fields on the mode they protect (e.g. the "am I surfaced enough to leave" threshold lives on `WaterModeConfig`, not duplicated elsewhere) — doc 02's transition table is the authoritative list of which threshold belongs where.

## `HoverModeConfig`

```csharp
namespace Game.Gameplay.Vehicles.Rover.Config;

[CreateAssetMenu(menuName = "Rover/Configs/Hover Mode Config", fileName = "RoverHoverConfig")]
public sealed class HoverModeConfig : VehicleModeConfig
{
    [Header("Suspension")]
    [field: SerializeField, Range(0.1f, 5f)] public float RestLength { get; private set; } = 1.2f;
    [field: SerializeField, Range(0f, 1f)] public float MaxTravel01 { get; private set; } = 0.5f;
    [field: SerializeField] public float SpringStrength { get; private set; } = 25f;
    [field: SerializeField] public float DamperStrength { get; private set; } = 4f;

    [Header("Orientation")]
    [field: SerializeField] public float AlignStrength { get; private set; } = 15f;
    [field: SerializeField] public float AlignDamping { get; private set; } = 2f;

    [Header("Drive")]
    [field: SerializeField] public float DriveForce { get; private set; } = 40f;
    [field: SerializeField] public float TurnTorque { get; private set; } = 12f;
    [field: SerializeField] public float MinSteerSpeed { get; private set; } = 0.5f;
    [field: SerializeField] public float MaxSpeed { get; private set; } = 30f;

    [Header("Surface Grip")]
    [field: SerializeField] public AnimationCurve GripBySlopeAngle { get; private set; }
        = AnimationCurve.Linear(0f, 1f, 85f, 0f);

    [Header("Transitions")]
    [field: SerializeField] public float MaxGroundSnapDistance { get; private set; } = 3f;
}
```

## `FlightModeConfig`

```csharp
[CreateAssetMenu(menuName = "Rover/Configs/Flight Mode Config", fileName = "RoverFlightConfig")]
public sealed class FlightModeConfig : VehicleModeConfig
{
    [Header("Gravity & Lift")]
    [field: SerializeField, Range(0f, 1f)] public float GravityScale { get; private set; } = 0.3f;
    [field: SerializeField] public float LiftCoefficient { get; private set; } = 0.02f;
    [field: SerializeField] public float MinLiftSpeed { get; private set; } = 5f;
    [field: SerializeField] public float MaxLiftSpeed { get; private set; } = 25f;

    [Header("Control")]
    [field: SerializeField] public float ControlTorqueStrength { get; private set; } = 8f;
    [field: SerializeField] public float AngularDamping { get; private set; } = 1.5f;
    [field: SerializeField, Range(0f, 1f)] public float AutoLevelStrength { get; private set; } = 0.4f;

    [Header("Presentation Thresholds")]
    [field: SerializeField, Range(0f, 1f)] public float WingDeployLiftThreshold { get; private set; } = 0.15f;

    [Header("Transitions")]
    [field: SerializeField] public float EntrySplashHeight { get; private set; } = 2f;
}
```

## `WaterModeConfig`

```csharp
[CreateAssetMenu(menuName = "Rover/Configs/Water Mode Config", fileName = "RoverWaterConfig")]
public sealed class WaterModeConfig : VehicleModeConfig
{
    [Header("Buoyancy")]
    [field: SerializeField] public float WaterDensity { get; private set; } = 1000f;
    [field: SerializeField] public float VolumePerSamplePoint { get; private set; } = 0.4f;
    [field: SerializeField] public float FullySubmergedDepth { get; private set; } = 0.6f;

    [Header("Drag")]
    [field: SerializeField] public float SurfaceDrag { get; private set; } = 0.2f;
    [field: SerializeField] public float UnderwaterDrag { get; private set; } = 3f;
    [field: SerializeField, Range(0f, 1f)] public float UnderwaterThrustMultiplier { get; private set; } = 0.5f;

    [Header("Transitions")]
    [field: SerializeField, Range(0f, 1f)] public float SurfacedThreshold { get; private set; } = 0.1f;
}
```

## `UnderWaterModeConfig`

```csharp
[CreateAssetMenu(menuName = "Rover/Configs/UnderWater Mode Config", fileName = "RoverUnderWaterConfig")]
public sealed class UnderWaterModeConfig : VehicleModeConfig
{
    [Header("Propulsion")]
    [field: SerializeField] public float ThrustForce { get; private set; } = 30f;
    [field: SerializeField] public float AscendThrust { get; private set; } = 18f;
    [field: SerializeField] public float ControlTorqueStrength { get; private set; } = 10f;
    [field: SerializeField] public float AngularDrag { get; private set; } = 4f;
    [field: SerializeField] public float LinearDrag { get; private set; } = 2.5f;

    [Header("Depth")]
    [field: SerializeField] public float MaxDiveDepth { get; private set; } = 60f;
    [field: SerializeField, Range(0f, 1f)] public float CrushWarningDepth01 { get; private set; } = 0.8f;
    [field: SerializeField] public float ObstacleWarningDistance { get; private set; } = 10f;

    [Header("Transformation")]
    [field: SerializeField] public float TransformDuration { get; private set; } = 1.2f;
    [field: SerializeField] public float SurfaceEmergeThreshold { get; private set; } = 0.5f;

    [Header("Presentation Thresholds")]
    [field: SerializeField] public float HeadlightActivationDepth { get; private set; } = 8f;
}
```

`SurfaceEmergeThreshold` is deliberately a shallower depth than `WaterModeConfig.FullySubmergedDepth` would suggest for a *floating* hull — it only needs to be shallow enough that the transformation animation back to a surface hull reads correctly, not literally at the water line. Tune the two independently; they protect different transitions (doc 02).

## Asset layout

```text
Assets/Game/DataAndSettings/Vehicles/Rover/
├── RoverHoverConfig.asset
├── RoverFlightConfig.asset
├── RoverWaterConfig.asset
└── RoverUnderWaterConfig.asset
```

Mirrors the existing `DataAndSettings/Input/` and `DataAndSettings/Render/` convention already in the project. `VehicleController` references the three assets directly as serialized fields (see doc 02's `VehicleController` listing) — no runtime lookup or Addressables indirection needed for something this small and always-loaded.

## Whole-vehicle presets

For multiple rover variants (e.g. a scout rover vs. a heavy cargo rover) that need different physics everywhere at once, add one more SO that just groups references rather than duplicating fields:

```csharp
[CreateAssetMenu(menuName = "Rover/Configs/Vehicle Profile", fileName = "RoverProfile")]
public sealed class VehicleProfile : ScriptableObject
{
    [field: SerializeField] public HoverModeConfig Hover { get; private set; }
    [field: SerializeField] public FlightModeConfig Flight { get; private set; }
    [field: SerializeField] public WaterModeConfig Water { get; private set; }
    [field: SerializeField] public UnderWaterModeConfig UnderWater { get; private set; }
}
```

Swap one `VehicleProfile` reference on the prefab variant instead of three separate config fields. Don't build this until a second vehicle variant actually exists — it's a trivial extraction later, not worth guessing the shape of now.

## Editor validation

Add an `OnValidate` to each concrete config that clamps obviously-invalid combinations (e.g. `MaxLiftSpeed` must stay greater than `MinLiftSpeed`), using `Debug.Assert` or `Mathf.Max` clamps rather than throwing — configs must never hard-fail the editor, only correct themselves with a console warning.

## Live tuning

Processors (doc 03) read every field off the config instance each `FixedTick` — nothing is cached into a local copy at `Awake`/`Enter`. This means changing any slider on `RoverHoverConfig.asset` in the Inspector while in Play mode takes effect on the very next physics step, which is the real-time tuning workflow the spec calls for. The only cost is re-reading a handful of fields 50 times/second, which is negligible next to the raycasts already happening in the same step.
