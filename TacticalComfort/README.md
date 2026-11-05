# TacticalComfort

TacticalComfort reduces configured handling and mobility penalties on armor, armored equipment, armored rigs, tactical rigs, backpacks, helmets, armor plates, and built-in inserts. It uses SPT's item templates and runs after the database has loaded. Attachments and other item categories are not changed.

## Installation

1. Extract the release ZIP. It contains an `SPT_Runtime\user\mods\TacticalComfort` folder.
2. Copy `SPT_Runtime\user\mods\TacticalComfort` into the SPT server's `user\mods` folder.
3. Start the server once and check its log for the TacticalComfort initialization message.
4. Edit `user\mods\TacticalComfort\config\config.json` to adjust the percentage reductions, then restart the server.

The project targets SPT 4.1.x. A Release build writes `TacticalComfort_v<version>.zip` to the project `dist` folder, using the project `Version` property with periods replaced by hyphens (for example, version `2.0.4` produces `TacticalComfort_v2-0-4.zip`).

## Configuration

Each category (`armor`, `armoredEquipment`, `armorRig`, `tacticalRig`, `backpack`, `helmet`, `armorPlate`, and `builtInInserts`) has independent reductions for `ergonomics`, `movementSpeedPenalty`, `turnSpeedPenalty`, and `weaponErgonomicPenalty`. Values are percentages from `0` to `100`; `0` preserves the original item property, and omitted properties default to `0`. In SPT's item template, movement-speed reduction maps to `speedPenaltyPercent`, while turn-speed reduction maps to `mousePenalty`. Existing configs using `speedPenaltyPercent` or `mousePenalty` are supported; when both a friendly name and legacy field name are present, the friendly name takes precedence. Armor, armored equipment, armor plate, and built-in insert defaults use 75% movement and turn reductions; adjust categories independently as needed.

For example:

```json
"armor": {
  "ergonomics": 25,
  "movementSpeedPenalty": 25,
  "turnSpeedPenalty": 25,
  "weaponErgonomicPenalty": 25
}
```

Final changed values are rounded to the nearest whole number, with signed penalties reduced toward zero. A setting reduces the original value proportionally, so a `25` percent reduction retains 75 percent of the original penalty. Positive ergonomics values are left unchanged because they are bonuses rather than penalties. Every changed property is logged with its item ID, SPT property name, original value, reduction percentage, and final value. Startup logs also report how many items were found and modified in each category.
