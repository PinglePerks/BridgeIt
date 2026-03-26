using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.BiddingEngine.Constraints;

[TestFixture]
public class ShortageConstraintTests
{
    private static DecisionContext MakeContext(Dictionary<Suit, int> shape)
    {
        var handEval = new HandEvaluation
        {
            Hcp = 12,
            Shape = shape,
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var history = new AuctionHistory(Seat.North);
        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, new TableKnowledge(Seat.North));
    }

    [Test]
    public void IsMet_WhenShortInSuit_ReturnsTrue()
    {
        var shape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 1 }
        };

        var constraint = new ShortageConstraint(Suit.Clubs, 2);
        Assert.That(constraint.IsMet(MakeContext(shape)), Is.True);
    }

    [Test]
    public void IsMet_WhenVoidInSuit_ReturnsTrue()
    {
        var shape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 5 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 0 }
        };

        var constraint = new ShortageConstraint(Suit.Clubs, 2);
        Assert.That(constraint.IsMet(MakeContext(shape)), Is.True);
    }

    [Test]
    public void IsMet_WhenTooLongInSuit_ReturnsFalse()
    {
        var shape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 }
        };

        var constraint = new ShortageConstraint(Suit.Clubs, 2);
        Assert.That(constraint.IsMet(MakeContext(shape)), Is.False);
    }

    [Test]
    public void IsMet_WhenExactlyAtMaxLength_ReturnsTrue()
    {
        var shape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 }
        };

        var constraint = new ShortageConstraint(Suit.Clubs, 2);
        Assert.That(constraint.IsMet(MakeContext(shape)), Is.True);
    }
}
