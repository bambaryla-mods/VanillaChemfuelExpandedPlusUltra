"""Build the Vanilla Chemfuel Expanded Plus Ultra player site.

    python site/build.py            # writes site/_build
    python site/build.py --serve    # builds, then serves it on http://localhost:8000
    python site/build.py --out DIR  # writes somewhere else (CI uses this)
    python site/build.py --strict   # exits with an error if the build found problems

Pages are the Markdown files in site/content. They can call the helpers in
Helpers below, e.g. {{ refineries() }} or {{ research_table() }}, to pull tables
read from the mod's defs into the text.
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import http.server
import re
import shutil
import sys
from pathlib import Path

import markdown
from jinja2 import Environment, FileSystemLoader, StrictUndefined, select_autoescape
from lxml import etree
from markupsafe import Markup, escape

SITE = Path(__file__).resolve().parent
ROOT = SITE.parent
BUILD_MARKER = ".vcepu-site-build"  # written into the output so a rebuild only ever deletes its own folder

GITHUB = "https://github.com/bambaryla-mods/VanillaChemfuelExpandedPlusUltra"
SITE_URL = "https://bambaryla-mods.github.io/VanillaChemfuelExpandedPlusUltra/"  # for link previews
SCREENSHOT_EXTS = (".png", ".jpg", ".jpeg", ".webp")

TICKS_PER_HOUR = 2500
TICKS_PER_DAY = 60000

# Top bar groups and the pages inside each, in reading order.
NAV = [
    ("Production", [("refineries", "Refineries"), ("networks", "Pipe networks"), ("research", "Research")]),
    ("Pipe limits", [("pipe-limits", "Pipe limits")]),
    ("Biotech", [("toxic-waste", "Toxic waste")]),
    ("Combat Extended", [("combat-extended", "FSX & prometheum")]),
    ("Compatibility", [("compatibility", "Other mods")]),
]

# Mods named in LoadFolders.xml conditions and MayRequire attributes.
MOD_NAMES = {
    "ludeon.rimworld.biotech": "Biotech",
    "ceteam.combatextended": "Combat Extended",
    "vanillaexpanded.helixiengas": "Vanilla Helixien Gas Expanded",
    "vanillaexpanded.vfeproduction": "Vanilla Furniture Expanded - Production",
}

# Defs from the game and other mods that this mod's defs name. Labels copied from those mods' defs.
EXTERNAL_LABELS = {
    # resources
    "Chemfuel": "chemfuel", "VCHE_Deepchem": "deepchem", "Neutroamine": "neutroamine",
    "Plasteel": "plasteel", "Synthread": "synthread", "Hyperweave": "hyperweave",
    "DevilstrandCloth": "devilstrand", "Dye": "dye", "Wastepack": "toxic wastepack",
    "FSX": "FSX", "Prometheum": "prometheum", "VHGE_Helixien": "helixien",
    "Steel": "steel", "ComponentIndustrial": "component", "ComponentSpacer": "advanced component",
    "Gold": "gold", "Silver": "silver", "Uranium": "uranium",
    # buildings
    "PS_DeepchemRefinery": "deepchem refinery", "ElectricSmelter": "electric smelter",
    "HiTechResearchBench": "hi-tech research bench", "ElectricTailoringBench": "electric tailoring bench",
    "HandTailoringBench": "hand tailoring bench", "VFE_FueledSmelter": "fueled smelter",
    "VHGE_GasPoweredSmelter": "gas-powered smelter", "VFE_TableTailorLarge": "large tailoring table",
    # research
    "BiofuelRefining": "biofuel refining", "VHGE_GasExtraction": "gas extraction",
}
# Pipe nets from other mods, by the resource name they show in game.
EXTERNAL_NETS = {"VCHE_ChemfuelNet": "Chemfuel", "VCHE_DeepchemNet": "Deepchem", "VHGE_HelixienNet": "Helixien gas"}

# The page that shows each module's refineries.
MODULE_PAGES = {"": "refineries", "Combat Extended": "combat-extended", "Biotech": "toxic-waste"}

ROLE_ORDER = ["Refineries", "Pipes", "PipeValves", "PipeContainers", "PipeTaps", "PipeDrains"]
ROLE_LABELS = {"Refineries": "refinery", "Pipes": "pipe", "PipeValves": "valve", "PipeContainers": "tank",
               "PipeTaps": "tap", "PipeDrains": "drain"}


# ---------------------------------------------------------------- formatting

def fmt_num(v, digits: int = 2) -> str:
    """1234.50 -> "1,234.5", 10000 -> "10,000"."""
    if v is None:
        return "—"
    s = f"{v:,.{digits}f}"
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s


def fmt_ticks(ticks: int | None) -> str:
    """Game time: 300 -> "0.12 h", 60000 -> "1 day", 120000 -> "2 days"."""
    if not ticks:
        return "—"
    if ticks >= TICKS_PER_DAY:
        days = ticks / TICKS_PER_DAY
        return f"{fmt_num(days)} day" + ("" if days == 1 else "s")
    return f"{fmt_num(ticks / TICKS_PER_HOUR)} h"


_RICH_TAG = re.compile(r"</?(color|b|i|size)(=[^>]*)?>", re.I)


def clean_desc(text: str | None) -> str:
    """Game descriptions use literal \\n and Unity rich-text tags."""
    if not text:
        return ""
    return _RICH_TAG.sub("", text.replace("\\n", "\n")).strip()


def fmt_desc(text: str | None) -> Markup:
    paras = [p.strip() for p in re.split(r"\n\s*\n", clean_desc(text)) if p.strip()]
    return Markup("<br><br>").join(Markup("<br>").join(escape(x) for x in p.split("\n")) for p in paras)


def cap(s: str | None) -> str:
    return s[:1].upper() + s[1:] if s else ""


def slugify(s: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


def split_front_matter(text: str) -> tuple[dict, str]:
    meta: dict[str, str] = {}
    if text.startswith("---\n"):
        head, _, body = text[4:].partition("\n---\n")
        for line in head.splitlines():
            key, sep, value = line.partition(":")
            if sep:
                meta[key.strip()] = value.strip()
        return meta, body
    return meta, text


# ---------------------------------------------------------------- reading the mod

@dataclasses.dataclass
class Def:
    tag: str
    name: str
    el: etree._Element
    module: str  # "" for the base mod, else the mod its load folder needs ("Biotech", "Combat Extended")
    requires: str  # mod named by a MayRequire attribute on the def, or ""

    def text(self, path: str) -> str | None:
        return self.el.findtext(path)

    @property
    def label(self) -> str:
        return self.el.findtext("label") or self.name


@dataclasses.dataclass
class Row:
    """One thing a refinery can make: a ProcessDef, or the single output of a simple processor."""
    label: str
    inputs: list[tuple[str, float]]
    outputs: list[tuple[str, float]]
    ticks: int
    research: list[str]
    requires: str = ""

    @property
    def per_day(self) -> list[tuple[str, float]]:
        return [(name, n * TICKS_PER_DAY / self.ticks) for name, n in self.outputs] if self.ticks else []


def parse_xml(path: Path) -> etree._Element:
    parser = etree.XMLParser(remove_comments=True, recover=False)
    return etree.parse(str(path), parser).getroot()


def read_load_folders() -> tuple[str, list[tuple[str, str]]]:
    """Newest version in LoadFolders.xml, and its folders with the mod each one needs ("" for none)."""
    root = parse_xml(ROOT / "LoadFolders.xml")
    versions = sorted(root, key=lambda v: tuple(int(x) for x in v.tag[1:].split(".")))
    newest = versions[-1]
    folders = []
    for li in newest.findall("li"):
        need = (li.get("IfModActive") or "").lower()
        folders.append((li.text.strip().strip("/"), MOD_NAMES.get(need, need)))
    return newest.tag[1:], folders


def may_require(el) -> str:
    req = (el.get("MayRequire") or "").lower()
    return MOD_NAMES.get(req, req)


class Mod:
    def __init__(self, problems: list[str]):
        self.problems = problems
        self.version, self.folders = read_load_folders()
        self.defs: dict[tuple[str, str], Def] = {}
        self.patch_processors: list[tuple[str, str, etree._Element]] = []  # (target defName, module, comp)
        self.keyed: dict[str, str] = {}
        for folder, module in self.folders:
            base = ROOT / folder if folder else ROOT
            for f in sorted((base / "Defs").rglob("*.xml")) if (base / "Defs").is_dir() else []:
                for el in parse_xml(f):
                    if not isinstance(el.tag, str):
                        continue
                    name = el.findtext("defName")
                    if name:
                        self.defs[(el.tag, name)] = Def(el.tag, name, el, module, may_require(el))
            for f in sorted((base / "Patches").rglob("*.xml")) if (base / "Patches").is_dir() else []:
                self._read_patch(f, module)
        for f in sorted((ROOT / "Languages" / "English" / "Keyed").glob("*.xml")):
            for el in parse_xml(f):
                if isinstance(el.tag, str):
                    self.keyed[el.tag] = el.text or ""

    def _read_patch(self, path: Path, module: str):
        """Patches that give another mod's building (VCE's deepchem refinery) a list of processes."""
        for comp in parse_xml(path).iter("li"):
            if comp.get("Class") != "PipeSystem.CompProperties_AdvancedResourceProcessor":
                continue
            op = comp.getparent()
            while op is not None and op.find("xpath") is None:
                op = op.getparent()
            if op is None:
                continue
            for target in re.findall(r'defName="([^"]+)"', op.findtext("xpath")):
                self.patch_processors.append((target, module, comp))

    def get(self, tag: str, name: str) -> Def | None:
        return self.defs.get((tag, name))

    def of(self, tag: str) -> list[Def]:
        return [d for (t, _), d in self.defs.items() if t == tag]

    def label(self, name: str) -> str:
        for tag in ("ThingDef", "ResearchProjectDef", "RecipeDef"):
            d = self.get(tag, name)
            if d:
                return d.label
        if name in EXTERNAL_LABELS:
            return EXTERNAL_LABELS[name]
        self.problems.append(f"no label for {name!r}: add it to EXTERNAL_LABELS in build.py")
        return name

    def thing_label(self, name: str) -> str:
        """A label for use mid-sentence: "Unrefined fibers" -> "unrefined fibers", but "FSX" stays."""
        s = self.label(name)
        return s if s[1:2].isupper() else s[:1].lower() + s[1:]

    def net_name(self, net: str) -> str:
        d = self.get("PipeSystem.PipeNetDef", net)
        if d is not None and d.text("resource/name"):
            return d.text("resource/name")
        if net in EXTERNAL_NETS:
            return EXTERNAL_NETS[net]
        self.problems.append(f"no name for pipe net {net!r}: add it to EXTERNAL_NETS in build.py")
        return net


# ---------------------------------------------------------------- turning defs into page data

class Extractor:
    def __init__(self, mod: Mod, textures: "Textures"):
        self.m = mod
        self.tex = textures

    def icon(self, d: Def | None) -> str | None:
        if d is None:
            return None
        return self.tex.icon(d.text("uiIconPath"), d.text("graphicData/texPath"), d.text("graphicData/graphicClass"))

    def research_names(self, el) -> list[str]:
        return [li.text for li in el.findall("researchPrerequisites/li") if li.text]

    def costs(self, d: Def) -> list[tuple[str, float]]:
        out = [(self.m.thing_label(c.tag), float(c.text)) for c in d.el.findall("costList/*")]
        n = d.text("costStuffCount")
        if n:
            out.insert(0, ("material of your choice", float(n)))
        return out

    def power(self, d: Def) -> float | None:
        v = d.el.findtext("comps/li[@Class='CompProperties_Power']/basePowerConsumption")
        return float(v) if v else None

    def process_row(self, name: str, requires: str) -> Row | None:
        p = self.m.get("PipeSystem.ProcessDef", name)
        if p is None:
            self.m.problems.append(f"unknown process {name!r}")
            return None
        thing = lambda li: (self.m.thing_label(li.findtext("thing")) if li.findtext("thing")
                            else self.m.net_name(li.findtext("pipeNet")).lower())
        return Row(
            label=cap((p.text("label") or name).rstrip(".")),
            inputs=[(thing(li), float(li.findtext("countNeeded") or 0)) for li in p.el.findall("ingredients/li")],
            outputs=[(thing(li), float(li.findtext("count") or 0)) for li in p.el.findall("results/li")],
            ticks=int(p.text("ticks") or 0),
            research=[self.m.label(r) for r in self.research_names(p.el)],
            requires=requires or p.requires,
        )

    def rows_for(self, comp, building: Def | None) -> list[Row]:
        cls = comp.get("Class")
        rows = []
        if cls == "PipeSystem.CompProperties_AdvancedResourceProcessor":
            for li in comp.findall("processes/li"):
                row = self.process_row(li.text, may_require(li))
                if row:
                    rows.append(row)
        elif cls == "PipeSystem.CompProperties_ResourceProcessor":
            net = self.m.net_name(comp.findtext("pipeNet")).lower()
            for li in comp.findall("results/li"):
                product = self.m.thing_label(li.findtext("thing"))
                rows.append(Row(
                    label=f"Make {product}",
                    inputs=[(net, float(li.findtext("countNeeded") or 0))],
                    outputs=[(product, float(li.findtext("thingCount") or 0))],
                    ticks=int(li.findtext("eachTicks") or 0),
                    research=[self.m.label(r) for r in self.research_names(building.el)] if building else [],
                ))
        return rows

    @staticmethod
    def has_pipe_limits(name: str, comps: list) -> bool:
        """Same rule as 1.7/Vanilla/Patches/ZZPipeLimits.xml: this mod's processors (and VCE's deepchem refinery)
        that are simple processors, or advanced ones with a pipe connection."""
        cls = {li.get("Class") for li in comps}
        return (name.startswith("VCEUE_") or name == "PS_DeepchemRefinery") and (
            "PipeSystem.CompProperties_ResourceProcessor" in cls
            or {"PipeSystem.CompProperties_AdvancedResourceProcessor", "PipeSystem.CompProperties_Resource"} <= cls)

    def building_card(self, d: Def | None, name: str, module: str, rows: list[Row], comps: list) -> dict:
        size = d.text("size") if d is not None else None
        return {
            "limits": self.has_pipe_limits(name, comps),
            "id": name,
            "label": cap(d.label if d is not None else self.m.label(name)),
            "icon": self.icon(d),
            "desc": fmt_desc(d.text("description")) if d is not None else "",
            "module": module,
            "requires": d.requires if d is not None else "",
            "foreign": d is None,
            "costs": self.costs(d) if d is not None else [],
            "power": self.power(d) if d is not None else None,
            "size": size.strip("()").replace(",", "×") if size else None,
            "research": [self.m.label(r) for r in self.research_names(d.el)] if d is not None else [],
            "rows": rows,
        }

    @functools.cached_property
    def refineries(self) -> list[dict]:
        """Every building that turns one resource into another, in load order."""
        cards = []
        for target, module, comp in self.m.patch_processors:
            if self.m.get("ThingDef", target) is None:  # another mod's building, changed by a patch
                cards.append(self.building_card(None, target, module, self.rows_for(comp, None),
                                                comp.getparent().findall("li")))
        for d in self.m.of("ThingDef"):
            comps = d.el.findall("comps/li")
            rows = [r for comp in comps for r in self.rows_for(comp, d)]
            if rows:
                cards.append(self.building_card(d, d.name, d.module, rows, comps))
        return cards

    @functools.cached_property
    def networks(self) -> list[dict]:
        """The mod's own pipe nets, each with the buildings that connect to it."""
        nets = []
        for net in self.m.of("PipeSystem.PipeNetDef"):
            members = []
            for d in self.m.of("ThingDef"):
                comps = [li for li in d.el.findall("comps/li") if li.findtext("pipeNet") == net.name]
                if not comps:
                    continue
                role = d.text("designatorDropdown") or ""
                capacity = next((float(li.findtext("storageCapacity")) for li in comps if li.findtext("storageCapacity")), None)
                members.append({
                    "label": cap(d.label), "icon": self.icon(d), "role": ROLE_LABELS.get(role, "building"),
                    "order": ROLE_ORDER.index(role) if role in ROLE_ORDER else len(ROLE_ORDER),
                    "capacity": capacity, "costs": self.costs(d), "power": self.power(d),
                    "research": [self.m.label(r) for r in self.research_names(d.el)],
                })
            members.sort(key=lambda x: (x["order"], x["label"]))
            color = net.text("resource/color") or ""
            rgb = [float(x) for x in re.findall(r"[\d.]+", color)[:3]]
            nets.append({
                "id": net.name, "name": net.text("resource/name") or net.name, "unit": net.text("resource/unit") or "",
                "module": net.module, "members": members,
                "color": "#%02x%02x%02x" % tuple(round(c * 255 if max(rgb) <= 1 else c) for c in rgb) if len(rgb) == 3 else None,
            })
        return nets

    @functools.cached_property
    def research(self) -> list[dict]:
        unlocks: dict[str, list[str]] = {}
        for card in self.refineries:
            for r in card["research"]:
                unlocks.setdefault(r, []).append(card["label"])
            for row in card["rows"]:
                for r in row.research:
                    unlocks.setdefault(r, []).append(row.label)
        for d in self.m.of("ThingDef"):  # pipes, tanks and anything else researchable
            for r in self.research_names(d.el):
                unlocks.setdefault(self.m.label(r), []).append(cap(d.label))
        out = []
        for d in self.m.of("ResearchProjectDef"):
            out.append({
                "id": d.name, "label": cap(d.label), "desc": clean_desc(d.text("description")),
                "cost": float(d.text("baseCost") or 0), "tech": d.text("techLevel") or "",
                "prereqs": [cap(self.m.label(li.text)) for li in d.el.findall("prerequisites/li")],
                "bench": cap(self.m.label(d.text("requiredResearchBuilding"))) if d.text("requiredResearchBuilding") else "",
                "unlocks": list(dict.fromkeys(unlocks.get(d.label, []))),
                "module": d.module, "requires": d.requires,
            })
        return out

    @functools.cached_property
    def recipes(self) -> list[dict]:
        out = []
        for d in self.m.of("RecipeDef"):
            ins = []
            for li in d.el.findall("ingredients/li"):
                for t in li.findall("filter/thingDefs/li"):
                    ins.append((self.m.thing_label(t.text), float(li.findtext("count") or 0)))
            out.append({
                "label": cap(d.label),
                "inputs": ins,
                "outputs": [(self.m.thing_label(p.tag), float(p.text)) for p in d.el.findall("products/*")],
                "work": float(d.text("workAmount") or 0),
                "users": [cap(self.m.label(u.text)) + (f" ({may_require(u)})" if may_require(u) else "")
                          for u in d.el.findall("recipeUsers/li")],
            })
        return out


