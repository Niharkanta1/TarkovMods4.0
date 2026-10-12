# CHANGELOG.md — AlternateHud

All notable changes to AlternateHud are documented in this file.

## [Unreleased]

### Added

- Initial implementation of the AlternateHud plugin for SPT 4.1.5.
- BepInEx configuration for HUD visibility, theme selection, scaling, opacity, feature toggles, and individual element positioning.
- Persistent Unity overlay canvas with resolution-independent positioning.
- XML-based theme loading with built-in defaults for missing configuration values.
- `Minimal`, `TextOnly`, and `Sample` themes.
- Player vitals display for health, energy, and hydration.
- Seven-part body silhouette with health-based colors, damage feedback, and body-part status indicators.
- Status effect indicators for bleeding, fractures, and pain.
- Equipment information, including weapon icons, localized weapon names, magazine counts, and grenade counts.
- Movement information covering stance, speed, sound-volume indicators, and body and hand stamina.
- Combat feedback with hit markers, floating damage numbers, and directional damage indicators.
- Kill feed with player information, body-part details, category-based colors, and animated notifications.
- F9 shortcut for reloading the active theme.
- F10 shortcut for previewing the HUD with sample content.
- Harmony patches and a centralized event system for communicating gameplay events to the HUD.
- Shared UI helpers, cached resources, and pooled combat indicators.
- Throttled grenade inventory scanning to reduce unnecessary runtime work.
- HUD lifecycle management, event cleanup, and recovery if the HUD root is unexpectedly destroyed.
- Documentation covering architecture, implementation planning, and testing.

### Changed

- Established a theme-driven UI system that separates visual configuration from gameplay logic.
- Organized the implementation into Core, Theme, and UI responsibilities.
- Centralized HUD construction, visibility management, and per-frame updates.
- Separated gameplay event handling from UI rendering to simplify maintenance.

### Fixed

- Initial implementation and testing will identify and document issues as they are resolved.

### Testing

- Build and packaging verification against SPT 4.1.5.
- Manual verification of HUD visibility, themes, configuration persistence, and gameplay features.
- Performance and stability checks during raids and scene transitions.

## [1.0.0] — Initial Release

The first stable release of AlternateHud, featuring a configurable, theme-driven HUD for SPT 4.1.5.

- Player vitals, body-part health, and status effects.
- Equipment and movement information.
- Hit markers, damage numbers, directional damage indicators, and kill feed.
- Minimal and TextOnly display options.
- BepInEx configuration for supported toggles, scale, opacity, and element offsets.
- F9 theme reload and F10 HUD preview.
