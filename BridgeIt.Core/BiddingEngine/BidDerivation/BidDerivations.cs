using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;


// Strategy 1: The "Old Way" (Static)
// Used when you write 'bid: "1H"' in YAML

// Strategy 2: The "Dynamic Way" (Calculated)
// Used when you use the new dynamic syntax
public class HcpStepBidDerivation : IBidDerivation
{
    private readonly Suit _suit;
    private readonly int _startHcp;
    private readonly int _startLevel;
    private readonly int _stepSize;

    public HcpStepBidDerivation(Suit suit, int startHcp, int startLevel, int stepSize = 1)
    {
        _suit = suit;
        _startHcp = startHcp;
        _startLevel = startLevel;
        _stepSize = stepSize;
    }

    public Bid? DeriveBid(BiddingContext ctx)
    {
        int actualHcp = ctx.HandEvaluation.Hcp;
        
        // If we are below the threshold, this generator can't produce a bid 
        // (though Constraints usually prevent us getting here if set correctly)
        if (actualHcp < _startHcp) return null;

        // Calculate offset. 
        // Example: Hand 8 HCP. Start 6 HCP. Diff = 2.
        int hcpDifference = actualHcp - _startHcp;
        
        // Calculate level steps. 
        // Example: Step 1. Steps = 2 / 1 = 2.
        int levelIncrease = hcpDifference / _stepSize;
        
        int finalLevel = _startLevel + levelIncrease;

        if (finalLevel > 7) return null; // Safety cap

        return Bid.SuitBid(finalLevel, _suit);
    }
}