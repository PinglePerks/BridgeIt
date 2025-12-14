using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.BiddingEngine.Rules;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Extensions; // Import your new extension
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Players;
using BridgeIt.Dealer.Deal;
using Microsoft.Extensions.Logging; 
using Microsoft.Extensions.Logging.Console;// Assuming your Deck/Hand moved here per previous advice

// --- 1. Setup ---
var services = new ServiceCollection();

// One-line setup for the entire engine
services.AddBridgeItCore(); 
services.AddSingleton<IBiddingObserver, ConsoleBiddingObserver>();

services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information); // Set to Debug to see your detailed logs
});
var provider = services.BuildServiceProvider();


// --- 2. Load Rules ---
// We define the path here, but the Logic is in the Core
const string RulesDirectory = "/Users/mattyperky/RiderProjects/BridgeIt/BridgeIt.CLI/BiddingRules";
var loader = provider.GetRequiredService<YamlRuleLoader>();
var loadedRules = loader.LoadRulesFromDirectory(RulesDirectory);
var rules = loadedRules.ToList();
rules.Add(new OpenerUnbalancedRebidRule());
//rules.Add(new RespondingToNaturalOpening());
//rules.Add(new ResponseTo2ntOpening());
// rules.Add(new GeneralGameObjectiveRule());

// Register the loaded rules into the Engine dynamically
// (Note: BiddingEngine needs to accept rules dynamically, or we register them back to DI)
// For this simple CLI, let's re-instantiate the engine or pass rules to it.
// BETTER APPROACH: Register rules into the container if possible, 
// OR pass them to the engine. For now, let's assume we pass them to the engine constructor 
// manually or register them as a list.

// Hack for CLI simplicity: Re-register the engine with the specific rules found
var logger = provider.GetRequiredService<ILogger<BiddingEngine>>();
var engine = new BiddingEngine(rules, logger); 
// In a real app, you might have a BiddingRuleRegistry service.



// --- 3. Play ---
// Injecting the engine manually for this CLI run since we created it after DI build
var table = new BiddingTable(
    provider.GetRequiredService<IAuctionRules>(),
    provider.GetRequiredService<IBiddingObserver>());

var dealer = new Dealer();

// var dict = dealer.GenerateConstrainedDeal( 
//     north => HighCardPoints.Count(north) >= 20 && HighCardPoints.Count(north) <= 22 && ShapeEvaluator.IsBalanced(north),
//     south => ShapeEvaluator.GetShape(south)[Suit.Spades] > 4 );

var dict = dealer.GenerateRandomDeal();

Console.WriteLine("\n--- Hands Dealt ---");
foreach(var seat in dict.Keys) Console.WriteLine($"{seat}: {dict[seat]}");
Console.WriteLine("-------------------\n");

var players = new Dictionary<Seat, IPlayer>
{
    { Seat.North, new RobotPlayer(engine, provider.GetRequiredService<IRuleLookupService>()) },
    { Seat.East, new RobotPlayer(engine, provider.GetRequiredService<IRuleLookupService>()) },
    { Seat.South, new RobotPlayer(engine, provider.GetRequiredService<IRuleLookupService>()) },
    { Seat.West, new RobotPlayer(engine, provider.GetRequiredService<IRuleLookupService>()) },
};

//await table.RunAuction(dict, players, Seat.North);
