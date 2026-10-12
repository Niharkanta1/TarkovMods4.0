# IMPLEMENTATION_PLAN.md — AlternateHud

## Purpose

Build AlternateHud incrementally from a clean starting point for SPT 4.1.5. Each phase should deliver one understandable, testable piece of the mod before the next phase begins.

This is a human-led implementation plan, not a request to generate the entire codebase in one pass. The developer must understand the code, review the changes, make the required decisions, and verify the result at every stage.

## Working Rules

1. **One phase at a time.** Do not begin the next phase until the current phase has passed its build and human review.
2. **Human input is required in every phase.** Review the proposed changes, answer any design questions, and approve the phase before implementation continues.
3. **Build after each meaningful change.** Run `dotnet build`; the phase is not complete until it builds with zero errors and zero warnings.
4. **Verify behavior, not just compilation.** When a phase adds in-game behavior, install the build in SPT 4.1.5 and test that behavior manually.
5. **Do not invent APIs.** Check the target game assemblies and existing technical notes before using EFT, Unity, BepInEx, or Harmony APIs.
6. **Keep changes small and reviewable.** Avoid generating multiple unrelated classes or features in one step. Explain the purpose of each file and important method.
7. **Do not claim tests passed unless they were run.** Record `PASS`, `FAIL`, `BLOCKED`, `NOT TESTED`, or `NOT IMPLEMENTED` with brief evidence.
8. **Preserve the agreed contracts.** Keep the plugin GUID (`com.alternate.hud`), initial version (`1.0.0`), `net472` target, current build/deployment behavior, configuration names, theme XML contract, theme asset names, and default F9/F10 shortcuts unless the developer explicitly approves a change.
9. **Keep source control checkpoints.** Review the diff and make a meaningful Git commit after each accepted phase.
10. **No silent scope changes.** If implementation reveals a missing requirement or an API uncertainty, stop and ask the developer before making a design decision.

## Definition of Done for Every Phase

A phase is complete only when all of the following are true:

- [x] The phase's scope and acceptance criteria were agreed with the developer.
- [x] The developer reviewed the implementation and understands the changed files.
- [x] `dotnet build` completes with zero errors and zero warnings.
- [x] Relevant manual checks have been performed, or are explicitly marked `NOT TESTED` if they require a later integrated build.
- [x] No unrelated files, configuration entries, theme assets, or project settings were changed.
- [x] The phase notes and test results have been updated.
- [x] The developer approves moving to the next phase.

---

## Phase 0 — Confirm Requirements and Prepare the Workspace

**Goal:** Agree on what the first release must do and establish a reproducible starting point before writing feature code.

### Implementation work

- Review `ARCHITECTURE.md`, `TEST_PLAN.md`, the project file, and available theme assets.
- Confirm the target SPT installation and assembly paths.
- Confirm the required plugin identity, version, target framework, build output, deployment target, and release package layout.
- Create or verify the initial project structure and source-control repository.
- Separate confirmed requirements from assumptions and open questions.
- Create a feature checklist covering configuration, themes, all six views, gameplay events, preview, persistence, and stability.

### Human input and verification

- Developer confirms the feature scope and what must be included in version `1.0.0`.
- Developer confirms the SPT 4.1.5 installation path and that the referenced assemblies are available.
- Developer reviews the folder structure, project settings, and initial checklist.
- Verify that no implementation has started based on unresolved assumptions.

### Acceptance criteria

- [x] Scope and first-release feature list approved.
- [x] Target framework and SPT assembly paths confirmed.
- [x] Plugin GUID and version confirmed.
- [x] Initial repository state is clean and understandable.
- [x] Open questions are documented rather than guessed.

**Build gate:** Run `dotnet build` if a compilable project already exists. If the project has not yet been created, record this phase as preparation and perform the first build in Phase 1.

**Deliverable:** Approved requirements checklist and a prepared workspace.

---

## Phase 1 — Create the Plugin Skeleton

**Goal:** Produce the smallest valid BepInEx plugin that compiles and loads.

### Implementation work

