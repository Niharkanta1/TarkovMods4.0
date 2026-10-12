# AlternateHud

AlternateHud is a lightweight, transparent HUD for SPT 4.1.5. The `Minimal` theme uses a seven-part health silhouette, compact vitals, active-effect indicators, weapon/ammunition/grenade information, stance/speed/stamina, a fine hit marker and an unboxed kill feed.

# AI Disclosure — Alternate HUD

Alternate HUD is developed using a **Spec-Driven AI Development** workflow, where the author defines the technical specifications, architecture, implementation requirements, and expected behavior, while AI assists with generating portions of the source code.

### What is created by the author?

- **Configuration and XML themes:** Written and designed by the author, including HUD layouts, positioning, colors, and theme configuration.
- **Visual assets and UI elements:** Handcrafted by the author using Adobe Photoshop.
- **Core hooks and logic calculations:** The main hook system and logic calculations are implemented by the author.
- **In-game UI generation boilerplate:** AI-assisted generation is used for boilerplate code that creates in-game icons and UI elements when a custom design or implementation is not provided.

### How AI is used

The project follows a specification-driven workflow. The author defines the technical requirements before implementation, including the intended behavior, methods to use, variables to reference, dependencies, and integration points. An AI coding orchestrator then generates code according to those specifications.

AI is therefore used as an implementation assistant within an author-directed development process, rather than as an unrestricted, autonomous developer. The author determines the intended design and technical approach, while AI assists in translating those requirements into code.

### What is Spec-Driven Development?

**Spec-Driven Development (SDD)** is a software development approach in which implementation is guided by explicit specifications established before or during development. These specifications describe what the software must do, how components should interact, which constraints must be followed, and what behavior is expected.

In Alternate HUD, the author defines the technical direction and implementation requirements, and AI generates portions of the code to meet those requirements. This approach aims to keep implementation aligned with the author's design decisions and project requirements.

### Transparency

Alternate HUD uses a combination of human-authored configuration, handcrafted visual assets, author-implemented core logic, and AI-generated implementation code. This disclosure is intended to make those contributions clear to users and reviewers.

## Author Involvement

The author is actively involved in the design, implementation, testing, and maintenance of Alternate HUD.

- **In-game testing and verification:** Tests the mod in-game, verifies theme reloading and configuration persistence, and measures performance to identify potential issues.
- **Performance optimization and refactoring:** Reviews and refactors the code to improve performance, readability, maintainability, and overall code quality.
- **UI and theme design:** Designs the user interface, writes the XML configuration, and creates and maintains the theme system and visual layouts.
- **Code maintenance and Unity practices:** Reviews generated and existing code, maintains the codebase, and applies appropriate C#, Unity, and modding practices to improve reliability and consistency.

AI-generated code is integrated into an author-directed development workflow. The author remains responsible for evaluating the implementation, testing its behavior, improving the code, and maintaining the project.

## Customize a theme

Themes live in `BepInEx/plugins/AlternateHud/Themes/<ThemeName>/`. Copy the `Minimal` folder, edit its `theme.xml`, then select the folder name in the BepInEx configuration. `TextOnly` is included as a graphic-free alternative (icons and silhouette disabled); select it in the `Theme` setting. Press F9 in raid to reload the XML and PNG artwork without restarting. The build copies theme XML and image assets to the plugin folder.

## Install a build

`dotnet build -c Release` creates `dist/AlternateHud-1.0.0.zip`, with the plugin DLL and themes under `BepInEx/plugins/AlternateHud`, ready to extract into the SPT installation directory. The ZIP also includes this readme at its root. The archive suffix and BepInEx plugin version both come from the project `Version` property. When the configured SPT installation is present, the build also deploys the plugin there automatically.

Theme positions use the 1920x1080 canvas reference, with positive X to the right and positive Y upwards from the selected anchor. Colors accept `#RRGGBB` or `#RRGGBBAA`. Every theme element and attribute is optional; omitted attributes use the built-in defaults.

### Silhouette artwork

Put your PNGs in `Themes/Minimal/` beside `theme.xml`. In the default XML, `prefix="body_minimal"` loads the full-body image `body_minimal_silhouette.png` and seven health-part images named `body_minimal_Head.png`, `body_minimal_Chest.png`, `body_minimal_Stomach.png`, `body_minimal_LeftArm.png`, `body_minimal_RightArm.png`, `body_minimal_LeftLeg.png`, and `body_minimal_RightLeg.png`. Filenames are case-sensitive; change `prefix` and `image` in `<Silhouette>` if you use different names. Keep all part images the same dimensions and aligned on the same canvas so their regions overlap the base silhouette correctly. The silhouette preserves the PNG's aspect ratio; its width and height set the image's containing box.

The supplied art has a full-body image with each selected body region shaded darker. The Minimal theme uses `maskMode="luminance"` to extract that dark region as a transparent health mask, then tints it by that part's health. `maskThreshold` controls which shades are included and `maskSoftness` controls antialiasing around the edge. For conventional white shapes on transparent backgrounds, set `maskMode="alpha"` instead. Missing mask images are generated on first use; generated white-on-transparent masks are detected and retain their alpha. Use `outline="none"` to omit an outline image (the default Minimal style does so). `opacity`, `baseColor`, and `flash` control the image treatment. Set `show="false"` in XML to disable silhouette in this theme, or toggle **Show Silhouette** in F12.

### Effects, equipment and movement panels

