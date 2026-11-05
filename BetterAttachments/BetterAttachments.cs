using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Nodes;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Common.Models.Logging;

namespace BetterAttachments;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.betterattachments";
    public string Name { get; init; } = "BetterAttachments";
    public string Author { get; init; } = "DeadW0Lf";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } =
        new(typeof(BetterAttachments).Assembly.GetName().Version!.ToString(3));
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public bool? IsBundleMod { get; init; } = false;
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class BetterAttachments(ISptLogger<BetterAttachments> logger, TemplateTable templateTable, GlobalTable globalTable) : IOnLoad
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

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.LogWithColor("[BetterAttachments] Initalizing BalancedMeds mod...", Spectre.Console.Color.Green);
        itemsDb = templateTable.Items;

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
        double missingAttachmentImprovementPercent =
            config["missingAttachmentImprovementPercent"]!.GetValue<double>();
        if (missingAttachmentImprovementPercent is < 0 or > 100)
        {
            throw new InvalidDataException(
                "[BetterAttachments] missingAttachmentImprovementPercent must be between 0 and 100.");
        }

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
                    UpdateRecoil(attachmentData, itemProps, betterForegrips);
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

        var fallbackCount = 0;
        if (missingAttachmentImprovementPercent > 0)
        {
            foreach (var (itemId, item) in itemsDb)
            {
                if (attachmentConfig.ContainsKey(itemId.ToString()) ||
                    !TryGetMissingAttachmentAdjustments(item.Parent, out bool improveErgonomics, out bool improveRecoil))
                {
                    continue;
                }

                if (improveErgonomics)
                {
                    item.Properties.Ergonomics = ApplyPercentageImprovement(
                        item.Properties.Ergonomics,
                        missingAttachmentImprovementPercent,
                        isRecoil: false);
                }

                if (improveRecoil)
                {
                    item.Properties.Recoil = ApplyPercentageImprovement(
                        item.Properties.Recoil,
                        missingAttachmentImprovementPercent,
                        isRecoil: true);
                }

                fallbackCount++;
            }
        }

        logger.Info($"[BetterAttachments] Updated {count} attachments...");
        logger.Info($"[BetterAttachments] Processed {fallbackCount} eligible unlisted attachments.");
        logger.LogWithColor("[BetterAttachments] Loading BalancedMeds Mod Completed.", Spectre.Console.Color.Green);
        return Task.CompletedTask;
    }

    private bool TryGetMissingAttachmentAdjustments(
        MongoId parentId,
        out bool improveErgonomics,
        out bool improveRecoil)
    {
        bool isMuzzle = muzzleTypes.Contains(parentId) || parentId.Equals(BaseClasses.FLASH_HIDER);
        bool isStock = parentId.Equals(BaseClasses.STOCK);
        bool isHandGuard = parentId.Equals(BaseClasses.HANDGUARD);
        bool isPistolGrip = parentId.Equals(BaseClasses.PISTOL_GRIP);
        bool isSilencer = parentId.Equals(BaseClasses.SILENCER);
        bool isForegrip = parentId.Equals(BaseClasses.FOREGRIP);

        improveErgonomics =
            (isForegrip && betterForegrips) ||
            (sightTypes.Contains(parentId) && betterSights) ||
            (isMuzzle && betterMuzzles) ||
            (isSilencer && betterSuppressors) ||
            (tacticalTypes.Contains(parentId) && betterTacticals) ||
            (isStock && betterStocks) ||
            (isHandGuard && betterHandGuards) ||
            (isPistolGrip && betterPistolGrips);

        improveRecoil =
            (isForegrip && betterForegrips) ||
            (isMuzzle && betterMuzzles && betterMuzzleRecoil) ||
            (isSilencer && betterSuppressorRecoil) ||
            ((isStock && betterStocks) ||
             (isHandGuard && betterHandGuards) ||
             (isPistolGrip && betterPistolGrips)) && betterMuzzleRecoil;

        return improveErgonomics || improveRecoil;
    }

    private static double? ApplyPercentageImprovement(double? value, double percentage, bool isRecoil)
    {
        if (!value.HasValue)
        {
            return null;
        }

        double adjustment = Math.Abs(value.Value) * percentage / 100;
        return isRecoil ? value.Value - adjustment : value.Value + adjustment;
    }

    private void UpdateRecoil(JsonObject attachmentData, TemplateItemProperties itemProps, bool recoilEnabled)
    {
        if (recoilEnabled && attachmentData.ContainsKey("recoil_updated"))
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