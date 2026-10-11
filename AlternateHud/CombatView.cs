using EFT;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    /// <summary>Hit marker, floating damage numbers and incoming damage direction.</summary>
    internal class CombatView
    {
        private class Number
        {
            public Text Text;
            public float Age = 99f, Life, Drift;
            public Vector2 Start;
        }

        private class Indicator
        {
            public RectTransform Pivot;
            public Image Core;
            public float Age = 99f, Strength;
            public Vector3 Direction;
        }

        public RectTransform Root { get; }
        public float ThemeScale => 1f;

        private readonly Theme _theme;
        private readonly XNode _hm, _dn, _dd;
        private readonly RectTransform _marker;
        private readonly Image[] _arms = new Image[4];
        private float _markerAge = 99f, _markerLife, _punch = 1f;
        private Color _markerColor = Color.white;
        private readonly Number[] _numbers = new Number[14];
        private readonly Indicator[] _indicators = new Indicator[6];
        private int _numberIdx, _indicatorIdx;

        public CombatView(Transform parent, Theme theme)
        {
            _theme = theme;
            _hm = theme.Root.Child("HitMarker");
            _dn = theme.Root.Child("DamageNumbers");
            _dd = theme.Root.Child("DamageDirection");

            var c = new Vector2(0.5f, 0.5f);
            Root = UiKit.Node(parent, "Combat", c, c, Vector2.zero, Vector2.zero);
            _marker = UiKit.Node(Root, "Marker", c, c, Vector2.zero, Vector2.zero);
            for (var i = 0; i < 4; i++)
                _arms[i] = UiKit.Img(_marker, "Arm" + i, UiKit.White, Color.white, c, c, Vector2.zero, Vector2.zero);
            _marker.gameObject.SetActive(false);

            for (var i = 0; i < _numbers.Length; i++)
            {
                var t = UiKit.Label(Root, "Dmg" + i, "", _dn.I("size", 24), theme.Text, TextAnchor.MiddleCenter, c,
                    c, Vector2.zero, new Vector2(120, 40));
                t.gameObject.SetActive(false);
                _numbers[i] = new Number { Text = t };
            }

            var radius = _dd.F("radius", 190f);
            for (var i = 0; i < _indicators.Length; i++)
            {
                var pivot = UiKit.Node(Root, "Dir" + i, c, c, Vector2.zero, Vector2.zero);
                var core = UiKit.Img(pivot, "Core", UiKit.White, theme.Bad, c, c, new Vector2(0, radius),
                    new Vector2(_dd.F("length", 80f), _dd.F("thickness", 2f)));
                pivot.gameObject.SetActive(false);
                _indicators[i] = new Indicator { Pivot = pivot, Core = core };
            }
        }

        public void Reset()
        {
            _markerAge = 99f;
            foreach (var n in _numbers) n.Age = 99f;
            foreach (var i in _indicators) i.Age = 99f;
        }

        private void ApplyMarkerShape()
        {
            var cfg = Settings.HitMarkerThickness.Value;
            var thickness = cfg > 0f ? cfg : _hm.F("thickness", 1.5f);
            var length = _hm.F("length", 7f);
            var gap = _hm.F("gap", 5f);
            var baseAngle = _hm.Str("style", "X").ToLowerInvariant() == "cross" ? 0f : 45f;
            for (var i = 0; i < 4; i++)
            {
                var angle = baseAngle + 90f * i;
                var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var rt = _arms[i].rectTransform;
                rt.sizeDelta = new Vector2(thickness, length);
                rt.anchoredPosition = dir * (gap + length * 0.5f);
                rt.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        public void OnHit(HitEvent e)
        {
            if (Settings.ShowHitMarker.Value && _hm.B("show", true))
            {
                ApplyMarkerShape();
                _marker.anchoredPosition = new Vector2(Settings.HitMarkerX.Value, Settings.HitMarkerY.Value);
                _markerAge = 0f;
                _markerLife = _hm.F("duration", 0.25f) * (e.Killed ? 2f : 1f);
                _punch = _hm.F("punch", 1.2f) * (e.Killed ? 1.3f : 1f);
                _markerColor = e.Killed
                    ? _hm.C("killColor", _theme.Bad)
                    : e.Part == EBodyPart.Head
                        ? _hm.C("headshotColor", _theme.Gold)
                        : _hm.C("color", _theme.Text);
            }

            if (!Settings.ShowDamageNumbers.Value || !_dn.B("show", true) || e.Damage < 0.5f) return;

            var cam = Camera.main;
            var local = Vector2.zero;
            if (cam != null)
            {
                var sp = cam.WorldToScreenPoint(e.Point);
                if (sp.z > 0) local = HudRoot.ScreenToLocal(sp);
            }

            var n = _numbers[_numberIdx++ % _numbers.Length];
            n.Age = 0f;
            n.Life = _dn.F("life", 0.9f) * (e.Killed ? 1.4f : 1f);
            n.Start = local + new Vector2(Settings.DamageNumbersX.Value, Settings.DamageNumbersY.Value) +
                      new Vector2(Random.Range(-25f, 25f), Random.Range(-10f, 15f));
            n.Drift = Random.Range(-15f, 15f);
            n.Text.text = Mathf.RoundToInt(e.Damage).ToString();
            n.Text.color = e.Killed
                ? _dn.C("killColor", _theme.Bad)
                : e.Part == EBodyPart.Head
                    ? _dn.C("headshotColor", _theme.Gold)
                    : _dn.C("color", _theme.Text);
            n.Text.gameObject.SetActive(true);
        }

        public void OnDamageTaken(DamageTakenEvent e)
        {
            if (!Settings.ShowDamageDirection.Value || !_dd.B("show", true) ||
                e.SourceDirection.sqrMagnitude < 0.0001f) return;
            var ind = _indicators[_indicatorIdx++ % _indicators.Length];
            ind.Age = 0f;
            ind.Direction = e.SourceDirection;
            ind.Strength = Mathf.Clamp01(0.6f + e.Damage / 60f);
            ind.Pivot.anchoredPosition =
                new Vector2(Settings.DamageDirectionX.Value, Settings.DamageDirectionY.Value);
            ind.Pivot.gameObject.SetActive(true);
        }

        public void Preview()
        {
            if (Settings.ShowHitMarker.Value && _hm.B("show", true))
            {
                ApplyMarkerShape();
                _markerAge = 0f;
                _markerLife = _hm.F("duration", 0.25f) * 3f;
                _punch = 1.2f;
                _markerColor = _theme.Gold;
                _marker.anchoredPosition = new Vector2(Settings.HitMarkerX.Value, Settings.HitMarkerY.Value);
            }

            var number = _numbers[_numberIdx++ % _numbers.Length];
            number.Age = 0f;
            number.Life = 2f;
            number.Start = new Vector2(Settings.DamageNumbersX.Value, Settings.DamageNumbersY.Value + 80);
            number.Drift = 0;
            number.Text.text = "42";
            number.Text.color = _theme.Text;
            number.Text.gameObject.SetActive(true);
            if (Settings.ShowDamageDirection.Value && _dd.B("show", true))
            {
                var indicator = _indicators[_indicatorIdx++ % _indicators.Length];
                indicator.Age = 0f;
                indicator.Direction = Vector3.forward;
                indicator.Strength = 1f;
                indicator.Pivot.gameObject.SetActive(true);
            }
        }

        public void Update(float dt)
        {
            _marker.anchoredPosition = new Vector2(Settings.HitMarkerX.Value, Settings.HitMarkerY.Value);
            UpdateMarker(dt);

            var rise = _dn.F("rise", 70f);
            foreach (var n in _numbers)
            {
                if (n.Age > n.Life)
                {
                    if (n.Text.gameObject.activeSelf) n.Text.gameObject.SetActive(false);
                    continue;
                }

                n.Age += dt;
                var k = n.Age / n.Life;
                n.Text.rectTransform.anchoredPosition =
                    n.Start + new Vector2(n.Drift * k, rise * (1f - Mathf.Pow(1f - k, 2f)));
                var c = n.Text.color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                n.Text.color = c;
            }

            var cam = Camera.main;
            var fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : Vector3.forward;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            var life = _dd.F("life", 1.5f);
            var col = _dd.C("color", _theme.Bad);
            foreach (var ind in _indicators)
            {
                if (ind.Age > life)
                {
                    if (ind.Pivot.gameObject.activeSelf) ind.Pivot.gameObject.SetActive(false);
                    continue;
                }

                ind.Age += dt;
                ind.Pivot.anchoredPosition =
                    new Vector2(Settings.DamageDirectionX.Value, Settings.DamageDirectionY.Value);
                var flat = Vector3.ProjectOnPlane(ind.Direction, Vector3.up);
                ind.Pivot.localRotation = Quaternion.Euler(0, 0, -Vector3.SignedAngle(fwd, flat, Vector3.up));
                col.a = ind.Strength * Mathf.Clamp01(1f - ind.Age / life);
                ind.Core.color = col;
            }
        }

        private void UpdateMarker(float dt)
        {
            if (_markerAge > _markerLife)
            {
                if (_marker.gameObject.activeSelf) _marker.gameObject.SetActive(false);
                return;
            }

            _marker.gameObject.SetActive(true);
            _markerAge += dt;
            var k = Mathf.Clamp01(_markerAge / _markerLife);
            var s = Mathf.Lerp(_punch, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(k * 4f), 3f));
            _marker.localScale = new Vector3(s, s, 1f);
            var c = _markerColor;
            c.a = k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f;
            foreach (var arm in _arms) arm.color = c;
        }
    }
}
