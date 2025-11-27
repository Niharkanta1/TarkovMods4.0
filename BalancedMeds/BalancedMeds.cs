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

namespace BalancedMeds;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.deadwolf.spt.balancedmeds";
    public override string Name { get; init; } = "BalancedMeds";
    public override string Author { get; init; } = "DeadW0Lf";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class BalancedMeds(ISptLogger<BalancedMeds> logger, DatabaseServer databaseServcer) : IOnLoad
{
    Dictionary<MongoId, TemplateItem> itemsDb = null!;
    Dictionary<string, IEnumerable<Buff>> globalBuffs = null!;

    public Task OnLoad()
    {
        logger.LogWithColor("[BalancedMeds] Loading BalancedMeds Mod...", LogTextColor.Green);
        itemsDb = databaseServcer.GetTables().Templates.Items;
        //logger.Info("[BalancedMeds] Items DB " + itemsDb.Count);

        // Load all configs
        var drugConfig = LoadJson("user/mods/BalancedMeds/config/drugs.json");
        var medicalConfig = LoadJson("user/mods/BalancedMeds/config/medicals.json");
        var medkitConfig = LoadJson("user/mods/BalancedMeds/config/medkits.json");
        var stimulatorConfig = LoadJson("user/mods/BalancedMeds/config/stimulators.json");

        UpdateDrugConfigData(drugConfig, itemsDb);
        UpdateMedicalConfigData(medicalConfig, itemsDb);
        UpdateMedkitConfigData(medkitConfig, itemsDb);

        globalBuffs = databaseServcer.GetTables().Globals.Configuration.Health.Effects.Stimulator.Buffs;
        UpdateStimulatorConfigData(stimulatorConfig, globalBuffs, itemsDb);
        logger.LogWithColor("[BalancedMeds] Loading BalancedMeds Mod Completed.", LogTextColor.Green);
        return Task.CompletedTask;
    }

    private void UpdateDrugConfigData(JsonObject drugConfig, Dictionary<MongoId, TemplateItem> itemsDb)
    {
        foreach (var entry in drugConfig)
        {
            string medicationId = entry.Key;
            JsonObject drugData = entry.Value!.AsObject();
            string name = drugData["name"]!.ToString();
            //logger.Info($"[BalancedMeds] Processing Drugs: {medicationId} - {name}");
            if (itemsDb.TryGetValue(medicationId, out TemplateItem item))
            {
                TemplateItemProperties itemProps = item.Properties;
                itemProps.MedUseTime = (double)drugData["medUseTime"];
                itemProps.MaxHpResource = (int)drugData["MaxHpResource"];
                itemProps.HpResourceRate = (double)drugData["hpResourceRate"];

                JsonObject damageEffectData = drugData["effects_damage"]!.AsObject();
                ApplyDamageEffects(damageEffectData, "Pain", DamageEffectType.Pain, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "Intoxication", DamageEffectType.Intoxication, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "Contusion", DamageEffectType.Contusion, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "RadExposure", DamageEffectType.RadExposure, itemProps.EffectsDamage);

                JsonObject healthEffectData = drugData["effects_health"]!.AsObject();
                ApplyHealthEffects(healthEffectData, "Energy", HealthFactor.Energy, itemProps.EffectsHealth);
                ApplyHealthEffects(healthEffectData, "Hydration", HealthFactor.Hydration, itemProps.EffectsHealth);
            }
        }
    }

    private void UpdateMedicalConfigData(JsonObject medicalConfig, Dictionary<MongoId, TemplateItem> itemsDb)
    {
        foreach (var entry in medicalConfig)
        {
            string medicationId = entry.Key;
            JsonObject medicalData = entry.Value!.AsObject();
            string name = medicalData["name"]!.ToString();
            //logger.Info($"[BalancedMeds] Processing Medicals: {medicationId} - {name}");
            if (itemsDb.TryGetValue(medicationId, out TemplateItem item))
            {
                TemplateItemProperties itemProps = item.Properties;
                itemProps.MedUseTime = (double)medicalData["medUseTime"];
                itemProps.MaxHpResource = (int)medicalData["MaxHpResource"];
                itemProps.HpResourceRate = (double)medicalData["hpResourceRate"];

                JsonObject damageEffectData = medicalData["effects_damage"]!.AsObject();
                ApplyDamageEffects(damageEffectData, "LightBleeding", DamageEffectType.LightBleeding, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "DestroyedPart", DamageEffectType.DestroyedPart, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "Fracture", DamageEffectType.Fracture, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "HeavyBleeding", DamageEffectType.HeavyBleeding, itemProps.EffectsDamage);
            }
        }
    }

    private void UpdateMedkitConfigData(JsonObject medkitConfig, Dictionary<MongoId, TemplateItem> itemsDb)
    {
        foreach (var entry in medkitConfig)
        {
            string medicationId = entry.Key;
            JsonObject medkitData = entry.Value!.AsObject();
            string name = medkitData["name"]!.ToString();
            //logger.Info($"[BalancedMeds] Processing Medkits: {medicationId} - {name}");
            if (itemsDb.TryGetValue(medicationId, out TemplateItem item))
            {
                TemplateItemProperties itemProps = item.Properties;
                itemProps.MedUseTime = (double)medkitData["medUseTime"];
                itemProps.MaxHpResource = (int)medkitData["MaxHpResource"];
                itemProps.HpResourceRate = (double)medkitData["hpResourceRate"];

                JsonObject damageEffectData = medkitData["effects_damage"]!.AsObject();
                ApplyDamageEffects(damageEffectData, "LightBleeding", DamageEffectType.LightBleeding, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "Contusion", DamageEffectType.Contusion, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "RadExposure", DamageEffectType.RadExposure, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "Fracture", DamageEffectType.Fracture, itemProps.EffectsDamage);
                ApplyDamageEffects(damageEffectData, "HeavyBleeding", DamageEffectType.HeavyBleeding, itemProps.EffectsDamage);
            }
        }
    }

    private void UpdateStimulatorConfigData(JsonObject stimulatorConfig,
        Dictionary<string, IEnumerable<Buff>> globalBuffs, Dictionary<MongoId, TemplateItem> itemsDb)
    {
        foreach (var entry in stimulatorConfig)
        {
            string stimulatorId = entry.Key;
            JsonObject stimData = entry.Value!.AsObject();
            string name = stimData["name"]!.ToString();
            //logger.Info($"[BalancedMeds] Processing Stimulators: {stimulatorId} - {name}");
            string buffName = stimData["buffName"]!.ToString();
            if (itemsDb.TryGetValue(stimulatorId, out TemplateItem item))
            {
                TemplateItemProperties itemProps = item.Properties;
                itemProps.MedUseTime = (double)stimData["medUseTime"];
                itemProps.MaxHpResource = (int)stimData["MaxHpResource"];
                itemProps.HpResourceRate = (double)stimData["hpResourceRate"];
                JsonArray buffEffectArray = stimData["effects_buffs"]!.AsArray();
                //logger.Info($"[BalancedMeds] Processing Stimulator Buffs: {buffName} - Effects Count: {(buffEffectArray != null ? buffEffectArray.Count : 0)}");
                if (itemProps.StimulatorBuffs != null && buffEffectArray != null)
                {
                    var newBuffs = BuildBuffList(buffEffectArray);
                    this.globalBuffs[buffName] = newBuffs;
                }
            }
        }
    }

    private IEnumerable<Buff> BuildBuffList(JsonArray buffArray)
    {
        List<Buff> list = [];

        foreach (JsonNode? node in buffArray)
        {
            JsonObject obj = node!.AsObject();

            Buff buff = new()
            {
                BuffType = (string)obj["BuffType"]!,
                Chance = (float?)obj["Chance"] ?? 1,
                Delay = (int?)obj["Delay"] ?? 0,
                Duration = (int?)obj["Duration"] ?? 0,
                Value = (float?)obj["Value"] ?? 0,
                AbsoluteValue = (bool?)obj["AbsoluteValue"] ?? false,
                SkillName = (string?)obj["SkillName"] ?? ""
            };

            list.Add(buff);
        }

        return list;
    }

    private void ApplyDamageEffects(JsonObject effectData, string effectName,
        DamageEffectType effectType, Dictionary<DamageEffectType,
        EffectsDamageProperties> effectDamage)
    {
        if (effectData == null || effectData[effectName] == null) return;
        JsonObject data = effectData[effectName]!.AsObject();
        if (data == null) return;
        //logger.Info($"[BalancedMeds] Applying Damage Effect: {(effectData != null ? effectData.ToJsonString() : "")} type: {effectName}");
        if (data != null && effectDamage.TryGetValue(effectType, out EffectsDamageProperties effectProperties))
        {
            effectProperties.Delay = (double)data["delay"];
            effectProperties.Duration = (double)data["duration"];
            effectProperties.FadeOut = (double)data["fadeOut"];
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
        //logger.Info($"[BalancedMeds] Applying Health Effect: {(effectData != null ? effectData.ToJsonString() : "")}  type: {effectName}");
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
            throw new FileNotFoundException($"[BalancedMeds] Config file missing: {fullPath}");
        }
        string json = File.ReadAllText(fullPath);
        return JsonNode.Parse(json)!.AsObject();
    }
}
