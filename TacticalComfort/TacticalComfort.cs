#nullable enable

using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using System.Text.Json.Nodes;

namespace TacticalComfort;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.TacticalComfort";
    public string Name { get; init; } = "TacticalComfort";
    public string Author { get; init; } = "DeadW0Lf";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.0.4");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class TacticalComfort(ISptLogger<TacticalComfort> logger, TemplateTable templateTable) : IOnLoad
{
    private readonly ISptLogger<TacticalComfort> _logger = logger;
    private readonly TemplateTable _templateTable = templateTable;

    private enum EquipmentCategory
    {
        Armor,
        ArmoredEquipment,
        ArmorRig,
        TacticalRig,
        Backpack,
        Helmet,
        ArmorPlate,
        BuiltInInserts
    }

    private sealed record PenaltyReductions(
        double Ergonomics,
        double MovementSpeedPenalty,
        double TurnSpeedPenalty,
        double WeaponErgonomicPenalty);

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _logger.LogWithColor("[TacticalComfort] Initializing TacticalComfort...", Spectre.Console.Color.Green);

        JsonObject config = LoadJson("user/mods/TacticalComfort/config/config.json");
        Dictionary<EquipmentCategory, PenaltyReductions> reductions = new()
        {
            [EquipmentCategory.Armor] = ReadReductions(config, "armor"),
            [EquipmentCategory.ArmoredEquipment] = ReadReductions(config, "armoredEquipment"),
            [EquipmentCategory.ArmorRig] = ReadReductions(config, "armorRig"),
            [EquipmentCategory.TacticalRig] = ReadReductions(config, "tacticalRig"),
            [EquipmentCategory.Backpack] = ReadReductions(config, "backpack"),
            [EquipmentCategory.Helmet] = ReadReductions(config, "helmet"),
            [EquipmentCategory.ArmorPlate] = ReadReductions(config, "armorPlate"),
            [EquipmentCategory.BuiltInInserts] = ReadReductions(config, "builtInInserts")
        };

        Dictionary<EquipmentCategory, int> categorizedItems = [];
        Dictionary<EquipmentCategory, int> modifiedItems = [];
        foreach (KeyValuePair<MongoId, TemplateItem> entry in _templateTable.Items)
        {
            TemplateItem item = entry.Value
                ?? throw new InvalidDataException($"[TacticalComfort] Template item {entry.Key} is null.");
            TemplateItemProperties properties = item.Properties
                ?? throw new InvalidDataException($"[TacticalComfort] Template item {entry.Key} has no properties.");
            EquipmentCategory? category = GetCategory(item, properties);
            if (category is null)
            {
                continue;
            }

            PenaltyReductions categoryReductions = reductions[category.Value];
            categorizedItems[category.Value] = categorizedItems.GetValueOrDefault(category.Value) + 1;
            bool itemModified = false;

            itemModified |= ReducePenalty(entry.Key, category.Value, "ergonomics",
                properties.Ergonomics, categoryReductions.Ergonomics, value => properties.Ergonomics = value);
            itemModified |= ReducePenalty(entry.Key, category.Value, "movementSpeedPenalty (speedPenaltyPercent)",
                properties.SpeedPenaltyPercent, categoryReductions.MovementSpeedPenalty,
                value => properties.SpeedPenaltyPercent = value);
            itemModified |= ReducePenalty(entry.Key, category.Value, "turnSpeedPenalty (mousePenalty)",
                properties.MousePenalty, categoryReductions.TurnSpeedPenalty, value => properties.MousePenalty = value);
            itemModified |= ReducePenalty(entry.Key, category.Value, "weaponErgonomicPenalty",
                properties.WeaponErgonomicPenalty, categoryReductions.WeaponErgonomicPenalty,
                value => properties.WeaponErgonomicPenalty = value);

            if (itemModified)
            {
                modifiedItems[category.Value] = modifiedItems.GetValueOrDefault(category.Value) + 1;
            }
        }

        foreach (EquipmentCategory category in Enum.GetValues<EquipmentCategory>())
        {
            _logger.Debug(
                $"[TacticalComfort] {category}: found {categorizedItems.GetValueOrDefault(category)} items; " +
                $"modified {modifiedItems.GetValueOrDefault(category)}.");
        }

        _logger.LogWithColor("[TacticalComfort] Loading completed.", Spectre.Console.Color.Green);
        return Task.CompletedTask;
    }

    private static EquipmentCategory? GetCategory(TemplateItem item, TemplateItemProperties properties)
    {
        MongoId parentId = item.Parent;

        if (parentId.Equals(BaseClasses.ARMOR))
        {
            return EquipmentCategory.Armor;
        }

        if (parentId.Equals(BaseClasses.ARMORED_EQUIPMENT))
        {
            return EquipmentCategory.ArmoredEquipment;
        }

        if (parentId.Equals(BaseClasses.VEST))
        {
            return properties.ArmorClass.GetValueOrDefault() > 0
                ? EquipmentCategory.ArmorRig
                : EquipmentCategory.TacticalRig;
        }

        if (parentId.Equals(BaseClasses.BACKPACK))
        {
            return EquipmentCategory.Backpack;
        }

        if (parentId.Equals(BaseClasses.ARMOR_PLATE))
        {
            return EquipmentCategory.ArmorPlate;
        }

        if (parentId.Equals(BaseClasses.BUILT_IN_INSERTS))
        {
            return EquipmentCategory.BuiltInInserts;
        }

        if (parentId.Equals(BaseClasses.HEADWEAR) &&
            (properties.ArmorClass.GetValueOrDefault() > 0 || HasHelmetPenalty(properties)))
        {
            return EquipmentCategory.Helmet;
        }

        return null;
    }

    private static bool HasHelmetPenalty(TemplateItemProperties properties)
    {
        return properties.Ergonomics < 0
            || properties.SpeedPenaltyPercent < 0
            || properties.MousePenalty < 0
            || properties.WeaponErgonomicPenalty < 0;
    }

    private bool ReducePenalty(
        MongoId itemId,
        EquipmentCategory category,
        string propertyName,
        double? originalValue,
        double reductionPercentage,
        Action<double> setValue)
    {
        if (reductionPercentage == 0 || originalValue is null || originalValue == 0)
        {
            return false;
        }

        if (propertyName == "ergonomics" && originalValue > 0)
        {
            return false;
        }

        double original = originalValue.Value;
        double remainingMagnitude = Math.Abs(original) * (1 - reductionPercentage / 100);
        double finalValue = Math.Sign(original) *
            Math.Round(remainingMagnitude, MidpointRounding.AwayFromZero);

        if (finalValue == original)
        {
            return false;
        }

        setValue(finalValue);
        _logger.Debug(
            $"[TacticalComfort] {category} item {itemId}: {propertyName} {original} -> {finalValue} " +
            $"({reductionPercentage}% reduction).");
        return true;
    }

    private static PenaltyReductions ReadReductions(JsonObject config, string categoryName)
    {
        if (!config.TryGetPropertyValue(categoryName, out JsonNode? categoryNode) ||
            categoryNode is not JsonObject categoryConfig)
        {
            throw new InvalidDataException($"[TacticalComfort] Config category '{categoryName}' must be an object.");
        }

        double turnSpeedPenalty = categoryConfig.ContainsKey("turnSpeedPenalty")
            ? ReadPercentage(categoryConfig, categoryName, "turnSpeedPenalty")
            : ReadPercentage(categoryConfig, categoryName, "mousePenalty");
        double movementSpeedPenalty = categoryConfig.ContainsKey("movementSpeedPenalty")
            ? ReadPercentage(categoryConfig, categoryName, "movementSpeedPenalty")
            : ReadPercentage(categoryConfig, categoryName, "speedPenaltyPercent");

        return new PenaltyReductions(
            ReadPercentage(categoryConfig, categoryName, "ergonomics"),
            movementSpeedPenalty,
            turnSpeedPenalty,
            ReadPercentage(categoryConfig, categoryName, "weaponErgonomicPenalty"));
    }

    private static double ReadPercentage(JsonObject config, string categoryName, string propertyName)
    {
        if (!config.TryGetPropertyValue(propertyName, out JsonNode? valueNode) || valueNode is null)
        {
            return 0;
        }

        double percentage;
        try
        {
            percentage = valueNode.GetValue<double>();
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"[TacticalComfort] '{categoryName}.{propertyName}' must be a number from 0 to 100.",
                exception);
        }

        if (!double.IsFinite(percentage) || percentage < 0 || percentage > 100)
        {
            throw new InvalidDataException(
                $"[TacticalComfort] '{categoryName}.{propertyName}' must be from 0 to 100.");
        }

        return percentage;
    }

    private static JsonObject LoadJson(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"[TacticalComfort] Config file missing: {fullPath}");
        }

        string json = File.ReadAllText(fullPath);
        return JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException($"[TacticalComfort] Config file is empty or invalid: {fullPath}");
    }
}
