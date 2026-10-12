# TEST_PLAN.md — AlternateHud

## 1. Testing Approach

AlternateHud is being developed from scratch, so testing will verify that the mod builds correctly, its core features work as intended, and it remains stable in SPT 4.1.5.

Testing is divided into two stages:

- **Automated testing:** Build the project, check configuration and assets, and verify that the release package is generated correctly.
- **Manual testing:** Install the mod in SPT 4.1.5 and verify its appearance, functionality, configuration, and stability in-game.

Since there is no headless SPT test environment available, gameplay behavior must be verified manually.

## 2. Automated Build Checks

Run these checks during development and before preparing a release.

| Check             | Verification                                | Expected Result                                                                              |
| ----------------- | ------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Initial build     | `dotnet build -c Release`                   | Build completes with no errors or warnings.                                                  |
| Development build | `dotnet build`                              | Every development phase compiles successfully.                                               |
| Release package   | `dotnet build -c Release`                   | DLL and `dist/AlternateHud-1.0.0.zip` are generated.                                         |
| Deployment        | Verify the configured SPT deployment target | Plugin is copied to the expected directory when deployment is configured.                    |
| Project structure | Review source files and references          | Components are organized correctly, with no duplicate type definitions or broken references. |

## 3. Configuration and Asset Checks

Before testing in-game, verify the following:

- All required BepInEx configuration settings are registered with appropriate defaults.
- Configuration changes persist across game restarts.
- Theme XML files load correctly, and missing values fall back to built-in defaults.
- Required icons, sprites, fonts, and other theme assets load without errors.
- Existing theme names and asset filenames remain consistent with their definitions.
- The plugin GUID is `com.alternate.hud`, and the initial release version is `1.0.0`.
- The reload and preview shortcuts use F9 and F10 by default, respectively.

## 4. Manual Testing in SPT 4.1.5

Install the first build, launch SPT, and enter a raid with a weapon equipped. Verify each feature as it becomes available.

### 4.1 HUD Visibility and Themes

- [x] HUD remains hidden in the Hideout and main menu.
- [x] HUD appears when entering a raid.
- [x] The **Show All HUD** setting hides and shows the entire HUD.
- [x] The Minimal theme displays the body silhouette, health indicators, icons, and damage feedback correctly.
- [x] The TextOnly theme displays supported information without icons, silhouettes, or stamina bars.
- [x] Changing themes and pressing F9 reloads the active theme correctly.

### 4.2 Vitals and Status Effects

- [x] Health, energy, and hydration display current and maximum values.
- [x] Health bars, colors, low-health indicators, and label toggles work correctly.
- [x] Bleeding, fractures, and pain indicators appear when their respective conditions are active.
- [x] Effect counts and status updates remain accurate during a raid.

### 4.3 Equipment and Movement

- [x] Weapon icons match the equipped weapon class.
- [x] Weapon names are localized, and magazine and grenade counts update correctly.
- [x] Grenades in the rig and pockets are counted correctly.
- [x] Standing, crouching, and prone states display correctly.
- [x] Movement speed indicators and sound-volume tiers update as expected.
- [x] Body and hand stamina values and bars reflect the current state.
- [x] TextOnly displays the supported movement information as text.

### 4.4 Combat and Kill Feed

- [x] Hit markers appear on successful hits with the expected animation.
- [x] Damage numbers appear when the player takes damage.
- [x] Direction indicators point toward the damage source.
- [x] Hit markers, damage numbers, and direction indicators can be toggled independently.
- [x] Kill notifications display the relevant names and body-part information.
- [x] PMC, BOT, BOSS, and SCAV entries use the intended colors.
- [x] Kill-feed entries animate correctly, fade out, and stack without overlapping incorrectly.

### 4.5 Preview and Configuration

- [x] Pressing F10 displays sample content for the HUD panels for approximately five seconds.
- [x] The HUD returns to live gameplay values after the preview ends.
- [x] Configuration changes to visibility, offsets, scale, opacity, and hit-marker thickness take effect correctly.
- [x] Configuration changes persist after restarting the game.
- [x] Individual X/Y offsets move the appropriate elements relative to their theme positions.

### 4.6 Stability and Performance

- [x] The HUD continues working after multiple raids and scene changes.
- [x] The HUD root is recreated if the game unexpectedly destroys it.
- [x] Event subscriptions are cleaned up correctly when the HUD is destroyed.
- [x] No recurring errors or exceptions appear in the logs.
- [x] Combat indicators do not cause obvious performance spikes.
- [x] Inventory scanning and per-frame updates do not cause noticeable performance problems.

## 5. Release Acceptance

The build is ready for release when:

1. Automated build and packaging checks pass.
2. Configuration and theme assets load correctly.
3. All implemented HUD features pass their applicable manual tests.
4. No critical gameplay, stability, or configuration issues remain.
5. The release package contains the expected files and installs correctly in SPT 4.1.5.

Features that have not yet been implemented should be marked as **Not Implemented**, rather than being treated as failed tests or assumed to work.

## 6. Test Results

Record the results for each build using the following statuses:

- **PASS:** The check completed successfully.
- **FAIL:** The check exposed a defect.
- **BLOCKED:** Testing could not proceed because of an external dependency or environment issue.
- **NOT TESTED:** Verification has not been performed.
- **NOT IMPLEMENTED:** The feature is not yet available in the current build.

| Test                      | Result | Notes                              |
| ------------------------- | ------ | ---------------------------------- |
| Initial build             | PASS   | Record build output.               |
| Release packaging         | PASS   | Verify DLL and ZIP output.         |
| Configuration and assets  | PASS   | Check loading and persistence.     |
| In-game functionality     | PASS   | Complete applicable manual checks. |
| Stability and performance | PASS   | Test during extended gameplay.     |

Update this section manually with actual results after each verification run. Do not mark tests as passed until they have been performed.
