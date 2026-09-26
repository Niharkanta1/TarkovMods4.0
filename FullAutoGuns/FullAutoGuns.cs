using System.Text.Json.Nodes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using System.Text.Json.Serialization;

namespace LogToConsole;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.fullautoguns";
    public override string Name { get; init; } = "FullAutoGuns";
    public override string Author { get; init; } = "SPTarkov";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.13");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class FullAutoGuns(
    ISptLogger<FullAutoGuns> logger, DatabaseServer databaseServcer)
    : IOnLoad
{
    Dictionary<MongoId, TemplateItem> itemsDb = null!;
    public Task OnLoad()
    {
        itemsDb = databaseServcer.GetTables().Templates.Items;
        var config = LoadJson("user/mods/FullAutoGuns/config/config.json");
        logger.Info($"[FullAutoGuns] Loaded configuration. Config: {config}");
        foreach (TemplateItem item in itemsDb.Values)
        {
            MongoId parentId = item.Parent;
            if (IsModifiableGun(parentId))
            {
                TemplateItemProperties props = item.Properties;
                if (config.TryGetPropertyValue(item.Id.ToString(), out JsonNode? itemConfig)
                                    && itemConfig != null
                                    && itemConfig["enabled"]?.GetValue<bool>() == true)
                {
                    props.WeapFireType.Add("fullauto");
                    props.BFirerate = itemConfig["bFirerate"]?.GetValue<int>() ?? props.BFirerate;
                }
            }

        }
        logger.Success("[FullAutoGuns] Loading FullAutoGuns Mod Completed");
        return Task.CompletedTask;
    }

    private bool IsModifiableGun(MongoId parent)
    {
        return parent == BaseClasses.ASSAULT_CARBINE
            || parent == BaseClasses.ASSAULT_RIFLE
            || parent == BaseClasses.SMG
            || parent == BaseClasses.PISTOL
            || parent == BaseClasses.MARKSMAN_RIFLE;
    }

    private bool IsSingleFireOnly(HashSet<string> fireTypes)
    {
        return !fireTypes.Contains("fullauto");
    }

    private JsonObject LoadJson(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"[FullAutoGuns] Config file missing: {fullPath}");
        }
        string json = File.ReadAllText(fullPath);
        return JsonNode.Parse(json)!.AsObject();
    }
}