using System.Reflection;
using Gunwright.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;

namespace Gunwright;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class Main(
    ISptLogger<Main> logger,
    ModHelper modHelper,
    GunwrightRefresher refresher
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
            logger.Error("[Gunwright]: db/config.json not found or missing Base, aborting preset population");
            return Task.CompletedTask;
        }

        refresher.SetTraderId(traderBase.Id);
        refresher.InitialLoad();

        return Task.CompletedTask;
    }
}