class Textures:
    """Finds a def's icon among the mod's own textures and copies the ones a page uses."""

    def __init__(self):
        self.used: set[str] = set()

    def icon(self, ui_icon: str | None, tex_path: str | None, graphic_class: str | None) -> str | None:
        base = ROOT / "Textures"
        for path in (ui_icon, tex_path):
            if not path:
                continue
            candidates = [f"{path}.png", f"{path}_south.png", f"{path}_north.png"]
            if graphic_class in ("Graphic_StackCount", "Graphic_Random") and (base / path).is_dir():
                candidates += sorted(str(p.relative_to(base)).replace("\\", "/") for p in (base / path).glob("*.png"))
            for c in candidates:
                if (base / c).is_file() and not c.endswith("_m.png"):
                    self.used.add(c)
                    return "tex/" + c
        return None  # the texture comes from the game or another mod

    def copy(self, out: Path):
        for rel in self.used:
            dest = out / "assets" / "tex" / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / "Textures" / rel, dest)


# ---------------------------------------------------------------- page helpers

class Helpers:
    """Functions the Markdown pages can call. Each returns a placeholder that
    is swapped for real HTML after Markdown runs, so Markdown never mangles it."""

    def __init__(self, builder: "Builder"):
        self.b = builder

    def _stash(self, html: str) -> Markup:
        key = f"VCEPUBLOCK{len(self.b.blocks)}X"
        self.b.blocks[key] = html
        return Markup(f"\n\n{key}\n\n")

    def _macro(self, name: str, **kw) -> Markup:
        return self._stash(str(getattr(self.b.macros, name)(**kw)))

    def _module(self, items: list[dict], module: str | None) -> list[dict]:
        picked = items if module is None else [i for i in items if i["module"] == module]
        if not picked:
            self.b.problems.append(f"{self.b.current}: nothing found for module {module!r}")
        return picked

    def refineries(self, *names: str, module: str | None = None):
        """Refinery cards with their processes: the named ones in that order, or all of one module
        ("" base mod, "Biotech", "Combat Extended")."""
        cards = {c["id"]: c for c in self.b.x.refineries}
        if names:
            picked = [cards[n] for n in names if n in cards]
            for n in names:
                if n not in cards:
                    self.b.problems.append(f"{self.b.current}: {n!r} is not a refinery")
        else:
            picked = self._module(list(cards.values()), module)
        self.b.shown_refineries.update(c["id"] for c in picked)
        return self._macro("refineries", cards=picked)

    def networks(self, module: str | None = ""):
        return self._macro("networks", nets=self._module(self.b.x.networks, module))

    def research_table(self, module: str | None = None):
        return self._macro("research_table", rows=self._module(self.b.x.research, module))

    def recipes(self):
        return self._macro("recipes", rows=self.b.x.recipes)

    def pipe_limit_buildings(self):
        """A list of the buildings that get the Pipe limits tab."""
        cards = [c for c in self.b.x.refineries if c["limits"]]
        return self._stash("<ul>" + "".join(
            f'<li><a href="{MODULE_PAGES.get(c["module"], "refineries")}.html#{slugify(c["id"])}">{escape(c["label"])}</a>'
            + (f' <span class="badge">needs {escape(c["module"])}</span>' if c["module"] else "") + "</li>"
            for c in cards) + "</ul>")

    def things(self, *names: str):
        cards = []
        for n in names:
            d = self.b.mod.get("ThingDef", n)
            if d is None:
                self.b.problems.append(f"{self.b.current}: unknown ThingDef {n!r}")
                continue
            cards.append({"label": cap(d.label), "icon": self.b.x.icon(d), "desc": fmt_desc(d.text("description"))})
        return self._macro("things", cards=cards)

    def value(self, tag: str, name: str, path: str, fmt: str = "num") -> str:
        """A number from a def, for use inside a sentence."""
        d = self.b.mod.get(tag, name)
        found = d.el.xpath(path) if d is not None else []
        if not found:
            self.b.problems.append(f"{self.b.current}: value {tag}/{name}/{path} not found")
            return "?"
        v = float(found[0].text if hasattr(found[0], "text") else found[0])
        return {"num": fmt_num, "ticks": lambda x: fmt_ticks(int(x)), "pct": lambda x: f"{x * 100:.0f}%"}[fmt](v)

    def keyed(self, key: str) -> Markup:
        """An English Keyed string (game UI text), as a quote."""
        text = self.b.mod.keyed.get(key)
        if text is None:
            self.b.problems.append(f"{self.b.current}: unknown Keyed string {key!r}")
            return Markup("")
        return self._stash(f'<blockquote class="game-text">{fmt_desc(text)}</blockquote>')

    def requirements(self):
        about = parse_xml(ROOT / "About" / "About.xml")
        deps = [{"name": li.findtext("displayName") or li.findtext("packageId"),
                 "steam": li.findtext("steamWorkshopUrl"), "download": li.findtext("downloadUrl")}
                for li in about.findall("modDependencies/li")]
        versions = [li.text for li in about.findall("supportedVersions/li")]
        return self._macro("requirements", deps=deps, versions=versions)

    def screenshot(self, name: str, caption: str):
        for ext in SCREENSHOT_EXTS:
            src = SITE / "screenshots" / f"{name}{ext}"
            if src.is_file():
                self.b.screenshots.add(src.name)
                return self._stash(f'<figure class="shot"><img src="assets/screenshots/{escape(src.name)}" '
                                   f'alt="{escape(caption)}" loading="lazy"><figcaption>{escape(caption)}</figcaption></figure>')
        self.b.missing_shots.append(f"{name} ({self.b.current}): {caption}")
        return Markup("")


