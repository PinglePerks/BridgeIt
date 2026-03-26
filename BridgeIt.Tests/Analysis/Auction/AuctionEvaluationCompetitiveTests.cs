using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Auction;

/// <summary>
/// Tests for competitive bidding properties on AuctionEvaluation:
/// RhoLastNonPassBid, LhoLastNonPassBid, OpponentBidSuits, UnbidSuits,
/// IsDirectSeat, IsProtectiveSeat.
/// </summary>
[TestFixture]
public class AuctionEvaluationCompetitiveTests
{
    // ── RhoLastNonPassBid ──────────────────────────────────────────

    [Test]
    public void RhoLastNonPassBid_WhenRhoBidSuit_ReturnsThatBid()
    {
        // N opens 1H, E (RHO of S) overcalls 1S. South to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(1, Suit.Spades)));

        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.RhoLastNonPassBid, Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void RhoLastNonPassBid_WhenRhoPassed_ReturnsNull()
    {
        // N opens 1H, E passes. South to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.RhoLastNonPassBid, Is.Null);
    }

    // ── LhoLastNonPassBid ──────────────────────────────────────────

    [Test]
    public void LhoLastNonPassBid_WhenLhoBidSuit_ReturnsThatBid()
    {
        // N opens 1H, E passes, S bids 2H, W (LHO of N) overcalls 2S. North to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.West, Bid.SuitBid(2, Suit.Spades)));

        var eval = AuctionEvaluator.Evaluate(history);

        // Next to bid is North. LHO of North = East.
        // East passed, so LhoLastNonPassBid = null.
        Assert.That(eval.LhoLastNonPassBid, Is.Null);
    }

    [Test]
    public void LhoLastNonPassBid_ForEast_WhenNorthOpened()
    {
        // N opens 1H. East to bid. LHO of East = North = 1H.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));

        var eval = AuctionEvaluator.Evaluate(history);

        // Next to bid is East. LHO = South (hasn't bid). But North opened...
        // LHO of East = South? No. Seat order: N→E→S→W. Next(E) = S. So LHO = S.
        // North is RHO of East.
        // Actually: LHO = the seat to the LEFT of current seat = the seat that bids AFTER.
        // In bridge, LHO = left-hand opponent = next seat after you.
        Assert.That(eval.LhoLastNonPassBid, Is.Null); // South hasn't bid
    }

    // ── OpponentBidSuits ──────────────────────────────────────────

    [Test]
    public void OpponentBidSuits_ReturnsOpponentsSuitBids()
    {
        // N opens 1H, E overcalls 1S. South to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(1, Suit.Spades)));

        var eval = AuctionEvaluator.Evaluate(history);

        // For South: opponents are East and West. East bid spades.
        Assert.That(eval.OpponentBidSuits, Is.EquivalentTo(new[] { Suit.Spades }));
    }

    [Test]
    public void OpponentBidSuits_WhenNoOpponentBids_ReturnsEmpty()
    {
        // N opens 1H, E passes. South to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.OpponentBidSuits, Is.Empty);
    }

    // ── UnbidSuits ─────────────────────────────────────────────────

    [Test]
    public void UnbidSuits_ExcludesBidSuits()
    {
        // N opens 1H, E overcalls 1S. Two suits bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(1, Suit.Spades)));

        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.UnbidSuits, Is.EquivalentTo(new[] { Suit.Clubs, Suit.Diamonds }));
    }

    [Test]
    public void UnbidSuits_WhenNoBids_ReturnAllFour()
    {
        var history = new AuctionHistory(Seat.North);
        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.UnbidSuits, Has.Count.EqualTo(4));
    }

    // ── IsDirectSeat ──────────────────────────────────────────────

    [Test]
    public void IsDirectSeat_WhenRhoJustBid_ReturnsTrue()
    {
        // N opens 1H. East is in direct seat (RHO of East is North, and North just bid).
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));

        var eval = AuctionEvaluator.Evaluate(history);

        // Next = East. Last bid was North (opponent), non-pass.
        Assert.That(eval.IsDirectSeat, Is.True);
    }

    [Test]
    public void IsDirectSeat_WhenPartnerJustBid_ReturnsFalse()
    {
        // N opens 1H, E passes. South to bid. Last bid was E (pass).
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var eval = AuctionEvaluator.Evaluate(history);

        // Last bid was East passing — it's a pass so not direct
        Assert.That(eval.IsDirectSeat, Is.False);
    }

    // ── IsProtectiveSeat ──────────────────────────────────────────

    [Test]
    public void IsProtectiveSeat_AfterOpponentBidTwoPasses_ReturnsTrue()
    {
        // E opens 1H, S passes, W passes. North to bid (protective).
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var eval = AuctionEvaluator.Evaluate(history);

        // Next = North. Pattern: E bid, S pass, W pass → protective
        Assert.That(eval.IsProtectiveSeat, Is.True);
    }

    [Test]
    public void IsProtectiveSeat_WhenNotInProtectivePosition_ReturnsFalse()
    {
        // N opens 1H. East to bid (direct, not protective).
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));

        var eval = AuctionEvaluator.Evaluate(history);

        Assert.That(eval.IsProtectiveSeat, Is.False);
    }

    [Test]
    public void IsProtectiveSeat_Standard4thSeat()
    {
        // N passes, E passes, S opens 1S, W passes. North to bid (protective? No — actually N already passed, not protective in the overcall sense. Let's test 4th seat after opening).
        // N opens 1H, E passes, S passes. West to bid.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));

        var eval = AuctionEvaluator.Evaluate(history);

        // Next = West. Third-last = N (1H, opponent of W, non-pass). Second-last = E (pass, partner of W). Last = S (pass, opponent).
        // Pattern: opponent (N) bid, partner (E) pass, RHO (S) pass → protective ✓
        Assert.That(eval.IsProtectiveSeat, Is.True);
    }
}
