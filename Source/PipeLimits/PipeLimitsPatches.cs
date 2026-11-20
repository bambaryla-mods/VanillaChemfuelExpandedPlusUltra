using HarmonyLib;
using PipeSystem;

namespace VCEPlusUltra
{
    /// <summary>
    /// Advanced processors: don't start a new cycle while a limit is violated.
    /// A cycle that already consumed its ingredients is allowed to finish.
    /// </summary>
    [HarmonyPatch(typeof(Process), nameof(Process.Tick))]
    public static class Process_Tick_PipeLimits_Patch
    {
        public static bool Prefix(Process __instance)
        {
            if (__instance.tickLeft < __instance.ticksOrQualityTicks)
                return true;

            var limits = __instance.advancedProcessor?.parent.GetComp<CompPipeLimits>();
            return limits == null || !limits.ProductionBlockedCached;
        }
    }

    /// <summary>
    /// Simple processors: stop buffering resource from the net while a limit is violated.
    /// A buffer that already started filling keeps going.
    /// </summary>
    [HarmonyPatch(typeof(CompResourceProcessor), nameof(CompResourceProcessor.PushTo))]
    public static class CompResourceProcessor_PushTo_PipeLimits_Patch
    {
        public static bool Prefix(CompResourceProcessor __instance, ref float __result)
        {
            if (__instance.Storage > 0f)
                return true;

            var limits = __instance.parent.GetComp<CompPipeLimits>();
            if (limits == null || !limits.ProductionBlockedCached)
                return true;

            __result = 0f;
            return false;
        }
    }
}
