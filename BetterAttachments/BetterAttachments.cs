using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Nodes;
using SPTarkov.Server.Core.Models.Logging;

namespace BetterAttachments;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.deadwolf.betterattachments";
    public override string Name { get; init; } = "BetterAttachments";
    public override string Author { get; init; } = "SPTarkov";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class BetterAttachments(ISptLogger<BetterAttachments> logger, DatabaseServer databaseServcer) : IOnLoad
{
    Dictionary<MongoId, TemplateItem> itemsDb = null!;

    Boolean betterForegrips = true;
    Boolean betterSights = true;
    Boolean betterMuzzles = true;
    Boolean betterMuzzleRecoil = true;
    Boolean betterSuppressors = true;
    Boolean betterSuppressorRecoil = true;
    Boolean betterSuppressorHeating = true;
    Boolean betterTacticals = true;
    Boolean betterStocks = true;
    Boolean betterPistolGrips = true;
    Boolean betterHandGuards = true;

    HashSet<MongoId> sightTypes = [
        BaseClasses.IRON_SIGHT,
        BaseClasses.COMPACT_COLLIMATOR,
        BaseClasses.COLLIMATOR,
        BaseClasses.OPTIC_SCOPE,
        BaseClasses.SPECIAL_SCOPE,
        BaseClasses.ASSAULT_SCOPE,
    ];
    HashSet<MongoId> muzzleTypes = [
        BaseClasses.COMPENSATOR,
        BaseClasses.FLASH_HIDER,
        BaseClasses.MUZZLE_COMBO
    ];

    HashSet<MongoId> tacticalTypes = [
        BaseClasses.FLASHLIGHT,
        BaseClasses.LIGHT_LASER,
        BaseClasses.TACTICAL_COMBO
    ];

    public Task OnLoad()
    {
        logger.LogWithColor("[BetterAttachments] Initalizing BalancedMeds mod...", LogTextColor.Green);
        itemsDb = databaseServcer.GetTables().Templates.Items;

        var config = LoadJson("user/mods/BetterAttachments/config/config.json");
        betterForegrips = config["betterForegrips"]!.GetValue<bool>();
        betterSights = config["betterSights"]!.GetValue<bool>();
        betterMuzzles = config["betterMuzzles"]!.GetValue<bool>();
        betterMuzzleRecoil = config["betterMuzzleRecoil"]!.GetValue<bool>();
        betterSuppressors = config["betterSuppressors"]!.GetValue<bool>();
        betterSuppressorRecoil = config["betterSuppressorRecoil"]!.GetValue<bool>();
        betterSuppressorHeating = config["betterSuppressorHeating"]!.GetValue<bool>();
        betterTacticals = config["betterTacticals"]!.GetValue<bool>();
        betterStocks = config["betterStocks"]!.GetValue<bool>();
        betterPistolGrips = config["betterPistolGrips"]!.GetValue<bool>();
        betterHandGuards = config["betterHandGuards"]!.GetValue<bool>();

        var attachmentConfig = config["value"]!.AsObject();
        var count = 0;
        foreach (var entry in attachmentConfig)
        {
            string attachmentId = entry.Key;
            JsonObject attachmentData = entry.Value!.AsObject();
            string name = attachmentData["name"]!.ToString();

            //logger.Info($"[BetterAttachments] Processing Attachment: {attachmentId} - {name}");
            if (itemsDb.TryGetValue(attachmentId, out TemplateItem item))
            {
                TemplateItemProperties itemProps = item.Properties;
                MongoId parentId = item.Parent;
                count++;

                // Foregrips
                if (betterForegrips && parentId.Equals(BaseClasses.FOREGRIP))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                }

                // Sights
                if (betterSights && sightTypes.Contains(parentId))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                }

                // Muzzles
                if (betterMuzzles && muzzleTypes.Contains(parentId))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                    UpdateRecoil(attachmentData, itemProps, betterMuzzleRecoil);
                }

                if (betterMuzzles && parentId.Equals(BaseClasses.FLASH_HIDER))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                    UpdateRecoil(attachmentData, itemProps, betterMuzzleRecoil);
                }

                // Silencers
                if (parentId.Equals(BaseClasses.SILENCER))
                {
                    if (betterSuppressors)
                    {
                        UpdateErgonomics(attachmentData, itemProps);
                    }
                    if (betterSuppressorRecoil)
                    {
                        UpdateRecoil(attachmentData, itemProps, betterSuppressorRecoil);
                    }
                    if (betterSuppressorHeating)
                    {
                        if (attachmentData.ContainsKey("heatFactor_updated"))
                        {
                            itemProps.HeatFactor = attachmentData["heatFactor_updated"]!.GetValue<double>();
                        }
                        if (attachmentData.ContainsKey("coolFactor_updated"))
                        {
                            itemProps.CoolFactor = attachmentData["coolFactor_updated"]!.GetValue<double>();
                        }
                        if (attachmentData.ContainsKey("durabilityBurn_updated"))
                        {
                            itemProps.DurabilityBurnModificator = attachmentData["durabilityBurn_updated"]!.GetValue<double>();
                        }
                    }
                }

                // Tacticals (Lights/Lasers)
                if (betterTacticals && tacticalTypes.Contains(parentId))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                }

                // Stocks
                if (betterStocks && parentId.Equals(BaseClasses.STOCK))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                    UpdateRecoil(attachmentData, itemProps, betterMuzzleRecoil);
                }

                // Hand Guards
                if (betterHandGuards && parentId.Equals(BaseClasses.HANDGUARD))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                    UpdateRecoil(attachmentData, itemProps, betterMuzzleRecoil);
                }

                // Pistol Grips
                if (betterPistolGrips && parentId.Equals(BaseClasses.PISTOL_GRIP))
                {
                    UpdateErgonomics(attachmentData, itemProps);
                    UpdateRecoil(attachmentData, itemProps, betterMuzzleRecoil);
                }
            }
        }
        logger.Info($"[BetterAttachments] Updated {count} attachments...");
        logger.LogWithColor("[BetterAttachments] Loading BalancedMeds Mod Completed.", LogTextColor.Green);
        return Task.CompletedTask;
    }

    private void UpdateRecoil(JsonObject attachmentData, TemplateItemProperties itemProps, bool betterMuzzleRecoil)
    {
        if (betterMuzzleRecoil && attachmentData.ContainsKey("recoil_updated"))
        {
            itemProps.Recoil = attachmentData["recoil_updated"]!.GetValue<double>();
        }
    }

    private static void UpdateErgonomics(JsonObject attachmentData, TemplateItemProperties itemProps)
    {
        if (attachmentData.ContainsKey("ergonomics_updated"))
        {
            itemProps.Ergonomics = attachmentData["ergonomics_updated"]!.GetValue<double>();
        }
    }

    private JsonObject LoadJson(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"[BetterAttachments] Config file missing: {fullPath}");
        }
        string json = File.ReadAllText(fullPath);
        return JsonNode.Parse(json)!.AsObject();
    }
}