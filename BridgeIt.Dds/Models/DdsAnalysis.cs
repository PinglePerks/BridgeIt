namespace BridgeIt.Dds.Models;

/// <summary>
/// Complete DDS analysis for a deal: trick table + par results for all 4 vulnerabilities.
/// This is the payload broadcast to the frontend via SignalR.
/// </summary>
public class DdsAnalysis
{
    public DdsTrickTable TrickTable { get; set; } = new();

    /// <summary>
    /// Par results keyed by vulnerability: "none", "nsVul", "ewVul", "bothVul".
    /// </summary>
    public Dictionary<string, ParResult> Par { get; set; } = new();

    public static readonly string[] VulnerabilityKeys = ["none", "nsVul", "ewVul", "bothVul"];
}
