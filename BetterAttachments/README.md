# BetterAttachments

BetterAttachments improves ergonomics and recoil values on supported weapon attachments in SPT 4.1. It also improves supported attachments that are not listed in the configuration, which helps cover new items and attachments added by other mods.

## Installation

1. Build the Release package or download a release ZIP.
2. Extract the ZIP into the SPT installation directory, keeping the `SPT_Runtime` folder.
3. Confirm the mod files are under `SPT_Runtime\user\mods\BetterAttachments`.

The mod loads its settings from `SPT_Runtime\user\mods\BetterAttachments\config\config.json`.

## Configuration

The top-level Boolean settings enable or disable improvements by attachment type:

- `betterForegrips`
- `betterSights`
- `betterMuzzles`
- `betterMuzzleRecoil`
- `betterSuppressors`
- `betterSuppressorRecoil`
- `betterSuppressorHeating`
- `betterTacticals`
- `betterStocks`
- `betterPistolGrips`
- `betterHandGuards`

`missingAttachmentImprovementPercent` sets the percentage adjustment for supported attachments that are not present in the `value` object. It defaults to `10` and must be between `0` and `100`; set it to `0` to disable these automatic improvements. Positive ergonomics values are increased, while recoil values are improved in the reducing direction (more negative for negative values and closer to zero for positive values).

Entries in `value` are keyed by item template ID and use explicit `ergonomics_updated` and `recoil_updated` values. These explicit values take precedence over automatic percentage adjustments. Supported attachment types include foregrips, sights, muzzle devices, suppressors, tactical devices, stocks, handguards, and pistol grips.

## Building

Use the .NET 10 SDK:

```powershell
dotnet build -c Release
```

The build writes the mod under `bin\Release\SPT_Runtime` and creates a versioned archive in `dist`. The ZIP name is generated from the `<Version>` in `BetterAttachments.csproj`, with dots replaced by hyphens; for example, version `1.1.0` produces `dist\BetterAttachments_v1-1-0.zip`. The archive retains the `SPT_Runtime` folder at its root.
