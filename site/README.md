# Player site

The site at https://databobek.github.io/VanillaChemfuelExpandedPlusUltra/ shows players what the mod adds and what it needs. It isn't part of the mod, and `_PublisherPlus.xml` keeps this folder out of the Workshop upload.

## How it works

`build.py` reads the newest version in `LoadFolders.xml` (1.7 today) and loads the defs and patches from all its folders, Biotech and Combat Extended included. The refinery, network and research tables, building costs, power use and icons all come from those defs, so they stay correct when you rebalance something. The text in `content/` is written by hand and has to be updated when a mechanic changes.

Labels for things that live in other mods or the base game (chemfuel, deepchem, the electric smelter and so on) are in `EXTERNAL_LABELS` and `EXTERNAL_NETS` at the top of `build.py`. When a def starts naming something new from another mod, the build tells you which label to add.

## Building and previewing

```
pip install -r site/requirements.txt
python site/build.py --serve
```

Then open http://localhost:8000. The build lists any problems it finds, such as an unknown def name, a missing label, or a refinery that no page shows. It only ever deletes an output folder it created itself.

Pushing to `main` rebuilds and publishes the site through `.github/workflows/pages.yml`. One-time setup on GitHub: **Settings > Pages > Source: GitHub Actions**. On a free account, Pages only works for a public repository.

The workflow builds with `--strict`, so a problem fails the run and the live site stays as it was. The site is built from what's committed, so commit new defs together with the pages that describe them.

## Where things live

| Path | What |
|---|---|
| `content/*.md` | One Markdown file per page. The front matter sets the home page card (`summary`, and `icon` as a texture path). |
| `build.py` | Top bar groups and page order (`NAV`), external labels, def reading, and the helpers pages can call. |
| `templates/` | Page layout (`base.html`, `page.html`, `home.html`) and the tables and cards (`macros.html`). |
| `static/` | CSS, the menu script, the favicon and the Audiowide font (OFL). |
| `screenshots/` | In-game screenshots, if you add any. |

## Helpers a page can call

```
{{ refineries("VCEUE_DyeRefinery", "PS_DeepchemRefinery") }}   refinery cards with their processes, in this order
{{ refineries(module="Combat Extended") }}                    every refinery of one module ("" for the base mod)
{{ networks() }}                       the base mod's pipe networks; networks("Biotech") for one module
{{ research_table() }}                 every research project; research_table("Biotech") for one module
{{ recipes() }}                        the hand-crafting recipes (smelter, tailoring bench)
{{ things("VCEUE_RawPlasteel") }}      cards with icon and description for any ThingDefs
{{ pipe_limit_buildings() }}           the buildings that get the Pipe limits tab
{{ value("ThingDef", "VCEUE_ToxicWasteTank", "comps/li[@Class='CompProperties_Power']/basePowerConsumption") }}
                                       a number from a def, inside a sentence; fmt="ticks" or "pct" to format it
{{ keyed("VCEUE_PipeLimits_Desc") }}   quote an English Keyed string (game UI text)
{{ requirements() }}                   the required mods, from About.xml
{{ screenshot("name", "caption") }}    screenshots/name.png, once you add it
```

Use `value()` rather than typing a number into the text, so the page follows the def when it changes.

Every refinery has to appear on some page. If you add a refinery and forget it, the build reports it.