- Configure `AlternateHud.csproj` for `net472` and the confirmed SPT 4.1.5 references.
- Set the plugin name, GUID `com.alternate.hud`, and version `1.0.0`.
- Add `AlternateHudPlugin.cs` as the `BaseUnityPlugin` entry point.
- Add minimal startup and shutdown logging.
- Configure generated plugin version metadata if required by the project.
- Set up the existing deployment and Release packaging targets without adding HUD behavior yet.

### Human input and verification

- Developer reviews each project reference and confirms that it points to the intended SPT installation.
- Developer reviews the plugin metadata and startup code.
- Run `dotnet build -c Release`.
- Install the DLL in the test installation and confirm that BepInEx loads the plugin and logs its startup.
- Confirm that no game or plugin exception is introduced.

### Acceptance criteria

- [x] The project builds with zero errors and zero warnings.
- [x] BepInEx recognizes the plugin.
- [x] The expected GUID and version are visible in plugin metadata.
- [x] Deployment and packaging paths are correct.
- [x] No HUD features have been added prematurely.

**Deliverable:** A minimal, loadable plugin.

---

## Phase 2 — Configuration and Theme Discovery

**Goal:** Establish the configuration contract and discover available themes before implementing the UI.

### Implementation work

- Implement `Settings.cs` and a clear `Bind(ConfigFile, themesDir)` entry point.
- Add the agreed theme selector and F9/F10 shortcuts.
- Add global visibility, scale, opacity, and panel/feature toggles.
- Add panel and per-element X/Y offset settings in the agreed categories.
- Use floating-point offset values with the documented ±2000 range where applicable.
- Populate theme choices from available theme directories.
- Preserve the documented configuration names, categories, defaults, ranges, and descriptions.
- Do not add speculative settings without developer approval.

### Human input and verification

- Developer reviews the complete configuration list and confirms names, defaults, categories, ranges, and descriptions.
- Developer confirms whether all planned toggles and offsets are required for the first release.
- Launch the plugin and inspect the generated BepInEx configuration file.
- Change representative settings, restart the game, and confirm persistence.
- Confirm F9 and F10 defaults.

### Acceptance criteria

- [x] All agreed settings appear in the configuration file.
- [x] Theme choices reflect the available theme folders.
- [x] Defaults and ranges match the approved configuration contract.
- [x] Settings persist across restart.
- [x] Build completes with zero errors and zero warnings.

**Deliverable:** A working and reviewed configuration layer.

---

## Phase 3 — Theme XML Reader and Resource Loading

**Goal:** Load theme data reliably before using it to construct the HUD.

### Implementation work

- Implement `XNode` as a typed wrapper around `XmlElement`.
- Implement the `Theme` model and `theme.xml` loading.
- Read the documented sections: `Font`, `Colors`, `Vitals`, `Icons`, `IconColors`, `Effects`, `Equipment`, `Movement`, `HitMarker`, `DamageNumbers`, `DamageDirection`, and `KillFeed`.
- Support optional XML attributes and built-in fallback values.
- Implement color parsing for `#RRGGBB` and `#RRGGBBAA`.
- Implement supported rich-text tokens: `{c:#hex}...{/c}`, `{b}`, `{i}`, and `{size:N}`.
- Load and cache sprites and masks, including alpha/luminance mask handling.
- Implement the documented font selection behavior and fallback.
- Add useful logging for invalid XML or missing assets without crashing the plugin.

### Human input and verification

- Developer reviews the XML contract and confirms the sample theme is the reference.
- Developer chooses the first theme to validate and confirms the expected font and asset behavior.
- Test a complete theme, a theme with optional attributes omitted, and a theme with an invalid or missing asset.
- Confirm that fallback behavior is deliberate and visible in logs.
- Verify that shipped theme files are not silently rewritten or renamed.

### Acceptance criteria

- [x] Theme XML loads successfully.
- [x] Optional values receive the intended defaults.
- [x] Color and rich-text parsing work for supported formats.
- [x] Sprites and masks are cached and loaded correctly.
- [x] Missing or invalid data is handled safely.
- [x] Build completes with zero errors and zero warnings.

**Deliverable:** A reviewed theme-loading layer with known fallback behavior.

---

## Phase 4 — Canvas, HUD Root, and Lifecycle

**Goal:** Display an empty but correctly managed HUD overlay.

