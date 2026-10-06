using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Gunwright.Config;

public class MatchedSet
{
    public string? RootTpl { get; set; }
    public List<AttachmentRef>? Attachments { get; set; }
}

public class AttachmentRef
{
    public string? Slot { get; set; }
    public string? Tpl { get; set; }
}

public class PresetConfig
{
    public TraderBase? Base { get; set; }
    public Pricing.PriceConfig? PriceConfig { get; set; }
    public List<MatchedSet>? GearPresets { get; set; }
    public List<MatchedSet>? ArmorPresets { get; set; }
    public Dictionary<string, List<Item>>? WeaponPresets { get; set; }
}
