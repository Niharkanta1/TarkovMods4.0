using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Nodes;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace LogToConsole;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.fullautoguns";
    public string Name { get; init; } = "FullAutoGuns";
    public string Author { get; init; } = "DeadW0Lf";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.5");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public bool? IsBundleMod { get; init; } = false;
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class FullAutoGuns(
    ISptLogger<FullAutoGuns> logger, TemplateTable templateTable)
    : IOnLoad
{
    Dictionary<MongoId, TemplateItem> itemsDb = null!;
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        itemsDb = templateTable.Items;
        var config = LoadJson("user/mods/FullAutoGuns/config/config.json");
        int count = 0;
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
                    count++;
                }
            }

        }
        logger.Success($"[FullAutoGuns] Loading FullAutoGuns Mod Completed. Modified {count} weapons.");
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