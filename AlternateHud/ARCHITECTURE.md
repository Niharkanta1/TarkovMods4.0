# ARCHITECTURE.md — AlternateHud

## 1. Overview

AlternateHud is a BepInEx plugin for SPT 4.1.5 that replaces the standard presentation of selected player information with a configurable, theme-driven HUD.

It displays player vitals, body-part health, status effects, equipment, movement information, combat feedback, and kill notifications. Unity UI handles rendering, Harmony patches observe relevant gameplay events, and XML theme files define the visual presentation.

The implementation is organized around four responsibilities:

- **Core:** Plugin startup, configuration, HUD lifecycle, gameplay hooks, event routing, and localization.
- **Theme:** XML parsing, visual settings, sprite and mask loading, and fallback silhouette generation.
- **UI:** Shared UI helpers and the individual HUD views.
- **Themes:** The theme files and artwork shipped with the plugin.

This document describes the implementation and its technical boundaries. It is intended to guide development and maintenance of AlternateHud, not to describe a rebuild of an existing codebase.

## 2. Project and Build Details

| Item             | Value               |
| ---------------- | ------------------- |
| Plugin name      | `AlternateHud`      |
| Plugin GUID      | `com.alternate.hud` |
| Version          | `1.0.0`             |
| Target framework | `net472`            |
| Language version | C# `latest`         |
| Target game      | SPT 4.1.5           |
| Default SPT path | `E:\SPT-4.1.5`      |

The project targets the .NET Framework 4.7.2 runtime used by the Unity/Mono environment. Nullable reference types are disabled, and unsafe code is allowed by the project configuration but is not currently used.

### Build references

`AlternateHud.csproj` references the game assemblies from:

