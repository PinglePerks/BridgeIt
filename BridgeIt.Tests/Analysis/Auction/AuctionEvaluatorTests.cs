using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Auction;

[TestFixture]
public class AuctionEvaluatorTests
{
    // =============================================
    // BiddingRound
    // =============================================

    [Test]
    public void BiddingRound_NoHistory_Returns0()
    {
        var history = new AuctionHistory(Seat.North);
        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.BiddingRound, Is.EqualTo(0));
    }

    [Test]
    public void BiddingRound_AllPasses_Returns0()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.BiddingRound, Is.EqualTo(0));
    }

    [Test]
    public void BiddingRound_AfterOpening_ResponderIsRound1()
    {
        // North opens 1H, East passes — South (responder) is about to bid for first time
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        // South's turn: 0 turns since opening + 1 = 1
        Assert.That(result.BiddingRound, Is.EqualTo(1));
    }

    [Test]
    public void BiddingRound_OpenerRebid_IsRound2()
    {
        // North opens 1H, East passes, South responds 2H, West passes — North's rebid
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        // North's turn: 1 turn since opening (the 1H itself) + 1 = 2
        Assert.That(result.BiddingRound, Is.EqualTo(2));
    }

    [Test]
    public void BiddingRound_ResponderPassedThenBids_IsRound2()
    {
        // North: 1H, East: 2C, South: Pass, West: Pass, North: 2S, East: Pass
        // South about to bid — they passed in round 1, now it's round 2
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Spades)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        // South: 1 turn since opening (the pass) + 1 = 2
        Assert.That(result.BiddingRound, Is.EqualTo(2));
    }

    [Test]
    public void BiddingRound_ThirdSeatOpener_ResponderIsRound1()
    {
        // North passes, East passes, South opens 1NT, West passes — North is about to respond
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        // North: 0 turns since opening (North's Pass was before opening) + 1 = 1
        // Wait — North's pass at index 0 is BEFORE the opening at index 2.
        // Skip(2) gives [1NT(South), Pass(West)] — North has 0 turns in this range.
        // BiddingRound = 0 + 1 = 1
        Assert.That(result.BiddingRound, Is.EqualTo(1));
    }

    // =============================================
    // AuctionPhase
    // =============================================

    [Test]
    public void AuctionPhase_NoBids_IsPreOpening()
    {
        var history = new AuctionHistory(Seat.North);
        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.AuctionPhase, Is.EqualTo(AuctionPhase.PreOpening));
    }

    [Test]
    public void AuctionPhase_OneSideBids_IsUncontested()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.AuctionPhase, Is.EqualTo(AuctionPhase.Uncontested));
    }

    [Test]
    public void AuctionPhase_BothSidesBid_IsContested()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(2, Suit.Clubs)));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.AuctionPhase, Is.EqualTo(AuctionPhase.Contested));
    }

    [Test]
    public void AuctionPhase_ResponderBids_StillUncontested()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Hearts)));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.AuctionPhase, Is.EqualTo(AuctionPhase.Uncontested));
    }

    // =============================================
    // CurrentContract
    // =============================================

    [Test]
    public void CurrentContract_NoBids_IsNull()
    {
        var history = new AuctionHistory(Seat.North);
        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.CurrentContract, Is.Null);
    }

    [Test]
    public void CurrentContract_AfterPasses_IsNull()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.CurrentContract, Is.Null);
    }

    [Test]
    public void CurrentContract_AfterSuitBid_IsLastContract()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.CurrentContract, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void CurrentContract_HigherBid_Updates()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds)));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.CurrentContract, Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    // =============================================
    // OpeningBid & OpeningSeat
    // =============================================

    [Test]
    public void OpeningBid_FirstNonPass()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.NoTrumpsBid(1)));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.OpeningBid, Is.EqualTo(Bid.NoTrumpsBid(1)));
        Assert.That(result.OpeningSeat, Is.EqualTo(Seat.East));
    }

    [Test]
    public void OpeningBid_NoBids_IsNull()
    {
        var history = new AuctionHistory(Seat.North);
        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.OpeningBid, Is.Null);
        Assert.That(result.OpeningSeat, Is.Null);
    }

    // =============================================
    // PartnerLastBid
    // =============================================

    [Test]
    public void PartnerLastBid_FindsPartnerBid()
    {
        // North opens 1H, East passes — South is next, partner is North
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var result = AuctionEvaluator.Evaluate(history);
        // Next to bid is South, partner is North
        Assert.That(result.PartnerLastBid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    // =============================================
    // NextSeatToBid
    // =============================================

    [Test]
    public void NextSeatToBid_EmptyHistory_IsDealer()
    {
        var history = new AuctionHistory(Seat.East);
        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.NextSeatToBid, Is.EqualTo(Seat.East));
    }

    [Test]
    public void NextSeatToBid_AfterOneBid_IsNextClockwise()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));

        var result = AuctionEvaluator.Evaluate(history);
        Assert.That(result.NextSeatToBid, Is.EqualTo(Seat.East));
    }
}
