using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol.Rebid;

/// <summary>
/// System tests for responder rebid sequences after opener's second bid.
/// Tests the full 4-bid sequence: Opening -> Response -> Opener Rebid -> Responder Rebid.
///
/// Bid indices: [0]=Opening, [1]=East pass, [2]=Response, [3]=West pass,
///              [4]=Opener rebid, [5]=East pass, [6]=Responder rebid
/// </summary>
[TestFixture]
public class ResponderRebidSystemTests
{
    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer();
    }

    // =============================================
    // After opener raises responder's suit (1H-1S-2S)
    // =============================================

    [Test]
    public async Task ResponderRebid_PassesAfterSimpleRaise_WithMinimum()
    {
        // Opener: 12-15 unbalanced, 5H + 4S. Responder: 6-9, 4+ spades.
        // Sequence: 1H - 1S - 2S - Pass
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var response = auction.Bids[2].Bid.ToString();
            if (response != "1S") continue;

            var rebid = auction.Bids[4].Bid.ToString();
            if (rebid != "2S") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected Pass after 2S raise with 6-9 HCP. Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task ResponderRebid_InvitesAfterSimpleRaise_WithMedium()
    {
        // Opener: 12-15 unbalanced, 5H + 4S. Responder: 10-12, 4+ spades.
        // Sequence: 1H - 1S - 2S - 3S (invite)
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 12
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var response = auction.Bids[2].Bid.ToString();
            if (response != "1S") continue;

            var rebid = auction.Bids[4].Bid.ToString();
            if (rebid != "2S") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3S"),
                $"Expected 3S invite after 2S raise with 10-12 HCP. Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task ResponderRebid_BidsGameAfterSimpleRaise_WithStrong()
    {
        // Opener: 12-15 unbalanced, 5H + 4S. Responder: 13+, 4+ spades.
        // Sequence: 1H - 1S - 2S - 4S (game)
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 13 && HighCardPoints.Count(h) <= 16
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var response = auction.Bids[2].Bid.ToString();
            if (response != "1S") continue;

            var rebid = auction.Bids[4].Bid.ToString();
            if (rebid != "2S") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("4S"),
                $"Expected 4S game after 2S raise with 13+ HCP. Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // After opener rebids 1NT (1H-1S-1NT)
    // =============================================

    [Test]
    public async Task ResponderRebid_PassesAfter1NTRebid_WithMinimum()
    {
        // Opener: 15-17 balanced, longest hearts. Responder: 6-9, 4+ spades, <5 cards in own suit.
        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9
            && ShapeEvaluator.GetShape(h)[Suit.Spades] == 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 3;

        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 15 && HighCardPoints.Count(h) <= 17
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "1NT") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected Pass after 1NT rebid with 6-9 HCP (4 spades, <3 hearts). Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task ResponderRebid_Bids2NTAfter1NTRebid_WithInvitational()
    {
        // Opener: 15-17 balanced, longest hearts. Responder: 10-12, 4 spades.
        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 12
            && ShapeEvaluator.GetShape(h)[Suit.Spades] == 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 3;

        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 15 && HighCardPoints.Count(h) <= 17
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "1NT") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("2NT"),
                $"Expected 2NT invite after 1NT rebid with 10-12 HCP. Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task ResponderRebid_Bids3NTAfter1NTRebid_WithGameForce()
    {
        // Opener: 15-17 balanced, longest hearts. Responder: 13+, 4 spades.
        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 13 && HighCardPoints.Count(h) <= 16
            && ShapeEvaluator.GetShape(h)[Suit.Spades] == 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 3;

        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 15 && HighCardPoints.Count(h) <= 17
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "1NT") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3NT"),
                $"Expected 3NT after 1NT rebid with 13+ HCP. Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // After opener rebids own suit (1H-1S-2H)
    // =============================================

    [Test]
    public async Task ResponderRebid_PassesAfterSimpleRebid_WithMinimum()
    {
        // Opener: 12-15, 6+ hearts. Responder: 6-9, 4 spades, no heart support.
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 6
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 2
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 6;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "2H") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected Pass after 2H rebid with 6-9 HCP. Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task ResponderRebid_RaisesAfterSimpleRebid_WithInvitationalAndFit()
    {
        // Opener: 12-15, 6+ hearts. Responder: 10-12, 4 spades, 3+ hearts.
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 6
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 12
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 3;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "2H") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3H"),
                $"Expected 3H invite after 2H rebid with 10-12 HCP + heart support. Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // After opener bids new suit (1H-1S-2C)
    // =============================================

    [Test]
    public async Task ResponderRebid_GivesPreferenceAfterNewSuit_WithMinimum()
    {
        // Opener: 12-15, 5H + 4C. Responder: 6-9, 4 spades, 3+ hearts.
        // Should prefer opener's hearts over clubs.
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] >= 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 3;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "2C") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("2H"),
                $"Expected 2H preference after 1H-1S-2C with 6-9 HCP + 3 hearts. Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // After opener rebids 2NT (1H-1S-2NT)
    // =============================================

    [Test]
    public async Task ResponderRebid_Bids3NTAfter2NTRebid_WithGameValues()
    {
        // Opener: 18-19 balanced, longest hearts. Responder: 8-12, 4 spades.
        Func<Hand, bool> opener = h =>
            HighCardPoints.Count(h) >= 18 && HighCardPoints.Count(h) <= 19
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responder = h =>
            HighCardPoints.Count(h) >= 8 && HighCardPoints.Count(h) <= 12
            && ShapeEvaluator.GetShape(h)[Suit.Spades] == 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 3;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50, opener, HandSpecification.PassingOpponent, responder);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            if (auction.Bids[0].Bid.ToString() != "1H") continue;
            if (auction.Bids[2].Bid.ToString() != "1S") continue;
            if (auction.Bids[4].Bid.ToString() != "2NT") continue;

            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3NT"),
                $"Expected 3NT after 2NT rebid with 8+ HCP. Responder: {deal[Seat.South]}");
        }
    }
}
