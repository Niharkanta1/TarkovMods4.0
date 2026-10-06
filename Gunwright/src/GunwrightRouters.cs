using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;

namespace Gunwright;

[Injectable(TypePriority = OnLoadOrder.Routers - 1)]
public class GunwrightAssortRouter(
    JsonUtil jsonUtil,
    GunwrightRefresher refresher
) : DynamicRouter(
    jsonUtil,
    [
        new RouteAction<EmptyRequestData>(
            "/client/trading/api/getTraderAssort/",
            async (url, info, sessionID, output, cancellationToken) =>
            {
                var path = url.Split('?')[0];
                var traderId = path.Substring(path.LastIndexOf('/') + 1);

                if (traderId == refresher.GetTraderIdString())
                {
                    lock (refresher)
                    {
                        if (refresher.CanRefresh())
                        {
                            refresher.Refresh(traderId);
                        }
                    }
                }

                return string.Empty;
            }
        ),
    ]
)
{ }

[Injectable(TypePriority = OnLoadOrder.Routers - 1)]
public class GunwrightItemMovingRouter(
    JsonUtil jsonUtil,
    GunwrightRefresher refresher,
    ISptLogger<GunwrightItemMovingRouter> logger
) : StaticRouter(
    jsonUtil,
    [
        new RouteAction<ItemEventRouterRequest>(
            "/client/game/profile/items/moving",
            (url, info, sessionID, output, cancellationToken) =>
            {
                NotifyIfPurchaseFromTrader(info, refresher, logger);
                return ValueTask.FromResult(string.Empty);
            }
        ),
    ]
)
{
    private static void NotifyIfPurchaseFromTrader(
        ItemEventRouterRequest info,
        GunwrightRefresher refresher,
        ISptLogger<GunwrightItemMovingRouter> logger)
    {
        try
        {
            if (info.Data is not { Count: > 0 })
            {
                return;
            }

            var traderId = refresher.GetTraderIdString();
            if (string.IsNullOrEmpty(traderId))
            {
                return;
            }

            foreach (var evt in info.Data)
            {
                if (evt.ValueKind == JsonValueKind.Object
                    && evt.TryGetProperty("Action", out var actionEl)
                    && actionEl.GetString() == ItemEventActions.TRADING_CONFIRM
                    && evt.TryGetProperty("type", out var typeEl)
                    && typeEl.GetString() == "buy_from_trader"
                    && evt.TryGetProperty("tid", out var tidEl)
                    && tidEl.GetString() == traderId)
                {
                    logger.Debug(
                        $"[Gunwright]: Purchase detected from {traderId}, suppressing post-buy auto-refresh");
                    refresher.NotifyTrade();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[Gunwright]: Failed to inspect item moving event: {ex}");
        }
    }
}
