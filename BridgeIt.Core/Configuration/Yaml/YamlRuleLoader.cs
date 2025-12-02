using BridgeIt.Core.BiddingEngine.BidDerivation.Factories;
using BridgeIt.Core.BiddingEngine.Constraints.Factories;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BridgeIt.Core.Configuration.Yaml;

public class YamlRuleLoader
{
    private readonly IEnumerable<IConstraintFactory> _constraintFactories;
    
    private readonly IEnumerable<IBidDerivationFactory> _derivationFactories;

    // DI automatically injects all registered factories here
    public YamlRuleLoader(IEnumerable<IConstraintFactory> constraintFactories, IEnumerable<IBidDerivationFactory> derivationFactories)
    {
        _derivationFactories = derivationFactories;
        _constraintFactories = constraintFactories;
    }

    public IEnumerable<IBiddingRule> LoadRulesFromDirectory(string directoryPath)
    {
        var rules = new List<IBiddingRule>();

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: Directory '{directoryPath}' not found.");
            return rules;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yamlFiles = Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories);
        Console.WriteLine($"Found {yamlFiles.Length} YAML rule files. Loading...");

        foreach (var filePath in yamlFiles)
        {
            try
            {
                var yamlContent = File.ReadAllText(filePath);
                var yamlData = deserializer.Deserialize<YamlSystem>(yamlContent);

                // Use the injected factories to create the rule
                var rule = new YamlDerivedRule(yamlData, _constraintFactories, _derivationFactories);
                rules.Add(rule);
                
                Console.WriteLine($"  - Loaded: {Path.GetFileName(filePath)} ({yamlData.SystemName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  - ERROR {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        return rules;
    }
}