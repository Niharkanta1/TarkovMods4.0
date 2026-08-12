using System.Reflection;
using System.Text.Json.Nodes;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;

namespace ReadableKeys;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.readablekeys";
    public string Name { get; init; } = "ReadableKeys";
    public string Author { get; init; } = "DeadW0Lf";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public bool? IsBundleMod { get; init; } = false;
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
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

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class ReadableKeys(
        ISptLogger<ReadableKeys> logger,
        TemplateTable templateTable,
        LocaleTable localeTable,
        LocaleService localeService,
        ModHelper modHelper) : IOnLoad
{
    // Base classes from EFT
    private const string KeyMechanical = "5c99f98d86f7745c314214b3";
    private const string Keycard = "5c164d2286f774194c5e69fa";

    private ReadableKeysConfig? modConfig;
    private Dictionary<string, List<string>>? mapWithKeys;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // Resolve the absolute path to your mod folder safely
        string configFolderPath = System.IO.Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config");

        try
        {
            // Deserialize JSON directly into C# objects using ModHelper
            modConfig = modHelper.GetJsonDataFromFile<ReadableKeysConfig>(configFolderPath, "config.json");
            mapWithKeys = modHelper.GetJsonDataFromFile<Dictionary<string, List<string>>>(configFolderPath, "mapWithKeys.json");
        }
        catch (Exception ex)
        {
            logger.Error("Error loading ReadableKeys config data. Disabling mod.", ex);
            return Task.CompletedTask;
        }

        var itemsDb = templateTable.Items;

        // Fetch the English locale dictionary cleanly to match English item names for your map dictionary
        var enLocales = localeService.GetLocaleDb("en");

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
                    string itemNameLocal = enLocales.ContainsKey($"{itemId} Name") ? enLocales[$"{itemId} Name"] : "";

                    string mapName = FindMapForItem(itemNameLocal, mapWithKeys);
                    logger.LogWithColor($"[ReadableKeys] Processing item ID: {itemId} - Name: {itemNameLocal}. Found map: {mapName}", Spectre.Console.Color.Green);

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
                //     logger.LogWithColor($"[ReadableKeys] No configuration found for item ID: {itemId} - Name: {item.Name}. Skipping.", Spectre.Console.Color.Yellow);
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

        logger.LogWithColor($"[ReadableKeys] Load Successful. Processed {updatedCount} keys.", Spectre.Console.Color.Green);
        return Task.CompletedTask;
    }

    private void UpdateUsageCount(string itemId, TemplateItemProperties itemProps, bool changeNoOfUse, float multiplier, Dictionary<string, KeyUseConfig> itemsConfig)
    {
        if (!changeNoOfUse) return;

        if (itemsConfig.TryGetValue(itemId, out var itemConfig))
        {
            itemProps.MaximumNumberOfUsage = itemConfig.noofuse;
        }

        if (multiplier > 0)
        {
            itemProps.MaximumNumberOfUsage = (int)(itemProps.MaximumNumberOfUsage * multiplier);
        }
    }

    private string FindMapForItem(string itemName, Dictionary<string, List<string>> mapWithKeys)
    {
        if (string.IsNullOrEmpty(itemName) || mapWithKeys == null) return null;

        foreach (var entry in mapWithKeys)
        {
            string mapName = entry.Key;
            List<string> items = entry.Value;

            if (items != null && items.Contains(itemName))
            {
                return mapName;
            }
        }
        return null;
    }
}