### Implementation work

- Implement `HudRoot.cs` as the owner of the Canvas, CanvasScaler, and CanvasGroup.
- Configure `ScreenSpaceOverlay`, sorting order `50`, a `1920 × 1080` reference resolution, match value `0.5`, and disabled raycast blocking.
- Create the persistent `AlternateHud` GameObject using `DontDestroyOnLoad` and the intended hide flags.
- Add a content root and a controlled initialization path.
- Implement raid visibility gating and the global Show All HUD behavior.
- Add the approximately two-second plugin watchdog to recover a missing or deactivated root.
- Implement cleanup for root destruction.
- Keep the overlay non-interactive unless an explicitly approved feature requires interaction.

### Human input and verification

- Developer reviews the Unity object hierarchy and lifecycle decisions.
- Run the build and install it in SPT.
- Verify that the canvas is visible only when the intended raid conditions are met.
- Check the Hideout, main menu/lobby, raid entry, raid exit, and a scene transition.
- Confirm that the overlay does not block normal game input.
- If the game destroys the root during testing, verify recovery and check that duplicate canvases are not created.

### Acceptance criteria

- [x] Canvas settings match the architecture.
- [x] The HUD stays hidden outside a raid.
- [x] Show All HUD controls global visibility.
- [x] Scene transitions do not create duplicate roots.
- [x] Watchdog recovery works without repeated recreation.
- [x] Build and relevant manual checks pass.

**Deliverable:** A persistent, correctly gated empty HUD root.

---

## Phase 5 — Shared UI Helpers and First Theme Preview

**Goal:** Build a small amount of UI through shared helpers before adding gameplay features.

### Implementation work

- Implement `UiKit.cs` helpers for creating nodes, images, labels, stretched elements, and aligned elements.
- Add cached white-sprite and font resolution behavior.
- Implement the Bender font special case and its fallback.
- Connect a minimal sample panel to the loaded theme.
- Implement F10 preview mode for approximately five seconds, using static sample data.
- Ensure preview content does not depend on live player state.

### Human input and verification

- Developer reviews the helper API and naming before more views depend on it.
- Developer chooses the sample elements and confirms expected font and alignment.
- Verify positioning at the 1920 × 1080 reference resolution and at a different display resolution.
- Press F10 in a raid and confirm the sample content appears for the expected duration.
- Confirm normal HUD updates resume when preview ends.

### Acceptance criteria

- [x] Shared helpers produce consistent UI elements.
- [x] Fonts and shared sprites are cached.
- [x] Theme positions scale as intended across resolutions.
- [x] Preview uses static data and exits cleanly.
- [x] Build and manual checks pass.

**Deliverable:** Reusable UI helpers and a working preview path.

---

## Phase 6 — Vitals and Body Silhouette

**Goal:** Implement the first live gameplay view, using player health as the first end-to-end integration.

### Implementation work

- Implement `VitalsView.cs`.
- Read player health, energy, and hydration using the confirmed SPT 4.1.5 APIs.
- Display current/maximum values, labels, bars, and health-based colors.
- Implement the seven body parts: Head, Chest, Stomach, Left Arm, Right Arm, Left Leg, and Right Leg.
- Apply health tinting, damage flashes, and LOST/BROKEN status text.
- Implement low-health pulsing and label toggles.
- Implement `SilhouetteGen.cs` for fallback body-part masks when required.
- Keep view layout values theme-driven and apply user offsets separately.

### Human input and verification

- Developer reviews how body-part health values map to silhouette parts and confirms the desired LOST/BROKEN presentation.
- Test the view with the Minimal theme and then TextOnly.
- Verify current/maximum values, bar updates, colors, labels, and low-health behavior.
- Take damage to confirm the correct body part flashes.
- Test missing masks to verify procedural fallback generation.
- Check behavior when player health data is temporarily unavailable.

### Acceptance criteria

- [x] Live vitals display correct values.
- [x] All seven body parts map correctly.
- [x] Damage feedback and status text behave as intended.
- [x] Theme and configuration settings affect the view correctly.
- [x] No exceptions occur when player data is unavailable.
- [x] Build and manual checks pass.

