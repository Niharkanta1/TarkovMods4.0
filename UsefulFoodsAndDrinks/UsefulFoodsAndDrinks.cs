using System.Text.Json.Nodes;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace LogToConsole;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.usefulfoodsanddrinks";
    public string Name { get; init; } = "UsefulFoodsAndDrinks";
    public string Author { get; init; } = "SPTarkov";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public bool? IsBundleMod { get; init; } = false;
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class UsefulFoodsAndDrinks(ISptLogger<UsefulFoodsAndDrinks> logger, TemplateTable templateTable, GlobalTable globalTable) : IOnLoad
{

    Dictionary<MongoId, TemplateItem> itemsDb = null!;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("[UsefulFoodsAndDrinks] Initalizing...");
        int count = 0;
        JsonObject config = LoadJson("user/mods/UsefulFoodsAndDrinks/config/config.json");

        itemsDb = templateTable.Items;
        foreach (var entry in config)
        {
            MongoId itemId = new(entry.Key);

            if (!itemsDb.ContainsKey(itemId))
            {
                logger.Warning($"[UsefulFoodsAndDrinks] Item with ID {itemId} not found in database, skipping...");
                continue;
            }
            count++;
            TemplateItem item = itemsDb[itemId];
            TemplateItemProperties itemProps = item.Properties;
            JsonObject itemConfig = entry.Value.AsObject();
            //logger.Info($"[UsefulFoodsAndDrinks] Processing Food/Drinks: {itemId} - {itemConfig["name"]}");
            itemProps.MaxResource = itemConfig["MaxResource"]!.GetValue<int>();

            if (itemConfig.TryGetPropertyValue("effects_damage", out JsonNode? effectsDamageNode))
            {
                //logger.Info($"[UsefulFoodsAndDrinks] {effectsDamageNode.ToJsonString()}");
                if (effectsDamageNode is JsonObject effectsDamage)
                {
                    ApplyDamageEffects(effectsDamage, "Pain", DamageEffectType.Pain, itemProps.EffectsDamage);
                }
                else
                {
                    logger.Warning($"[UsefulFoodsAndDrinks] effects_damage must be an object but was: {effectsDamageNode}");
                }
            }

            if (itemConfig.TryGetPropertyValue("effects_health", out JsonNode? effectsHealthNode))
            {
                //logger.Info($"[UsefulFoodsAndDrinks] {effectsHealthNode.ToJsonString()}");

                if (effectsHealthNode is JsonObject effectsHealth)
                {
                    ApplyHealthEffects(effectsHealth, "Energy", HealthFactor.Energy, itemProps.EffectsHealth);
                    ApplyHealthEffects(effectsHealth, "Hydration", HealthFactor.Hydration, itemProps.EffectsHealth);
                }
                else
                {
                    logger.Warning($"[UsefulFoodsAndDrinks] effects_health must be an object but was: {effectsHealthNode}");
                }
            }
        }

        logger.Info($"[UsefulFoodsAndDrinks] Updated {count} Foods/Drinks...");
        logger.LogWithColor("[UsefulFoodsAndDrinks] Loading UsefulFoodsAndDrinks Mod Is Completed.", Spectre.Console.Color.Green);
        return Task.CompletedTask;
    }

    private void ApplyDamageEffects(JsonObject effectData, string effectName,
        DamageEffectType effectType, Dictionary<DamageEffectType,
        EffectsDamageProperties> effectDamage)
    {
        if (effectData == null || effectData[effectName] == null) return;
        JsonObject data = effectData[effectName]!.AsObject();
        if (data == null) return;
        //logger.Info($"[UsefulFoodsAndDrinks] Applying Damage Effect: {(effectData != null ? effectData.ToJsonString() : "")} type: {effectName}");
        if (data != null && effectDamage.TryGetValue(effectType, out EffectsDamageProperties effectProperties))
        {
            effectProperties.Delay = (double)data["delay"];
            effectProperties.Duration = (double)data["duration"];
            if (effectType == DamageEffectType.DestroyedPart)
            {
                effectProperties.HealthPenaltyMin = (double)data["healthPenaltyMin"];
                effectProperties.HealthPenaltyMax = (double)data["healthPenaltyMax"];
            }
        }
    }

    private void ApplyHealthEffects(JsonObject effectData, string effectName,
        HealthFactor healthFactor, Dictionary<HealthFactor,
        EffectsHealthProperties> effectHealth)
    {
        if (effectData == null || effectData[effectName] == null) return;
        JsonObject data = effectData[effectName]!.AsObject();
        if (data == null) return;
        //logger.Info($"[UsefulFoodsAndDrinks] Applying Health Effect: {(effectData != null ? effectData.ToJsonString() : "")}  type: {effectName}");
        if (data != null && effectHealth.TryGetValue(healthFactor, out EffectsHealthProperties effectProperties))
        {
            effectProperties.Value = (double)data["value"];
        }
    }

    private JsonObject LoadJson(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"[UsefulFoodsAndDrinks] Config file missing: {fullPath}");
        }
        string json = File.ReadAllText(fullPath);
        return JsonNode.Parse(json)!.AsObject();
    }
}