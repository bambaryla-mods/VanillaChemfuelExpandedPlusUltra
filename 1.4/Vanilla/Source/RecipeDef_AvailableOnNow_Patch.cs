using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VCEPlusUltra
{
    [HarmonyPatch(typeof(RecipeDef), "AvailableOnNow")]
    public static class RecipeDef_AvailableOnNow_Patch
    {
        public static void Postfix(RecipeDef __instance, Thing thing, ref bool __result)
        {
            if (__result)
            {
                var extension = __instance.ProducedThingDef?.GetModExtension<RequiredLinkablesToCraft>();
                if (extension != null && thing is Building_WorkTable worktable)
                {
                    var comp = worktable.GetComp<CompAffectedByFacilities>();
                    if (comp != null)
                    {
                        if (comp.LinkedFacilitiesListForReading.Any(x => extension.requiredLinkables.Contains(x.def)) is false)
                        {
                            __result = false;
                        }
                    }
                }
            }
        }
    }

}
