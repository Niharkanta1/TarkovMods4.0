using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    private PresetConfig? _config;

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

    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
    {
        if (_traderId is null || secondsSinceLastRun < RefreshIntervalSeconds)
        {
            return Task.FromResult(false);
        }

        lock (_assortGate)
        {
            if (CurrentUnixMs() - Interlocked.Read(ref _lastPurchaseUnixMs) < PostPurchaseSuppressMs)
            {
                return Task.FromResult(false);
            }

            RefreshCore(_traderId.Value);
            return Task.FromResult(true);
        }
    }

    private static long CurrentUnixMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Must be called while holding _assortGate
    private PresetConfig? GetConfig()
    {
        if (_config is not null)
        {
            return _config;
        }

        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _config = modHelper.GetJsonDataFromFile<PresetConfig>(pathToMod, "db/config.json");
        return _config;
    }

    /// <summary>
    /// Builds a complete new assort off to the side, then swaps it in with a single
    /// reference assignment so readers never see an empty or half-filled assort.
    /// </summary>
    private int RefreshCore(MongoId traderId)
    {
        if (!tradersTable.TryGetValue(traderId, out var traderData))
        {
            logger.Error($"[Gunwright]: Trader {traderId} not found, cannot refresh presets");
            return 0;
        }

        var config = GetConfig();
        var priceCalculator = new PresetPriceCalculator(itemHelper, config?.PriceConfig);

        var staging = new Trader
        {
            Assort = new TraderAssort
            {
                Items = [],
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            },
            Base = traderData.Base,
            QuestAssort = traderData.QuestAssort,
            Dialogue = traderData.Dialogue
        };

        var total = PopulateWeaponBuilds(staging, priceCalculator)
                    + PopulateStaticPresets(staging, config?.GearPresets, priceCalculator, "gear presets")
                    + PopulateStaticPresets(staging, config?.ArmorPresets, priceCalculator, "armor presets")
                    + PopulateGunsmithPresets(staging, config?.WeaponPresets, priceCalculator);

        // Only swap if we actually built something, so a config error never leaves the trader empty
        if (total > 0)
        {
            traderData.Assort = staging.Assort;
        }
        else
        {
            logger.Warning("[Gunwright]: Refresh produced no offers, keeping the previous assort");
        }

        return total;
    }

    private int PopulateWeaponBuilds(Trader staging, PresetPriceCalculator priceCalculator)
    {
        var count = 0;

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

                var buildName = build.Name ?? build.Id.ToString();
                var items = cloner.Clone(build.Items);

                if (!TryPreparePreset(items, build.Root, $"build|{build.Id}", buildName, out var rootId))
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

                if (AddToAssort(staging, items!, rootId, price.Amount))
                {
                    logger.Debug(
                        $"[Gunwright]: Added build '{buildName}' ({build.Id}) (price: {price.Amount}₽ [{price.RarityName}])");
                    count++;
                }
            }
        }

        return count;
    }

    private int PopulateStaticPresets(
        Trader staging,
        List<MatchedSet>? sets,
        PresetPriceCalculator priceCalculator,
        string label)
    {
        if (sets is null or { Count: 0 })
        {
            logger.Warning($"[Gunwright]: {label} not found or empty, skipping presets");
            return 0;
        }

        var soldRootTpls = GetSoldRootTemplates(staging);
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

            var rootId = StableId($"{label}|{set.RootTpl}|root");
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

            var attIndex = 0;
            foreach (var att in set.Attachments ?? [])
            {
                if (att is { Tpl: not null, Slot: not null })
                {
                    items.Add(new Item
                    {
                        Id = StableId($"{label}|{set.RootTpl}|{attIndex++}"),
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

            if (AddToAssort(staging, items, rootId, price.Amount))
            {
                logger.Debug(
                    $"[Gunwright]: Added {label} '{set.RootTpl}' (price: {price.Amount}₽ [{price.RarityName}], {items.Count} items)");
                added++;
            }
        }

        return added;
    }

    private int PopulateGunsmithPresets(
        Trader staging,
        Dictionary<string, List<Item>>? presets,
        PresetPriceCalculator priceCalculator)
    {
        if (presets is null or { Count: 0 })
        {
            logger.Warning("[Gunwright]: gunsmith weapon presets not found or empty");
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

            // Clone: TryPreparePreset mutates ids/parents/upd, and the config is cached
            var items = cloner.Clone(presetItems);

            if (!TryPreparePreset(items, null, $"gunsmith|{name}", name, out var rootId))
            {
                logger.Warning($"[Gunwright]: Skipping weapon preset '{name}' - invalid item hierarchy");
                continue;
            }

            var price = priceCalculator.Compute(items!.Select(i => i.Template));
            if (price.Amount <= 0)
            {
                logger.Warning($"[Gunwright]: Skipping weapon preset '{name}' - price is 0");
                continue;
            }

            if (AddToAssort(staging, items!, rootId, price.Amount))
            {
                logger.Debug(
                    $"[Gunwright]: Added weapon preset '{name}' (price: {price.Amount}₽ [{price.RarityName}], {items!.Count} items)");
                added++;
            }
        }

        return added;
    }

    #region Helpers

    /// <summary>Same key always produces the same 24-char hex id.</summary>
    private static MongoId StableId(string key)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(key));
        return new MongoId(Convert.ToHexString(hash, 0, 12).ToLowerInvariant());
    }

    private static HashSet<MongoId> GetSoldRootTemplates(Trader staging)
    {
        return staging.Assort.Items
            .Where(i => i.SlotId == TraderRootParentId)
            .Select(i => i.Template)
            .ToHashSet();
    }

    private bool AddToAssort(Trader staging, List<Item> items, MongoId rootId, int price)
    {
        if (staging.Assort.BarterScheme.ContainsKey(rootId))
        {
            logger.Warning($"[Gunwright]: Duplicate offer id {rootId}, skipping");
            return false;
        }

        staging.Assort.Items.AddRange(items);
        staging.Assort.BarterScheme[rootId] = [[new() { Count = price, Template = Money.ROUBLES }]];
        staging.Assort.LoyalLevelItems[rootId] = 1;
        return true;
    }

    /// <summary>
    /// Gives every item in the preset a deterministic id derived from <paramref name="key"/>
    /// and its position, and rewires ParentId references. Returns the new root (offer) id.
    /// </summary>
    private static MongoId RemapIds(List<Item> items, Item rootItem, string key)
    {
        var map = new Dictionary<string, MongoId>();
        var rootId = StableId($"{key}|root");
        map[rootItem.Id.ToString()] = rootId;

        for (var i = 0; i < items.Count; i++)
        {
            var old = items[i].Id.ToString();
            if (!map.ContainsKey(old))
            {
                map[old] = StableId($"{key}|{i}");
            }
        }

        foreach (var item in items)
        {
            if (!ReferenceEquals(item, rootItem)
                && item.ParentId is not null
                && map.TryGetValue(item.ParentId.ToString(), out var newParent))
            {
                item.ParentId = newParent;
            }
        }

        foreach (var item in items)
        {
            item.Id = map[item.Id.ToString()];
        }

        return rootId;
    }

    private bool TryPreparePreset(
        List<Item>? items,
        MongoId? knownRootId,
        string key,
        string presetName,
        out MongoId rootId)
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

        rootId = RemapIds(items, rootItem, key);

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

    #endregion
}