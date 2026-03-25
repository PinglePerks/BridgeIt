using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class AcolStrongOpening : BiddingRuleBase
{
    public override string Name { get; } = "Acol Strong Opening";
    public override int Priority { get; }
    public override bool IsAlertable => true;

    private readonly int _minHcpUnbalanced;
    private readonly int _minHcpBalanced;
    private readonly int _maxHcp;
    private readonly int _bidLevel;
    private readonly Suit _bidSuit;

    public AcolStrongOpening(int minHcpUnbalanced = 20, int minHcpBalanced = 23, int maxHcp = 35,
        int bidLevel = 2, Suit bidSuit = Suit.Clubs, int priority = 19)
    {
        _minHcpUnbalanced = minHcpUnbalanced;
        _minHcpBalanced = minHcpBalanced;
        _maxHcp = maxHcp;
        _bidLevel = bidLevel;
        _bidSuit = bidSuit;
        Priority = priority;
    }

    private CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(_minHcpUnbalanced, _maxHcp) } };

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp > _maxHcp) return false;

        if (ctx.HandEvaluation.IsBalanced)
            return hcp >= _minHcpBalanced;

        return hcp >= _minHcpUnbalanced;
    }

    public override Bid? Apply(DecisionContext ctx)
        => Bid.SuitBid(_bidLevel, _bidSuit);

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit && bid.Level == _bidLevel && bid.Suit == _bidSuit;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, BuildConstraints(), PartnershipBiddingState.ConstructiveSearch);
}