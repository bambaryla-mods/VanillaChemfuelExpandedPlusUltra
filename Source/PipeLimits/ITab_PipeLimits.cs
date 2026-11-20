using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VCEPlusUltra
{
    /// <summary>
    /// Shows a bar for every pipe net connected to the building, with a stop-below and a stop-above limit for each.
    /// </summary>
    public class ITab_PipeLimits : ITab
    {
        private const float RowHeight = 116f;
        private const float LineHeight = 24f;
        private const float MarkerGrab = 6f;

        private static readonly Vector2 WinSize = new Vector2(440f, 420f);
        private static readonly Color MinColor = new Color(1f, 0.6f, 0.15f);
        private static readonly Color MaxColor = new Color(0.9f, 0.25f, 0.25f);

        private static readonly Dictionary<Color, Texture2D> barTextures = new Dictionary<Color, Texture2D>();
        private static readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        // Created lazily, ITabs get constructed while loading, off the main thread
        private static Texture2D barBackground;

        // Marker being dragged right now
        private static string dragKey;

        private Vector2 scrollPosition;

        public ITab_PipeLimits()
        {
            size = WinSize;
            labelKey = "VCEUE_PipeLimits_Tab";
        }

        public override bool IsVisible => SelThing?.TryGetComp<CompPipeLimits>()?.Gauges.Count > 0;

        public override void FillTab()
        {
            var comp = SelThing.TryGetComp<CompPipeLimits>();
            if (comp == null)
                return;

            var outRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Widgets.BeginGroup(outRect);

            // Status line
            var statusRect = new Rect(0f, 0f, outRect.width, LineHeight);
            var reason = comp.BlockedReason;
            GUI.color = reason == null ? Color.white : new Color(1f, 0.6f, 0.6f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(statusRect, reason ?? "VCEUE_PipeLimits_Allowed".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            TooltipHandler.TipRegion(statusRect, "VCEUE_PipeLimits_Desc".Translate());

            var gauges = comp.Gauges;
            var scrollOut = new Rect(0f, LineHeight + 4f, outRect.width, outRect.height - LineHeight - 4f);
            var viewRect = new Rect(0f, 0f, scrollOut.width - 16f, gauges.Count * RowHeight);
            Widgets.BeginScrollView(scrollOut, ref scrollPosition, viewRect);
            for (int i = 0; i < gauges.Count; i++)
            {
                DoGauge(new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 6f), comp, gauges[i]);
            }
            Widgets.EndScrollView();

            if (dragKey != null && !Input.GetMouseButton(0))
                dragKey = null;

            Widgets.EndGroup();
        }

        private static void DoGauge(Rect rect, CompPipeLimits comp, PipeGauge gauge)
        {
            comp.GetReading(gauge, out var current, out var capacity);
            var limit = comp.GetLimit(gauge, true);

            // Title and amounts
            var titleRect = new Rect(rect.x, rect.y, rect.width, LineHeight - 2f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, $"{gauge.Label} ({RoleLabel(gauge.role)})");
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(titleRect, $"{current:0} / {capacity:0} {gauge.Unit}");
            Text.Anchor = TextAnchor.UpperLeft;

            var barRect = new Rect(rect.x, rect.y + LineHeight, rect.width, 22f);
            float scale = capacity;
            Widgets.FillableBar(barRect, scale > 0f ? Mathf.Clamp01(current / scale) : 0f, BarTexture(gauge.Color), barBackground ??= SolidColorMaterials.NewSolidColorTexture(new Color(0.15f, 0.15f, 0.15f)), true);

            DoMarker(barRect, scale, gauge, limit, false, MinColor);
            DoMarker(barRect, scale, gauge, limit, true, MaxColor);

            // Limits
            float y = barRect.yMax + 4f;
            DoLimitRow(new Rect(rect.x, y, rect.width, LineHeight), gauge, limit, false, scale);
            DoLimitRow(new Rect(rect.x, y + LineHeight + 2f, rect.width, LineHeight), gauge, limit, true, scale);
        }

        private static void DoMarker(Rect barRect, float scale, PipeGauge gauge, PipeLimit limit, bool isMax, Color color)
        {
            if (isMax ? !limit.useMax : !limit.useMin)
                return;

            int value = isMax ? limit.max : limit.min;
            float x = barRect.x + Mathf.Clamp01(scale > 0f ? value / scale : 0f) * barRect.width;
            Widgets.DrawBoxSolid(new Rect(x - 1.5f, barRect.y - 3f, 3f, barRect.height + 6f), color);

            // Dragging
            string key = gauge.key + (isMax ? "|max" : "|min");
            var grabRect = new Rect(x - MarkerGrab, barRect.y - 3f, MarkerGrab * 2f, barRect.height + 6f);
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(grabRect))
            {
                dragKey = key;
                e.Use();
            }
            if (dragKey == key && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
            {
                int dragged = Mathf.RoundToInt(Mathf.Clamp01((e.mousePosition.x - barRect.x) / barRect.width) * scale);
                SetValue(limit, isMax, dragged, true);
                buffers.Remove(key);
                e.Use();
            }
        }

        private static void DoLimitRow(Rect rect, PipeGauge gauge, PipeLimit limit, bool isMax, float scale)
        {
            bool enabled = isMax ? limit.useMax : limit.useMin;
            bool wasEnabled = enabled;
            Widgets.Checkbox(new Vector2(rect.x, rect.y), ref enabled, LineHeight);
            if (enabled != wasEnabled)
            {
                if (isMax)
                {
                    limit.useMax = enabled;
                    // Start somewhere sensible instead of 0
                    if (enabled && limit.max <= 0 && scale > 0f)
                        limit.max = Mathf.RoundToInt(scale);
                }
                else
                {
                    limit.useMin = enabled;
                }
                buffers.Remove(gauge.key + (isMax ? "|max" : "|min"));
            }

            var labelRect = new Rect(rect.x + LineHeight + 6f, rect.y, 190f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, (isMax ? "VCEUE_PipeLimits_StopAbove" : "VCEUE_PipeLimits_StopBelow").Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            if (!enabled)
                return;

            string key = gauge.key + (isMax ? "|max" : "|min");
            buffers.TryGetValue(key, out var buffer);
            int value = isMax ? limit.max : limit.min;
            var fieldRect = new Rect(labelRect.xMax, rect.y, 80f, rect.height);
            if (limit.Invalid)
                GUI.color = new Color(1f, 0.4f, 0.4f);
            Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, 0f, 1000000f);
            GUI.color = Color.white;
            if (limit.Invalid)
                TooltipHandler.TipRegion(fieldRect, "VCEUE_PipeLimits_Invalid".Translate());
            buffers[key] = buffer;
            if (value != (isMax ? limit.max : limit.min))
                SetValue(limit, isMax, value, false);

            if (scale > 0f)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(labelRect.xMax + 86f, rect.y + 2f, 60f, rect.height), (value / scale).ToStringPercent("F0"));
                GUI.color = Color.white;
            }
        }

        // Dragging can't push a marker past the other one. Typed values are left alone (the player is mid-edit) and flagged when invalid.
        private static void SetValue(PipeLimit limit, bool isMax, int value, bool clamp)
        {
            if (isMax)
                limit.max = clamp && limit.useMin ? Mathf.Max(value, limit.min) : value;
            else
                limit.min = clamp && limit.useMax ? Mathf.Min(value, limit.max) : value;
        }

        private static string RoleLabel(GaugeRole role)
        {
            switch (role)
            {
                case GaugeRole.Output: return "VCEUE_PipeLimits_Output".Translate();
                case GaugeRole.Input: return "VCEUE_PipeLimits_Input".Translate();
                default: return "VCEUE_PipeLimits_Other".Translate();
            }
        }

        private static Texture2D BarTexture(Color color)
        {
            if (!barTextures.TryGetValue(color, out var tex))
            {
                tex = SolidColorMaterials.NewSolidColorTexture(color);
                barTextures[color] = tex;
            }
            return tex;
        }
    }
}
