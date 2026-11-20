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
            foreach (var compResource in __instance.parent.GetComps<CompResource>())
            {
                var net = compResource.PipeNet;
                if (net?.def?.linkToRefuelables != null && net.def.linkToRefuelables.Any(x => x?.thing == ThingDefOf.Wastepack) 
                    && net.AvailableCapacity >= amountToMake)
                {
                    net.DistributeAmongStorage(amountToMake, out _);
                    return false;
                }
            }
            return true;
        }

        public static void Postfix(CompWasteProducer __instance)
        {
            ThingOwner thingOwner = __instance.parent.TryGetInnerInteractableThingOwner();
            if (thingOwner == null)
            {
                return;
            }

            // Iterate by index, from the end, since TryDrop removes the item from thingOwner.
            // Using foreach here would invalidate the enumerator and throw InvalidOperationException
            // (e.g. when the building is deconstructed, which calls ProduceWaste one last time).
            for (int i = thingOwner.Count - 1; i >= 0; i--)
            {
                Thing item = thingOwner[i];
                if (item.def == ThingDefOf.Wastepack)
                {
                    thingOwner.TryDrop(item, __instance.parent.Position, __instance.parent.Map, ThingPlaceMode.Near, out Thing _);
                }
            }
		}
    }
}