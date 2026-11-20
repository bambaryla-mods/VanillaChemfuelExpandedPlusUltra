# Build and Packaging

## Purpose
Define how the mod's code is split into assemblies and delivered for RimWorld 1.7.

## Requirements

### Requirement: Three independent assemblies
The build SHALL produce `VCEPlusUltraPipeLimits.dll`, `VCEPlusUltraTankExtension.dll` and `VCEPlusUltraWasteManagement.dll`, replacing the single `VCEPlusUltra.dll` used in 1.5/1.6.

#### Scenario: Output location
- WHEN any project under `Source/` is built
- THEN its DLL is written to `1.7/Vanilla/Assemblies/` with no debug symbols or deps file

#### Scenario: Shared namespace
- WHEN a def references a comp or tab
- THEN the type name is `VCEPlusUltra.<Type>` regardless of which assembly defines it

### Requirement: Build configuration
All projects SHALL inherit `Source/Directory.Build.props`: `net10.0`, latest C#, optimized, deterministic, nullable and implicit usings disabled, `Assembly-CSharp` publicized via Krafs.Publicizer, referenced from the RimWorld install directory (not copied locally).

### Requirement: Dependencies
PipeLimits and WasteManagement SHALL reference Lib.Harmony with runtime assets excluded. TankExtension SHALL NOT use Harmony. All three SHALL reference `PipeSystem.dll` (Steam workshop item 2023507013, 1.7 build) without copying it.

### Requirement: Version-gated loading
`LoadFolders.xml` SHALL load `1.7/Vanilla` always, `1.7/Mods/CE` only if `CETeam.CombatExtended` is active, and `1.7/Mods/Biotech` only if `Ludeon.RimWorld.Biotech` is active.

#### Scenario: Biotech-only comps
- GIVEN Biotech is not active
- THEN `CompProperties_DamageWhenUnpowered` and `CompProperties_CauseConditionWhenDestroyed` (used only by `1.7/Mods/Biotech/Defs/ToxicWasteNet.xml`) are never instantiated, but the assembly still loads
