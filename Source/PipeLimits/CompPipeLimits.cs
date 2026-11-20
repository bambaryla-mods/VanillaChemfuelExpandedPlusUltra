using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace VCEPlusUltra
{
    public class CompProperties_PipeLimits : CompProperties
    {
        public CompProperties_PipeLimits()
        {
            compClass = typeof(CompPipeLimits);
        }
    }

    public enum GaugeRole
    {
        Output,
        Input,
        Other
    }

    /// <summary>
    /// One bar of the pipe limits tab: a pipe net connected to the building.
    /// </summary>
    public class PipeGauge
    {
        public string key;
        public PipeNetDef net;
        public GaugeRole role;

        public string Label => net.resource.name.CapitalizeFirst();

        public string Unit => net.resource.unit;

        public Color Color => net.resource.color;
    }

    public class PipeLimit : IExposable
    {
        public string key;
        public bool useMin;
        public bool useMax;
        public int min;
        public int max;

        // "Stop at or above" below "stop when below" can never be satisfied
        public bool Invalid => useMin && useMax && max < min;

        public void ExposeData()
        {
            Scribe_Values.Look(ref key, "key");
            Scribe_Values.Look(ref useMin, "useMin");
            Scribe_Values.Look(ref useMax, "useMax");
            Scribe_Values.Look(ref min, "min");
            Scribe_Values.Look(ref max, "max");
        }
    }

    /// <summary>
    /// Lets the player stop a pipe system processor when a connected pipe net is below or above given amounts.
    /// Works with both CompAdvancedResourceProcessor and CompResourceProcessor, see PipeLimitsPatches.
    /// </summary>
    public class CompPipeLimits : ThingComp
    {
        private const int CacheTicks = 30;

        private List<PipeLimit> limits = new List<PipeLimit>();
        private List<PipeGauge> gauges;

        private int cachedAtTick = -CacheTicks;
        private bool cachedBlocked;
        private string cachedReason;

        public List<PipeGauge> Gauges => gauges ??= BuildGauges();

        public override void PostExposeData()
        {
            Scribe_Collections.Look(ref limits, "limits", LookMode.Deep);
            limits ??= new List<PipeLimit>();
        }

        public PipeLimit GetLimit(PipeGauge gauge, bool create)
        {
            for (int i = 0; i < limits.Count; i++)
            {
                if (limits[i].key == gauge.key)
                    return limits[i];
            }

            if (!create)
                return null;

            var limit = new PipeLimit { key = gauge.key };
            limits.Add(limit);
            return limit;
        }

        /// <summary>
        /// Current and maximum amount of a gauge. Disconnected nets read as empty.
        /// </summary>
        public void GetReading(PipeGauge gauge, out float current, out float capacity)
        {
            current = capacity = 0f;
            var net = parent.GetComps<CompResource>().FirstOrDefault(c => c.Props.pipeNet == gauge.net)?.PipeNet;
            if (net == null)
                return;

            current = net.CurrentStored();
            for (int i = 0; i < net.storages.Count; i++)
                capacity += net.storages[i].Props.storageCapacity;
        }

        /// <summary>
        /// Same as the check below, but safe to call every tick.
        /// </summary>
        public bool ProductionBlockedCached
        {
            get
            {
                int tick = Find.TickManager.TicksGame;
                if (tick - cachedAtTick >= CacheTicks || tick < cachedAtTick)
                {
                    cachedAtTick = tick;
                    cachedBlocked = CheckBlocked(out cachedReason);
                }
                return cachedBlocked;
            }
        }

        public string BlockedReason => ProductionBlockedCached ? cachedReason : null;

        /// <summary>
        /// Stops when below the minimum, or at/above the maximum.
        /// </summary>
        public bool CheckBlocked(out string reason)
        {
            reason = null;
            var list = Gauges;
            for (int i = 0; i < list.Count; i++)
            {
                var gauge = list[i];
                var limit = GetLimit(gauge, false);
                if (limit == null || (!limit.useMin && !limit.useMax))
                    continue;

                if (limit.Invalid)
                {
                    reason = "VCEUE_PipeLimits_PausedInvalid".Translate(gauge.Label);
                    return true;
                }

                GetReading(gauge, out var current, out _);
                if (limit.useMin && current < limit.min)
                {
                    reason = "VCEUE_PipeLimits_PausedBelow".Translate(gauge.Label, limit.min);
                    return true;
                }
                if (limit.useMax && current >= limit.max)
                {
                    reason = "VCEUE_PipeLimits_PausedAbove".Translate(gauge.Label, limit.max);
                    return true;
                }
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            return BlockedReason;
        }

        private List<PipeGauge> BuildGauges()
        {
            var inputs = new HashSet<PipeNetDef>();
            var outputs = new HashSet<PipeNetDef>();

            var advanced = parent.GetComp<CompAdvancedResourceProcessor>();
            if (advanced?.Props.processes != null)
            {
                foreach (var process in advanced.Props.processes)
                {
                    foreach (var ingredient in process.ingredients)
                    {
                        if (ingredient.pipeNet != null)
                            inputs.Add(ingredient.pipeNet);
                    }
                    foreach (var result in process.results)
                    {
                        if (result.pipeNet != null)
                            outputs.Add(result.pipeNet);
                    }
                }
            }

            var processor = parent.GetComp<CompResourceProcessor>();
            if (processor != null)
            {
                inputs.Add(processor.Props.pipeNet);
                foreach (var result in processor.Props.results)
                {
                    if (result.net != null)
                        outputs.Add(result.net);
                }
            }

            var built = new List<PipeGauge>();
            foreach (var comp in parent.GetComps<CompResource>())
            {
                var netDef = comp.Props.pipeNet;
                if (netDef == null || built.Any(g => g.net == netDef))
                    continue;

                built.Add(new PipeGauge
                {
                    key = netDef.defName,
                    net = netDef,
                    role = outputs.Contains(netDef) ? GaugeRole.Output : inputs.Contains(netDef) ? GaugeRole.Input : GaugeRole.Other
                });
            }

            // Main output first, then inputs
            return built.OrderBy(g => (int)g.role).ToList();
        }
    }
}
