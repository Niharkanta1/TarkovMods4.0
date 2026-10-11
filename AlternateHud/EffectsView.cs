using System.Collections.Generic;
using BepInEx.Configuration;
using EFT;
using EFT.HealthSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    internal class EffectsView
    {
        private class EffectRow
        {
            public RectTransform Root;
            public Image Icon;
            public Text Text;
            public Color Color;
            public string Label;
        }

        public RectTransform Root { get; }
        public float ThemeScale { get; }
        public bool ThemeShow { get; }

        private readonly Theme _theme;
        private readonly XNode _settings;
        private readonly Vector2 _position;
        private readonly List<EffectRow> _rows = new List<EffectRow>();
        private readonly Vector2[] _rowPositions = new Vector2[4];
        private readonly ConfigEntry<float>[] _rowX =
        {
            Settings.EffectsLightX, Settings.EffectsHeavyX, Settings.EffectsFractureX, Settings.EffectsPainX
        };
        private readonly ConfigEntry<float>[] _rowY =
        {
            Settings.EffectsLightY, Settings.EffectsHeavyY, Settings.EffectsFractureY, Settings.EffectsPainY
        };

        public EffectsView(Transform parent, Theme theme)
        {
            _theme = theme;
            _settings = theme.Root.Child("Effects");
            ThemeShow = _settings.B("show", true);
            ThemeScale = _settings.F("scale", 1f);
            var anchor = _settings.Anchor("anchor", new Vector2(0, 1));
            _position = new Vector2(_settings.F("x", 260), _settings.F("y", -38));
            Root = UiKit.Node(parent, "Effects", anchor, anchor, _position, Vector2.zero);

            AddRow("lightBleed", "LIGHT BLEED", theme.Bad, 0);
            AddRow("heavyBleed", "HEAVY BLEED", theme.Bad, 1);
            AddRow("fracture", "FRACTURE", theme.Warn, 2);
            AddRow("pain", "PAIN", theme.Gold, 3);
        }

        private void AddRow(string key, string label, Color color, int index)
        {
            var iconSize = _settings.F("iconSize", 16);
            var rowHeight = _settings.F("rowHeight", 22);
            var row = UiKit.Node(Root, label, Vector2.zero, Vector2.zero,
                new Vector2(0, -index * rowHeight), new Vector2(190, rowHeight));
            _rowPositions[index] = new Vector2(0, -index * rowHeight);

            Image icon = null;
            if (_theme.IconsEnabled)
            {
                var file = _settings.Str(key + "Icon", "");
                var sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", file));
                if (sprite)
                {
                    icon = UiKit.Img(row, "Icon", sprite, _theme.IconColor(key, color), Vector2.zero, Vector2.zero,
                        new Vector2(0, 2), new Vector2(iconSize, iconSize));
                    icon.preserveAspect = true;
                }
            }

            var text = UiKit.Label(row, "Text", "", _settings.I("fontSize", 13), color, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.zero, new Vector2(_theme.IconsEnabled ? iconSize + 7 : 0, 0),
                new Vector2(180, rowHeight));
            _rows.Add(new EffectRow { Root = row, Icon = icon, Text = text, Color = color, Label = label });
        }

        public void Update(Player player)
        {
            Root.anchoredPosition = _position + new Vector2(Settings.EffectsX.Value, Settings.EffectsY.Value);
            ApplyRowPositions();
            var light = 0;
            var heavy = 0;
            var fracture = 0;
            var pain = 0;

            foreach (var effect in player.HealthController.GetAllActiveEffects())
            {
                if (effect is IHeavyBleeding) heavy++;
                else if (effect is ILightBleeding) light++;
                else if (effect is IFracture) fracture++;
                else if (effect is IPain) pain++;
            }

            SetRow(0, light);
            SetRow(1, heavy);
            SetRow(2, fracture);
            SetRow(3, pain);
        }

        public void Preview()
        {
            Root.anchoredPosition = _position + new Vector2(Settings.EffectsX.Value, Settings.EffectsY.Value);
            ApplyRowPositions();
            for (var i = 0; i < _rows.Count; i++)
                SetRow(i, 1);
        }

        private void ApplyRowPositions()
        {
            for (var i = 0; i < _rows.Count; i++)
                _rows[i].Root.anchoredPosition = _rowPositions[i] + new Vector2(_rowX[i].Value, _rowY[i].Value);
        }

        private void SetRow(int index, int count)
        {
            var row = _rows[index];
            var show = count > 0;
            row.Root.gameObject.SetActive(show);
            if (show)
                row.Text.text = row.Label + (count > 1 ? "  x" + count : "");
        }
    }
}
