namespace Gunwright.Pricing;

public class RarityTier
{
    public string Name { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double Multiplier { get; set; } = 1.0;
}

public class PriceConfig
{
    public double DefaultMultiplier { get; set; } = 1.0;
    public List<RarityTier>? RarityTiers { get; set; }
}
