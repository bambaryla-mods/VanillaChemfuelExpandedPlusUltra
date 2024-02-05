using HarmonyLib;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace VCEPlusUltra
{
    [HarmonyPatch(typeof(CompPollutionPump), nameof(CompPollutionPump.Pump))]
    public static class CompPollutionPump_Pump_Patch
    {
        public static bool Prefix(CompPollutionPump __instance)
        {
            var compResource = __instance.parent.GetComp<CompResource>();
            if (compResource != null)
            {
                var net = compResource.PipeNet;
                if (net != null && net.def.linkToRefuelables.Any(x => x.thing == ThingDefOf.Wastepack))
                {
                    Pump(__instance, net);
                    return false;
                }
            }
            return true;
        }

        private static void Pump(CompPollutionPump __instance, PipeNet net)
        {
            IntVec3 cellToUnpollute = __instance.GetCellToUnpollute();
            if (!cellToUnpollute.IsValid)
            {
                return;
            }
            Map map = __instance.parent.Map;
            map.pollutionGrid.SetPolluted(cellToUnpollute, isPolluted: false);
            __instance.currentIntervalPumps++;
            if (__instance.Props.pumpEffecterDef != null)
            {
                __instance.Props.pumpEffecterDef.Spawn(__instance.parent.Position, map).Cleanup();
            }
            if (__instance.Props.pumpsPerWastepack > 0 && __instance.currentIntervalPumps % __instance.Props.pumpsPerWastepack == 0)
            {
                net.DistributeAmongStorage(1, out _);
                __instance.currentIntervalPumps = 0;
            }
            MoteMaker.MakeAttachedOverlay(__instance.parent, ThingDefOf.Mote_PollutionPump, Vector3.zero);
        }
    }
}
