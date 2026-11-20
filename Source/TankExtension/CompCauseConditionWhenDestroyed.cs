using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace VCEPlusUltra
{
    public class CompProperties_CauseConditionWhenDestroyed : CompProperties
    {
        public IncidentDef condition;

        public CompProperties_CauseConditionWhenDestroyed()
        {
            this.compClass = typeof(CompCauseConditionWhenDestroyed);
        }
    }

    public class CompCauseConditionWhenDestroyed : ThingComp
    {
        public CompProperties_CauseConditionWhenDestroyed Props => (CompProperties_CauseConditionWhenDestroyed)props;
        public CompResourceStorage compStorage;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compStorage = parent.GetComp<CompResourceStorage>();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // Can run on things that never spawned (no cached comp), or on tanks without storage / a configured condition
            compStorage ??= parent.GetComp<CompResourceStorage>();
            if (compStorage == null || previousMap == null || Props.condition == null || compStorage.AmountStoredPct <= 0)
                return;

            var manager = previousMap.gameConditionManager;
            var condition = manager.GetActiveCondition(Props.condition.gameCondition);
            if (condition != null)
            {
                condition.Duration += (int)(12500 * (compStorage.AmountStoredPct * 100));
                return;
            }

            var parms = StorytellerUtility.DefaultParmsNow(Props.condition.category, previousMap);
            Props.condition.Worker.TryExecute(parms);
            // The incident may refuse to fire, in which case there is no condition to extend
            condition = manager.GetActiveCondition(Props.condition.gameCondition);
            if (condition != null)
                condition.Duration = (int)(25000 * (compStorage.AmountStoredPct * 100));
        }
    }
}
