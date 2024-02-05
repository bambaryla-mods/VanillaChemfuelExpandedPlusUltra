using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VCEPlusUltra
{
    [HarmonyPatch(typeof(Bill_Production), "ShouldDoNow")]
    public static class Bill_Production_ShouldDoNow_Patch
    {
        public static void Postfix(Bill_Production __instance, ref bool __result)
        {
            if (__result)
            {
                var extension = __instance.recipe.ProducedThingDef?.GetModExtension<RequiredLinkablesToCraft>();
                if (extension != null && __instance.billStack.billGiver is Building_WorkTable worktable)
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
