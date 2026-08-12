using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace LogToConsole;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "UNIQUE PACKAGE NAME HERE";
    public string Name { get; init; } = "MOD NAME HERE";
    public string Author { get; init; } = "AUHTOR NAME HERE";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public bool? IsBundleMod { get; init; } = false;
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class SampleProject(ISptLogger<SampleProject> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("[BalancedMeds] This is an info message");

        return Task.CompletedTask;
    }
}