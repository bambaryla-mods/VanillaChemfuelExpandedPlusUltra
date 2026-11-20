using PipeSystem;
using RimWorld;
using Verse;

namespace VCEPlusUltra
{
    public class CompProperties_DamageWhenUnpowered : CompProperties
    {
        public int tickRateInterval = 1;
        public int damageAmount;

        public CompProperties_DamageWhenUnpowered()
        {
            this.compClass = typeof(CompDamageWhenUnpowered);
        }
    }

    public class CompDamageWhenUnpowered : ThingComp
    {
        public CompProperties_DamageWhenUnpowered Props => (CompProperties_DamageWhenUnpowered)props;
        public CompPowerTrader compPower;
        public CompResourceStorage compStorage;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPower = parent.GetComp<CompPowerTrader>();
            compStorage = parent.GetComp<CompResourceStorage>();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (compPower == null || compStorage == null)
                return;
            if (parent.IsHashIntervalTick(Props.tickRateInterval) && compPower.PowerOn is false && compStorage.AmountStored > 0)
            {
                if (parent.HitPoints > Props.damageAmount)
                {
                    parent.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damageAmount));
                }
            }
        }
    }
}
