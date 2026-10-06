using System.Reflection;
using Gunwright.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using Path = System.IO.Path;

namespace Gunwright;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 1)]
public class GunwrightRegistration(
    ISptLogger<GunwrightRegistration> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TraderConfig traderConfig,
    TimeUtil timeUtil,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable
)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var config = modHelper.GetJsonDataFromFile<PresetConfig>(pathToMod, "db/config.json");
        var traderBase = config?.Base;
        if (traderBase is null)
        {
            logger.Error("[Gunwright]: db/config.json not found or missing Base, aborting trader registration");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(traderBase.Avatar))
        {
            logger.Error("[Gunwright]: Trader avatar is missing, aborting trader registration");
            return Task.CompletedTask;
        }

        var traderImagePath = Path.Combine(pathToMod, "db", "gunwright.png");
        if (!File.Exists(traderImagePath))
        {
            logger.Error($"[Gunwright]: Trader image not found: {traderImagePath}");
            return Task.CompletedTask;
        }

        var traderAvatar = Path.ChangeExtension(traderBase.Avatar, null);
        imageRouter.AddRoute(traderAvatar, traderImagePath);

        if (traderConfig.UpdateTime.All(x => x.TraderId != traderBase.Id))
        {
            traderConfig.UpdateTime.Add(new UpdateTime
            {
                TraderId = traderBase.Id,
                Seconds = new MinMax<int>(
                    timeUtil.GetHoursAsSeconds(1),
                    timeUtil.GetHoursAsSeconds(2))
            });
        }

        if (!AddTraderWithEmptyAssortToDb(traderBase))
        {
            return Task.CompletedTask;
        }

        AddTraderToLocales(
            traderBase,
            "Gunwright",
            "Sells weapon presets built and saved in your stash.");

        logger.Success(
            $"[Gunwright]: Registered trader {traderBase.Id} ahead of profile validation");

        return Task.CompletedTask;
    }

    private bool AddTraderWithEmptyAssortToDb(TraderBase traderDetails)
    {
        var traderData = new Trader
        {
            Assort = new TraderAssort
            {
                Items = [],
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            },
            Base = cloner.Clone(traderDetails)!,
            QuestAssort = new()
            {
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },
            Dialogue = []
        };

        if (!tradersTable.TryAdd(traderDetails.Id, traderData))
        {
            logger.Error(
                $"[Gunwright]: Failed to add trader {traderDetails.Id}, id already exists; aborting");

            return false;
        }

        return true;
    }

    private void AddTraderToLocales(
        TraderBase baseJson,
        string firstName,
        string description)
    {
        var newTraderId = baseJson.Id;
        var fullName = baseJson.Name;
        var nickName = baseJson.Nickname;
        var location = baseJson.Location;

        foreach (var (_, localeKvP) in localeTable.Global)
        {
            localeKvP.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return localeData;
                }

                localeData[$"{newTraderId} FullName"] = fullName;
                localeData[$"{newTraderId} FirstName"] = firstName;
                localeData[$"{newTraderId} Nickname"] = nickName ?? fullName;
                localeData[$"{newTraderId} Location"] = location ?? string.Empty;
                localeData[$"{newTraderId} Description"] = description;

                return localeData;
            });
        }
    }
}
