using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace FatmanServer;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.m0rt1s0114.wttr61fatman";
    public string Name { get; init; } = "WTT-R61 Fatman (SPT 4.1.x)";
    public string Author { get; init; } = "MadManBeavis";
    public List<string>? Contributors { get; init; } = ["M0rt1s0114"];
    public Version Version { get; init; } = new("1.1.1");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/M0rt1s0114/WTT-R61-Fatman-4.1.X";
    public string License { get; init; } = "MIT";
}