The Minimal theme shows active light/heavy bleeding, fractures and pain beside the vitals; the equipped weapon, magazine count and grenade count at bottom-right; and stance, movement speed, body stamina and hand stamina at bottom-left. Both stamina pools show a numeric value and a progress bar. Toggle these panels with **Show Effects**, **Show Equipment**, and **Show Movement** in F12. Each section has its own `show`, `anchor`, `x`, `y` and optional `scale` attributes in `theme.xml`.

Movement speed is rounded to stable even-numbered tiers from **2 to 14 KM/H**, with hysteresis to reduce flickering. Minimal uses six configurable volume-level icons instead of separate walking/running icons. Speed levels 2-3, 4-5, 6-7, 8-9, 10-11, and 12+ map to icon levels 0-5; TextOnly shows the speed value as text. Stamina labels are hidden while current/maximum values appear beside the thin bars.

The Minimal weapon, magazine, and grenade display uses three compact horizontal groups styled after the supplied reference image. The background is transparent; icon sizes, group widths and spacing can be changed in the `<Equipment>` section of the theme.

The ammo icon follows weapon class: assault rifles, assault carbines and shotguns use medium ammo; SMGs and pistols use small ammo; sniper rifles and machineguns use large ammo. Other weapon classes default to medium. The grenade count includes throwable items in the equipped rig and pockets.

Use **Show All HUD** as the master visibility switch. Independent F12 toggles control stance, the volume/sprint indicator, weapon name/icon, ammo, grenades, and both stamina bars. **Leg Stamina Bar X/Y** and **Hand Stamina Bar X/Y** independently position the two bars without moving their value text.

Replaceable transparent PNG icons are in `Themes/Minimal/Icons/`. The XML attributes select the image files:

| Panel     | XML icon attributes and default PNGs                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Effects   | `lightBleedIcon` → `bleed_light.png`, `heavyBleedIcon` → `bleed_heavy.png`, `fractureIcon` → `fracture.png`, `painIcon` → `pain.png`                                                                                                                                                                                                                                                                                                                                                                                                                      |
| Equipment | General: `weaponIcon` → `weapon.png`, `ammoIcon` → `ammo.png`, `grenadeIcon` → `grenade.png`. Weapon classes: `assaultRifleIcon` → `weapon_assault_rifle.png`, `assaultCarbineIcon` → `weapon_assault_carbine.png`, `sniperIcon` → `weapon_sniper.png`, `shotgunIcon` → `weapon_shotgun.png`, `smgIcon` → `weapon_smg.png`, `lmgIcon` → `weapon_lmg.png`, `pistolIcon` → `weapon_pistol.png`, `knifeIcon` → `weapon_knife.png`. Ammo sizes: `smallAmmoIcon` → `ammo_small.png`, `mediumAmmoIcon` → `ammo_medium.png`, `largeAmmoIcon` → `ammo_large.png`. |
| Movement  | `stanceIcon` → `stance_stand.png`, `crouchIcon` → `stance_crouch.png`, `proneIcon` → `stance_prone.png`, `sprintIcon` → `stance_sprint.png`, `speedIcon0`-`speedIcon5` → six volume-level PNGs; fastest tier uses `volume_5.png`, `staminaIcon` → `snamina.png`                                                                                                                                                                                                                                                                                           |

The supplied `weapon_shotgun.png` is used for shotguns; the fastest speed tier explicitly uses `volume_5.png`.

Set `<Icons enabled="false" />` to hide panel icons while keeping their text. `TextOnly` does this by default, hides the silhouette and visual hit/direction markers, and uses text-only stamina progress bars. Its vitals, effects, equipment, movement and kill feed use separate, text-oriented positions. For custom icons, keep transparent padding around the artwork and use the same filename or update the corresponding XML attribute.

### Preview

Use the **Preview HUD Key** shortcut (F10 by default) to display sample values for every HUD panel for five seconds while in a raid. The HUD, including preview, stays hidden outside a raid and in the Hideout. Change the shortcut in the BepInEx F12 configuration screen.

## Settings and placement

The BepInEx configuration for this plugin is saved separately as `BepInEx/config/com.alternate.hud.cfg`. F12 changes save immediately and are restored on the next launch. Select the theme from the **Theme** dropdown; the list is populated from the theme folders at startup, so restart the game after adding a new folder.

## Vitals labels, colors and position

The Minimal theme displays current/maximum values (for example `480/480`) in green for Health, blue for Hydration, and yellow for Energy. Change each `<Stat color="#RRGGBB">` in `theme.xml` to change its color. The labels can be switched off individually in F12 (**Show Health Label**, **Show Hydration Label**, and **Show Energy Label**) or by setting `showLabel="false"` on that stat in XML.

TextOnly has no HUD icons or stamina bars. It displays stance and the leg/hand stamina values as text. The hit marker remains available through the **Show Hit Marker** toggle.

Open F12 and edit AlternateHud **Layout** X/Y settings to reposition panels, then use **Element Positions** X/Y settings to adjust effect rows, weapon/ammo/grenade rows, stance/speed/stamina elements, the kill feed, hit marker, damage numbers and damage direction individually. These are offsets from each theme's XML positions: positive X moves right and positive Y moves up. The movement defaults place the stance and volume icons 40 px down, leg-stamina value at X -50/Y +15, and hand-stamina value at X -50/Y +10 with its bar at Y +5. Changes apply immediately and persist in the plugin configuration. Set offsets to `0` to return to the XML position.

### Hit marker

Set `style` to `X` or `Cross`; adjust `thickness`, `length`, `gap`, `duration`, and `punch` in the theme XML. The BepInEx setting **Hit Marker Thickness** overrides the XML thickness when set above zero; zero uses the theme value.

### Theme file

The shipped `Themes/Minimal/theme.xml` lists the available sections and the default layout. It can be used as a starting point for custom themes.
