using BridgeIt.Core.BiddingEngine.BidDerivation;
using BridgeIt.Core.BiddingEngine.BidDerivation.Factories;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Constraints.Factories;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class YamlDerivedRule : BiddingRuleBase
{
    private readonly YamlSystem _definition;
    private readonly List<(IBidDerivation Bid, IBidConstraint Logic, string NextState, string Reason)> _options;
    private readonly IEnumerable<IConstraintFactory> _constraintFactories;
    
    private readonly IEnumerable<IBidDerivationFactory> _derivationFactories;
    public override int Priority => _definition.Priority;
    
    private readonly IBidConstraint _triggerConstraint; 

    // Unified Constructor: Takes the Definition (Data) and the Factories (Services)
    public YamlDerivedRule(YamlSystem definition, IEnumerable<IConstraintFactory> constraintFactories, IEnumerable<IBidDerivationFactory> derivationFactories)
    {
        _definition = definition;
        _constraintFactories = constraintFactories;
        _options = new List<(IBidDerivation, IBidConstraint, string, string)>();
        _derivationFactories = derivationFactories;

        _triggerConstraint = ParseConstraints(definition.Trigger.Conditions);

        // Pre-compile the constraints
        foreach (var node in definition.Nodes)
        {
            // Handle null constraints (e.g., a forced bid with no requirements)
            var constraintsDict = node.Constraints;
            IBidDerivation bidDerivation;
            if (node.DynamicBid.Count > 0)
            {
                bidDerivation = ParseDerivation(node.DynamicBid);
            }
            else
            {
                bidDerivation = new StaticBidDerivation(node.Bid.ToBid()!);
            }
            
            var logic = ParseConstraints(constraintsDict);
            
            _options.Add((bidDerivation, logic, node.NextState, node.Meaning));
        }
    }

    public override bool IsApplicable(BiddingContext ctx)
    {
        return _triggerConstraint.IsMet(ctx);
    }

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        foreach (var option in _options)
        {
            if (option.Logic.IsMet(ctx))
            {
                var bid = option.Bid.DeriveBid(ctx);

                if(IsValidBid(bid, ctx.AuctionEvaluation.CurrentContract))
                    return new BiddingDecision(bid, option.Reason, option.NextState, option.Logic);
            }
        }
        return null;
    }

    // --- Helpers ---
    private IBidDerivation ParseDerivation(Dictionary<string, object> items)
    {
        var factory = _derivationFactories.FirstOrDefault(f => f.CanCreate(items["bid_type"].ToString()!));

        return factory!.Create(items);
    }
    
    private IBidConstraint ParseConstraints(Dictionary<string, object> rawConstraints)
    {
        var composite = new CompositeConstraint();

        foreach (var kvp in rawConstraints)
        {
            if (kvp.Key == "any_of")
            {
                var orConstraint = new OrConstraint();
            
                // In YamlDotNet, a list is usually List<object>
                if (kvp.Value is List<object> alternatives)
                {
                    foreach (var alt in alternatives)
                    {
                        // Each alternative is a Dictionary (a set of AND constraints)
                        // We recursively parse this dictionary
                        if (alt is Dictionary<object, object> altDict)
                        {
                            // Convert Dictionary<object, object> to Dictionary<string, object>
                            var cleanDict = altDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
                        
                            // RECURSION: Parse the inner scenario
                            var innerScenario = ParseConstraints(cleanDict);
                            orConstraint.Add(innerScenario);
                        }
                    }
                }
                composite.Add(orConstraint);
                continue; // Skip the factory check for this key
            }
            var factory = _constraintFactories.FirstOrDefault(f => f.CanCreate(kvp.Key));
            
            if (factory != null)
            {
                composite.Add(factory.Create(kvp.Value));
            }
            else 
            {
                // In production, log a warning here: $"Unknown constraint: {kvp.Key}"
                Console.WriteLine($"Unknown constraint: {kvp.Key}");
            }
        }
        return composite;
    }
    
    private bool IsValidBid(Bid candidate, Bid? currentContract)
    {
        // Pass is always valid (unless specific competitive rules prevent it, but standard bridge allows pass)
        if (candidate.Type == BidType.Pass) return true;

        // Double is valid ONLY if opponents are currently winning and last bid wasn't Pass/Double
        // (This logic belongs in rule selection usually, but as a sanity check:)
        if (candidate.Type == BidType.Double) return true; // Simplification: Engine assumes rule checked context
        if (candidate.Type == BidType.Redouble) return true;

        // If no current contract, any Suit/NT is valid
        if (currentContract == null) return true;

        // Compare Levels
        if (candidate.Level > currentContract.Level) return true;
        if (candidate.Level < currentContract.Level) return false;

        // Same Level: Suit Rank matters (C < D < H < S < NT)
        // We need to compare suit enum values + NT special case
        return CompareSuits(candidate, currentContract) > 0;
    }

    private int CompareSuits(Bid candidate, Bid current)
    {
        // Helper to assign value: C=0, D=1, H=2, S=3, NT=4
        int GetValue(Bid b) => b.Type == BidType.NoTrumps ? 4 : (int)b.Suit!.Value;

        return GetValue(candidate) - GetValue(current);
    }
}