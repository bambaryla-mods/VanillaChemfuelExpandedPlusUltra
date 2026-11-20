---
summary: Every refinery in the base mod, what each process takes and makes, how long it runs and which research unlocks it.
icon: Things/Building/Production/neutroRefinery/neutroRefinery
---
A refinery pulls its inputs from the pipe networks it touches and pushes the product into the output network. If nothing is connected on the output side, the product comes out as items next to it. Choose what it makes in the refinery's tab. The autoclave and autoweave are the exception: colonists bring them raw plasteel or unrefined fibers by hand.

Each product has up to three tiers, and the textile and plasteel lines have a fourth. The first tier needs industrial research, and the later ones need spacer or ultra research. A higher tier makes more of the same product per day, so switch to it once you unlock it.

The "Per day" column is the output if the refinery runs nonstop with enough input. Refineries fed by pipes also get a [Pipe limits](pipe-limits.html) tab.

## Chemfuel, neutroamine and dye

Vanilla Chemfuel Expanded's deepchem refinery now runs on the same process system as the rest, with three chemfuel tiers.

{{ refineries("PS_DeepchemRefinery", "VCEUE_NeutroamineRefinery", "VCEUE_DyeRefinery") }}

## Plasteel

The raw plasteel refinery turns deepchem into raw plasteel, and the autoclave cures it into plasteel. At the fourth tier, the auto plasteel refinery skips the raw step.

{{ refineries("VCEUE_RawPlasteelRefinery", "VCEUE_Autoclave", "VCEUE_AutoPlasteelRefinery") }}

## Textiles

The automatic weavery turns deepchem into unrefined fibers, and the autoweave makes devilstrand, synthread or hyperweave from them. At the fourth tier, the thread molder makes the cloth straight from deepchem or chemfuel.

{{ refineries("VCEUE_AdvancedAutoWeavery", "VCEUE_Autoweave", "VCEUE_AutoWeaver") }}

## Finishing by hand

Raw plasteel and unrefined fibers are new resources:

{{ things("VCEUE_RawPlasteel", "VCEUE_RawThread") }}

Without an autoclave or autoweave, a crafter can finish them by hand:

{{ recipes() }}

With [Vegetable Garden Project](compatibility.html), looms can also weave unrefined fibers.
