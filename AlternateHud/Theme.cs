using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace AlternateHud
{
    /// <summary>Thin wrapper over an XML element. Missing elements/attributes fall back to defaults.</summary>
    internal class XNode
    {
        private readonly XmlElement _e;

        public XNode(XmlElement e)
        {
            _e = e;
        }

        public bool Exists => _e != null;

        public static XNode Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return new XNode(doc.DocumentElement);
        }

        public string Str(string name, string def)
        {
            var a = _e?.GetAttribute(name);
            return string.IsNullOrEmpty(a) ? def : a;
        }

        public float F(string name, float def)
        {
            return float.TryParse(Str(name, null), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v
                : def;
        }

        public int I(string name, int def) => Mathf.RoundToInt(F(name, def));

        public bool B(string name, bool def)
        {
            var s = Str(name, null);
            if (s == null) return def;
            return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
        }

        public Color C(string name, Color def)
        {
            var s = Str(name, null);
            return s != null && ColorUtility.TryParseHtmlString(s, out var c) ? c : def;
        }

        public Vector2 Anchor(string name, Vector2 def)
        {
            switch ((Str(name, "") ?? "").ToLowerInvariant())
            {
                case "topleft": return new Vector2(0, 1);
                case "topcenter": return new Vector2(0.5f, 1);
                case "topright": return new Vector2(1, 1);
                case "middleleft": return new Vector2(0, 0.5f);
                case "center":
                case "middlecenter": return new Vector2(0.5f, 0.5f);
                case "middleright": return new Vector2(1, 0.5f);
                case "bottomleft": return new Vector2(0, 0);
                case "bottomcenter": return new Vector2(0.5f, 0);
                case "bottomright": return new Vector2(1, 0);
                default: return def;
            }
        }

        public XNode Child(string name)
        {
            if (_e != null)
                foreach (XmlNode n in _e.ChildNodes)
                    if (n is XmlElement el && el.Name == name)
                        return new XNode(el);
            return new XNode(null);
        }

        public List<XNode> Children(string name)
        {
            var list = new List<XNode>();
            if (_e != null)
                foreach (XmlNode n in _e.ChildNodes)
                    if (n is XmlElement el && el.Name == name)
                        list.Add(new XNode(el));
            return list;
        }
    }

    internal class Theme
    {
        public XNode Root;
        public string Dir, Name;
        public Color Good, Warn, Bad, Text, Dim, Gold, Lost;
        public string FontName;
        public bool Shadow;
        public bool IconsEnabled;
        private XNode _iconColors;
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public static Theme Load(string themesDir, string name)
        {
            var t = new Theme { Name = name, Dir = Path.Combine(themesDir, name) };
            Directory.CreateDirectory(t.Dir);
            var file = Path.Combine(t.Dir, "theme.xml");
            t.Root = new XNode(null);
            try
            {
                if (File.Exists(file))
                {
                    var doc = new XmlDocument();
                    doc.Load(file);
                    t.Root = new XNode(doc.DocumentElement);
                }
                else
                {
                    AlternateHudPlugin.Log.LogWarning("Theme file not found, using defaults: " + file);
                }
            }
            catch (Exception e)
            {
                AlternateHudPlugin.Log.LogError("Theme XML error in " + file + ": " + e.Message);
            }

            var c = t.Root.Child("Colors");
            t.Good = c.C("good", Hex("#8fd16f"));
            t.Warn = c.C("warn", Hex("#e6c04a"));
            t.Bad = c.C("bad", Hex("#d9473d"));
            t.Text = c.C("text", Hex("#e2e2e2"));
            t.Dim = c.C("dim", Hex("#8d8d8d"));
            t.Gold = c.C("highlight", Hex("#f2cf5b"));
            t.Lost = c.C("lost", Hex("#3a2a2a"));
            var f = t.Root.Child("Font");
            t.FontName = f.Str("name", "Bender");
            t.Shadow = f.B("shadow", true);
            t.IconsEnabled = t.Root.Child("Icons").B("enabled", true);
            t._iconColors = t.Root.Child("IconColors");
            return t;
        }

        public Color IconColor(string name, Color fallback)
        {
            return _iconColors.C(name, fallback);
        }

        private static Color Hex(string s)
        {
            ColorUtility.TryParseHtmlString(s, out var c);
            return c;
        }

        public Color HealthColor(float ratio)
        {
            return ratio > 0.5f
                ? Color.Lerp(Warn, Good, (ratio - 0.5f) * 2f)
                : Color.Lerp(Bad, Warn, ratio * 2f);
        }

        public Sprite LoadSprite(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (_sprites.TryGetValue(file, out var cached)) return cached;

            var path = Path.Combine(Dir, file);
            var tex = LoadTexture(path);
            var sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
            _sprites[file] = sprite;
            return sprite;
        }

        public Sprite LoadPartMask(string file, XNode settings)
        {
            if (string.IsNullOrEmpty(file)) return null;
            var cacheKey = "mask:" + file;
            if (_sprites.TryGetValue(cacheKey, out var cached)) return cached;

            var path = Path.Combine(Dir, file);
            var tex = LoadTexture(path);
            if (tex == null)
            {
                _sprites[cacheKey] = null;
                return null;
            }

            if (settings.Str("maskMode", "alpha").Equals("luminance", StringComparison.OrdinalIgnoreCase))
            {
                var threshold = Mathf.Clamp01(settings.F("maskThreshold", 0.84f));
                var softness = Mathf.Max(0.001f, settings.F("maskSoftness", 0.12f));
                var pixels = tex.GetPixels32();
                var darkPixels = 0;
                var visiblePixels = 0;
                foreach (var pixel in pixels)
                {
                    if (pixel.a == 0) continue;
                    visiblePixels++;
                    if ((0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f < threshold)
                        darkPixels++;
                }

                // Generated/custom white-on-transparent masks already use alpha as the mask.
                var useLuminance = visiblePixels > 0 && darkPixels > visiblePixels * 0.005f;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    var alpha = pixel.a / 255f;
                    if (useLuminance)
                    {
                        var luma = (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f;
                        alpha *= Mathf.Clamp01((threshold - luma) / softness);
                    }

                    pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, false);
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _sprites[cacheKey] = sprite;
            return sprite;
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path)) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                if (tex.LoadImage(File.ReadAllBytes(path))) return tex;
            }
            catch (Exception e)
            {
                AlternateHudPlugin.Log.LogError("Could not load theme image " + path + ": " + e.Message);
            }

            UnityEngine.Object.Destroy(tex);
            AlternateHudPlugin.Log.LogWarning("Invalid or unreadable theme image: " + path);
            return null;
        }

        /// <summary>Converts {c:#hex}..{/c}, {b}, {i}, {size:N} markup into Unity rich text.</summary>
        public static string Rich(string s)
        {
            s = Regex.Replace(s, @"\{c:([^}]+)\}", "<color=$1>");
            s = Regex.Replace(s, @"\{size:(\d+)\}", "<size=$1>");
            return s.Replace("{/c}", "</color>").Replace("{/size}", "</size>").Replace("{b}", "<b>")
                .Replace("{/b}", "</b>").Replace("{i}", "<i>").Replace("{/i}", "</i>");
        }
    }
}
