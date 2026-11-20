# Tank Extension (VCEPlusUltraTankExtension.dll)

## Purpose
Two ThingComps for Pipe System storage tanks (buildings with `CompResourceStorage`). Only the toxic waste tank in `1.7/Mods/Biotech/Defs/ToxicWasteNet.xml` uses them today. The assembly has no Harmony patches and no `Mod` class.

## Requirements

### Requirement: Damage when unpowered
`CompProperties_DamageWhenUnpowered` (`tickRateInterval` = 1, `damageAmount`) / `CompDamageWhenUnpowered` SHALL damage a tank that is unpowered but still holds resource.

#### Scenario: Damage tick
- GIVEN `CompPowerTrader` and `CompResourceStorage` exist on the parent
- WHEN `parent.IsHashIntervalTick(tickRateInterval)`, power is off, and `AmountStored > 0`
- THEN the parent takes `damageAmount` Blunt damage

#### Scenario: Never destroys
- GIVEN `HitPoints <= damageAmount`
- THEN no damage is applied (the tank is left at low HP)

#### Scenario: Missing comps
- GIVEN no power trader or no storage comp
- THEN the comp does nothing

### Requirement: Condition when destroyed
`CompProperties_CauseConditionWhenDestroyed` (`condition`: IncidentDef) / `CompCauseConditionWhenDestroyed` SHALL trigger or extend a game condition when a non-empty tank is destroyed.

#### Scenario: Guard
- WHEN `PostDestroy` runs
- THEN it exits if the storage comp is missing (resolved lazily if the thing never spawned), `previousMap` is null, `condition` is unset, or stored percent <= 0

#### Scenario: Condition already active
- GIVEN the incident's game condition is active on the map
- THEN its duration is extended by `12500 * (storedPct * 100)` ticks

#### Scenario: Condition not active
- THEN the incident is executed with `StorytellerUtility.DefaultParmsNow(category, map)`
- AND if that produced an active condition, its duration is set to `25000 * (storedPct * 100)` ticks
- AND if the incident refused to fire, nothing else happens
