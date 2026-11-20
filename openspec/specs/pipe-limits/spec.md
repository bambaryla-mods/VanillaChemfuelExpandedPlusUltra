# Pipe Limits (VCEPlusUltraPipeLimits.dll)

## Purpose
Let the player pause a Pipe System processor when a connected pipe net holds too little or too much of its resource.

## Types
- `CompProperties_PipeLimits` / `CompPipeLimits` (ThingComp): stores the limits and evaluates them.
- `PipeGauge`, `GaugeRole { Output, Input, Other }`, `PipeLimit` (IExposable): one gauge per connected net, and the limit saved for it.
- `ITab_PipeLimits`: the inspector tab.
- `Process_Tick_PipeLimits_Patch`, `CompResourceProcessor_PushTo_PipeLimits_Patch`: Harmony prefixes.
- `VCEPlusUltraPipeLimitsMod`: applies the Harmony id `VCEPlusUltra.PipeLimits` with `PatchAll()`.

## Requirements

### Requirement: Attachment via XML patch
`1.7/Vanilla/Patches/ZZPipeLimits.xml` SHALL add `CompProperties_PipeLimits` to target buildings and ensure `VCEPlusUltra.ITab_PipeLimits` is in their `inspectorTabs` (appending to an existing list, or creating it).

### Requirement: Gauge construction
The comp SHALL build one gauge per distinct `PipeNetDef` among the building's `CompResource` comps (lazily, once).

#### Scenario: Role assignment
- WHEN a net is a result of any process of `CompAdvancedResourceProcessor`, or a result net of `CompResourceProcessor`
- THEN its gauge role is Output
- WHEN it is only an ingredient net (advanced) or the `CompResourceProcessor` input net
- THEN its role is Input
- OTHERWISE the role is Other

#### Scenario: Ordering
- THEN gauges are ordered Output, Input, Other
- AND the gauge key is the `PipeNetDef.defName`

### Requirement: Reading
`GetReading` SHALL return the net's `CurrentStored()` and the summed `storageCapacity` of its storages. A building not connected to the net reads 0 / 0.

### Requirement: Limit evaluation
A limit has an optional `min` (stop when current < min) and an optional `max` (stop when current >= max). Gauges with no enabled limit are ignored.

#### Scenario: Below minimum
- GIVEN `useMin` and current < min
- THEN production is blocked with reason `VCEUE_PipeLimits_PausedBelow(label, min)`

#### Scenario: At or above maximum
- GIVEN `useMax` and current >= max
- THEN production is blocked with reason `VCEUE_PipeLimits_PausedAbove(label, max)`

#### Scenario: Invalid range
- GIVEN both enabled and max < min
- THEN production is blocked with reason `VCEUE_PipeLimits_PausedInvalid(label)`

### Requirement: Cached evaluation
`ProductionBlockedCached` SHALL re-evaluate at most every 30 ticks (and when the game tick moves backwards). `BlockedReason` is the cached reason or null. `CompInspectStringExtra` SHALL return `BlockedReason`.

### Requirement: Persistence
Limits SHALL be saved under `limits` (deep list of key, useMin, useMax, min, max); a missing list loads as empty. Limits are created on demand by the tab, never by evaluation.

### Requirement: Advanced processors honour limits
The patch of `Process.Tick` SHALL skip the tick when limits block production, only if no cycle is in progress (`tickLeft >= ticksOrQualityTicks`).

#### Scenario: Cycle in progress
- GIVEN ingredients were consumed and the cycle started
- THEN ticking continues and the cycle finishes even if limits are violated

### Requirement: Simple processors honour limits
The patch of `CompResourceProcessor.PushTo` SHALL return 0 and skip the original when blocked, only if the processor's buffer `Storage` is 0.

#### Scenario: Buffer already filling
- GIVEN `Storage > 0`
- THEN the original runs unchanged

### Requirement: Inspector tab
`ITab_PipeLimits` SHALL be visible only when the selected thing has `CompPipeLimits` with at least one gauge, titled `VCEUE_PipeLimits_Tab`, size 440x420.

#### Scenario: Layout
- THEN a status line shows the block reason (red-tinted) or `VCEUE_PipeLimits_Allowed`, with a `VCEUE_PipeLimits_Desc` tooltip
- AND a scroll list shows per gauge: label with role, `current / capacity unit`, a fillable bar in the resource colour, min (orange) and max (red) markers, and "stop below"/"stop above" rows with checkbox, numeric field (0-1,000,000) and percent of capacity

#### Scenario: Dragging markers
- WHEN the player drags an enabled marker on the bar
- THEN the value follows the mouse, rounded to an int of capacity, and cannot cross the other enabled marker
- AND releasing the mouse ends the drag

#### Scenario: Typed values
- WHEN the player types a value
- THEN it is stored unclamped, and if max < min the field is tinted red and shows `VCEUE_PipeLimits_Invalid`

#### Scenario: Enabling max
- WHEN max is enabled while its value is <= 0 and capacity > 0
- THEN it defaults to the capacity
