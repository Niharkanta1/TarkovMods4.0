using BepInEx.Configuration;
using System;
using System.IO;
using UnityEngine;

namespace AlternateHud
{
    internal static class Settings
    {
        public static ConfigEntry<bool> ShowVitals, ShowHitMarker, ShowDamageNumbers, ShowKillFeed, ShowDamageDirection;
        public static ConfigEntry<bool> ShowSilhouette, ShowHealthLabel, ShowHydrationLabel, ShowEnergyLabel;
        public static ConfigEntry<bool> ShowAllHud, ShowEffects, ShowEquipment, ShowMovement;
        public static ConfigEntry<bool> ShowStance, ShowSpeedIndicator, ShowWeaponName, ShowAmmo, ShowGrenades;
        public static ConfigEntry<bool> ShowStaminaBars;
        public static ConfigEntry<float> Scale, Opacity, HitMarkerThickness;
        public static ConfigEntry<float> VitalsX, VitalsY, SilhouetteX, SilhouetteY;
        public static ConfigEntry<float> HealthX, HealthY, HydrationX, HydrationY, EnergyX, EnergyY;
        public static ConfigEntry<float> StatusX, StatusY;
        public static ConfigEntry<float> EffectsX, EffectsY, EquipmentX, EquipmentY, MovementX, MovementY;
        public static ConfigEntry<string> ThemeName;
        public static ConfigEntry<KeyboardShortcut> ReloadKey, PreviewKey;
        public static ConfigEntry<float> EffectsLightX, EffectsLightY, EffectsHeavyX, EffectsHeavyY;
        public static ConfigEntry<float> EffectsFractureX, EffectsFractureY, EffectsPainX, EffectsPainY;
        public static ConfigEntry<float> EquipmentWeaponX, EquipmentWeaponY, EquipmentAmmoX, EquipmentAmmoY;
        public static ConfigEntry<float> EquipmentGrenadesX, EquipmentGrenadesY;
        public static ConfigEntry<float> MovementStanceX, MovementStanceY, MovementSpeedX, MovementSpeedY;
        public static ConfigEntry<float> MovementStaminaX, MovementStaminaY, MovementHandsX, MovementHandsY;
        public static ConfigEntry<float> MovementStaminaBarX, MovementStaminaBarY;
        public static ConfigEntry<float> MovementHandsBarX, MovementHandsBarY;
        public static ConfigEntry<float> KillFeedX, KillFeedY, HitMarkerX, HitMarkerY;
        public static ConfigEntry<float> DamageNumbersX, DamageNumbersY, DamageDirectionX, DamageDirectionY;

