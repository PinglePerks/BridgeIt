using System.Net.Http.Json;
using BridgeIt.Core.BiddingEngine.Rules;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.BiddingEngine.Constraints.Factories; // Needed for factories
using BridgeIt.Core.BiddingEngine.BidDerivation.Factories;
using BridgeIt.Core.BiddingEngine.Core; // Needed for factories

namespace BridgeIt.Web.Services;

public class WebRuleProvider : IRuleProvider
{
    private readonly HttpClient _http;
    private readonly IEnumerable<IConstraintFactory> _constraintFactories;
    private readonly IEnumerable<IBidDerivationFactory> _derivationFactories;

    public WebRuleProvider(
        HttpClient http, 
        IEnumerable<IConstraintFactory> constraintFactories,
        IEnumerable<IBidDerivationFactory> derivationFactories)
    {
        _http = http;
        _constraintFactories = constraintFactories;
        _derivationFactories = derivationFactories;
    }

    public async Task<IEnumerable<IBiddingRule>> LoadRulesAsync()
    {
        var rules = new List<IBiddingRule>();

        // In WASM, we can't list a directory. 
        // OPTION A: Maintain a manifest.json file that lists all rule files.
        // OPTION B: Hardcode the list here (Easier for MVP).
        
        var ruleFiles = new[] 
        { 
            "/rules/00_Openings_BasicAcol.yaml",
            "/rules/00_Openings_2Level.yaml",
            "/rules/AcolResponseTo1NT.yaml",
            // Add all your rule files here...
        };

        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .Build();

        foreach (var file in ruleFiles)
        {
            try 
            {
                // Fetch the YAML content via HTTP
                var yamlContent = await _http.GetStringAsync(file);
                
                var yamlData = deserializer.Deserialize<YamlSystem>(yamlContent);
                var rule = new YamlDerivedRule(yamlData, _constraintFactories, _derivationFactories);
                rules.Add(rule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load rule {file}: {ex.Message}");
            }
        }
        
        // Add Code-Based Rules manually if needed
        // rules.Add(new RedSuitTransfer()); 

        return rules;
    }
}