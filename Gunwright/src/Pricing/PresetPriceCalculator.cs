using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;

namespace Gunwright.Pricing;

public readonly record struct PresetPrice(int Amount, string RarityName);

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
        var rawValue = 0.0;
        foreach (var template in templates)
        {
            var minPrice = _itemHelper.GetItemPrice(template) ?? 0;
            var maxPrice = _itemHelper.GetItemMaxPrice(template);
            rawValue += (minPrice + maxPrice) / 2.0;
        }

        var (multiplier, rarityName) = ResolveTier(rawValue);
        return new PresetPrice((int)(rawValue * multiplier), rarityName);
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

        // _defaultMultiplier == 1, use the matched tier; otherwise use _defaultMultiplier directly
        if (_defaultMultiplier == 1 && matched is not null)
        {
            return (matched.Multiplier, matched.Name);
        }

        return (_defaultMultiplier, "Default");
    }
}
