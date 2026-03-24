using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol.Rebid;

/// <summary>
/// System tests for opener rebid sequences after a 1-suit opening.
/// Tests the full 3-bid sequence: Opening -> Response -> Opener Rebid.
/// </summary>
[TestFixture]
public class OpenerRebidSystemTests
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
    // Opener rebids own suit — minimum (12-15, simple rebid at 2-level)
    // =============================================

    [Test]
    public async Task OpenerRebid_SimpleRebid2H_WithMinimum6CardHearts()
    {
        // Minimum opener (12-15) with 6+ hearts → rebids 2H
        Func<Hand, bool> minimumOpener6Hearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 6
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            minimumOpener6Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1Suit_1NT(Suit.Hearts));

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1H"),
                $"Expected 1H opening. Opener: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("1NT"),
                $"Expected 1NT response. Responder: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"),
                $"Expected 2H simple rebid. Opener: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task OpenerRebid_SimpleRebid2S_WithMinimum6CardSpades()
    {
        Func<Hand, bool> minimumOpener6Spades = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 6
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Spades;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            minimumOpener6Spades,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1Suit_1NT(Suit.Spades));

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1S"),
                $"Expected 1S opening. Opener: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("1NT"),
                $"Expected 1NT response. Responder: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2S"),
                $"Expected 2S simple rebid. Opener: {deal[Seat.North]}");
        }
    }

    // =============================================
    // Opener rebids own suit — strong (16+, jump rebid at 3-level)
    // =============================================

    [Test]
    public async Task OpenerRebid_JumpRebid3H_WithStrong6CardHearts()
    {
        // Strong opener (16-19) with 6+ hearts → jump rebids 3H
        Func<Hand, bool> strongOpener6Hearts = h =>
            HighCardPoints.Count(h) >= 16 && HighCardPoints.Count(h) <= 19
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 6
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            strongOpener6Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1Suit_1NT(Suit.Hearts));

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1H"),
                $"Expected 1H opening. Opener: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("1NT"),
                $"Expected 1NT response. Responder: {deal[Seat.South]}");

            var rebid = auction.Bids[4].Bid.ToString();
            Assert.That(rebid, Is.AnyOf("3H", "4H"),
                $"Expected jump rebid (3H or 4H) with 16+ HCP. Opener: {deal[Seat.North]}");
        }
    }

    // =============================================
    // Opener bids new suit (5-4 shape, minimum)
    // =============================================

    [Test]
    public async Task OpenerRebid_BidsNewSuit_WithMinimum5Hearts4Clubs()
    {
        // Must have exactly 5 hearts (not 6+) so rebid-own-suit doesn't fire first
        Func<Hand, bool> minimumOpener5H4C = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] >= 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            minimumOpener5H4C,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1Suit_1NT(Suit.Hearts));

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1H"),
                $"Expected 1H opening. Opener: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("1NT"),
                $"Expected 1NT response. Responder: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2C"),
                $"Expected 2C rebid showing second suit. Opener: {deal[Seat.North]}");
        }
    }

    // =============================================
    // Opener raises responder's suit (minimum)
    // =============================================

    [Test]
    public async Task OpenerRebid_RaisesRespondersSpades_WithMinimum()
    {
        // Minimum opener (12-15) opens 1H with 4 spades, responder bids 1S
        // Must have exactly 5 hearts (not 6+) so rebid-own-suit doesn't fire first
        // No 4+ card minor so new-suit rule doesn't fire before raise
        Func<Hand, bool> minimumOpenerWith5H4S = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 15
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 4
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        Func<Hand, bool> responderWith4Spades = h =>
            HighCardPoints.Count(h) >= 6
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            minimumOpenerWith5H4S,
            HandSpecification.PassingOpponent,
            responderWith4Spades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            // Responder should bid 1S (new suit at 1-level)
            var response = auction.Bids[2].Bid.ToString();
            if (response != "1S") continue; // Skip if responder chose a different valid bid

            var rebid = auction.Bids[4].Bid.ToString();
            Assert.That(rebid, Is.EqualTo("2S"),
                $"Expected 2S raise with minimum. Opener: {deal[Seat.North]}, Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // Opener rebids balanced (1NT rebid = 15-17)
    // =============================================

    [Test]
    public async Task OpenerRebid_Opens1Suit_WithBalanced15to17()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtRebid,
            HandSpecification.PassingOpponent);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();

            // 15-17 balanced opens 1-suit (not 1NT which is 12-14)
            Assert.That(openingBid, Does.Match("1[CDHS]"),
                $"Expected 1-suit opening with 15-17 balanced. Hand: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task OpenerRebid_Bids1NT_WithBalanced15to17_After1SuitResponse()
    {
        // Opener has 15-17 balanced, opens 1-suit, responder bids new suit → opener rebids 1NT
        Func<Hand, bool> responderWith4Spades6Plus = h =>
            HighCardPoints.Count(h) >= 6
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4;

        // Use hearts opener to ensure responder can bid 1S
        Func<Hand, bool> balanced15to17LongestHearts = h =>
            HighCardPoints.Count(h) >= 15 && HighCardPoints.Count(h) <= 17
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            balanced15to17LongestHearts,
            HandSpecification.PassingOpponent,
            responderWith4Spades6Plus);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var opening = auction.Bids[0].Bid.ToString();
            var response = auction.Bids[2].Bid.ToString();
            if (response != "1S") continue; // Skip if responder chose different bid

            var rebid = auction.Bids[4].Bid.ToString();
            Assert.That(rebid, Is.EqualTo("1NT"),
                $"Expected 1NT rebid (15-17 balanced). Opener: {deal[Seat.North]}");
        }
    }
}
