using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    internal class MovementView
    {
        public RectTransform Root { get; }
        public float ThemeScale { get; }
        public bool ThemeShow { get; }

        private readonly Theme _theme;
        private readonly XNode _settings;
        private readonly Vector2 _position;
        private readonly Text _stance, _speed, _stamina;
        private readonly Image _stanceIcon, _speedIcon, _staminaIcon, _staminaFill;
        private readonly Image _staminaBack;
        private readonly string _stanceStandIcon, _stanceCrouchIcon, _stanceProneIcon, _stanceSprintIcon;
        private readonly string[] _speedIcons = new string[6];
        private readonly Text _handsStamina;
        private readonly Image _handsStaminaFill, _handsStaminaBack;
        private readonly bool _textOnly;
        private readonly bool _showStanceText, _showSpeedText;
        private readonly bool _showStaminaLabels;
        private readonly Vector2 _stanceIconPosition, _stanceTextPosition, _speedIconPosition, _speedTextPosition;
        private readonly Vector2 _staminaIconPosition, _staminaTextPosition, _staminaBarPosition;
        private readonly Vector2 _handsTextPosition, _handsBarPosition;
        private int _speedTier;
        private string _lastSpeedIcon, _lastStanceIcon;

        public MovementView(Transform parent, Theme theme)
        {
            _theme = theme;
            _settings = theme.Root.Child("Movement");
            _textOnly = !_theme.IconsEnabled;
            ThemeShow = _settings.B("show", true);
            ThemeScale = _settings.F("scale", 1f);
            var anchor = _settings.Anchor("anchor", Vector2.zero);
            _position = new Vector2(_settings.F("x", 28), _settings.F("y", 28));
            Root = UiKit.Node(parent, "Movement", anchor, anchor, _position, Vector2.zero);

            var stanceSize = _settings.F("stanceIconSize", _settings.F("iconSize", 19));
            var speedSize = _settings.F("speedIconSize", 14);
            var textOffset = _theme.IconsEnabled ? stanceSize + 7 : 0;
            _showStanceText = _settings.B("showStanceText", _textOnly);
            _showSpeedText = _settings.B("showSpeedText", _textOnly);
            _showStaminaLabels = _settings.B("showStaminaLabels", _textOnly);
            _stanceStandIcon = _settings.Str("stanceIcon", "stance_stand.png");
            _stanceCrouchIcon = _settings.Str("crouchIcon", "stance_crouch.png");
            _stanceProneIcon = _settings.Str("proneIcon", "stance_prone.png");
            _stanceSprintIcon = _settings.Str("sprintIcon", "stance_sprint.png");
            _speedIcons[0] = _settings.Str("speedIcon0", "volume_0.png");
            _speedIcons[1] = _settings.Str("speedIcon1", "volume_1.png");
            _speedIcons[2] = _settings.Str("speedIcon2", "volume_2.png");
            _speedIcons[3] = _settings.Str("speedIcon3", "volume_4.png");
            _speedIcons[4] = _settings.Str("speedIcon4", "volume_5.png");
            _speedIcons[5] = _settings.Str("speedIcon5", "volume_5.png");
            _stanceIconPosition = new Vector2(_settings.F("stanceX", 0),
                _settings.F("stanceY", 58));
            _stanceTextPosition = new Vector2(textOffset + _settings.F("stanceX", 0),
                _settings.F("stanceY", 58));
            _speedIconPosition = new Vector2(_settings.F("speedX", stanceSize + 10),
                _settings.F("speedY", 58));
            _speedTextPosition = new Vector2(textOffset + _settings.F("speedX", 0),
                _settings.F("speedY", 34));
            _staminaIconPosition = new Vector2(_settings.F("staminaX", 0), _settings.F("staminaY", 20));
            _staminaTextPosition = new Vector2(textOffset + _settings.F("staminaX", 0),
                _settings.F("staminaY", 20));
            _staminaBarPosition = new Vector2(_settings.F("staminaBarX", 0),
                _settings.F("staminaBarY", 4));
            _handsTextPosition = new Vector2(textOffset + _settings.F("handsStaminaX", 0),
                _settings.F("handsStaminaY", -14));
            _handsBarPosition = new Vector2(_settings.F("handsBarX", 0),
                _settings.F("handsBarY", -30));
            _stanceIcon = Icon("stanceIcon", "stance_stand.png", _stanceIconPosition, stanceSize);
            _stance = Label("Stance", "", _stanceTextPosition, _settings.I("fontSize", 13), new Vector2(180, 22));
            _stance.gameObject.SetActive(_showStanceText);
            _speedIcon = Icon("speedIcon", _speedIcons[0], _speedIconPosition, speedSize);
            _speed = Label("Speed", "", _speedTextPosition, _settings.I("fontSize", 13), new Vector2(180, 22));
            _speed.gameObject.SetActive(_showSpeedText);
            _staminaIcon = Icon("staminaIcon", "stamina.png", _staminaIconPosition, _settings.F("iconSize", 19));
            _stamina = Label("Stamina", "", _staminaTextPosition, _settings.I("fontSize", 12), theme.Text,
                new Vector2(180, 20));
            if (_staminaIcon) _staminaIcon.color = _theme.IconColor("stamina", theme.Good);

            var barWidth = _settings.F("staminaBarWidth", 100);
            if (_textOnly)
            {
                _staminaBack = null;
                _staminaFill = null;
                _handsStaminaBack = null;
                _handsStaminaFill = null;
                _handsStamina = Label("HandsStamina", "", _handsTextPosition, _settings.I("fontSize", 12), theme.Text,
                    new Vector2(230, 20));
            }
            else
            {
                _staminaBack = Bar("StaminaBack", _staminaBarPosition, barWidth, out _staminaFill);
                _handsStaminaBack = Bar("HandsStaminaBack", _handsBarPosition, barWidth,
                    out _handsStaminaFill);
                _handsStamina = Label("HandsStamina", "", _handsTextPosition, _settings.I("fontSize", 12), theme.Text,
                    new Vector2(180, 20));
            }
            if (_staminaBack) _staminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
            if (_handsStaminaBack) _handsStaminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
            ApplyFeatureToggles();
            ApplyElementPositions();
        }

        private Image Bar(string name, Vector2 pos, float width, out Image fill)
        {
            var back = UiKit.Img(Root, name, UiKit.White, _settings.C("staminaBarBackColor", new Color(1, 1, 1, 0.32f)), Vector2.zero,
                Vector2.zero, pos, new Vector2(width, _settings.F("staminaBarHeight", 6)));
            fill = UiKit.Img(back.transform, name + "Fill", UiKit.White, _theme.Good,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UiKit.Stretch(fill.rectTransform);
            return back;
        }

        private Image Icon(string key, string fallback, Vector2 pos, float size)
        {
            if (!_theme.IconsEnabled) return null;
            var file = _settings.Str(key, fallback);
            var sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", file));
            if (!sprite) return null;
            var image = UiKit.Img(Root, key, sprite, _theme.IconColor(key == "stanceIcon" ? "stance" : "movement",
                key == "stanceIcon" ? _theme.Good : _theme.Warn), Vector2.zero, Vector2.zero, pos,
                new Vector2(size, size));
            image.preserveAspect = true;
            return image;
        }

        private Text Label(string name, string text, Vector2 pos, int size, Vector2 box) =>
            Label(name, text, pos, size, _theme.Text, box);

        private Text Label(string name, string text, Vector2 pos, int size, Color color, Vector2 box)
        {
            return UiKit.Label(Root, name, text, size, color, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero,
                pos, box);
        }

        public void Update(EFT.Player player, float dt)
        {
            Root.anchoredPosition = _position + new Vector2(Settings.MovementX.Value, Settings.MovementY.Value);
            ApplyFeatureToggles();
            ApplyElementPositions();
            var poseLevel = player.MovementContext.PoseLevel;
            var isSprinting = player.IsSprintEnabled;
            var stance = isSprinting ? "SPRINT"
                : player.IsInPronePose ? "PRONE"
                : poseLevel < 0.5f ? "CROUCH"
                : "STANDING";
            if (_showStanceText) _stance.text = stance;

            if (_stanceIcon)
            {
                var icon = isSprinting ? _stanceSprintIcon
                    : player.IsInPronePose ? _stanceProneIcon
                    : poseLevel < 0.5f ? _stanceCrouchIcon
                    : _stanceStandIcon;
                if (icon != _lastStanceIcon)
                {
                    _lastStanceIcon = icon;
                    _stanceIcon.sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", icon));
                }
                _stanceIcon.color = _theme.IconColor("stance", _theme.Good);
            }

            var actualSpeed = player.MovementContext.ActualLinearVelocity * 3.6f;
            _speedTier = GetSpeedTier(actualSpeed, _speedTier);
            _speed.text = _speedTier == 0 ? "STOP  0 KM/H"
                : (isSprinting || _speedTier >= 8 ? "SPRINT  " : "MOVE  ") + _speedTier + " KM/H";
            if (_speedIcon)
            {
                var speedIndex = _speedTier == 0 ? 0 : Mathf.Clamp(_speedTier / 2 - 1, 0, _speedIcons.Length - 1);
                var icon = _speedIcons[speedIndex];
                if (icon != _lastSpeedIcon)
                {
                    _lastSpeedIcon = icon;
                    _speedIcon.sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", icon));
                }
                _speedIcon.color = _theme.IconColor(_speedTier >= 8 ? "movementRun" : "movement", _theme.Warn);
            }

            var stamina = player.Physical.Stamina;
            var max = stamina != null ? (float)stamina.TotalCapacity : 0f;
            var current = stamina != null ? stamina.Current : 0f;
            var ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            var hands = player.Physical.HandsStamina;
            var handsMax = hands != null ? (float)hands.TotalCapacity : 0f;
            var handsCurrent = hands != null ? hands.Current : 0f;
            var handsRatio = handsMax > 0 ? Mathf.Clamp01(handsCurrent / handsMax) : 0f;
            UpdateStaminaDisplay(_stamina, "LEG", current, max, ratio, _staminaFill);
            UpdateStaminaDisplay(_handsStamina, "HAND", handsCurrent, handsMax, handsRatio,
                _handsStaminaFill);
            if (_staminaBack) _staminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
            if (_handsStaminaBack) _handsStaminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
        }

        public void Preview()
        {
            Root.anchoredPosition = _position + new Vector2(Settings.MovementX.Value, Settings.MovementY.Value);
            ApplyFeatureToggles();
            ApplyElementPositions();
            _stance.text = "STANDING";
            UpdateStaminaDisplay(_stamina, "LEG", 72, 100, 0.72f, _staminaFill);
            UpdateStaminaDisplay(_handsStamina, "HAND", 46, 100, 0.46f, _handsStaminaFill);
            if (_staminaBack) _staminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
            if (_handsStaminaBack) _handsStaminaBack.gameObject.SetActive(Settings.ShowStaminaBars.Value);
        }

        private void UpdateStaminaDisplay(Text text, string label, float current, float max, float ratio, Image fill)
        {
            var currentText = Mathf.CeilToInt(current) + "/" + Mathf.CeilToInt(max);
            if (_textOnly)
            {
                text.text = (_showStaminaLabels ? label + "  " : "") + currentText;
            }
            else
            {
                text.text = _showStaminaLabels ? label + "  " + currentText : currentText;
                fill.rectTransform.anchorMax = new Vector2(ratio, 1);
                fill.color = ratio < 0.2f ? _theme.Bad : _theme.Good;
            }
        }

        private void ApplyElementPositions()
        {
            SetPosition(_stanceIcon, _stanceIconPosition, Settings.MovementStanceX.Value, Settings.MovementStanceY.Value);
            SetPosition(_stance, _stanceTextPosition, Settings.MovementStanceX.Value, Settings.MovementStanceY.Value);
            SetPosition(_speedIcon, _speedIconPosition, Settings.MovementSpeedX.Value, Settings.MovementSpeedY.Value);
            SetPosition(_speed, _speedTextPosition, Settings.MovementSpeedX.Value, Settings.MovementSpeedY.Value);
            SetPosition(_staminaIcon, _staminaIconPosition, Settings.MovementStaminaX.Value, Settings.MovementStaminaY.Value);
            SetPosition(_stamina, _staminaTextPosition, Settings.MovementStaminaX.Value, Settings.MovementStaminaY.Value);
            SetPosition(_staminaBack, _staminaBarPosition, Settings.MovementStaminaBarX.Value, Settings.MovementStaminaBarY.Value);
            SetPosition(_handsStamina, _handsTextPosition, Settings.MovementHandsX.Value, Settings.MovementHandsY.Value);
            SetPosition(_handsStaminaBack, _handsBarPosition, Settings.MovementHandsBarX.Value, Settings.MovementHandsBarY.Value);
        }

        private void ApplyFeatureToggles()
        {
            SetActive(_stanceIcon, Settings.ShowStance.Value);
            SetActive(_stance, Settings.ShowStance.Value && _showStanceText);
            SetActive(_speedIcon, Settings.ShowSpeedIndicator.Value);
            SetActive(_speed, Settings.ShowSpeedIndicator.Value && _showSpeedText);
            SetActive(_staminaBack, Settings.ShowStaminaBars.Value);
            SetActive(_handsStaminaBack, Settings.ShowStaminaBars.Value);
        }

        private static void SetActive(Graphic graphic, bool active)
        {
            if (graphic) graphic.gameObject.SetActive(active);
        }

        private static void SetPosition(Graphic graphic, Vector2 position, float offsetX, float offsetY)
        {
            if (graphic) graphic.rectTransform.anchoredPosition = position + new Vector2(offsetX, offsetY);
        }

        private static int GetSpeedTier(float speed, int previous)
        {
            if (speed < 0.8f) return 0;
            var tier = Mathf.Clamp(Mathf.RoundToInt(speed / 2f) * 2, 2, 14);
            if (previous <= 0 || tier == previous) return tier;

            var midpoint = (previous + tier) * 0.5f;
            var hysteresis = 0.35f;
            if (tier > previous && speed < midpoint + hysteresis) return previous;
            if (tier < previous && speed > midpoint - hysteresis) return previous;
            return tier;
        }
    }
}