# ---------------------------------------------------------------- building pages

class Builder:
    def __init__(self, out: Path):
        self.out = out
        self.problems: list[str] = []
        self.missing_shots: list[str] = []
        self.screenshots: set[str] = set()
        self.shown_refineries: set[str] = set()
        self.blocks: dict[str, str] = {}
        self.current = ""
        self.mod = Mod(self.problems)
        self.textures = Textures()
        self.x = Extractor(self.mod, self.textures)
        self.env = Environment(loader=FileSystemLoader(SITE / "templates"), autoescape=select_autoescape(["html"]),
                               undefined=StrictUndefined, trim_blocks=True, lstrip_blocks=True)
        self.env.filters.update(num=fmt_num, ticks=fmt_ticks, slug=slugify)
        self.macros = self.env.get_template("macros.html").module
        self.md_env = Environment(undefined=StrictUndefined, autoescape=False)

    def common(self) -> dict:
        about = parse_xml(ROOT / "About" / "About.xml")
        fid = ROOT / "About" / "PublishedFileId.txt"
        workshop = (f"https://steamcommunity.com/sharedfiles/filedetails/?id={fid.read_text().strip()}"
                    if fid.is_file() else GITHUB)
        return {"nav": NAV, "github": GITHUB, "workshop": workshop, "site_url": SITE_URL,
                "mod_name": about.findtext("name"), "authors": about.findtext("author"), "game_version": self.mod.version}

    def render_markdown(self, slug: str, text: str) -> tuple[dict, Markup, list[dict]]:
        self.current = slug
        meta, body = split_front_matter(text)
        helpers = Helpers(self)
        names = {n: getattr(helpers, n) for n in dir(helpers) if not n.startswith("_")}
        body = self.md_env.from_string(body).render(**names)
        md = markdown.Markdown(extensions=["tables", "attr_list", "md_in_html", "toc"],
                               extension_configs={"toc": {"slugify": lambda s, sep: slugify(s)}})
        html = md.convert(body)
        for key, block in self.blocks.items():
            html = html.replace(f"<p>{key}</p>", block).replace(key, block)
        toc = [{"id": t["id"], "name": t["name"]} for t in md.toc_tokens if t["level"] == 2]
        return meta, Markup(html), toc

    def build(self):
        if self.out.exists():
            if not (self.out / BUILD_MARKER).exists():
                sys.exit(f"{self.out} exists and wasn't made by this script; refusing to delete it")
            shutil.rmtree(self.out)
        (self.out / "assets").mkdir(parents=True)
        (self.out / BUILD_MARKER).write_text("site/build.py output\n")
        common = self.common()

        order = [(group, slug, title) for group, pages in NAV for slug, title in pages]
        cards = []
        for i, (group, slug, title) in enumerate(order):
            src = SITE / "content" / f"{slug}.md"
            if not src.is_file():
                self.problems.append(f"missing page content/{slug}.md")
                continue
            meta, content, toc = self.render_markdown(slug, src.read_text(encoding="utf-8"))
            page = {"slug": slug, "title": title, "group": group}
            group_pages = next(p for g, p in NAV if g == group)
            prev = {"slug": order[i - 1][1], "title": order[i - 1][2]} if i > 0 else None
            nxt = {"slug": order[i + 1][1], "title": order[i + 1][2]} if i + 1 < len(order) else None
            html = self.env.get_template("page.html").render(
                **common, page=page, meta=meta, content=content, toc=toc, group_pages=group_pages,
                active_group=group, prev=prev, next=nxt)
            (self.out / f"{slug}.html").write_text(html, encoding="utf-8")
            # A card icon is a texture path from the page's front matter, e.g. Things/Building/Production/DyeRefinery/DyeRefinery
            icon = self.textures.icon(None, meta["icon"], "Graphic_Multi") if meta.get("icon") else None
            if meta.get("icon") and icon is None:
                self.problems.append(f"{slug}: card icon {meta['icon']!r} not found")
            cards.append({"slug": slug, "title": title, "group": group, "summary": meta.get("summary", ""),
                          "icon": icon})

        meta, content, _ = self.render_markdown("index", (SITE / "content" / "index.md").read_text(encoding="utf-8"))
        html = self.env.get_template("home.html").render(**common, meta=meta, content=content, cards=cards,
                                                         active_group=None)
        (self.out / "index.html").write_text(html, encoding="utf-8")

        shutil.copytree(SITE / "static", self.out / "assets", dirs_exist_ok=True)
        shutil.copyfile(ROOT / "About" / "Preview.png", self.out / "assets" / "preview.png")
        self.textures.copy(self.out)
        for name in self.screenshots:
            (self.out / "assets" / "screenshots").mkdir(exist_ok=True)
            shutil.copyfile(SITE / "screenshots" / name, self.out / "assets" / "screenshots" / name)
        (self.out / ".nojekyll").write_text("")

        for c in self.x.refineries:
            if c["id"] not in self.shown_refineries:
                self.problems.append(f"refinery {c['id']} ({c['label']}) isn't on any page: add it to a refineries() call")


def serve(out: Path):
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(out))
    print("Serving on http://localhost:8000 (Ctrl+C to stop)")
    http.server.ThreadingHTTPServer(("localhost", 8000), handler).serve_forever()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=Path, default=SITE / "_build")
    ap.add_argument("--serve", action="store_true")
    ap.add_argument("--strict", action="store_true", help="fail if the build found problems")
    args = ap.parse_args()
    b = Builder(args.out.resolve())
    b.build()
    print(f"Built {args.out}")
    for p in dict.fromkeys(b.problems):
        print(f"  problem: {p}")
    if b.missing_shots:
        print("  screenshots not added yet:")
        for s in b.missing_shots:
            print(f"    {s}")
    if args.strict and b.problems:
        sys.exit(1)
    if args.serve:
        serve(args.out)


if __name__ == "__main__":
    main()
