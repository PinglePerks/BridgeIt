using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Assuming your Deck/Hand moved here per previous advice


// --- 1. Setup ---
var services = new ServiceCollection();

// One-line setup for the entire engine
services.AddBridgeItCore(); 

var provider = services.BuildServiceProvider();

// --- 2. Load Rules ---
// We define the path here, but the Logic is in the Core
const string RulesDirectory = "/Users/mattyperky/RiderProjects/BridgeIt/BridgeIt.CLI/BiddingRules";
var loader = provider.GetRequiredService<YamlRuleLoader>();
var loadedRules = loader.LoadRulesFromDirectory(RulesDirectory);

// Register the loaded rules into the Engine dynamically
// (Note: BiddingEngine needs to accept rules dynamically, or we register them back to DI)
// For this simple CLI, let's re-instantiate the engine or pass rules to it.
// BETTER APPROACH: Register rules into the container if possible, 
// OR pass them to the engine. For now, let's assume we pass them to the engine constructor 
// manually or register them as a list.

// Hack for CLI simplicity: Re-register the engine with the specific rules found
var logger = provider.GetRequiredService<ILogger<BiddingEngine>>();
var engine = new BiddingEngine(loadedRules,logger, new EngineObserver()); 
// In a real app, you might have a BiddingRuleRegistry service.

// --- 3. Play ---
// Injecting the engine manually for this CLI run since we created it after DI build
//var table = new BiddingTable(
    //provider.GetRequiredService<IAuctionRules>()
//);

