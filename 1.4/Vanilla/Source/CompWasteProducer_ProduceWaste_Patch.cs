using HarmonyLib;
using PipeSystem;
using RimWorld;
using Verse;

namespace VCEPlusUltra
{
    [HarmonyPatch(typeof(CompWasteProducer), nameof(CompWasteProducer.ProduceWaste))]
    public static class CompWasteProducer_ProduceWaste_Patch
    {
        public static bool Prefix(CompWasteProducer __instance, int amountToMake)
        {
            var compResource = __instance.parent.GetComp<CompResource>();
            if (compResource != null)
            {
                var net = compResource.PipeNet;
                if (net != null && net.def.linkToRefuelables.Any(x => x.thing == ThingDefOf.Wastepack) 
                    && net.AvailableCapacity >= amountToMake)
                {
                    net.DistributeAmongStorage(amountToMake, out _);
                    return false;
                }
            }
            return true;
        }
    }
}
