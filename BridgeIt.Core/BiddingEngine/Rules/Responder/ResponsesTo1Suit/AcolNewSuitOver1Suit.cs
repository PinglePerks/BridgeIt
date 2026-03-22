using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolNewSuitOver1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol new suit over 1 suit";
    public override int Priority { get; } = 40;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 1)
            return false;
        if (auction.OpeningBid!.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < 6) return false;

        // Must have at least one 4+ card suit that is NOT the opening suit
        // and that we can afford to bid at the required level
        var openingSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var hcp = ctx.HandEvaluation.Hcp;

        var candidates = ctx.HandEvaluation.SuitsWithMinLength(4)
            .Where(s => s != openingSuit)
            .ToList();

        if (!candidates.Any()) return false;

        // Check if any candidate can be bid at an affordable level
        var contract = ctx.AuctionEvaluation.CurrentContract;
        foreach (var suit in candidates)
        {
            var level = GetNextSuitBidLevel(suit, contract);
            if (level == 1) return true;         // 1-level: 6+ HCP is enough
            if (level == 2 && hcp >= 10) return true; // 2-level: need 10+
        }

        return false;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var openingSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var hcp = ctx.HandEvaluation.Hcp;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        var candidates = ctx.HandEvaluation.SuitsWithMinLength(4)
            .Where(s => s != openingSuit)
            .ToList();

        // Separate into 1-level and 2-level candidates
        var oneLevelSuits = candidates
            .Where(s => GetNextSuitBidLevel(s, contract) == 1)
            .ToList();

        var twoLevelSuits = candidates
            .Where(s => GetNextSuitBidLevel(s, contract) == 2 && hcp >= 10)
            .ToList();

        // Two 5+ card suits? Bid the higher-ranking first (regardless of level)
        var fiveCardCandidates = candidates
            .Where(s => ctx.HandEvaluation.Shape[s] >= 5)
            .ToList();
        if (fiveCardCandidates.Count >= 2)
        {
            var highest = fiveCardCandidates.First(); // already ordered by rank desc
            var level = GetNextSuitBidLevel(highest, contract);
            if (level == 1 || (level == 2 && hcp >= 10))
                return Bid.SuitBid(level, highest);
        }

        // 1-level available? Bid cheapest (up the line) to explore
        if (oneLevelSuits.Any())
        {
            var cheapest = oneLevelSuits.Last(); // SuitsWithMinLength orders high→low, so Last = cheapest
            return Bid.SuitBid(1, cheapest);
        }

        // 2-level: bid highest ranking (down the line)
        if (twoLevelSuits.Any())
        {
            var highest = twoLevelSuits.First();
            return Bid.SuitBid(2, highest);
        }

        return null;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        // Must not be the same suit as the opening (that's a raise, not new suit)
        if (bid.Suit == ctx.AuctionEvaluation.OpeningBid!.Suit)
            return false;

        // Must be at the correct next level for this suit
        var nextBidLevel = GetNextSuitBidLevel((Suit)bid.Suit!, ctx.AuctionEvaluation.CurrentContract);
        if (bid.Level != nextBidLevel)
            return false;

        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();

        if (bid.Level == 1)
        {
            constraints.Add(new HcpConstraint(6, 30));
            constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 10));
        }
        else if (bid.Level == 2)
        {
            constraints.Add(new HcpConstraint(10, 30));
            // Majors at 2-level typically show 5+ (e.g. 2H over 1S)
            if (bid.Suit == Suit.Hearts || bid.Suit == Suit.Spades)
                constraints.Add(new SuitLengthConstraint(bid.Suit, 5, 10));
            else
                constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 10));
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
