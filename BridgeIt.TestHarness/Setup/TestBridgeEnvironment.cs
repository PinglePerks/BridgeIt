using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;


namespace BridgeIt.TestHarness.Setup;

public class TestBridgeEnvironment
{
    public IServiceProvider Provider { get; private set; }
    public BiddingEngine Engine { get; private set; }
    public BiddingTable Table { get; private set; }

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
        
        // 2. Register Services needed for the Table if not covered by AddBridgeItCore
        // (Assuming AddBridgeItCore registers these, but being safe based on your Program.cs)
        services.TryAddSingleton<IAuctionRules, StandardAuctionRules>();
        services.TryAddSingleton<ISeatRotationService, ClockwiseSeatRotationService>();
        services.TryAddSingleton<IBiddingObserver, ConsoleBiddingObserver>(); // Or a silent Mock for tests
        services.TryAddSingleton<IHandFormatter, HandFormatter>();
        services.TryAddSingleton<BiddingTable>();
        
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information); // Set to Debug to see your detailed logs
        });

        Provider = services.BuildServiceProvider();
        

    }

    
    
    public TestBridgeEnvironment WithAllRules(string directoryPath)
    {
        var loader = Provider.GetRequiredService<YamlRuleLoader>();
        var rules = loader.LoadRulesFromDirectory(directoryPath).ToList();
        //rules.Add(new RespondingToNaturalOpening());
        rules.Add(new ResponseTo2ntOpening());
        rules.Add(new MajorFitWithPartner());
        
        // Re-register or Instantiate Engine with these specific rules
        var logger = Provider.GetRequiredService<ILogger<BiddingEngine>>();
        Engine = new BiddingEngine(rules,logger);
        RebuildTable();
        return this;
    }

    public TestBridgeEnvironment WithSpecificRules(params string[] filePaths)
    {
        var loader = Provider.GetRequiredService<YamlRuleLoader>();
        var rules = new List<IBiddingRule>();
        
        // Manually load specific files
        // (You might need to expose a LoadRuleFromFile method in loader, or just use this logic)
        foreach (var path in filePaths)
        {
            // Assuming loader has a public method or we replicate the logic
            // For now, leveraging the loader to load a directory containing these, 
            // or assuming you add a method LoadSingleFile to YamlRuleLoader.
            // Let's implement a simple file loader here for flexibility:
            
            var text = File.ReadAllText(path);
            // We need access to the deserializer/factories. 
            // ideally YamlRuleLoader exposes LoadRule(string yaml).
            // If not, we can just use directory loading for now.
        }
        
        // Fallback: Load directory but filter? 
        // Ideally update YamlRuleLoader to support single files.
        return this;
    }

    private void RebuildTable()
    {
        var logger = Provider.GetRequiredService<ILogger<BiddingTable>>();
        // We need a table that uses OUR specific Engine instance, not the empty one from DI
        Table = new BiddingTable(
            Engine,
            Provider.GetRequiredService<IAuctionRules>(),
            Provider.GetRequiredService<ISeatRotationService>(),
            Provider.GetRequiredService<IBiddingObserver>(),
            logger
        );
    }
}

// Helper extension to safely add if missing
public static class ServiceCollectionExtensions
{
    public static void TryAddSingleton<TService, TImplementation>(this IServiceCollection services) 
        where TService : class 
        where TImplementation : class, TService
    {
        if (services.All(x => x.ServiceType != typeof(TService)))
        {
            services.AddSingleton<TService, TImplementation>();
        }
    }
}