**Deliverable:** A complete, manually verified Vitals view.

---

## Phase 7 — Status Effects

**Goal:** Add the status-effect panel without coupling it to Harmony or unrelated views.

### Implementation work

- Implement `EffectsView.cs`.
- Display light bleeding, heavy bleeding, fractures, and pain.
- Load effect icons and colors through the theme system.
- Display counts where supported by the available game data.
- Apply panel visibility and per-element offsets.
- Handle inactive, cleared, or unavailable effect data safely.

### Human input and verification

- Developer confirms the expected meaning of each effect count and how simultaneous effects should be displayed.
- Test each available condition in-game.
- Verify that indicators appear, update, and disappear at the correct times.
- Test the Minimal and TextOnly themes.
- Confirm that disabling the panel does not affect other views.

### Acceptance criteria

- [x] Supported effects display accurately.
- [x] Counts and state changes are correct.
- [x] Theme assets and offsets are respected.
- [x] The view can be hidden independently.
- [x] Build and manual checks pass.

**Deliverable:** A working Effects view.

---

## Phase 8 — Equipment

**Goal:** Display weapon and grenade information with minimal inventory overhead.

### Implementation work

- Implement `EquipmentView.cs`.
- Classify supported weapon types: assault rifle, carbine, sniper rifle, shotgun, SMG, LMG, pistol, and knife.
- Use one weapon classification result for both icon selection and icon coloring.
- Display localized weapon names and current magazine counts.
- Count grenades in the rig and pockets using the verified SPT 4.1.5 equipment structure.
- Throttle grenade inventory traversal to once every 20 frames.
- Support the theme's card/list presentation and relevant visibility settings.

### Human input and verification

- Developer reviews the weapon-class mapping and confirms icon assets for every supported class.
- Verify weapon names, icons, magazine values, and color selection with representative weapons.
- Test grenade counts with items in the rig and pockets, including empty slots.
- Confirm behavior when changing weapons or when no weapon is equipped.
- Validate the rig/pocket slot assumptions against the target game version before relying on them.

### Acceptance criteria

- [x] Weapon classification, icon, and color agree.
- [x] Localization and magazine counts work.
- [x] Grenade counts update correctly without scanning every frame.
- [x] Missing equipment is handled safely.
- [x] Build and manual checks pass.

**Deliverable:** A working Equipment view.

---

## Phase 9 — Movement and Stamina

**Goal:** Display stance, movement/sound indicators, and stamina information.

### Implementation work

- Implement `MovementView.cs`.
- Read stance from `MovementContext.PoseLevel`.
- Read movement velocity from `MovementContext.ActualLinearVelocity`.
- Display standing, crouching, and prone states.
- Implement the six speed/sound-volume tiers and hysteresis to prevent rapid switching around thresholds.
- Display body and hand stamina values and bars.
- Support TextOnly presentation and individual element offsets.

### Human input and verification

- Developer reviews the speed-tier thresholds and hysteresis behavior before they are finalized.
- Test standing, crouching, prone, walking, and faster movement.
- Verify that indicators do not flicker when values hover near a threshold.
- Confirm body and hand stamina values and bars change with gameplay.
- Verify TextOnly output and the related toggles.

### Acceptance criteria

- [x] Stance matches player movement state.
- [x] Speed tiers update consistently with hysteresis.
- [x] Stamina values and bars track the game data.
- [x] TextOnly and theme-driven layouts work.
- [x] Build and manual checks pass.

**Deliverable:** A working Movement view.

---

## Phase 10 — Gameplay Events and Harmony Patches

**Goal:** Introduce event-based combat integration after the basic HUD views are stable.

### Implementation work

- Implement the `HitEvent`, `DamageTakenEvent`, and `KillEvent` contracts and the `HudEvents` hub.
- Add Harmony patches for `Player.ApplyDamageInfo(...)` and `Player.OnBeenKilledByAggressor(...)`.
- Keep patches focused on reading game data and publishing events.
- Route events through `HudRoot` to the relevant views.
- Use the plugin's BepInEx logger for patch errors.
- Ensure subscribers are removed when the HUD root is destroyed and are not duplicated after rebuilding the UI.

### Human input and verification

