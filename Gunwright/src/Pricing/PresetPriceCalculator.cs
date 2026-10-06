using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;

namespace Gunwright.Pricing;

public readonly record struct PriceBreakdown(
    MongoId Template,
    double RawValue,
    double ModifiedValue,
    double Multiplier,
    string RarityName);

public readonly record struct PresetPrice(
    int Amount,
    string RarityName,
    double RawTotal,
    IReadOnlyList<PriceBreakdown> Breakdown);

public class PresetPriceCalculator
{
    private readonly ItemHelper _itemHelper;
    private readonly double _defaultMultiplier;
    private readonly List<RarityTier> _tiersAscending;

    public PresetPriceCalculator(ItemHelper itemHelper, PriceConfig? config)
    {
        _itemHelper = itemHelper;

        _defaultMultiplier = config?.DefaultMultiplier ?? 1.0;

        _tiersAscending = (config?.RarityTiers ?? [])
            .OrderBy(tier => tier.MinValue)
            .ToList();
    }

    public PresetPrice Compute(IEnumerable<MongoId> templates)
    {
        var breakdown = new List<PriceBreakdown>();

        var rawTotal = 0.0;
        var modifiedTotal = 0.0;

        foreach (var template in templates)
        {
            var minPrice = _itemHelper.GetItemPrice(template) ?? 0;
            var maxPrice = _itemHelper.GetItemMaxPrice(template);

            // Average min/max price of the item
            var rawValue = (minPrice + maxPrice) / 2.0;

            // Resolve rarity and multiplier for THIS item
            var (multiplier, rarityName) = ResolveTier(rawValue);

            var modifiedValue = rawValue * multiplier;

            rawTotal += rawValue;
            modifiedTotal += modifiedValue;

            breakdown.Add(new PriceBreakdown(
                template,
                rawValue,
                modifiedValue,
                multiplier,
                rarityName));
        }

        // Overall rarity of the complete preset is based
        // on the total raw value.
        var (_, presetRarityName) = ResolveTier(rawTotal);

        return new PresetPrice(
            (int)modifiedTotal,
            presetRarityName,
            rawTotal,
            breakdown);
    }

    private (double Multiplier, string RarityName) ResolveTier(double rawValue)
    {
        RarityTier? matched = null;

        foreach (var tier in _tiersAscending)
        {
            if (rawValue < tier.MinValue)
            {
                break;
            }

            matched = tier;
        }

        // If DefaultMultiplier is 1,
        // use the matched rarity multiplier.
        if (_defaultMultiplier == 1 && matched is not null)
        {
            return (matched.Multiplier, matched.Name);
        }

        // If DefaultMultiplier is anything other than 1,
        // use it directly.
        return (_defaultMultiplier, "Default");
    }
}