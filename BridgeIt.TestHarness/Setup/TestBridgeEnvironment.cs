using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.BiddingEngine.Rules;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Players;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace BridgeIt.TestHarness.Setup;

public class TestBridgeEnvironment
{
    public IServiceProvider Provider { get; private set; }
    public BiddingEngine Engine { get; private set; }
    public BiddingTable Table { get; private set; }
    public Dictionary<Seat, IPlayer> Players { get; private set; }

    // Builder Pattern for flexibility
    public static TestBridgeEnvironment Create()
    {
        var env = new TestBridgeEnvironment();
        env.Initialize();
        return env;
    }

    private void Initialize()
    {
        var services = new ServiceCollection();
        
        // 1. Core Dependencies (Factories, etc.)
        services.AddBridgeItCore(); 
        
        // 2. Register Observers
        services.AddSingleton<IBiddingObserver, ConsoleBiddingObserver>();
        
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug); // Set to Debug to see your detailed logs
        });

        Provider = services.BuildServiceProvider();
    }
    
    public TestBridgeEnvironment WithAllRules()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "BiddingRules");
        
        var loader = Provider.GetRequiredService<YamlRuleLoader>();
        var rules = loader.LoadRulesFromDirectory(fullPath).ToList();
        
        //rules.Add(new ResponseTo2ntOpening());
        var observer = Provider.GetRequiredService<IEngineObserver>();
        var logger = Provider.GetRequiredService<ILogger<BiddingEngine>>();
        
        Engine = new BiddingEngine(rules, logger, observer);
        
        var robotPlayer = new RobotPlayer(Engine, Provider.GetRequiredService<IRuleLookupService>());

        Players = new Dictionary<Seat, IPlayer>()
        {
            { Seat.North, robotPlayer },
            { Seat.East, robotPlayer },
            { Seat.South, robotPlayer },
            { Seat.West, robotPlayer }
        };
        RebuildTable();
        return this;
    }

    private void RebuildTable()
    {
        Table = Provider.GetRequiredService<BiddingTable>();
    }
}