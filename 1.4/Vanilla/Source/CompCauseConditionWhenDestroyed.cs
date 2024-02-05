using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace VCEPlusUltra
{
    public class CompProperties_CauseConditionWhenDestroyed : CompProperties
    {
        public GameConditionDef condition;

        public CompProperties_CauseConditionWhenDestroyed()
        {
            this.compClass = typeof(CompCauseConditionWhenDestroyed);
        }
    }

    public class CompCauseConditionWhenDestroyed : ThingComp
    {
        public CompProperties_CauseConditionWhenDestroyed Props => base.props as CompProperties_CauseConditionWhenDestroyed;
        public CompResourceStorage compStorage;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compStorage = parent.GetComp<CompResourceStorage>();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (compStorage.AmountStoredPct > 0)
            {
                var condition = previousMap.gameConditionManager.GetActiveCondition(Props.condition);
                if (condition != null)
                {
                    condition.Duration += (int)(12500 * (compStorage.AmountStoredPct * 100));
                }
                else
                {
                    condition = GameConditionMaker.MakeCondition(Props.condition);
                    condition.Duration = (int)(25000 * (compStorage.AmountStoredPct * 100));
                    previousMap.gameConditionManager.RegisterCondition(condition);
                }
            }
        }
    }
}
