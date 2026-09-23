using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Nodes;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Server;
using System.Text.Json;

namespace ReadableKeys;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.deadwolf.spt.readablekeys";
    public override string Name { get; init; } = "ReadableKeys";
    public override string Author { get; init; } = "DeadW0Lf";
    public override List<string>? Contributors { get; init; } = new();
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; } = new();
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

// 1. Define Typed Configuration Classes (Matches your JSON structure)
public class ReadableKeysConfig
{
    public bool changeNoOfUse { get; set; } = false;
    public float keyUsageMultiplier { get; set; } = 0f;
    public bool changeShortName { get; set; } = false;
    public string locales { get; set; } = "en";
    public Dictionary<string, string> prefix { get; set; } = new();
    public Dictionary<string, KeyUseConfig> config { get; set; } = new();
}

public class KeyUseConfig
{
    public int noofuse { get; set; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ReadableKeys(
        ISptLogger<ReadableKeys> logger, DatabaseServer databaseServcer) : IOnLoad
{
    // Base classes from EFT
    private const string KeyMechanical = "5c99f98d86f7745c314214b3";
    private const string Keycard = "5c164d2286f774194c5e69fa";

    private ReadableKeysConfig? modConfig;
    private Dictionary<string, List<string>>? mapWithKeys;

    public Task OnLoad()
    {
        // Resolve the absolute path to your mod folder safely
        string configFolderPath = "user/mods/ReadableKeys/config";

        try
        {
            // Deserialize JSON directly into C# objects using ModHelper
            modConfig = LoadJson<ReadableKeysConfig>(System.IO.Path.Combine(configFolderPath, "config.json"));

            mapWithKeys = LoadJson<Dictionary<string, List<string>>>(System.IO.Path.Combine(configFolderPath, "mapWithKeys.json"));
        }
        catch (Exception ex)
        {
            logger.Error("Error loading ReadableKeys config data. Disabling mod.", ex);
            return Task.CompletedTask;
        }

        var itemsDb = databaseServcer.GetTables().Templates.Items;
        var localeTable = databaseServcer.GetTables().Locales;

        // Fetch the English locale dictionary cleanly to match English item names for your map dictionary
        var enLocales = localeTable.Global["en"];

        // We will store all our desired string updates here so the Transformers can apply them later
        Dictionary<string, (string MapName, string Prefix)> keyUpdates = new();

        int updatedCount = 0;

        // 1. Iterate Items DB for usage count and prepare Locale data
        foreach (var kvp in itemsDb)
        {
            string itemId = kvp.Key.ToString();
            TemplateItem item = kvp.Value;
            TemplateItemProperties itemProps = item.Properties;

            if (item.Parent.ToString() == KeyMechanical || item.Parent.ToString() == Keycard)
            {
                if (modConfig.config != null && modConfig.config.ContainsKey(itemId))
                {
                    // Get the English name to match against mapWithKeys.json
                    //string itemNameLocal = enLocales.ContainsKey($"{itemId} Name") ? enLocales[$"{itemId} Name"] : "";

                    string mapName = FindMapForItem(itemId, mapWithKeys);
                    //logger.LogWithColor($"[ReadableKeys] Processing item ID: {itemId} - Name: {itemNameLocal}. Found map: {mapName}", Spectre.Console.Color.Green);

                    if (!string.IsNullOrEmpty(mapName))
                    {
                        // A) Update Usage Count directly on the item properties
                        UpdateUsageCount(itemId, itemProps, modConfig.changeNoOfUse, modConfig.keyUsageMultiplier, modConfig.config);

                        // B) Queue up string updates for the Transformers
                        if (modConfig.changeShortName)
                        {
                            string prefix = modConfig.prefix != null && modConfig.prefix.ContainsKey(mapName) ? modConfig.prefix[mapName] : mapName;
                            keyUpdates[itemId] = (mapName, prefix);

                            // Update base property (client fallback)
                            itemProps.ShortName = $"{mapName}: {itemProps.ShortName}";
                        }
                        updatedCount++;
                    }
                }
                // else
                // {
                //     logger.LogWithColor($"[ReadableKeys] No configuration found for item ID: {itemId} - Name: {item.Name}. Skipping.", LogTextColor.Yellow);
                // }
            }
        }

        // 2. Attach LazyLoad Transformers to apply names
        if (modConfig.changeShortName && keyUpdates.Count > 0)
        {
            // Apply to Global Locales
            foreach (var lazyloadedValue in localeTable.Global.Values)
            {
                lazyloadedValue.AddTransformer(lazyloadedLocaleData =>
                {
                    if (lazyloadedLocaleData == null) return lazyloadedLocaleData;

                    foreach (var (itemId, updateData) in keyUpdates)
                    {
                        string nameKey = $"{itemId} Name";
                        string shortNameKey = $"{itemId} ShortName";
                        string descKey = $"{itemId} Description";

                        if (lazyloadedLocaleData.TryGetValue(shortNameKey, out string oldShort))
                        {
                            lazyloadedLocaleData[shortNameKey] = updateData.MapName != "RESERVE"
                                ? $"{updateData.Prefix}-{oldShort}"
                                : $"{updateData.Prefix}-{oldShort}";
                        }

                        if (lazyloadedLocaleData.TryGetValue(nameKey, out string oldName))
                        {
                            lazyloadedLocaleData[nameKey] = $"{updateData.MapName}: {oldName}";
                        }

                        if (lazyloadedLocaleData.TryGetValue(descKey, out string oldDesc))
                        {
                            lazyloadedLocaleData[descKey] = $"{updateData.MapName}: {oldDesc}";
                        }
                    }
                    return lazyloadedLocaleData;
                });
            }
        }

        logger.LogWithColor($"[ReadableKeys] Load Successful. Processed {updatedCount} keys.", LogTextColor.Green);
        return Task.CompletedTask;
    }

    private void UpdateUsageCount(string itemId, TemplateItemProperties itemProps, bool changeNoOfUse, float multiplier, Dictionary<string, KeyUseConfig> itemsConfig)
    {
        if (!changeNoOfUse) return;

        if (itemsConfig.TryGetValue(itemId, out var itemConfig))
        {
            itemProps.MaximumNumberOfUsage = itemConfig.noofuse;
        }
        else
        {
            itemProps.MaximumNumberOfUsage = 50; // Default value if not specified in config
        }

        if (multiplier > 0)
        {
            itemProps.MaximumNumberOfUsage = (int)(itemProps.MaximumNumberOfUsage * multiplier);
        }
    }

    private string FindMapForItem(string itemId, Dictionary<string, List<string>> mapWithKeys)
    {
        if (string.IsNullOrEmpty(itemId) || mapWithKeys == null) return null;

        foreach (var entry in mapWithKeys)
        {
            string mapName = entry.Key;
            List<string> items = entry.Value;

            if (items != null && items.Contains(itemId))
            {
                return mapName;
            }
        }
        return null;
    }

    private T LoadJson<T>(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"[ReadableKeys] Config file missing: {fullPath}");
        }

        string json = File.ReadAllText(fullPath);

        T? result = JsonSerializer.Deserialize<T>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null)
        {
            throw new InvalidOperationException(
                $"[ReadableKeys] Failed to deserialize config: {fullPath}");
        }

        return result;
    }
}