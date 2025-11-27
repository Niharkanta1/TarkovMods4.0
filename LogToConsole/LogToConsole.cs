using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace LogToConsole;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.logtoconsole.logging";
    public override string Name { get; init; } = "LogToConsole";
    public override string Author { get; init; } = "SPTarkov";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class Logging(
    ISptLogger<Logging> logger, DatabaseServer databaseServcer) // We inject a logger for use inside our class, it must have the class inside the diamond <> brackets
    : IOnLoad // Implement the IOnLoad interface so that this mod can do something on server load
{
    Dictionary<MongoId, TemplateItem> itemsDb = null!;
    public Task OnLoad()
    {
        // We can access the logger and call its methods to log to the server window and the server log file
        logger.Success("[LogToConsole] This is a success message");

        itemsDb = databaseServcer.GetTables().Templates.Items;

        foreach (TemplateItem item in itemsDb.Values)
        {
            MongoId parentId = item.Parent;
            if (parentId.Equals(BaseClasses.STIMULATOR))
            {
                TemplateItemProperties props = item.Properties;
                var buff = (props != null) ? props.StimulatorBuffs : "";
                logger.Info($"{item.Id} - {item.Name} - {buff}");
            }

        }

        // logger.Warning("[LogToConsole] This is a warning message");
        // logger.Error("[LogToConsole] This is an error message");
        // logger.Info("[LogToConsole] This is an info message");
        // logger.Critical("[LogToConsole] This is a critical message");

        // // Logging with colors requires you to 'pass' the text color and background color
        // logger.LogWithColor("[LogToConsole] This is a message with custom colors", LogTextColor.Red, LogBackgroundColor.Black);
        // logger.Debug("[LogToConsole] This is a debug message that gets written to the log file, not the console");

        // Inform the server our mod has finished doing work
        return Task.CompletedTask;
    }
}