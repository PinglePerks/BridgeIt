using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class GeneralGameObjectiveRule : BiddingRuleBase
{
    public override string Name { get; } = "Codebased: General Game Objective";
    public override int Priority { get; } = 1;
    public override bool IsApplicable(BiddingContext ctx)
    {
        return ctx.AuctionEvaluation.CurrentContract != null;
    }

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        // 1. Gather Information
        var myHcp = ctx.HandEvaluation.Hcp;
        var partnerMinHcp = ctx.PartnershipKnowledge.PartnerHcpMin;
        var totalHcp = myHcp + partnerMinHcp;

        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        
        // Find best fit (prioritize Majors)
        var bestFit = GetBestFit(ctx);
        var isBalanced = ctx.HandEvaluation.IsBalanced; // Simplified; ideally check partnership balance

        // 2. Determine Optimal Contract Level & Strain
        Bid? targetContract = DetermineTargetContract(totalHcp, bestFit, isBalanced);

        if (targetContract == null) 
            return new BiddingDecision(Bid.Pass(), $"{Name} || Reason: No game interest found", "part_score");

        // 3. Compare Target vs Current
        
        // If we have already reached or exceeded the target level in the target suit/NT -> PASS
        if (IsContractSufficient(currentContract, targetContract))
        {
            return new BiddingDecision(Bid.Pass(), 
                $"Contract {currentContract} satisfies target {targetContract}", "contract_reached");
        }

        // 4. Bid towards the target
        // If the target is a suit we fit in, and we aren't there yet, raise or jump to it.
        if (targetContract.Suit.HasValue && bestFit == targetContract.Suit)
        {
            // If we can jump directly to game (e.g., 1H -> 4H), do it if it's safe (simplified logic)
            // Ideally, we check if we need to show features (cue bidding), but this is a "Game" engine.
            
            // Check if target level is valid (not below current)
            if (targetContract.Level > currentContract.Level || 
               (targetContract.Level == currentContract.Level && targetContract.Suit > currentContract.Suit))
            {
                return new BiddingDecision(targetContract, 
                    $"Driving to game/slam based on {totalHcp} combined HCP", "driving_to_game");
            }
        }
        else if (targetContract.Type == BidType.NoTrumps)
        {
            // Bid 3NT if we are close enough and current contract is lower
            if (currentContract.Level < 3 || (currentContract.Level == 3 && currentContract.Type != BidType.NoTrumps))
            {
                // If partner is currently in a suit, this might be a "correction" to NT
                // Simple logic: Jump to 3NT
                return new BiddingDecision(targetContract, 
                    $"Combined strength {totalHcp} suggests 3NT", "driving_to_game");
            }
        }

        // Fallback: If we can't bid the target directly safely (e.g. need to find a suit first), 
        // we might bid a new suit (forcing). 
        // For this implementation, let's look for the cheapest bid up the ladder or Pass if stuck.
        
        return null; // Let the engine fallback to Pass or other rules if we can't construct a path
    }

    private Suit? GetBestFit(BiddingContext ctx)
    {
        // Check Majors first
        if (ctx.PartnershipKnowledge.HasFit(Suit.Hearts, ctx.HandEvaluation.Shape[Suit.Hearts])) return Suit.Hearts;
        if (ctx.PartnershipKnowledge.HasFit(Suit.Spades, ctx.HandEvaluation.Shape[Suit.Spades])) return Suit.Spades;
        
        // Then Minors
        if (ctx.PartnershipKnowledge.HasFit(Suit.Diamonds, ctx.HandEvaluation.Shape[Suit.Diamonds])) return Suit.Diamonds;
        if (ctx.PartnershipKnowledge.HasFit(Suit.Clubs, ctx.HandEvaluation.Shape[Suit.Clubs])) return Suit.Clubs;

        return null;
    }

    private Bid? DetermineTargetContract(int totalHcp, Suit? fit, bool amBalanced)
    {
        // === SLAM ZONE (33+) ===
        if (totalHcp >= 33)
        {
            // If fit, 6-Major/Minor. If balanced, 6NT.
            if (fit.HasValue) return Bid.SuitBid(6, fit.Value);
            return Bid.NoTrumpsBid(6);
        }

        // === GAME ZONE (25-32) ===
        if (totalHcp >= 25)
        {
            // Priority 1: Major Suit Game (4H / 4S) - Best Score/Safety balance
            if (fit.HasValue && (fit == Suit.Hearts || fit == Suit.Spades))
            {
                return Bid.SuitBid(4, fit.Value);
            }

            // Priority 2: 3NT (Requires 25 HCP)
            // We usually prefer 3NT over 5-Minor because 9 tricks is easier than 11.
            // We bid this if balanced OR if we have a minor fit but 3NT scores better.
            if (amBalanced || (fit.HasValue && (fit == Suit.Clubs || fit == Suit.Diamonds)))
            {
                return Bid.NoTrumpsBid(3);
            }
            
            // Priority 3: Minor Game (5C / 5D)
            // Only if we are very strong (29+) or very unbalanced. 
            // Simplified: If we have a fit and < 29, we probably stop at part score or gamble 3NT.
            // Let's strictly bid 5-minor only if 29+ to be safe.
            if (fit.HasValue && totalHcp >= 29)
            {
                return Bid.SuitBid(5, fit.Value);
            }
        }

        // === PART SCORE (Invitational 23-24) ===
        // If we are close, we might bid "One more for the road" or 2NT
        if (totalHcp >= 23)
        {
             if (fit.HasValue) return Bid.SuitBid(3, fit.Value); // Invite
             return Bid.NoTrumpsBid(2); // Invite
        }

        // === SIGN OFF ===
        // We don't set a target here, effectively telling the Apply method to Pass 
        // if the partner's bid is acceptable.
        return null;
    }

    private bool IsContractSufficient(Bid current, Bid target)
    {
        // 1. If Current Level > Target Level, we definitely stop (don't bid 5H if target is 4H)
        if (current.Level > target.Level) return true;

        // 2. If Same Level, check type
        if (current.Level == target.Level)
        {
            // If we are in 3NT and target is 3NT -> Stop.
            if (current.Type == target.Type && current.Suit == target.Suit) return true;
            
            // If we are in 4H and target is 4H -> Stop.
            // Note: If we are in 4S and target is 4H, we went past it.
            if (current.Suit > target.Suit) return true; 
        }

        // 3. Special: If we are in 4H (Major Game) and target was 3NT.
        // Usually 4H is preferred over 3NT if we found a fit late. 
        // So if target is 3NT, but we are in 4H/4S, we are happy.
        if (target.Type == BidType.NoTrumps && current.Type == BidType.Suit && current.Level >= 4)
            return true;

        return false;
    }
}