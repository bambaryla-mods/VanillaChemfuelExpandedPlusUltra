---
summary: With Biotech, a pipe network for wastepacks, sealed tanks that must stay powered, a detoxifier and dye made from waste.
icon: Things/Building/Production/ToxicWasteTank/ToxicWasteTank
---
This page needs the **Biotech** DLC. Without it, none of this loads.

## Waste goes into pipes

Pollution pumps, mech gestators, mech rechargers and anything else that makes wastepacks can connect to the toxic waste network. Your colonists no longer have to haul the packs:

- When a building makes wastepacks and its network has room for all of them, they go straight into the network. If the network is full, or the building isn't connected, the packs come out as usual.
- A connected pollution pump sends its waste into the network as it cleans. It doesn't check whether the network has room, so a pump on a full network loses the waste it pumps.
- Wastepacks that got stuck inside a building are dropped next to it.

The waste then goes to tanks, to the detoxifier, or to the dye refinery.

## Waste tanks

Toxic waste tanks hold {{ value("ThingDef", "VCEUE_ToxicWasteTank", "comps/li[@Class='PipeSystem.CompProperties_ResourceStorage']/storageCapacity") }} wastepacks each and use {{ value("ThingDef", "VCEUE_ToxicWasteTank", "comps/li[@Class='CompProperties_Power']/basePowerConsumption") }} W. Keep them powered:

- An unpowered tank with waste in it takes {{ value("ThingDef", "VCEUE_ToxicWasteTank", "comps/li[@Class='VCEPlusUltra.CompProperties_DamageWhenUnpowered']/damageAmount") }} damage every {{ value("ThingDef", "VCEUE_ToxicWasteTank", "comps/li[@Class='VCEPlusUltra.CompProperties_DamageWhenUnpowered']/tickRateInterval", "ticks") }}. The corrosion never destroys it, but it leaves the tank badly damaged.
- A tank destroyed with waste in it explodes and starts toxic fallout. The fuller the tank, the longer the fallout lasts, and if fallout is already going on, it lasts longer still.

## Getting rid of waste

The waste detofixier burns wastepacks with power. It uses {{ value("ThingDef", "VCEUE_WasteDetofixier", "comps/li[@Class='CompProperties_Power']/basePowerConsumption") }} W, takes up {{ value("ThingDef", "VCEUE_WasteDetofixier", "comps/li[@Class='CompProperties_Refuelable']/fuelCapacity") }} wastepacks at a time, and releases toxic gas if it explodes.

The dye refinery can also make dye from waste and neutroamine. Because dye is easy to make with this mod, its market value is lowered to 0.65 silver.

## Buildings

{{ things("VCEUE_WasteDetofixier") }}

{{ networks("Biotech") }}

## Research

{{ research_table("Biotech") }}

If you use Toxic Wastepack Generator Plus, this network replaces its pipes. On a running save, rebuild them with the new parts.
