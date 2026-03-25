using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Systems.Config;

namespace BridgeIt.Systems;

/// <summary>
/// The result of loading a bidding system from config.
/// Contains the instantiated rule list, the source config, and any warnings.
/// </summary>
public record LoadedSystem(
    string Name,
    IReadOnlyList<IBiddingRule> Rules,
    BridgeSystemConfig Config,
    List<string> Warnings);
