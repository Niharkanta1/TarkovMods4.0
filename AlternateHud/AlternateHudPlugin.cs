using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace AlternateHud
{
    [BepInPlugin("com.deadly.hud", "AlternateHud", "1.0.0")]
    public class AlternateHudPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string ThemesDir;

        private GameObject _root;
        private float _next;
        private void Update()
        {
            // Watchdog: recreate the HUD if the game destroyed or disabled it.
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 2f;
            if (_root == null || !_root.activeInHierarchy || _root.GetComponent<HudRoot>() == null)
            {
                Log.LogWarning("HUD object missing/inactive, recreating");
                if (_root != null) Destroy(_root);
                _root = new GameObject("AlternateHud") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(_root);
                _root.AddComponent<HudRoot>();
            }
        }

        private void Awake()
        {
            Log = Logger;
            ThemesDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "Themes");
            Log.LogInfo("Awake");
            try
            {
                Config.SaveOnConfigSet = true;
                Settings.Bind(Config, ThemesDir);
                new Harmony("com.deadly.hud").PatchAll(typeof(AlternateHudPlugin).Assembly);
                Log.LogInfo("Patched");

                _root = new GameObject("AlternateHud") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(_root);
                _root.AddComponent<HudRoot>();
                Log.LogInfo("HudRoot created");
            }
            catch (System.Exception e)
            {
                Log.LogError(e.ToString());
            }
        }
    }
}
