using Comfort.Common;
using EFT;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    /// <summary>Owns the overlay canvas and drives all HUD modules while a raid is running.</summary>
    internal class HudRoot : MonoBehaviour
    {
        public static Player MainPlayer { get; private set; }
        public static RectTransform Canvas { get; private set; }

        private CanvasGroup _group;
        private RectTransform _content;
        private VitalsView _vitals;
        private EffectsView _effects;
        private EquipmentView _equipment;
        private MovementView _movement;
        private CombatView _combat;
        private KillFeedView _feed;
        private string _builtTheme;
        private string _lastProfile;
        private float _previewUntil;
        private bool _previewRendered;
        private bool _ready;

        private void Awake()
        {
            try
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                _group = gameObject.AddComponent<CanvasGroup>();
                _group.blocksRaycasts = false;
                _group.interactable = false;
                Canvas = (RectTransform)transform;

                Rebuild();
                _ready = true;
                AlternateHudPlugin.Log.LogInfo("HUD built");
            }
            catch (System.Exception e)
            {
                AlternateHudPlugin.Log.LogError("Build failed: " + e);
            }
        }

        private void Rebuild()
        {
            _builtTheme = Settings.ThemeName.Value;
            Unsubscribe();
            _previewRendered = false;
            if (_content) Destroy(_content.gameObject);

            var theme = Theme.Load(AlternateHudPlugin.ThemesDir, _builtTheme);
            UiKit.Theme = theme;
            _content = UiKit.Node(transform, "Content", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero);
            UiKit.Stretch(_content);

            _vitals = new VitalsView(_content, theme);
            _effects = new EffectsView(_content, theme);
            _equipment = new EquipmentView(_content, theme);
            _movement = new MovementView(_content, theme);
            _combat = new CombatView(_content, theme);
            _feed = new KillFeedView(_content, theme);

            HudEvents.DamageTaken += OnDamageTaken;
            HudEvents.HitDealt += _combat.OnHit;
            HudEvents.Kill += _feed.OnKill;
            _lastProfile = null;
            AlternateHudPlugin.Log.LogInfo("Theme loaded: " + _builtTheme);
        }

        private void Unsubscribe()
        {
            HudEvents.DamageTaken -= OnDamageTaken;
            if (_combat != null) HudEvents.HitDealt -= _combat.OnHit;
            if (_feed != null) HudEvents.Kill -= _feed.OnKill;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDamageTaken(DamageTakenEvent e)
        {
            _vitals.OnDamage(e);
            _combat.OnDamageTaken(e);
        }

        public static Vector2 ScreenToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas, screen, null, out var local);
            return local;
        }

        private void Update()
        {
            if (!_ready) return;
            try
            {
                Tick();
            }
            catch (System.Exception e)
            {
                AlternateHudPlugin.Log.LogError("Tick failed: " + e);
            }
        }

        private void Tick()
        {
            if (Settings.ReloadKey.Value.IsDown() || Settings.ThemeName.Value != _builtTheme) Rebuild();
            var gw = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
            MainPlayer = gw != null ? gw.MainPlayer : null;
            if (MainPlayer == null && gw != null && gw.AllAlivePlayersList != null)
            {
                foreach (var p in gw.AllAlivePlayersList)
                {
                    if (p != null && p.IsYourPlayer)
                    {
                        MainPlayer = p;
                        break;
                    }
                }
            }

            var inRaid = gw != null && !string.Equals(gw.LocationId, "hideout", StringComparison.OrdinalIgnoreCase) &&
                         MainPlayer != null && MainPlayer.IsYourPlayer &&
                         MainPlayer.HealthController != null;
            if (Settings.PreviewKey.Value.IsDown() && inRaid)
            {
                _previewUntil = Time.unscaledTime + 5f;
                _previewRendered = false;
            }

            var preview = inRaid && Time.unscaledTime < _previewUntil;
            _group.alpha = inRaid && Settings.ShowAllHud.Value ? Settings.Opacity.Value : 0f;
            if (!inRaid) return;

            if (inRaid && _lastProfile != MainPlayer.ProfileId)
            {
                _lastProfile = MainPlayer.ProfileId;
                _vitals.Reset();
                _combat.Reset();
                _feed.Reset();
            }

            var s = Settings.Scale.Value;
            _vitals.Root.localScale = Vector3.one * (_vitals.ThemeScale * s);
            _effects.Root.localScale = Vector3.one * (_effects.ThemeScale * s);
            _equipment.Root.localScale = Vector3.one * (_equipment.ThemeScale * s);
            _movement.Root.localScale = Vector3.one * (_movement.ThemeScale * s);
            _feed.Root.localScale = Vector3.one * (_feed.ThemeScale * s);
            _combat.Root.localScale = Vector3.one * s;
            _vitals.Root.gameObject.SetActive(preview || (_vitals.ThemeShow && Settings.ShowVitals.Value));
            _effects.Root.gameObject.SetActive(preview || (_effects.ThemeShow && Settings.ShowEffects.Value));
            _equipment.Root.gameObject.SetActive(preview || (_equipment.ThemeShow && Settings.ShowEquipment.Value));
            _movement.Root.gameObject.SetActive(preview || (_movement.ThemeShow && Settings.ShowMovement.Value));
            _feed.Root.gameObject.SetActive(preview || (_feed.ThemeShow && Settings.ShowKillFeed.Value));
            _combat.Root.gameObject.SetActive(preview || inRaid);

            var dt = Time.unscaledDeltaTime;
            if (preview)
            {
                if (!_previewRendered)
                {
                    _vitals.Preview();
                    _effects.Preview();
                    _equipment.Preview();
                    _movement.Preview();
                    _combat.Preview();
                    _feed.Preview();
                    _previewRendered = true;
                }
            }
            else
            {
                if (_vitals.Root.gameObject.activeSelf) _vitals.Update(MainPlayer, dt);
                if (_effects.Root.gameObject.activeSelf) _effects.Update(MainPlayer);
                if (_equipment.Root.gameObject.activeSelf) _equipment.Update(MainPlayer);
                if (_movement.Root.gameObject.activeSelf) _movement.Update(MainPlayer, dt);
                _combat.Root.gameObject.SetActive(true);
            }

            _combat.Update(dt);
            _feed.Update(dt);
        }
    }
}