- Developer reviews the exact Harmony signatures and parameter mappings against the SPT 4.1.5 assemblies.
- Developer confirms what qualifies as a hit, damage taken, and a kill for HUD purposes.
- Test hits dealt, damage received, kills, and events involving non-local players where applicable.
- Check logs for patch errors and confirm that a single gameplay event does not produce duplicate HUD responses.
- Re-enter raids and verify that event subscriptions remain correct.

### Acceptance criteria

- [x] Patches bind to the intended game methods.
- [x] Events contain the expected information.
- [x] Patches do not manipulate UI directly.
- [x] No duplicate subscriptions or repeated event responses occur.
- [x] Build and manual checks pass.

**Deliverable:** A verified gameplay-event bridge.

---

## Phase 11 — Combat Feedback

**Goal:** Use the event system to implement hit markers, damage numbers, and damage direction.

### Implementation work

- Implement the combat portion of `CombatView.cs`.
- Add configurable hit-marker geometry, X/Cross style, punch animation, and fade.
- Add floating damage numbers using a pool of 14 objects.
- Add damage-direction indicators using a pool of 6 objects.
- Use camera/player position data only where required, and handle missing camera or source information safely.
- Add independent toggles, theme styling, and per-element offsets.

### Human input and verification

- Developer approves hit-marker shape, thickness, animation timing, and damage-number presentation.
- Test successful hits, incoming damage, and different damage directions in-game.
- Verify the direction indicator points toward the correct source.
- Toggle each feature independently and test repeated events to exercise the pools.
- Inspect performance during combat and check for stale indicators or objects remaining active.

### Acceptance criteria

- [x] Hit markers display only for the intended hit events.
- [x] Damage numbers appear and expire correctly.
- [x] Direction indicators point toward the correct source when available.
- [x] Pools reuse objects without stale state.
- [x] Each feature can be toggled independently.
- [x] Build and manual checks pass.

**Deliverable:** A verified Combat view.

---

## Phase 12 — Kill Feed

**Goal:** Display kill notifications using the event system and theme-defined formatting.

### Implementation work

- Implement `KillFeedView.cs`.
- Format killer, body part, and victim information using the agreed event data.
- Apply category-based colors for PMC, BOT, BOSS, and SCAV entries where classification data is available.
- Implement rich-text formatting, a configurable line limit, slide-in animation, and fade-in/fade-out behavior.
- Reuse UI helpers and pooled or otherwise bounded UI objects where appropriate.
- Ensure new entries stack correctly and old entries are removed or reused safely.

### Human input and verification

- Developer approves the line format, category colors, ordering, maximum line count, and animation timings.
- Test different kill types and body parts.
- Verify localized names and missing/unknown classification behavior.
- Generate multiple notifications in quick succession and confirm that lines stack and expire correctly.
- Confirm that rebuilding the HUD clears old lines and does not leave stale event subscribers.

### Acceptance criteria

- [x] Kill information is accurate for supported events.
- [x] Category colors and rich text render correctly.
- [x] Entries animate, stack, and expire as intended.
- [x] The configured line limit is respected.
- [x] Build and manual checks pass.

**Deliverable:** A verified Kill Feed view.

---

## Phase 13 — Integrate Themes, Settings, and Layout

**Goal:** Verify that all views work together under the shipped themes and configuration options.

### Implementation work

- Integrate all six views into `HudRoot`.
- Confirm the `Minimal`, `TextOnly`, and `Sample` theme behavior.
- Apply theme scale together with the configured HUD scale.
- Apply panel-level and element-level X/Y offsets consistently.
- Confirm global and per-panel visibility rules, including preview behavior.
- Verify F9 theme reload and F10 preview without restarting the game.
- Confirm that optional XML values and missing assets use their documented fallbacks.

### Human input and verification

- Developer reviews the complete HUD layout and approves any visual differences from the intended theme design.
- Test every shipped theme at the reference resolution and at least one other resolution.
- Change theme, offsets, scale, opacity, and toggles; confirm the changes take effect as expected.
- Restart the game and confirm configuration persistence.
- Check that preview mode does not overwrite live gameplay values or leave temporary content visible.

### Acceptance criteria

