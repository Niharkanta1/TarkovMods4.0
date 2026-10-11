using System.Collections.Generic;
using EFT.Ballistics;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    /// <summary>Minimal text-line kill feed driven entirely by the theme.</summary>
    internal class KillFeedView
    {
        private class Line
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public float Age, Y;
        }

        public RectTransform Root { get; }
        public float ThemeScale { get; }
        public bool ThemeShow { get; }

        private readonly Theme _theme;
        private readonly List<Line> _lines = new List<Line>();
        private readonly int _max, _size;
        private readonly float _life, _spacing, _ax;
        private readonly string _format, _headFormat;
        private readonly bool _up;
        private readonly TextAnchor _align;
        private readonly Vector2 _position;
        private int _kills;

        public KillFeedView(Transform parent, Theme theme)
        {
            _theme = theme;
            var k = theme.Root.Child("KillFeed");
            ThemeShow = k.B("show", true);
            ThemeScale = k.F("scale", 1f);
            var anchor = k.Anchor("anchor", new Vector2(0.5f, 0.5f));
            _ax = anchor.x;
            _up = k.Str("grow", anchor.y < 0.25f ? "Up" : "Down").ToLowerInvariant() == "up";
            _position = new Vector2(k.F("x", 0), k.F("y", -170));
            Root = UiKit.Node(parent, "KillFeed", anchor, anchor, _position, Vector2.zero);
            _max = k.I("max", 4);
            _life = k.F("life", 5f);
            _size = k.I("size", 18);
            _spacing = k.F("spacing", 4f);
            _align = UiKit.AlignFor(anchor.x);
            _format = k.Str("format",
                "{name}  {c:#8d8d8d}{weapon}  {distance}m  {part}{/c}");
            _headFormat = k.Str("headshotFormat", _format + "  {c:#f2cf5b}HEADSHOT{/c}");
        }

        public void Reset()
        {
            foreach (var l in _lines) Object.Destroy(l.Rt.gameObject);
            _lines.Clear();
            _kills = 0;
        }

        private string Fill(string f, KillEvent e)
        {
            f = f.Replace("{name}", string.IsNullOrEmpty(e.Name) ? "UNKNOWN" : e.Name)
                .Replace("{weapon}", string.IsNullOrEmpty(e.Weapon) ? "" : e.Weapon.ToUpperInvariant())
                .Replace("{distance}", Mathf.RoundToInt(e.Distance).ToString())
                .Replace("{part}", VitalsView.Name(e.Part))
                .Replace("{level}", e.Level.ToString())
                .Replace("{tag}", e.Tag ?? "")
                .Replace("{kills}", _kills.ToString());
            return Theme.Rich(f);
        }

        public void OnKill(KillEvent e)
        {
            _kills++;
            var h = _size + 8f;
            var rt = UiKit.Node(Root, "Line", new Vector2(0.5f, 0.5f), new Vector2(_ax, 0.5f), Vector2.zero,
                new Vector2(700, h));
            var text = rt.gameObject.AddComponent<Text>();
            text.font = UiKit.GetFont();
            text.fontSize = _size;
            text.color = _theme.Text;
            text.alignment = _align;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = Fill(e.Headshot ? _headFormat : _format, e);
            if (_theme.Shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.9f);
                s.effectDistance = new Vector2(1f, -1f);
            }

            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            _lines.Insert(0, new Line { Rt = rt, Group = group });
            while (_lines.Count > _max)
            {
                Object.Destroy(_lines[_lines.Count - 1].Rt.gameObject);
                _lines.RemoveAt(_lines.Count - 1);
            }
        }

        public void Preview()
        {
            Root.anchoredPosition = _position + new Vector2(Settings.KillFeedX.Value, Settings.KillFeedY.Value);
            OnKill(new KillEvent
            {
                Name = "RAIDER",
                Weapon = "M4A1",
                Part = EBodyPart.Head,
                Distance = 42,
                Level = 35,
                Headshot = true,
                Tag = "PMC BOT"
            });
        }

        public void Update(float dt)
        {
            Root.anchoredPosition = _position + new Vector2(Settings.KillFeedX.Value, Settings.KillFeedY.Value);
            for (var i = _lines.Count - 1; i >= 0; i--)
            {
                _lines[i].Age += dt;
                if (_lines[i].Age > _life)
                {
                    Object.Destroy(_lines[i].Rt.gameObject);
                    _lines.RemoveAt(i);
                }
            }

            var step = (_size + 8f + _spacing) * (_up ? 1f : -1f);
            for (var i = 0; i < _lines.Count; i++)
            {
                var l = _lines[i];
                var target = i * step;
                l.Y = l.Age < 0.02f ? target : Mathf.Lerp(l.Y, target, 1f - Mathf.Exp(-12f * dt));
                var fin = Mathf.Clamp01(l.Age / 0.2f);
                l.Rt.anchoredPosition = new Vector2((1f - fin) * 20f * (_ax > 0.5f ? 1f : -1f), l.Y);
                l.Group.alpha = fin * Mathf.Clamp01((_life - l.Age) / 0.6f);
            }
        }
    }
}
