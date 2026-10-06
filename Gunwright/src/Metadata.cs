using SPTarkov.Server.Core.Models.Spt.Mod;

namespace Gunwright;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.deadwolf.spt.gunwright";
    public string Name { get; init; } = "Gunwright";
    public string Author { get; init; } = "DeadWolf";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.3.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.2");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Niharkanta1/TarkovMods4.0";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }
}
