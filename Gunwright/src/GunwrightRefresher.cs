using System.Reflection;
using Gunwright.Config;
using Gunwright.Pricing;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace Gunwright;

[Injectable(InjectionType.Singleton, TypePriority = OnUpdateOrder.InsuranceCallbacks)]
public class GunwrightRefresher(
    ISptLogger<GunwrightRefresher> logger,
    ProfileHelper profileHelper,
    TradersTable tradersTable,
    ItemHelper itemHelper,
    ICloner cloner,
    ModHelper modHelper)
    : IOnUpdate
{
    private const string TraderRootParentId = "hideout";
    private const int RefreshIntervalSeconds = 30;
    private const int PostPurchaseSuppressMs = 1500;

    private readonly object _assortGate = new();

    private MongoId? _traderId;
    private long _lastPurchaseUnixMs;
    private readonly Dictionary<MongoId, MongoId> _buildRootIds = new();
    private readonly Dictionary<MongoId, string> _buildSignatures = new();

    public void SetTraderId(MongoId traderId)
    {
        _traderId = traderId;
    }

    public string GetTraderIdString()
    {
        return _traderId?.ToString() ?? string.Empty;
    }

    public bool CanRefresh()
    {
        return CurrentUnixMs() - Interlocked.Read(ref _lastPurchaseUnixMs) >= PostPurchaseSuppressMs;
    }

    public void Refresh(MongoId traderId)
    {
        lock (_assortGate)
        {
            if (!tradersTable.TryGetValue(traderId, out _))
            {
                return;
            }

            RefreshCore(traderId);
        }
    }

    public void NotifyTrade()
    {
        Interlocked.Exchange(ref _lastPurchaseUnixMs, CurrentUnixMs());
    }

    private static long CurrentUnixMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
    {
        if (_traderId is null || secondsSinceLastRun < RefreshIntervalSeconds)
        {
            return Task.FromResult(false);
        }

        lock (_assortGate)
        {
            var traderId = _traderId.Value;

            if (CurrentUnixMs() - Interlocked.Read(ref _lastPurchaseUnixMs) < PostPurchaseSuppressMs)
            {
                return Task.FromResult(false);
            }

            RefreshCore(traderId);
            return Task.FromResult(true);
        }
    }

    private int RefreshCore(MongoId traderId)
    {
        if (!tradersTable.TryGetValue(traderId, out var traderData))
        {
            logger.Error($"[Gunwright]: Trader {traderId} not found, cannot refresh presets");
            return 0;
        }

        traderData.Assort.Items.Clear();
        traderData.Assort.BarterScheme.Clear();
        traderData.Assort.LoyalLevelItems.Clear();
        _buildRootIds.Clear();
        _buildSignatures.Clear();

        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var config = modHelper.GetJsonDataFromFile<PresetConfig>(pathToMod, "db/config.json");
        var priceCalculator = new PresetPriceCalculator(itemHelper, config?.PriceConfig);

        var buildCount = PopulateWeaponBuilds(traderData, priceCalculator);

        var gearCount = PopulateStaticPresets(traderData, config?.GearPresets, priceCalculator, "gear presets");
        var armorCount = PopulateStaticPresets(traderData, config?.ArmorPresets, priceCalculator, "armor presets");

        var gunsmithCount = PopulateGunsmithPresets(traderData, config?.WeaponPresets, priceCalculator);

        return buildCount + gearCount + armorCount + gunsmithCount;
    }

    private int PopulateWeaponBuilds(Trader traderData, PresetPriceCalculator priceCalculator)
    {
        var syncedCount = 0;
        var currentBuildIds = new HashSet<MongoId>();

        foreach (var (_, profile) in profileHelper.GetProfiles())
        {
            var weaponBuilds = profile?.UserBuildData?.WeaponBuilds;
            if (weaponBuilds is null or { Count: 0 })
            {
                continue;
            }

            foreach (var build in weaponBuilds)
            {
                if (build?.Items is null or { Count: 0 })
                {
                    logger.Warning($"[Gunwright]: Skipping build '{build?.Name}' ({build?.Id}) - no items");
                    continue;
                }

                currentBuildIds.Add(build.Id);

                var signature = ComputeBuildSignature(build.Items);

                if (_buildRootIds.TryGetValue(build.Id, out var existingRootId)
                    && _buildSignatures.TryGetValue(build.Id, out var existingSignature)
                    && existingSignature == signature)
                {
                    continue;
                }

                if (_buildRootIds.TryGetValue(build.Id, out var staleRootId))
                {
                    RemoveAssortHierarchy(traderData, staleRootId);
                    _buildRootIds.Remove(build.Id);
                    _buildSignatures.Remove(build.Id);
                    logger.Debug($"[Gunwright]: Replacing modified build '{build.Name}' ({build.Id})");
                }

                var buildName = build.Name ?? build.Id.ToString();
                var items = cloner.Clone(build.Items);

                if (!TryPreparePreset(items, build.Root, buildName, out var rootId))
                {
                    logger.Warning($"[Gunwright]: Failed to add build '{buildName}' ({build.Id}) to assort");
                    continue;
                }

                var price = priceCalculator.Compute(items!.Select(i => i.Template));
                if (price.Amount <= 0)
                {
                    logger.Warning($"[Gunwright]: Skipping build '{buildName}' ({build.Id}) - price is 0");
                    continue;
                }

                AddToAssort(traderData, items!, rootId, price.Amount);
                _buildRootIds[build.Id] = rootId;
                _buildSignatures[build.Id] = signature;

                logger.Debug(
                    $"[Gunwright]: Added build '{buildName}' ({build.Id}) (price: {price.Amount}₽ [{price.RarityName}])");
                syncedCount++;
            }
        }

        foreach (var kvp in _buildRootIds.ToList())
        {
            if (!currentBuildIds.Contains(kvp.Key))
            {
                RemoveAssortHierarchy(traderData, kvp.Value);
                _buildRootIds.Remove(kvp.Key);
                _buildSignatures.Remove(kvp.Key);
                logger.Debug($"[Gunwright]: Removed deleted build ({kvp.Key}) from trader");
            }
        }

        return syncedCount;
    }

    private int PopulateStaticPresets(Trader traderData, List<MatchedSet>? sets, PresetPriceCalculator priceCalculator, string label)
    {
        if (sets is null or { Count: 0 })
        {
            logger.Warning($"[Gunwright]: {label} not found or empty, skipping presets");
            return 0;
        }

        var soldRootTpls = GetSoldRootTemplates(traderData);
        var added = 0;

        foreach (var set in sets)
        {
            if (set is null || string.IsNullOrWhiteSpace(set.RootTpl))
            {
                logger.Warning($"[Gunwright]: Skipping {label} entry - null or missing RootTpl");
                continue;
            }

            var rootTemplate = itemHelper.GetItem(set.RootTpl).Value;
            if (rootTemplate is null)
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - tpl not found in item DB");
                continue;
            }

            if (!ValidateAttachments(set, rootTemplate))
            {
                continue;
            }

            if (!soldRootTpls.Add(new MongoId(set.RootTpl)))
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - already sold");
                continue;
            }

            var rootId = new MongoId();
            var items = new List<Item>
            {
                new()
                {
                    Id = rootId,
                    Template = set.RootTpl,
                    ParentId = TraderRootParentId,
                    SlotId = TraderRootParentId,
                    Upd = new Upd
                    {
                        StackObjectsCount = 999,
                        UnlimitedCount = true,
                        BuyRestrictionCurrent = 0
                    }
                }
            };

            foreach (var att in set.Attachments ?? [])
            {
                if (att is { Tpl: not null, Slot: not null })
                {
                    items.Add(new Item
                    {
                        Id = new MongoId(),
                        Template = att.Tpl,
                        ParentId = rootId,
                        SlotId = att.Slot,
                        Upd = new Upd()
                    });
                }
            }

            var price = priceCalculator.Compute(items.Select(i => i.Template));
            if (price.Amount <= 0)
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - price is 0");
                continue;
            }

            AddToAssort(traderData, items, rootId, price.Amount);
            logger.Debug($"[Gunwright]: Added {label} '{set.RootTpl}' (price: {price.Amount}₽ [{price.RarityName}], {items.Count} items)");
            added++;
        }

        return added;
    }

    private int PopulateGunsmithPresets(Trader traderData, Dictionary<string, List<Item>>? presets, PresetPriceCalculator priceCalculator)
    {
        if (presets is null or { Count: 0 })
        {
            logger.Warning($"[Gunwright]: gunsmith weapon presets not found or empty");
            return 0;
        }

        var added = 0;

        foreach (var (name, presetItems) in presets)
        {
            if (presetItems is not { Count: > 0 })
            {
                logger.Warning($"[Gunwright]: Skipping weapon preset '{name}' - no items");
                continue;
            }

            if (!TryPreparePreset(presetItems, knownRootId: null, name, out var rootId))
            {
                logger.Warning($"[Gunwright]: Skipping weapon preset '{name}' - invalid item hierarchy");
                continue;
            }

            var price = priceCalculator.Compute(presetItems.Select(i => i.Template));
            if (price.Amount <= 0)
            {
                logger.Warning($"[Gunwright]: Skipping weapon preset '{name}' - price is 0");
                continue;
            }

            AddToAssort(traderData, presetItems, rootId, price.Amount);
            logger.Debug($"[Gunwright]: Added weapon preset '{name}' (price: {price.Amount}₽ [{price.RarityName}], {presetItems.Count} items)");
            added++;
        }

        return added;
    }

    public void InitialLoad()
    {
        if (_traderId is null)
        {
            return;
        }

        var traderId = _traderId.Value;

        if (!tradersTable.TryGetValue(traderId, out _))
        {
            logger.Error($"[Gunwright]: Trader {traderId} not found, cannot load presets");
            return;
        }

        lock (_assortGate)
        {
            RefreshCore(traderId);
        }
    }

    private void RemoveAssortHierarchy(Trader traderData, MongoId rootId)
    {
        var toRemove = new HashSet<MongoId> { rootId };
        var queue = new Queue<MongoId>([rootId]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var item in traderData.Assort.Items)
            {
                if (item.ParentId is not null && item.ParentId.Equals(current) && toRemove.Add(item.Id))
                {
                    queue.Enqueue(item.Id);
                }
            }
        }

        traderData.Assort.Items.RemoveAll(i => toRemove.Contains(i.Id));
        traderData.Assort.BarterScheme.Remove(rootId);
        traderData.Assort.LoyalLevelItems.Remove(rootId);
    }

    #region Helpers

    private static HashSet<MongoId> GetSoldRootTemplates(Trader traderData)
    {
        return traderData.Assort.Items
            .Where(i => i.SlotId == TraderRootParentId)
            .Select(i => i.Template)
            .ToHashSet();
    }

    private void AddToAssort(Trader traderData, List<Item> items, MongoId rootId, int price)
    {
        traderData.Assort.Items.AddRange(items);
        traderData.Assort.BarterScheme[rootId] = [[new() { Count = price, Template = Money.ROUBLES }]];
        traderData.Assort.LoyalLevelItems[rootId] = 1;
    }

    private bool TryPreparePreset(List<Item>? items, MongoId? knownRootId, string presetName, out MongoId rootId)
    {
        rootId = default;

        if (items is null or { Count: 0 })
        {
            return false;
        }

        var rootItem = ResolveRootItem(items, knownRootId, presetName);
        if (rootItem is null)
        {
            logger.Warning($"[Gunwright]: Skipping '{presetName}' - could not determine root item");
            return false;
        }

        rootId = RemapRootId(items, rootItem);

        if (!ValidatePresetHierarchy(items, rootId, presetName))
        {
            return false;
        }

        PrepareItemForAssort(rootItem, TraderRootParentId);
        foreach (var part in items)
        {
            part.Upd ??= new Upd();
            part.Upd.SpawnedInSession = itemHelper.IsOfBaseclass(part.Template, BaseClasses.AMMO) ? null : true;
            NormalizeDurability(part);
        }

        return true;
    }

    private Item? ResolveRootItem(List<Item> items, MongoId? knownRootId, string presetName)
    {
        if (knownRootId is { IsEmpty: false } id)
        {
            var match = items.FirstOrDefault(i => i.Id.Equals(id));
            if (match is not null)
            {
                return match;
            }

            logger.Warning(
                $"[Gunwright]: Preset '{presetName}' - declared root '{id}' not found among its items, falling back to first item");
        }

        return items.FirstOrDefault();
    }

    private static MongoId RemapRootId(List<Item> items, Item rootItem)
    {
        var oldRootId = rootItem.Id;
        var newRootId = new MongoId();
        rootItem.Id = newRootId;

        foreach (var item in items)
        {
            if (item.Id.Equals(newRootId))
            {
                continue;
            }

            if (item.ParentId is not null && item.ParentId == oldRootId)
            {
                item.ParentId = newRootId;
            }
        }

        return newRootId;
    }

    private void PrepareItemForAssort(Item item, string parentId)
    {
        item.ParentId = parentId;
        item.SlotId = parentId;
        item.Upd ??= new Upd();
        item.Upd.StackObjectsCount = 999;
        item.Upd.UnlimitedCount = true;
        item.Upd.BuyRestrictionCurrent = 0;
    }

    private bool ValidateAttachments(MatchedSet set, TemplateItem rootTemplate)
    {
        var attachments = set.Attachments;
        if (attachments is null or { Count: 0 })
        {
            return true;
        }

        foreach (var att in attachments)
        {
            if (att is null || string.IsNullOrWhiteSpace(att.Tpl) || string.IsNullOrWhiteSpace(att.Slot))
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - malformed attachment entry");
                return false;
            }

            if (!itemHelper.GetItem(att.Tpl).Key)
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - attachment tpl '{att.Tpl}' not found");
                return false;
            }

            if (rootTemplate.Properties?.Slots?.Any(s => s.Name == att.Slot) != true)
            {
                logger.Warning($"[Gunwright]: Skipping preset '{set.RootTpl}' - slot '{att.Slot}' not declared on root template");
                return false;
            }
        }

        return true;
    }

    private bool ValidatePresetHierarchy(List<Item> items, MongoId rootId, string presetName)
    {
        var itemsById = items.ToDictionary(i => i.Id.ToString());

        foreach (var child in items)
        {
            if ((MongoId)child.Id == default || child.ParentId is null)
            {
                continue;
            }

            if ((MongoId)child.Id == rootId)
            {
                continue;
            }

            if (!itemsById.TryGetValue(child.ParentId.ToString(), out var parent))
            {
                logger.Warning($"[Gunwright]: Skipping '{presetName}' - item '{child.Template}' has parent '{child.ParentId}' not found in preset tree");
                return false;
            }

            if (!itemHelper.GetItem(child.Template).Key)
            {
                logger.Warning($"[Gunwright]: Skipping '{presetName}' - item tpl '{child.Template}' not found in item DB");
                return false;
            }

            var parentProps = itemHelper.GetItem(parent.Template).Value?.Properties;
            var slotDeclared = parentProps?.Slots?.Any(s => s.Name == child.SlotId) == true
                || parentProps?.Cartridges?.Any(s => s.Name == child.SlotId) == true
                || parentProps?.Chambers?.Any(s => s.Name == child.SlotId) == true;

            if (!slotDeclared)
            {
                logger.Warning($"[Gunwright]: Skipping '{presetName}' - slot '{child.SlotId}' (item '{child.Template}') not declared on parent '{parent.Template}'");
                return false;
            }
        }

        return true;
    }

    #endregion

    private static string ComputeBuildSignature(List<Item> items)
    {
        var parts = items
            .Select(item => $"{item.Template}|{item.SlotId ?? string.Empty}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return string.Join(";", parts);
    }

    private void NormalizeDurability(Item item)
    {
        if (item.Upd?.Repairable is null)
        {
            return;
        }

        var maxDurability = itemHelper.GetItem(item.Template).Value?.Properties?.MaxDurability;
        if (maxDurability is null or <= 0)
        {
            return;
        }

        item.Upd.Repairable.MaxDurability = maxDurability;
        item.Upd.Repairable.Durability = maxDurability;
    }
}