- [x] All views render together without unintended overlap.
- [x] Shipped themes behave according to their definitions.
- [x] F9 reloads the active theme correctly.
- [x] F10 preview starts and ends cleanly.
- [x] Settings and offsets work and persist.
- [x] Build and manual checks pass.

**Deliverable:** An integrated HUD ready for release hardening.

---

## Phase 14 — Stability, Performance, and Regression Testing

**Goal:** Check the complete implementation under normal gameplay and common edge cases.

### Implementation work

- Review update paths for avoidable allocations, repeated lookups, and unnecessary inventory traversal.
- Verify object-pool reset behavior and cached-resource lifetimes.
- Check event unsubscription and HUD-root recreation.
- Test missing or unavailable player, health controller, camera, weapon, and theme resources.
- Review logs for recurring errors and exceptions.
- Confirm project references, deployment output, and package contents.
- Update `TEST_PLAN.md` with actual results and unresolved issues.

### Human input and verification

- Developer runs the complete manual test checklist in SPT 4.1.5.
- Exercise multiple raids, scene transitions, preview mode, theme reloads, and configuration changes.
- Inspect logs and, if available, use the Unity profiler to investigate obvious allocation spikes.
- Review known assumptions, especially equipment slot indexing and reflection-based localization.
- Decide which issues block release and which are documented limitations.

### Acceptance criteria

- [x] Release build completes with zero errors and zero warnings.
- [x] All implemented features pass their applicable tests.
- [x] No critical crashes, duplicate HUD roots, or persistent event subscriptions occur.
- [x] Configuration and theme contracts remain intact.
- [x] Known limitations and any untested cases are documented.
- [x] Developer approves the release candidate.

**Deliverable:** A tested release candidate with recorded evidence.

---

## Phase 15 — Release Packaging and Final Review

**Goal:** Prepare a reproducible release package and document exactly what was verified.

### Implementation work

- Run `dotnet build -c Release`.
- Verify the generated `AlternateHud.dll`.
- Verify `dist\AlternateHud-1.0.0.zip` contains the expected DLL, `Themes` directory, and `README.md`.
- Install the package into a clean SPT 4.1.5 plugin directory and verify that it loads.
- Review `README.md`, `ARCHITECTURE.md`, `CHANGELOG.md`, and `TEST_PLAN.md`.
- Record known limitations and the exact SPT target.
- Review the final Git diff and commit the accepted release state.

### Human input and verification

- Developer inspects the ZIP contents and confirms no development-only files or unintended assets are included.
- Developer installs the packaged build rather than relying only on the working-tree deployment.
- Developer performs a final smoke test: launch, enter raid, inspect core HUD features, reload theme, preview, and exit.
- Developer approves the version number and release notes.

### Acceptance criteria

- [x] Release build and package are generated successfully.
- [x] The package installs and loads in SPT 4.1.5.
- [x] Documentation matches the implemented behavior.
- [x] Test results reflect what was actually run.
- [x] Final changes are reviewed and committed.
- [x] Release is explicitly approved by the developer.

**Deliverable:** A reviewed and installable AlternateHud release.

---

## Phase Tracking Template

Copy this section for each phase and complete it before moving on.

### Phase: `[number and name]`

**Scope agreed with developer:**  
`[Record the agreed work and any exclusions.]`

**Human decisions/input:**  
`[Record choices, thresholds, UI expectations, API confirmations, or other decisions made by the developer.]`

**Files changed:**  
`[List each file and why it changed.]`

**Implementation explanation:**  
`[Explain the main methods, data flow, and lifecycle implications in plain language.]`

**Build result:** `NOT TESTED`

**Manual verification:** `NOT TESTED`

**Evidence:**  
`[Build output, log excerpt, screenshot, or reproducible test steps.]`

**Open issues or assumptions:**  
`[Record unresolved questions; do not silently guess.]`

**Developer review:** `PENDING`

**Decision:** `PENDING — continue only after approval`

---

## Final Principle

AlternateHud should be developed as a sequence of small, understandable changes. Each phase must have a clear goal, explicit developer input, a clean build, and appropriate human verification before work proceeds. Compilation confirms that the code fits together; only in-game testing confirms that the HUD behaves correctly.
