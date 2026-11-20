---
summary: A tab on every refinery that pauses production when an input network runs low or an output tank fills up.
icon: Things/Building/Production/DyeRefinery/DyeRefinery
---
Refineries normally run until they run out of input. The **Pipe limits** tab lets you stop them earlier: keep a reserve of chemfuel for your generators, or stop making neutroamine once the tanks hold enough.

Every refinery that takes its input through pipes has the tab, Vanilla Chemfuel Expanded's deepchem refinery included:

{{ pipe_limit_buildings() }}

The autoclave and autoweave don't have it. Colonists load them by hand, so there's no network to measure.

## How it works

The tab shows one gauge for each network the refinery is connected to. Outputs come first, then inputs, then any other connected network. Each gauge shows how much the network holds out of its total tank capacity.

On each gauge you can turn on either limit, or both:

- **Stop when below**: pause while the network holds less than this amount. Use it on inputs to keep a reserve.
- **Stop when at or above**: pause once the network holds this much. Use it on outputs so production stops when the tanks are full enough.

Drag the orange and red markers on the bar, or type the amounts in. The field next to each one shows the amount as a share of the network's capacity. The game's own help text for the tab says:

{{ keyed("VCEUE_PipeLimits_Desc") }}

The top of the tab says whether production is allowed, and if not, which limit stopped it. The same line appears when you select the refinery.

## Details

- Limits only stop a **new** cycle. A cycle that has already taken its input always finishes.
- The raw plasteel refinery and the automatic weavery work a little differently. They fill an internal buffer before they produce anything, so a limit stops them only while that buffer is empty.
- If you set the "at or above" amount lower than the "below" amount, the limits contradict each other. The field turns red and the refinery stays paused until you fix it.
- A refinery that isn't connected to a network reads that network as empty.
- Limits are saved with the refinery, so they survive save and load.
