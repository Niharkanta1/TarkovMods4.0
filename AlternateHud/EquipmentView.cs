using System;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    internal class EquipmentView
    {
        public RectTransform Root { get; }
        public float ThemeScale { get; }
        public bool ThemeShow { get; }

        private readonly Theme _theme;
        private readonly XNode _settings;
        private readonly Vector2 _position;
        private readonly Text _weaponName, _ammo, _grenades;
        private readonly Image _weaponIcon, _ammoIcon, _grenadeIcon;
        private readonly bool _cards;
        private readonly bool _rightAligned;
        private string _lastWeaponKey;
        private string _localizedWeaponName;
        private string _lastWeaponIcon, _lastAmmoIcon;
        private Player _lastGrenadePlayer;
        private int _lastGrenadeScanFrame = -1;
        private int _lastGrenadeCount = -1;
        private readonly Vector2 _weaponIconPos, _weaponTextPos, _ammoIconPos, _ammoTextPos;
        private readonly Vector2 _grenadeIconPos, _grenadeTextPos;

        public EquipmentView(Transform parent, Theme theme)
        {
            _theme = theme;
            _settings = theme.Root.Child("Equipment");
            ThemeShow = _settings.B("show", true);
            ThemeScale = _settings.F("scale", 1f);
            var anchor = _settings.Anchor("anchor", Vector2.one);
            _rightAligned = anchor.x > 0.75f;
            _cards = _theme.IconsEnabled && _settings.B("cards", true);
            _position = new Vector2(_settings.F("x", -28), _settings.F("y", 34));
            Root = UiKit.Node(parent, "Equipment", anchor, anchor, _position, Vector2.zero);

            var iconSize = _settings.F("iconSize", 36);
            var rowHeight = _settings.F("rowHeight", 36);
            if (_cards)
            {
                var weaponWidth = _settings.F("weaponPanelWidth", 126);
                var ammoWidth = _settings.F("ammoPanelWidth", 84);
                var grenadeWidth = _settings.F("grenadePanelWidth", 48);
                var panelHeight = _settings.F("panelHeight", 46);
                var gap = _settings.F("panelGap", 2);
                var grenadeOffset = 0f;
                var ammoOffset = grenadeWidth + gap;
                var weaponOffset = ammoOffset + ammoWidth + gap;
                var weaponIconSize = _settings.F("weaponIconSize", 30);
                var ammoIconSize = _settings.F("ammoIconSize", 14);
                var grenadeIconSize = _settings.F("grenadeIconSize", 18);

                _weaponIconPos = new Vector2(-weaponOffset - 8, (panelHeight - weaponIconSize) * 0.5f);
                _ammoIconPos = new Vector2(-ammoOffset - 6, (panelHeight - ammoIconSize) * 0.5f);
                _grenadeIconPos = new Vector2(-grenadeOffset - 6, (panelHeight - grenadeIconSize) * 0.5f);
                _weaponTextPos = new Vector2(-weaponOffset - weaponWidth + 8, 0);
                _ammoTextPos = new Vector2(-ammoOffset - ammoWidth + 4, 0);
                _grenadeTextPos = new Vector2(-grenadeOffset - grenadeWidth + 4, 0);

                _weaponIcon = Icon("weaponIcon", "weapon.png", _weaponIconPos, weaponIconSize);
                _weaponName = CardLabel("WeaponName", "", _settings.I("fontSize", 15), theme.Text,
                    _weaponTextPos, new Vector2(weaponWidth - weaponIconSize - 20, panelHeight), TextAnchor.MiddleLeft);
                _ammoIcon = Icon("ammoIcon", "ammo.png", _ammoIconPos, ammoIconSize);
                _ammo = CardLabel("Ammo", "", _settings.I("ammoSize", 20), theme.Text,
                    _ammoTextPos, new Vector2(ammoWidth - ammoIconSize - 18, panelHeight), TextAnchor.MiddleCenter);
                _grenadeIcon = Icon("grenadeIcon", "grenade.png", _grenadeIconPos, grenadeIconSize);
                _grenades = CardLabel("Grenades", "", _settings.I("fontSize", 14), theme.Text,
                    _grenadeTextPos, new Vector2(grenadeWidth - grenadeIconSize - 18, panelHeight), TextAnchor.MiddleCenter);
            }
            else
            {
                var textX = _theme.IconsEnabled ? iconSize + 10 : 0;
                _weaponIconPos = new Vector2(0, 4);
                _weaponTextPos = new Vector2(_rightAligned ? -textX : textX, 4);
                _ammoIconPos = new Vector2(0, -rowHeight);
                _ammoTextPos = new Vector2(_rightAligned ? -textX : textX, -rowHeight);
                _grenadeIconPos = new Vector2(0, -rowHeight * 2);
                _grenadeTextPos = new Vector2(_rightAligned ? -textX : textX, -rowHeight * 2);
                _weaponIcon = Icon("weaponIcon", "weapon.png", _weaponIconPos,
                    _settings.F("weaponIconSize", iconSize));
                _weaponName = UiKit.Label(Root, "WeaponName", "", _settings.I("fontSize", 15), theme.Text,
                    _rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, Vector2.zero,
                    _rightAligned ? Vector2.right : Vector2.zero,
                    _weaponTextPos, new Vector2(250, rowHeight));
                _ammoIcon = Icon("ammoIcon", "ammo.png", _ammoIconPos,
                    _settings.F("ammoIconSize", iconSize));
                _ammo = UiKit.Label(Root, "Ammo", "", _settings.I("ammoSize", 20), theme.Text,
                    _rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, Vector2.zero,
                    _rightAligned ? Vector2.right : Vector2.zero,
                    _ammoTextPos, new Vector2(180, rowHeight));
                _grenadeIcon = Icon("grenadeIcon", "grenade.png", _grenadeIconPos,
                    _settings.F("grenadeIconSize", iconSize));
                _grenades = UiKit.Label(Root, "Grenades", "", _settings.I("fontSize", 15), theme.Text,
                    _rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, Vector2.zero,
                    _rightAligned ? Vector2.right : Vector2.zero,
                    _grenadeTextPos, new Vector2(180, rowHeight));
            }
            if (_weaponIcon) _weaponIcon.color = _theme.IconColor("weapon", theme.Text);
            if (_ammoIcon) _ammoIcon.color = _theme.IconColor("ammo", theme.Text);
            if (_grenadeIcon) _grenadeIcon.color = _theme.IconColor("grenade", theme.Text);
        }

        private Image Icon(string key, string fallback, Vector2 position, float size)
        {
            if (!_theme.IconsEnabled) return null;
            var file = _settings.Str(key, fallback);
            var sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", file));
            if (!sprite) return null;
            var image = UiKit.Img(Root, key, sprite, _theme.Text, Vector2.zero,
                _rightAligned ? Vector2.right : Vector2.zero, position, new Vector2(size, size));
            image.preserveAspect = true;
            return image;
        }

        private Text CardLabel(string name, string text, int size, Color color, Vector2 position, Vector2 box,
            TextAnchor alignment)
        {
            return UiKit.Label(Root, name, text, size, color, alignment, Vector2.right, Vector2.zero,
                position, box);
        }

        public void Update(Player player)
        {
            Root.anchoredPosition = _position + new Vector2(Settings.EquipmentX.Value, Settings.EquipmentY.Value);
            SetRowPosition(_weaponIcon, _weaponName, _weaponIconPos, _weaponTextPos,
                Settings.EquipmentWeaponX.Value, Settings.EquipmentWeaponY.Value);
            SetRowPosition(_ammoIcon, _ammo, _ammoIconPos, _ammoTextPos,
                Settings.EquipmentAmmoX.Value, Settings.EquipmentAmmoY.Value);
            SetRowPosition(_grenadeIcon, _grenades, _grenadeIconPos, _grenadeTextPos,
                Settings.EquipmentGrenadesX.Value, Settings.EquipmentGrenadesY.Value);
            ApplyFeatureToggles();
            var item = player.HandsController?.Item;
            var weapon = item as Weapon;
            var weaponKey = item != null ? item.ShortName : null;
            if (weaponKey != _lastWeaponKey)
            {
                _lastWeaponKey = weaponKey;
                _localizedWeaponName = (weaponKey != null ? HudLocalization.Localized(weaponKey) : "UNARMED")
                    .ToUpperInvariant();
            }

            _weaponName.text = _localizedWeaponName;
            var weaponIcon = WeaponIcon(item, weapon);
            var ammoIcon = AmmoIcon(item, weapon);
            SetIcon(_weaponIcon, ref _lastWeaponIcon, weaponIcon);
            SetIcon(_ammoIcon, ref _lastAmmoIcon, ammoIcon);
            if (_weaponIcon) _weaponIcon.color = _theme.IconColor(WeaponColorKey(item, weapon), _theme.Text);
            if (_ammoIcon) _ammoIcon.color = _theme.IconColor(AmmoColorKey(ammoIcon), _theme.Text);
            _ammo.text = weapon != null
                ? weapon.GetCurrentMagazineCount() + "/" + weapon.GetMaxMagazineCount()
                : "--/--";

            // Inventory traversal is relatively expensive, so refresh grenade counts every 20 frames.
            if (_lastGrenadePlayer != player || _lastGrenadeScanFrame < 0 ||
                Time.frameCount - _lastGrenadeScanFrame >= 20)
            {
                _lastGrenadePlayer = player;
                _lastGrenadeScanFrame = Time.frameCount;
                _lastGrenadeCount = CountGrenades(player);
                _grenades.text = _cards ? _lastGrenadeCount.ToString()
                    : _settings.Str("grenadeLabel", "GRENADES") + "  " + _lastGrenadeCount;
            }
        }

        public void Preview()
        {
            Root.anchoredPosition = _position + new Vector2(Settings.EquipmentX.Value, Settings.EquipmentY.Value);
            _weaponName.text = "MP-133";
            _ammo.text = "8/8";
            _grenades.text = _cards ? "2" : _settings.Str("grenadeLabel", "GRENADES") + "  2";
            SetRowPosition(_weaponIcon, _weaponName, _weaponIconPos, _weaponTextPos,
                Settings.EquipmentWeaponX.Value, Settings.EquipmentWeaponY.Value);
            SetRowPosition(_ammoIcon, _ammo, _ammoIconPos, _ammoTextPos,
                Settings.EquipmentAmmoX.Value, Settings.EquipmentAmmoY.Value);
            SetRowPosition(_grenadeIcon, _grenades, _grenadeIconPos, _grenadeTextPos,
                Settings.EquipmentGrenadesX.Value, Settings.EquipmentGrenadesY.Value);
            SetIcon(_weaponIcon, ref _lastWeaponIcon, _settings.Str("assaultRifleIcon", "weapon_assault_rifle.png"));
            SetIcon(_ammoIcon, ref _lastAmmoIcon, _settings.Str("mediumAmmoIcon", "ammo_medium.png"));
            if (_weaponIcon) _weaponIcon.color = _theme.IconColor("assaultRifle", _theme.Good);
            if (_ammoIcon) _ammoIcon.color = _theme.IconColor("mediumAmmo", _theme.Warn);
            ApplyFeatureToggles();
        }

        private static void SetRowPosition(Image icon, Text text, Vector2 iconPosition, Vector2 textPosition,
            float x, float y)
        {
            if (icon) icon.rectTransform.anchoredPosition = iconPosition + new Vector2(x, y);
            if (text) text.rectTransform.anchoredPosition = textPosition + new Vector2(x, y);
        }

        private void ApplyFeatureToggles()
        {
            SetActive(_weaponIcon, Settings.ShowWeaponName.Value);
            SetActive(_weaponName, Settings.ShowWeaponName.Value);
            SetActive(_ammoIcon, Settings.ShowAmmo.Value);
            SetActive(_ammo, Settings.ShowAmmo.Value);
            SetActive(_grenadeIcon, Settings.ShowGrenades.Value);
            SetActive(_grenades, Settings.ShowGrenades.Value);
        }

        private static void SetActive(Graphic graphic, bool active)
        {
            if (graphic) graphic.gameObject.SetActive(active);
        }

        private void SetIcon(Image image, ref string lastFile, string file)
        {
            if (!image || file == lastFile) return;
            lastFile = file;
            image.sprite = _theme.LoadSprite(System.IO.Path.Combine("Icons", file));
        }

        private string WeaponIcon(Item item, Weapon weapon)
        {
            if (item != null && item.GetItemComponent<KnifeComponent>() != null)
                return _settings.Str("knifeIcon", "weapon_knife.png");
            if (weapon == null) return _settings.Str("weaponIcon", "weapon.png");
            var kind = weapon.WeapClass ?? "";
            if (IsShotgun(weapon, kind, item.ShortName))
                return _settings.Str("shotgunIcon", "weapon_shotgun.png");
            if (string.Equals(kind, "assaultCarbine", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("assaultCarbineIcon", "weapon_assault_carbine.png");
            if (string.Equals(kind, "sniperRifle", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("sniperIcon", "weapon_sniper.png");
            if (string.Equals(kind, "smg", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("smgIcon", "weapon_smg.png");
            if (string.Equals(kind, "machinegun", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("lmgIcon", "weapon_lmg.png");
            if (string.Equals(kind, "pistol", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("pistolIcon", "weapon_pistol.png");
            if (string.Equals(kind, "knife", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("knifeIcon", "weapon_knife.png");
            return _settings.Str("assaultRifleIcon", "weapon_assault_rifle.png");
        }

        private string WeaponColorKey(Item item, Weapon weapon)
        {
            if (item != null && item.GetItemComponent<KnifeComponent>() != null) return "knife";
            if (weapon == null) return "weapon";
            var kind = weapon.WeapClass ?? "";
            if (IsShotgun(weapon, kind, item.ShortName)) return "shotgun";
            if (string.Equals(kind, "assaultCarbine", StringComparison.OrdinalIgnoreCase)) return "assaultCarbine";
            if (string.Equals(kind, "sniperRifle", StringComparison.OrdinalIgnoreCase)) return "sniper";
            if (string.Equals(kind, "smg", StringComparison.OrdinalIgnoreCase)) return "smg";
            if (string.Equals(kind, "machinegun", StringComparison.OrdinalIgnoreCase)) return "lmg";
            if (string.Equals(kind, "pistol", StringComparison.OrdinalIgnoreCase)) return "pistol";
            return "assaultRifle";
        }

        private static bool IsShotgun(Weapon weapon, string weaponClass, string shortName)
        {
            if (weaponClass.IndexOf("shotgun", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            var caliber = (weapon.AmmoCaliber ?? "").Replace(".", "").Replace("/", "")
                .Replace("-", "").Replace(" ", "");
            return caliber.StartsWith("12x70", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("12x76", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("20x70", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("23x75", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("410x", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("1270", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("1276", StringComparison.OrdinalIgnoreCase) ||
                   caliber.StartsWith("2070", StringComparison.OrdinalIgnoreCase) ||
                   caliber.IndexOf("12g", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   caliber.IndexOf("20g", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (shortName ?? "").IndexOf("shotgun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (shortName ?? "").IndexOf("12ga", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string AmmoColorKey(string icon)
        {
            if (icon.IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0) return "largeAmmo";
            if (icon.IndexOf("medium", StringComparison.OrdinalIgnoreCase) >= 0) return "mediumAmmo";
            return "smallAmmo";
        }

        private string AmmoIcon(Item item, Weapon weapon)
        {
            if (weapon == null) return _settings.Str("mediumAmmoIcon", "ammo_medium.png");

            var kind = weapon.WeapClass ?? "";
            if (IsShotgun(weapon, kind, item != null ? item.ShortName : ""))
                return _settings.Str("mediumAmmoIcon", "ammo_medium.png");
            if (string.Equals(kind, "smg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kind, "pistol", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("smallAmmoIcon", "ammo_small.png");
            if (string.Equals(kind, "sniperRifle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kind, "machinegun", StringComparison.OrdinalIgnoreCase))
                return _settings.Str("largeAmmoIcon", "ammo_large.png");
            return _settings.Str("mediumAmmoIcon", "ammo_medium.png");
        }

        private static int CountGrenades(Player player)
        {
            var slots = player.Inventory?.Equipment?.Slots;
            if (slots == null) return 0;

            // Rig and pockets hold the inventory contents; grenade quick slots can reference the same items.
            var count = slots.Length > 6 ? CountGrenadeItems(slots[6]?.ContainedItem) : 0;
            if (slots.Length > 10) count += CountGrenadeItems(slots[10]?.ContainedItem);
            return count;
        }

        private static int CountGrenadeItems(Item item)
        {
            var count = item is ThrowWeap ? Math.Max(1, item.StackObjectsCount) : 0;
            var compound = item as CompoundItem;
            if (compound == null) return count;

            foreach (var grid in compound.Grids)
                foreach (var containedItem in grid.Items)
                    count += CountGrenadeItems(containedItem);

            foreach (var slot in compound.Slots)
                if (slot?.ContainedItem != null)
                    count += CountGrenadeItems(slot.ContainedItem);
            return count;
        }
    }
}
