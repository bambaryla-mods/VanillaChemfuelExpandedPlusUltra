using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace VCEPlusUltra
{
    [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new Type[] { typeof(Pawn), typeof(SkillDef) })]
    public static class QualityUtility_GenerateQualityCreatedByPawn_Patch
    {
        public static void Postfix(ref QualityCategory __result, Pawn pawn)
        {
            var worktable = pawn.CurJob?.bill?.billStack?.billGiver as Building_WorkTable;
            if (worktable != null)
            {
                var comp = worktable.GetComp<CompAffectedByFacilities>();
                if (comp != null)
                {
                    foreach (var link in comp.LinkedFacilitiesListForReading)
                    {
                        var props = link.TryGetComp<CompQualityOffset>();
                        if (props != null)
                        {
                            __result = (QualityCategory)((int)__result + props.Props.qualityOffset);
                            __result = (QualityCategory)Mathf.Clamp((int)__result, (int)QualityCategory.Awful, (int)QualityCategory.Legendary);
                            return;
                        }
                    }
                }
            }
        }
    }

}