        public static void Bind(ConfigFile c, string themesDir)
        {
            const string g = "AlternateHud";
            var themes = Directory.Exists(themesDir)
                ? Array.ConvertAll(Directory.GetDirectories(themesDir), Path.GetFileName)
                : Array.Empty<string>();
            if (themes.Length == 0) themes = new[] { "Minimal", "TextOnly" };
            Array.Sort(themes, StringComparer.OrdinalIgnoreCase);
            ThemeName = c.Bind(g, "Theme", "Minimal",
                new ConfigDescription("HUD theme folder. Changes apply instantly.",
                    new AcceptableValueList<string>(themes)));
            ReloadKey = c.Bind(g, "Reload Theme Key", new KeyboardShortcut(KeyCode.F9), "Reloads theme.xml without restarting the game.");
            PreviewKey = c.Bind(g, "Preview HUD Key", new KeyboardShortcut(KeyCode.F10), "Shows a sample of all HUD panels for five seconds.");
            ShowAllHud = c.Bind(g, "Show All HUD", true, "Master toggle for every AlternateHud panel.");
            ShowVitals = c.Bind(g, "Show Vitals Panel", true, "Body silhouette, health, energy and hydration.");
            ShowHitMarker = c.Bind(g, "Show Hit Marker", true, "Marker when you hit an enemy.");
            ShowDamageNumbers = c.Bind(g, "Show Damage Numbers", true, "Floating damage numbers at the impact point.");
            ShowKillFeed = c.Bind(g, "Show Kill Feed", true, "Kill lines with weapon, distance and body part.");
            ShowDamageDirection = c.Bind(g, "Show Damage Direction", true, "Indicators pointing to where damage came from.");
            ShowSilhouette = c.Bind(g, "Show Silhouette", true, "Show or hide the seven-part health silhouette.");
            ShowHealthLabel = c.Bind(g, "Show Health Label", true, "Show or hide the Health label.");
            ShowHydrationLabel = c.Bind(g, "Show Hydration Label", true, "Show or hide the Hydration label.");
            ShowEnergyLabel = c.Bind(g, "Show Energy Label", true, "Show or hide the Energy label.");
            ShowEffects = c.Bind(g, "Show Effects", true, "Show active bleeding, fracture and pain effects.");
            ShowEquipment = c.Bind(g, "Show Equipment", true, "Show weapon, ammunition and grenade count.");
            ShowMovement = c.Bind(g, "Show Movement", true, "Show stance, movement speed and stamina.");
            ShowStance = c.Bind(g, "Show Stance", true, "Show the stance icon and any enabled stance text.");
            ShowSpeedIndicator = c.Bind(g, "Show Volume Indicator", true, "Show the movement volume/sprint-speed icon.");
            ShowWeaponName = c.Bind(g, "Show Weapon Name", true, "Show the equipped weapon icon and name.");
            ShowAmmo = c.Bind(g, "Show Ammo", true, "Show the ammunition icon and magazine count.");
            ShowGrenades = c.Bind(g, "Show Grenades", true, "Show the grenade icon and count.");
            ShowStaminaBars = c.Bind(g, "Show Stamina Bars", true, "Show both leg/body and hand stamina bars.");
            HitMarkerThickness = c.Bind(g, "Hit Marker Thickness", 0f, new ConfigDescription("Line thickness in pixels. 0 uses the theme value.", new AcceptableValueRange<float>(0f, 8f)));
            Scale = c.Bind(g, "HUD Scale", 1f, new ConfigDescription("Overall HUD scale.", new AcceptableValueRange<float>(0.5f, 2f)));
            Opacity = c.Bind(g, "HUD Opacity", 1f, new ConfigDescription("Opacity multiplier for the whole HUD.", new AcceptableValueRange<float>(0.2f, 1f)));

            const string layout = "Layout";
            VitalsX = Offset(c, layout, "Vitals X", "Move the entire vitals HUD horizontally.");
            VitalsY = Offset(c, layout, "Vitals Y", "Move the entire vitals HUD vertically.");
            SilhouetteX = Offset(c, layout, "Silhouette X", "Move the silhouette horizontally relative to the vitals panel.");
            SilhouetteY = Offset(c, layout, "Silhouette Y", "Move the silhouette vertically relative to the vitals panel.");
            HealthX = Offset(c, layout, "Health X", "Move the Health value and label horizontally.");
            HealthY = Offset(c, layout, "Health Y", "Move the Health value and label vertically.");
            HydrationX = Offset(c, layout, "Hydration X", "Move the Hydration value and label horizontally.");
            HydrationY = Offset(c, layout, "Hydration Y", "Move the Hydration value and label vertically.");
            EnergyX = Offset(c, layout, "Energy X", "Move the Energy value and label horizontally.");
            EnergyY = Offset(c, layout, "Energy Y", "Move the Energy value and label vertically.");
            StatusX = Offset(c, layout, "Status X", "Move the body-part status text horizontally.");
            StatusY = Offset(c, layout, "Status Y", "Move the body-part status text vertically.");
            EffectsX = Offset(c, layout, "Effects X", "Move the status effects display horizontally.");
            EffectsY = Offset(c, layout, "Effects Y", "Move the status effects display vertically.");
            EquipmentX = Offset(c, layout, "Equipment X", "Move the weapon and grenade display horizontally.");
            EquipmentY = Offset(c, layout, "Equipment Y", "Move the weapon and grenade display vertically.");
            MovementX = Offset(c, layout, "Movement X", "Move stance, speed and stamina display horizontally.");
            MovementY = Offset(c, layout, "Movement Y", "Move stance, speed and stamina display vertically.");

            const string elements = "Element Positions";
            EffectsLightX = Offset(c, elements, "Light Bleeding X", "Move the light-bleeding row horizontally.");
            EffectsLightY = Offset(c, elements, "Light Bleeding Y", "Move the light-bleeding row vertically.");
            EffectsHeavyX = Offset(c, elements, "Heavy Bleeding X", "Move the heavy-bleeding row horizontally.");
            EffectsHeavyY = Offset(c, elements, "Heavy Bleeding Y", "Move the heavy-bleeding row vertically.");
            EffectsFractureX = Offset(c, elements, "Fracture X", "Move the fracture row horizontally.");
            EffectsFractureY = Offset(c, elements, "Fracture Y", "Move the fracture row vertically.");
            EffectsPainX = Offset(c, elements, "Pain X", "Move the pain row horizontally.");
            EffectsPainY = Offset(c, elements, "Pain Y", "Move the pain row vertically.");
            EquipmentWeaponX = Offset(c, elements, "Weapon X", "Move the weapon row horizontally.");
            EquipmentWeaponY = Offset(c, elements, "Weapon Y", "Move the weapon row vertically.");
            EquipmentAmmoX = Offset(c, elements, "Ammo X", "Move the ammo row horizontally.");
            EquipmentAmmoY = Offset(c, elements, "Ammo Y", "Move the ammo row vertically.");
            EquipmentGrenadesX = Offset(c, elements, "Grenades X", "Move the grenade row horizontally.");
            EquipmentGrenadesY = Offset(c, elements, "Grenades Y", "Move the grenade row vertically.");
            MovementStanceX = Offset(c, elements, "Stance X", "Move the stance indicator horizontally.");
            MovementStanceY = Offset(c, elements, "Stance Y", "Move the stance indicator vertically.", -40f);
            MovementSpeedX = Offset(c, elements, "Movement Icon X", "Move the volume/sprint indicator horizontally.");
            MovementSpeedY = Offset(c, elements, "Movement Icon Y", "Move the volume/sprint indicator vertically.", -40f);
            MovementStaminaX = Offset(c, elements, "Stamina X", "Move the body stamina display horizontally.", -50f);
            MovementStaminaY = Offset(c, elements, "Stamina Y", "Move the body stamina display vertically.", 15f);
            MovementHandsX = Offset(c, elements, "Hand Stamina X", "Move the hand stamina display horizontally.", -50f);
            MovementHandsY = Offset(c, elements, "Hand Stamina Y", "Move the hand stamina display vertically.", 10f);
            MovementStaminaBarX = Offset(c, elements, "Leg Stamina Bar X", "Move the leg/body stamina bar horizontally.");
            MovementStaminaBarY = Offset(c, elements, "Leg Stamina Bar Y", "Move the leg/body stamina bar vertically.");
            MovementHandsBarX = Offset(c, elements, "Hand Stamina Bar X", "Move the hand stamina bar horizontally.");
            MovementHandsBarY = Offset(c, elements, "Hand Stamina Bar Y", "Move the hand stamina bar vertically.", 5f);
            KillFeedX = Offset(c, elements, "Kill Feed X", "Move the kill feed horizontally.");
            KillFeedY = Offset(c, elements, "Kill Feed Y", "Move the kill feed vertically.");
            HitMarkerX = Offset(c, elements, "Hit Marker X", "Move the hit marker horizontally.");
            HitMarkerY = Offset(c, elements, "Hit Marker Y", "Move the hit marker vertically.");
            DamageNumbersX = Offset(c, elements, "Damage Numbers X", "Move damage numbers horizontally.");
            DamageNumbersY = Offset(c, elements, "Damage Numbers Y", "Move damage numbers vertically.");
            DamageDirectionX = Offset(c, elements, "Damage Direction X", "Move the damage indicator horizontally.");
            DamageDirectionY = Offset(c, elements, "Damage Direction Y", "Move the damage indicator vertically.");
        }

        private static ConfigEntry<float> Offset(ConfigFile config, string category, string key, string description,
            float defaultValue = 0f)
        {
            return config.Bind(category, key, defaultValue, new ConfigDescription(
                description + " This is an offset from the theme's XML position, in HUD reference pixels.",
                new AcceptableValueRange<float>(-2000f, 2000f)));
        }
    }
}
