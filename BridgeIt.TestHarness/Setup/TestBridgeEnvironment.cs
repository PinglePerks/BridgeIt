using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Players;
using BridgeIt.Systems;
using Microsoft.Extensions.Logging;


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
        var loader = new BiddingSystemLoader();
        var systemPath = FindSystemFile("acol-foundation.json");
        var loaded = loader.LoadFromFile(systemPath);

        var observer = Provider.GetRequiredService<IEngineObserver>();
        var logger = Provider.GetRequiredService<ILogger<BiddingEngine>>();

        Engine = new BiddingEngine(loaded.Rules, logger, observer);

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

    private static string FindSystemFile(string filename)
    {
        // Walk up from test output directory to find BridgeIt.Systems/Systems/
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "BridgeIt.Systems", "Systems", filename);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new FileNotFoundException($"Could not find system file: {filename}");
    }

    private void RebuildTable()
    {
        Table = Provider.GetRequiredService<BiddingTable>();
    }
}
