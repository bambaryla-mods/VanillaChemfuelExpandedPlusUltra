<<<<<<< HEAD:1.4/Vanilla/Source/CompWasteProducer_ProduceWaste_Patch.cs
﻿using HarmonyLib;
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

			foreach (Thing item in thingOwner)
            {
                if (item.def == ThingDefOf.Wastepack)
                {
                    thingOwner.TryDrop(item, __instance.parent.Position, __instance.parent.Map, ThingPlaceMode.Near, out Thing _);
                }
            }
		}
    }
}
=======
﻿using HarmonyLib;
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
    }
}
>>>>>>> 5a0ce8438d8dab59ab06b6f6e404cb25694eac64:Source/CompWasteProducer_ProduceWaste_Patch.cs
