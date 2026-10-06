# Gunwright

A server-side mod for SPT 4.1.x: a trader that sells your own weapon builds plus ready-made headgear, armor, rig and plate presets — unlimited stock, automatic prices.

## Requirements

- [SPT 4.1.x](https://sp-tarkov.com)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (build only)

## Install

- Download the zip from the [latest release](https://github.com/DeadWolf/SPTarkov.Gunwright/releases/latest) and extract it into your server root (it contains the `SPT_Runtime` tree), or
- Copy only the `Gunwright` folder into `<server root>\SPT_Runtime\user\mods\`

Start the server. Weapon builds saved in-game show up at the trader within ~30 seconds.

## Presets

Headgear, armor, rig and plate presets live in `user\mods\Gunwright\db\` as plain JSON — edit and restart the server, no rebuild needed:

```json
[
  {
    "RootTpl": "5b40e2bc5acfc40016388216",
    "Attachments": [
      { "Slot": "Helmet_top", "Tpl": "657112234269e9a568089eac" }
    ]
  }
]
```

- `RootTpl`: the `_tpl` of the sold item; an empty `Attachments` list sells it as a plain item
- `Attachments`: children pre-installed on the root, `Slot` must match one of its slot names
- Keys are case-sensitive (`RootTpl`, `Attachments`, `Slot`, `Tpl`)

Prices are automatic: for each item, the average of its handbook and ragfair baseline price is summed over the preset to get a raw value, then a rarity-based multiplier is applied. Unlimited stock on everything.

## Pricing

`PriceConfig` in `db/config.json` controls the multiplier applied on top of each preset's raw value:

```json
"PriceConfig": {
  "DefaultMultiplier": 1.0,
  "RarityTiers": [
    { "Name": "Common", "MinValue": 0, "Multiplier": 1.0 },
    { "Name": "Uncommon", "MinValue": 100000, "Multiplier": 0.95 },
    { "Name": "Rare", "MinValue": 300000, "Multiplier": 0.9 },
    { "Name": "Epic", "MinValue": 600000, "Multiplier": 0.85 },
    { "Name": "Legendary", "MinValue": 1000000, "Multiplier": 0.8 }
  ]
}
```

- `RarityTiers`: each preset's raw value is bucketed into the highest tier whose `MinValue` it meets or exceeds, and that tier's `Multiplier` is applied. Tiers may be listed in any order - they're sorted by `MinValue` internally.
- `DefaultMultiplier`: used when no tier applies (empty/missing `RarityTiers`, or the raw value is below every tier's `MinValue`).
- Edit and restart the server; no rebuild needed.

## Build

Double-click `build.bat`, or run:

```powershell
dotnet build SPTarkov.Gunwright.sln -c Release
```

Output goes to `Build\Release\SPT_Runtime\user\mods\Gunwright`. A Release build also packages a ready-to-ship `Build\Gunwright.zip` containing the `SPT_Runtime` tree (DLL, PDB and `db` config files) - extract it straight into your server root.

## License

[AGPL-3.0](LICENSE)
