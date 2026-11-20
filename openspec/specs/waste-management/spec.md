# Waste Management (VCEPlusUltraWasteManagement.dll)

## Purpose
Route wastepack production and pollution pumping into Pipe System nets that accept wastepacks, and stop wastepacks getting stuck inside buildings.

## Types
- `CompWasteProducer_ProduceWaste_Patch`: prefix and postfix on `CompWasteProducer.ProduceWaste`.
- `CompPollutionPump_Pump_Patch`: prefix on `CompPollutionPump.Pump`.
- `VCEPlusUltraWasteManagementMod`: applies the Harmony id `VCEPlusUltra.WasteManagement` with `PatchAll()`.

## Definitions
A "waste net" is a `PipeNet` whose def's `linkToRefuelables` contains an entry with `thing == ThingDefOf.Wastepack`.

## Requirements

### Requirement: Producers feed waste nets
#### Scenario: Net has room
- GIVEN the producer's parent has a `CompResource` on a waste net with `AvailableCapacity >= amountToMake`
- WHEN `ProduceWaste(amountToMake)` runs
- THEN the amount is distributed among the net's storage and the vanilla method is skipped

#### Scenario: No waste net or net full
- THEN vanilla behaviour runs unchanged

### Requirement: Drop trapped wastepacks
After `ProduceWaste` (original or skipped), any Wastepack in the parent's inner interactable `ThingOwner` SHALL be dropped near the building (`ThingPlaceMode.Near`).

#### Scenario: Safe iteration
- THEN items are iterated by index from the end, so removal during the drop (including the final `ProduceWaste` on deconstruct) never throws
- AND a parent with no inner owner is ignored

### Requirement: Pumps feed waste nets
#### Scenario: Connected to a waste net
- WHEN `CompPollutionPump.Pump` runs on a building with a `CompResource` on a waste net
- THEN, if a cell to unpollute exists, it is cleaned, `currentIntervalPumps` increments, the pump effecter spawns, and the pollution mote is attached
- AND every `pumpsPerWastepack` pumps (when > 0) one unit is distributed to the net's storage and the counter resets
- AND the vanilla Pump is skipped

#### Scenario: No valid cell
- THEN nothing happens and vanilla Pump is still skipped

#### Scenario: Not connected
- THEN vanilla Pump runs unchanged

### Requirement: Known behaviour to preserve
The pump patch does not check net capacity. When the net is full, `DistributeAmongStorage` discards the unit it was given.
