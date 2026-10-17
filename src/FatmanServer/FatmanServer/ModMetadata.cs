using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace FatmanServer;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.madmanbeavis.wttr61fatman";
    public string Name { get; init; } = "WTT-R61 Fatman";
    public string Author { get; init; } = "MadManBeavis";
    public List<string>? Contributors { get; init; } = [];
    public Version Version { get; init; } = new("1.1.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = string.Empty;
    public string License { get; init; } = "MIT";
}
