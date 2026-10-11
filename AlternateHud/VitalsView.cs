using System.Collections.Generic;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    internal class VitalsView
    {
        private const string DefaultStats =
            "<Stats>" +
            "<Stat id='Health' label='HEALTH' x='100' y='128'/>" +
            "<Stat id='Energy' label='ENERGY' x='100' y='70'/>" +
            "<Stat id='Hydration' label='HYDRATION' x='100' y='12'/>" +
            "</Stats>";

        private class StatUi
        {
            public string Id, Format, Suffix;
            public RectTransform Root;
            public Text Label;
            public Text Value;
            public Image Fill;
            public Vector2 Position;
            public ConfigEntry<float> OffsetX, OffsetY;
            public float BarWidth, Shown = 1f;
            public bool Auto;
            public Color Color;
            public bool ThemeLabel;
        }

        public RectTransform Root { get; }
        public float ThemeScale { get; }
        public bool ThemeShow { get; }

        private readonly Theme _theme;
        private readonly Dictionary<EBodyPart, Image> _parts = new Dictionary<EBodyPart, Image>();
        private readonly Dictionary<EBodyPart, float> _flash = new Dictionary<EBodyPart, float>();
        private readonly Dictionary<EBodyPart, float> _shown = new Dictionary<EBodyPart, float>();
        private readonly List<StatUi> _stats = new List<StatUi>();
        private readonly Text _status;
        private readonly RectTransform _silhouette;
        private readonly Vector2 _silhouettePosition;
        private readonly Vector2 _rootPosition;
        private readonly Vector2 _statusPosition;
        private readonly ConfigEntry<float> _statusOffsetX, _statusOffsetY;
        private readonly float _silOpacity;
        private readonly bool _flashEnabled;
        private readonly bool _themeShowSilhouette;

        public VitalsView(Transform parent, Theme theme)
        {
            _theme = theme;
            var v = theme.Root.Child("Vitals");
            ThemeShow = v.B("show", true);
            ThemeScale = v.F("scale", 1f);
            var anchor = v.Anchor("anchor", Vector2.zero);
            _rootPosition = new Vector2(v.F("x", 40), v.F("y", 40));
            Root = UiKit.Node(parent, "Vitals", anchor, anchor, _rootPosition, Vector2.zero);

            var sil = v.Child("Silhouette");
            _silOpacity = sil.F("opacity", 0.9f);
            _flashEnabled = sil.B("flash", true);
            _themeShowSilhouette = sil.B("show", true);
            _silhouettePosition = new Vector2(sil.F("x", 0), sil.F("y", 0));
            _silhouette = _themeShowSilhouette ? BuildSilhouette(sil) : null;

            var statNodes = v.Children("Stat");
            if (statNodes.Count == 0) statNodes = XNode.Parse(DefaultStats).Children("Stat");
            foreach (var s in statNodes) BuildStat(s);

            var st = v.Child("Status");
            _status = st.B("show", true)
                ? UiKit.Label(Root, "Status", "", st.I("size", 14), theme.Bad, TextAnchor.LowerLeft, Vector2.zero,
                    Vector2.zero, _statusPosition = new Vector2(st.F("x", 100), st.F("y", -20)), new Vector2(300, 20))
                : null;
            if (_status == null) _statusPosition = Vector2.zero;
            _statusOffsetX = Settings.StatusX;
            _statusOffsetY = Settings.StatusY;
        }

        private RectTransform BuildSilhouette(XNode sil)
        {
            var prefix = sil.Str("prefix", "body");
            SilhouetteGen.EnsureMasks(_theme.Dir, prefix);
            var size = new Vector2(sil.F("width", 86), sil.F("height", 132));
            var box = UiKit.Node(Root, "Silhouette", Vector2.zero, Vector2.zero,
                _silhouettePosition, size);

            var baseImage = _theme.LoadSprite(sil.Str("image", prefix + "_silhouette.png"));
            if (baseImage)
            {
                var baseTint = UiKit.Img(box, "BaseImage", baseImage, sil.C("baseColor", new Color(1, 1, 1, 0.12f)),
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                UiKit.Stretch(baseTint.rectTransform);
                baseTint.preserveAspect = true;
            }

            foreach (var name in SilhouetteGen.Parts)
            {
                var part = (EBodyPart)System.Enum.Parse(typeof(EBodyPart), name);
                var sprite = _theme.LoadPartMask(prefix + "_" + name + ".png", sil);
                if (!sprite) continue;
                var img = UiKit.Img(box, name, sprite, Color.white, Vector2.zero, Vector2.zero, Vector2.zero,
                    Vector2.zero);
                UiKit.Stretch(img.rectTransform);
                img.preserveAspect = true;
                _parts[part] = img;
                _flash[part] = 0f;
                _shown[part] = 1f;
            }

            var outlineFile = sil.Str("outline", prefix + "_outline.png");
            var outline = outlineFile.Equals("none", System.StringComparison.OrdinalIgnoreCase)
                ? null
                : _theme.LoadSprite(outlineFile);
            if (outline)
            {
                var o = UiKit.Img(box, "Outline", outline, sil.C("outlineColor", new Color(1, 1, 1, 0.3f)),
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                UiKit.Stretch(o.rectTransform);
                o.preserveAspect = true;
            }

            return box;
        }

        private void BuildStat(XNode s)
        {
            var id = s.Str("id", "Health");
            var x = s.F("x", 100);
            var y = s.F("y", 0);
            var size = s.I("size", 34);
            var labelSize = s.I("labelSize", 11);
            var position = new Vector2(x, y);
            var offsetX = id == "Health" ? Settings.HealthX
                : id == "Hydration" ? Settings.HydrationX
                : id == "Energy" ? Settings.EnergyX
                : null;
            var offsetY = id == "Health" ? Settings.HealthY
                : id == "Hydration" ? Settings.HydrationY
                : id == "Energy" ? Settings.EnergyY
                : null;
            var statRoot = UiKit.Node(Root, id, Vector2.zero, Vector2.zero, position, Vector2.zero);

            var stat = new StatUi
            {
                Id = id,
                Format = s.Str("format", "{cur}"),
                Suffix = s.Str("suffix", "/{max}"),
                Auto = s.Str("color", id == "Health" ? "auto" : "") == "auto",
                Color = s.C("color", _theme.Text),
                BarWidth = s.F("barWidth", 90),
                Root = statRoot,
                Position = position,
                OffsetX = offsetX,
                OffsetY = offsetY,
                ThemeLabel = s.B("showLabel", true)
            };

            var label = s.Str("label", id.ToUpperInvariant());
            stat.Label = UiKit.Label(statRoot, id + "Label", label, labelSize, s.C("labelColor", _theme.Dim),
                TextAnchor.LowerLeft, Vector2.zero, Vector2.zero, new Vector2(1, size * 1.05f),
                new Vector2(200, 16));

            stat.Value = UiKit.Label(statRoot, id + "Value", "", size, stat.Color, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(240, size + 6));

            var barH = s.F("barHeight", 2f);
            if (barH > 0)
            {
                var back = UiKit.Img(statRoot, id + "BarBack", UiKit.White, new Color(1, 1, 1, 0.12f),
                    Vector2.zero, Vector2.zero, new Vector2(2, -4), new Vector2(stat.BarWidth, barH));
                stat.Fill = UiKit.Img(back.transform, "Fill", UiKit.White, stat.Color, Vector2.zero, Vector2.zero,
                    Vector2.zero, Vector2.zero);
                UiKit.Stretch(stat.Fill.rectTransform);
            }

            _stats.Add(stat);
        }

        public void OnDamage(DamageTakenEvent e)
        {
            if (_flash.ContainsKey(e.Part)) _flash[e.Part] = 1f;
        }

        public void Reset()
        {
            var keys = new List<EBodyPart>(_flash.Keys);
            foreach (var k in keys) _flash[k] = 0f;
        }

        public void Preview()
        {
            Root.anchoredPosition = _rootPosition + new Vector2(Settings.VitalsX.Value, Settings.VitalsY.Value);
            if (_silhouette)
            {
                _silhouette.anchoredPosition =
                    _silhouettePosition + new Vector2(Settings.SilhouetteX.Value, Settings.SilhouetteY.Value);
                _silhouette.gameObject.SetActive(_themeShowSilhouette && Settings.ShowSilhouette.Value);
            }

            foreach (var stat in _stats)
            {
                if (stat.OffsetX != null && stat.OffsetY != null)
                    stat.Root.anchoredPosition =
                        stat.Position + new Vector2(stat.OffsetX.Value, stat.OffsetY.Value);
                stat.Label.gameObject.SetActive(stat.ThemeLabel && IsLabelEnabled(stat.Id));
                var cur = stat.Id == "Health" ? 438 : 78;
                var max = stat.Id == "Health" ? 480 : 100;
                var ratio = (float)cur / max;
                stat.Value.color = stat.Color;
                stat.Value.text = stat.Format.Replace("{cur}", cur.ToString()).Replace("{max}", max.ToString())
                    .Replace("{pct}", Mathf.RoundToInt(ratio * 100f).ToString()) +
                    stat.Suffix.Replace("{cur}", cur.ToString()).Replace("{max}", max.ToString());
                if (stat.Fill)
                {
                    stat.Fill.rectTransform.anchorMax = new Vector2(ratio, 1);
                    stat.Fill.color = stat.Color;
                }
            }

            foreach (var part in _parts)
                part.Value.color = _theme.HealthColor(0.82f);
            if (_status)
            {
                _status.rectTransform.anchoredPosition =
                    _statusPosition + new Vector2(_statusOffsetX.Value, _statusOffsetY.Value);
                _status.text = "R.ARM BROKEN";
            }
        }

        public void Update(Player player, float dt)
        {
            Root.anchoredPosition = _rootPosition + new Vector2(Settings.VitalsX.Value, Settings.VitalsY.Value);
            if (_silhouette)
            {
                _silhouette.anchoredPosition =
                    _silhouettePosition + new Vector2(Settings.SilhouetteX.Value, Settings.SilhouetteY.Value);
                _silhouette.gameObject.SetActive(_themeShowSilhouette && Settings.ShowSilhouette.Value);
            }

            if (_status)
                _status.rectTransform.anchoredPosition =
                    _statusPosition + new Vector2(_statusOffsetX.Value, _statusOffsetY.Value);

            var hc = player.HealthController;
            var k = 1f - Mathf.Exp(-12f * dt);

            foreach (var stat in _stats)
            {
                if (stat.OffsetX != null && stat.OffsetY != null)
                    stat.Root.anchoredPosition =
                        stat.Position + new Vector2(stat.OffsetX.Value, stat.OffsetY.Value);
                stat.Label.gameObject.SetActive(stat.ThemeLabel && IsLabelEnabled(stat.Id));

                var v = stat.Id == "Energy" ? hc.Energy
                    : stat.Id == "Hydration" ? hc.Hydration
                    : hc.GetBodyPartHealth(EBodyPart.Common);
                var ratio = v.Maximum > 0 ? v.Current / v.Maximum : 0f;
                var cur = Mathf.CeilToInt(v.Current);
                var max = Mathf.CeilToInt(v.Maximum);
                var color = stat.Auto ? _theme.HealthColor(ratio) : stat.Color;
                if (stat.Auto && ratio < 0.3f && ratio > 0f)
                    color.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f);

                var main = stat.Format.Replace("{cur}", cur.ToString()).Replace("{max}", max.ToString())
                    .Replace("{pct}", Mathf.RoundToInt(ratio * 100f).ToString());
                var suffix = stat.Suffix.Replace("{cur}", cur.ToString()).Replace("{max}", max.ToString());
                stat.Value.color = color;
                stat.Value.text = main + (suffix.Length > 0
                    ? "<size=" + Mathf.Max(10, stat.Value.fontSize / 2) + "><color=#" +
                      ColorUtility.ToHtmlStringRGBA(_theme.Dim) + ">" + suffix + "</color></size>"
                    : "");

                if (stat.Fill)
                {
                    stat.Shown = Mathf.Lerp(stat.Shown, ratio, k);
                    stat.Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(stat.Shown), 1);
                    stat.Fill.color = color;
                }
            }

            var issues = new List<string>();
            foreach (var kv in _parts)
            {
                var part = kv.Key;
                var h = hc.GetBodyPartHealth(part);
                var r = h.Maximum > 0 ? h.Current / h.Maximum : 0f;
                _shown[part] = Mathf.Lerp(_shown[part], r, k);
                _flash[part] = Mathf.MoveTowards(_flash[part], 0f, dt * 2.5f);

                var destroyed = hc.IsBodyPartDestroyed(part);
                var c = destroyed ? _theme.Lost : _theme.HealthColor(_shown[part]);
                if (_flashEnabled) c = Color.Lerp(c, Color.white, _flash[part] * 0.85f);
                c.a = _silOpacity;
                kv.Value.color = c;

                if (destroyed) issues.Add(Name(part) + " LOST");
                else if (hc.IsBodyPartBroken(part)) issues.Add(Name(part) + " BROKEN");
            }

            if (_status) _status.text = string.Join("  ", issues);
        }

        private static bool IsLabelEnabled(string id)
        {
            switch (id)
            {
                case "Health": return Settings.ShowHealthLabel.Value;
                case "Hydration": return Settings.ShowHydrationLabel.Value;
                case "Energy": return Settings.ShowEnergyLabel.Value;
                default: return true;
            }
        }

        public static string Name(EBodyPart part)
        {
            switch (part)
            {
                case EBodyPart.Chest: return "THORAX";
                case EBodyPart.LeftArm: return "L.ARM";
                case EBodyPart.RightArm: return "R.ARM";
                case EBodyPart.LeftLeg: return "L.LEG";
                case EBodyPart.RightLeg: return "R.LEG";
                default: return part.ToString().ToUpperInvariant();
            }
        }
    }
}