`$(SptPath)\EscapeFromTarkov_Data\Managed\`

These include `Assembly-CSharp`, `ItemComponent.Types`, `Comfort`, the required UnityEngine modules, `Unity.TextMeshPro`, and `UnityEngine.UI`.

BepInEx and Harmony references are loaded from:

`$(SptPath)\BepInEx\core\`

The project generates an intermediate `PluginInfo.g.cs` file before compilation. This exposes the project version through `AlternateHud.PluginInfo.Version`, keeping the plugin metadata aligned with the MSBuild `Version` property.

### Deployment and packaging

- **Deployment:** After a build, the `DeployToSpt` target copies the compiled DLL and the `Themes` directory into `BepInEx\plugins\AlternateHud`, provided the configured SPT plugin directory exists.
- **Release package:** A Release build runs the `PackageMod` target, which prepares `dist\AlternateHud` with the DLL, themes, and `README.md`, then creates `dist\AlternateHud-<Version>.zip`.
- **Source discovery:** C# files are included through the `**/*.cs` glob. Source files can therefore be grouped into folders without maintaining a separate explicit compile list.

## 3. Source Responsibilities

### Core

- **`AlternateHudPlugin.cs`** — The `BaseUnityPlugin` entry point. Binds configuration, applies Harmony patches, creates the persistent HUD root, and runs a watchdog that checks approximately every two seconds whether the root needs to be recreated.
- **`Settings.cs`** — Defines the shared BepInEx configuration entries. A common `Bind(ConfigFile, themesDir)` method registers the settings, while `Offset()` supports the layout-offset entries.
- **`HudRoot.cs`** — Owns the Canvas, CanvasScaler, and CanvasGroup. Creates the views, subscribes to HUD events, resolves the local player, handles raid visibility and preview mode, applies scale and visibility settings, and updates the views.
- **`Patches.cs`** — Contains Harmony patches and the gameplay event contract: `HitEvent`, `DamageTakenEvent`, `KillEvent`, and the `HudEvents` event hub.
- **`HudLocalization.cs`** — Locates EFT's `Localized(string, string)` method through reflection. If the method cannot be found, it returns the original key rather than preventing the HUD from displaying text.

### Theme

- **`Theme.cs`** — Loads `theme.xml` and provides the theme model, palette, font configuration, icon settings, sprite and mask caches, health-color ramp, and rich-text formatting support.
- **`XNode`** — A typed wrapper around `XmlElement` that simplifies access to XML values.
- **`SilhouetteGen.cs`** — Generates default body-part mask PNGs when a theme does not provide the required masks. The masks are generated procedurally using capsule/polygon distance fields.

### UI

- **`UiKit.cs`** — Shared UI factory helpers for creating nodes, images, labels, stretched elements, and aligned elements. It also caches commonly used resources, including the white sprite and font.
- **`VitalsView.cs`** — Displays the seven-part body silhouette, health tinting, damage flashes, LOST/BROKEN status text, and Health, Energy, and Hydration rows. Stat rows include values, labels, bars, automatic colors, and low-value pulsing.
- **`EffectsView.cs`** — Displays light bleeding, heavy bleeding, fractures, and pain, including icons and counts.
- **`EquipmentView.cs`** — Displays the weapon-class icon, localized weapon name, magazine count, and grenade count. Supports card/list layouts and rescans grenade inventory every 20 frames.
- **`MovementView.cs`** — Displays stance, speed/sound-volume tiers, and body and hand stamina values and bars. Speed tiers use hysteresis to avoid rapid switching around thresholds.
- **`CombatView.cs`** — Displays hit markers, floating damage numbers, and directional damage indicators. Damage numbers use a pool of 14 objects; directional indicators use a pool of 6.
- **`KillFeedView.cs`** — Displays pooled kill-feed lines with configurable limits, rich-text formatting, slide-in behavior, and fade-in/fade-out animations.

### Shipped themes

The plugin includes three theme directories:

- **`Minimal`** — Uses icons and a seven-part body silhouette with luminance-masked assets.
- **`TextOnly`** — Presents supported information as text without icons, the body silhouette, or stamina bars.
- **`Sample`** — Provides a commented theme template for reference and customization.

## 4. Runtime Flow

### 4.1 Plugin startup

When `AlternateHudPlugin.Awake()` runs, it binds configuration using `Settings.Bind(Config, ThemesDir)`, applies the Harmony patches using the plugin's Harmony instance, and creates a persistent `GameObject` named `AlternateHud`. The object uses `DontDestroyOnLoad` and `HideAndDontSave`, and hosts `HudRoot`.

### 4.2 HUD initialization

`HudRoot.Awake()` creates the overlay Canvas with these settings:

- Render mode: `ScreenSpaceOverlay`
- Sorting order: `50`
- Canvas reference resolution: `1920 × 1080`
- CanvasScaler match value: `0.5`
- CanvasGroup raycast blocking: disabled

The root then builds its content and marks itself ready once initialization is complete.

### 4.3 Building the views

The HUD build/rebuild process loads the selected theme, creates the content root, constructs the six views, subscribes to the `HudEvents` events, and resets the tracked player profile. Theme changes and the reload shortcut cause the HUD to rebuild so that the selected visual configuration is applied.

### 4.4 Per-frame updates

`HudRoot.Tick()` coordinates the runtime behavior:

1. Checks whether the theme needs to be reloaded because F9 was pressed or the selected theme changed.
2. Resolves the local player from `Singleton<GameWorld>`, with a fallback scan of `AllAlivePlayersList`.
3. Determines whether the player is in a raid. The raid gate requires that the player is not in the Hideout, a local player exists, `IsYourPlayer` is true, and a health controller is available.
4. Enables the five-second preview when F10 is pressed during a raid.
5. Sets the canvas alpha based on raid state, the **Show All HUD** setting, and configured opacity.
6. Resets Vitals, Combat, and Kill Feed state when a new player profile is detected.
7. Applies the combined theme and user scale (`ThemeScale × Scale`), then applies each panel's visibility settings.
8. Calls each view's `Update()` during normal gameplay or `Preview()` while preview mode is active.

Outside a raid, the canvas is hidden and normal view updates are skipped.

### 4.5 Gameplay event flow

Harmony postfixes observe game methods and publish events through `HudEvents`. `HudRoot` receives these events and forwards them to the relevant views.

The current patch points are:

- `Player.ApplyDamageInfo(DamageInfo, EBodyPart, EBodyPartColliderType, float)` — used for damage-related events, including damage taken and hits dealt.
- `Player.OnBeenKilledByAggressor(IPlayer, DamageInfo, EBodyPart, EDamageType)` — used for kill notifications.

The patches may declare only the parameters they need. They communicate with the HUD through the event contract rather than modifying UI elements directly.

The watchdog in `AlternateHudPlugin.Update()` checks approximately every two seconds and recreates the HUD root if it is missing or has been deactivated.

## 5. Configuration

Configuration is stored in the BepInEx configuration file for `com.alternate.hud`. Settings are registered under the `AlternateHud` category, with layout-related values grouped into `Layout` and `Element Positions` subcategories.

The configuration surface includes approximately 50 entries:

- **Theme and controls:** Selected theme, reload shortcut (F9), preview shortcut (F10), and the `Show All HUD` toggle.
- **Panel toggles:** Vitals, Effects, Equipment, Movement, Kill Feed, Hit Marker, Damage Numbers, and Damage Direction.
- **Feature toggles:** Silhouette, Health/Hydration/Energy labels, stance, speed indicator, weapon name, ammunition, grenades, and stamina bars.
- **Global presentation:** HUD scale, HUD opacity, and hit-marker thickness.
- **Panel positioning:** X/Y offsets for Vitals, Effects, Equipment, and Movement.
- **Element positioning:** Individual X/Y offsets for the silhouette, vitals rows, status and effect rows, weapon/ammunition/grenade elements, stance/speed/stamina and stamina bars, kill feed, hit marker, damage numbers, and damage direction.

The theme list is derived from the available theme directories. Position offsets are floating-point values with a range of ±2000.

Configuration names, categories, defaults, ranges, and descriptions form part of the mod's user-facing contract. Changes to them should be deliberate because renaming or removing entries can disrupt existing user configuration files.

## 6. Theme XML Contract

Theme files use `theme.xml` to define the visual appearance of the HUD. Supported sections are:

- `Font`
- `Colors`
- `Vitals`, including `Silhouette`, `Stat`, and `Status`
- `Icons`
- `IconColors`
- `Effects`
- `Equipment`
- `Movement`
- `HitMarker`
- `DamageNumbers`
- `DamageDirection`
- `KillFeed`

All XML attributes are optional and use built-in defaults when omitted. `Sample/Theme.xml` serves as the commented template.

Positions are specified in reference pixels relative to the relevant section anchor on the 1920 × 1080 reference canvas. Colors use `#RRGGBB` or `#RRGGBBAA` notation.

Text formatting supports the following markup:

- `{c:#hex}...{/c}` — colored text
- `{b}` — bold text
- `{i}` — italic text
- `{size:N}` — text size

The theme loader translates this markup into the rich-text format used by Unity TextMeshPro. Sprite and mask resources are cached to avoid repeatedly loading the same assets.

## 7. Game and Unity Integration

The implementation targets the assemblies shipped with SPT 4.1.5. The following APIs have been identified in the target game assemblies:

- `Player.ApplyDamageInfo(...)` and `Player.OnBeenKilledByAggressor(...)` are the current Harmony patch targets.
- `EBodyPart` is in the global namespace and includes Head, Chest, Stomach, LeftArm, RightArm, LeftLeg, RightLeg, and Common.
- `IHealthController.GetBodyPartHealth(...)`, `Energy`, and `Hydration` expose `EFT.HealthSystem.ValueStruct`, which provides `Current`, `Maximum`, `Normalized`, `AtMinimum`, and `AtMaximum`.
- `MovementContext.PoseLevel` and `MovementContext.ActualLinearVelocity` provide stance and velocity information; velocity is measured in metres per second.
- `Weapon.WeapClass`, `Weapon.AmmoCaliber`, `GetCurrentMagazineCount()`, and `GetMaxMagazineCount()` support weapon and ammunition display.
- `ItemComponent.Types.Components.KnifeComponent` is available for knife classification.
- `EFT.LocalizationExtensions.Localized(string, string)` is the localization method used by `HudLocalization`.
- `Comfort.Common.Singleton<T>.Instance` and `.Instantiated` are available for singleton access.

These integrations are tied to the target SPT version. When updating to a different SPT release, confirm that the relevant signatures and assemblies remain compatible before changing the patches or data access code.

## 8. Lifecycle and Resource Management

The Canvas and HUD root persist across scene changes. The root controls the lifetime of the content subtree and the view objects that reference its Unity elements.

When the content is rebuilt, the old subtree is destroyed and its views are replaced. Views must not continue using Unity objects from a destroyed subtree. When the root is destroyed, it unsubscribes from the HUD events to avoid leaving stale subscribers.

Static state is intentionally limited to shared plugin/configuration data, the event hub, the player and Canvas references required by patches and camera calculations, and cached UI resources. This shared state should remain explicit and should not become a general storage location for view-specific state.

## 9. Performance Considerations

The HUD updates every frame during a raid, but most work consists of updating existing UI properties and visibility. It does not rebuild the entire UI every frame.

Performance-conscious implementation details include:

- A pool of 14 damage-number objects and 6 directional indicators.
- Cached theme sprites, masks, fonts, and the shared white sprite.
- Grenade inventory scanning throttled to every 20 frames.
- Preview mode using sample data instead of live gameplay queries.
- Skipping normal HUD updates when outside a raid.
- Accepting limited string formatting allocations where needed to display readable values.

`HudLocalization` searches the `Assembly-CSharp` type list once at startup to locate the localization method. This is a one-time cost. `UiKit` also has a special case for the Bender font, using the TextMeshPro asset path `ui/fonts/jovanny lemonad - bender normal sdf`, before falling back to an operating-system dynamic font.

Grenade counting currently uses `Equipment.Slots[6]` for the rig and `Equipment.Slots[10]` for pockets. This relies on the slot order used by the target game version, so it should be revisited if the game's equipment structure changes.

## 10. Maintenance Guidelines

When extending AlternateHud, keep these rules in mind:

1. Keep Harmony patches focused on observing game behavior and publishing events. Do not put UI construction or rendering logic into patches.
2. Keep `HudEvents` as a small event contract that does not depend on views or Unity UI.
3. Keep view classes focused on their own panel and use shared helpers from `UiKit`.
4. Read visual settings and assets through the theme model instead of hard-coding theme-specific presentation values in views.
5. Preserve the existing configuration keys and theme XML contract unless a deliberate breaking change is intended.
6. Respect Unity object lifetimes when rebuilding or destroying HUD content.
7. Prefer resource caching, object reuse, and throttled scans for work that does not need to happen every frame.
8. Recheck Harmony signatures and game API references when changing the target SPT version.
9. Keep the project version, generated plugin metadata, deployment target, and release packaging behavior consistent.

## 11. Summary

AlternateHud is built around a persistent Unity overlay, XML-driven themes, six focused UI views, and an event-based bridge between game behavior and HUD rendering. BepInEx provides user configuration, Harmony observes gameplay events, and `HudRoot` coordinates visibility, updates, preview mode, and cleanup.

The architecture aims to keep the mod configurable and maintainable while avoiding unnecessary per-frame work. Its main compatibility boundaries are the SPT 4.1.5 game assemblies, Harmony patch signatures, the BepInEx configuration contract, and the shipped theme XML and assets.
