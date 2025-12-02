using System.Data;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public interface IBidDerivation
{
    Bid DeriveBid(BiddingContext ctx);
}

public abstract class BidDerivationBase : IBidDerivation
{
    public abstract Bid DeriveBid(BiddingContext ctx);
    
    protected int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
    {
        if (currentContract == null) return 1;
        var level = currentContract.Level;
        if (currentContract.Type == BidType.NoTrumps) return level + 1;
        if (suit <= currentContract.Suit) return level + 1;
        return level;
    }

    protected int GetNextNtBidLevel(Bid? currentContract)
    {
        if (currentContract == null) return 1;
        if (currentContract.Type == BidType.NoTrumps) return currentContract.Level + 1;
        return currentContract.Level;
    }
}

public class LengthBidDerivation : BidDerivationBase
{
    public override Bid DeriveBid(BiddingContext ctx)
    {
        var suit 
            = ctx.HandEvaluation.Shape.OrderByDescending(s => s.Value).First().Key;
        
        var length = ctx.HandEvaluation.Shape[suit];

        var bidLevel = length - 4;

        return Bid.SuitBid(bidLevel, suit);
    }
}

public class SimpleRaise : BidDerivationBase
{
    private int _level { get; init; }
    public SimpleRaise(int level)
    {
        _level = level;
    }
    public override Bid DeriveBid(BiddingContext ctx)
    {
        var suit  = ctx.PartnershipKnowledge.FitInSuit;
        //if(suit == null) return Bid.NoTrumpsBid(5);

        return Bid.SuitBid(_level, suit!.Value);
    }
}

public class TransferBidDerivation : BidDerivationBase
{
    public override Bid DeriveBid(BiddingContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        var level = partnerBid!.Level!;
        var suit = partnerBid.Suit!;

        var transfer = (int)suit + 1;
        
        if (transfer == 4)
        {
            transfer = 0 ;
            level++;
        }
        
        return Bid.SuitBid(level, (Suit)transfer);
    }
}

public class OneLevelResponderBidDerivation() : BidDerivationBase
{

    public override Bid DeriveBid(BiddingContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        var currentBid = ctx.AuctionEvaluation.CurrentContract;
        
        //find suit to bid
        var numHearts = ctx.HandEvaluation.Shape[Suit.Hearts];
        var numSpades = ctx.HandEvaluation.Shape[Suit.Spades];
        if (numHearts >= numSpades && Math.Max(numHearts, numSpades) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Hearts, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Hearts);
        }

        if (Math.Max(numSpades, numHearts) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Spades, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Spades);
        }

        if (ctx.HandEvaluation.Shape[Suit.Diamonds] >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Diamonds, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Diamonds);
        }

        return Bid.NoTrumpsBid(1);
    }
}

public class ResponderBidDerivation() : BidDerivationBase
{

    public override Bid DeriveBid(BiddingContext ctx)
    {
        var currentBid = ctx.AuctionEvaluation.CurrentContract;
        
        //find suit to bid
        var numHearts = ctx.HandEvaluation.Shape[Suit.Hearts];
        var numSpades = ctx.HandEvaluation.Shape[Suit.Spades];
        
        if (numHearts >= numSpades && Math.Max(numHearts, numSpades) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Hearts, currentBid);
            if(nxt > 1 && numHearts >= 5)
                return Bid.SuitBid(nxt, Suit.Hearts);
            if(nxt == 1) return Bid.SuitBid(nxt, Suit.Hearts);
        }

        if (Math.Max(numSpades, numHearts) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Spades, currentBid);
            if(nxt > 1 && numSpades >= 5)
                return Bid.SuitBid(nxt, Suit.Spades);
            if(nxt == 1) return Bid.SuitBid(nxt, Suit.Spades);
        }
        if (ctx.HandEvaluation.Shape[Suit.Clubs] >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Clubs, currentBid);
            return Bid.SuitBid(nxt, Suit.Clubs);
        }

        if (ctx.HandEvaluation.Shape[Suit.Diamonds] >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Diamonds, currentBid);
            return Bid.SuitBid(nxt, Suit.Diamonds);
        }

        return Bid.NoTrumpsBid(1);
    }
}