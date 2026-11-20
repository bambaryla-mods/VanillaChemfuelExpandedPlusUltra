# Project: Vanilla Chemfuel Expanded Plus Ultra (RimWorld mod)

The C# code lives in `Source/` and builds into three assemblies for RimWorld 1.7, written to `1.7/Vanilla/Assemblies/`.
All code uses the namespace `VCEPlusUltra`, and XML defs reference comps as `Class="VCEPlusUltra.<Type>"`.

Dependencies: RimWorld (Assembly-CSharp, publicized), Pipe System (`PipeSystem.dll`) and Harmony 2.x. The Harmony mod provides Harmony at runtime, so the mod doesn't ship it.

The specs describe the current 1.7 behaviour:
- `specs/build-packaging`: assemblies, build targets and load order
- `specs/pipe-limits`: `VCEPlusUltraPipeLimits.dll`
- `specs/tank-extension`: `VCEPlusUltraTankExtension.dll`
- `specs/waste-management`: `VCEPlusUltraWasteManagement.dll`
