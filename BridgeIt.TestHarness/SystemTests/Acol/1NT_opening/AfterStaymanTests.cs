using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol._1NT_opening;

/// <summary>
/// System tests for responder's rebid after a Stayman sequence:
/// 1NT -> 2C -> 2D/2H/2S -> responder places the contract.
///
/// Scenarios:
/// - Fit found (4+4 in a major) → raise to game (4M) or invite (3M)
/// - No fit → bid 3NT (game) or 2NT (invite)
/// </summary>
[TestFixture]
public class AfterStaymanTests
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
    // After 2D denial (opener has no 4-card major)
    // =============================================

    [Test]
    public async Task AfterStayman_2D_GameForcing_Bids3NT()
    {
        // Opener: 12-14 balanced, no 4-card major
        Func<Hand, bool> openerNoMajor = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerNoMajor,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_GameForcing_BothMajors);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2D"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3NT"),
                $"Expected 3NT with 13+ HCP and no fit. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task AfterStayman_2D_Invitational_Bids2NT()
    {
        // Opener: 12-14 balanced, no 4-card major
        Func<Hand, bool> openerNoMajor = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerNoMajor,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_Invitational_BothMajors);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2D"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("2NT"),
                $"Expected 2NT invite with 11-12 HCP and no fit. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    // =============================================
    // After 2H (opener shows 4+ hearts)
    // =============================================

    [Test]
    public async Task AfterStayman_2H_HeartFit_GameForcing_Bids4H()
    {
        // Opener: 12-14 balanced with 4+ hearts
        Func<Hand, bool> openerWith4Hearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4;

        // Responder: 13+ HCP, exactly 4 hearts (has the fit)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_GameForcing_WithHearts);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("4H"),
                $"Expected 4H with heart fit and 13+ HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task AfterStayman_2H_HeartFit_Invitational_Bids3H()
    {
        // Opener: 12-14 balanced with 4+ hearts
        Func<Hand, bool> openerWith4Hearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4;

        // Responder: 11-12 HCP, exactly 4 hearts (has the fit)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_Invitational_WithHearts);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3H"),
                $"Expected 3H invite with heart fit and 11-12 HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task AfterStayman_2H_NoFit_GameForcing_Bids3NT()
    {
        // Opener: 12-14 balanced with 4+ hearts but <4 spades
        Func<Hand, bool> openerWith4HeartsNoSpades = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

        // Responder: 13+ HCP, exactly 4 spades, <4 hearts (no heart fit)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4HeartsNoSpades,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_GameForcing_WithSpades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3NT"),
                $"Expected 3NT with no heart fit and 13+ HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task AfterStayman_2H_NoFit_Invitational_Bids2NT()
    {
        // Opener: 12-14 balanced with 4+ hearts but <4 spades
        Func<Hand, bool> openerWith4HeartsNoSpades = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

        // Responder: 11-12 HCP, exactly 4 spades, <4 hearts (no heart fit)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4HeartsNoSpades,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_Invitational_WithSpades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("2NT"),
                $"Expected 2NT invite with no heart fit and 11-12 HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    // =============================================
    // After 2S (opener shows 4+ spades, denied hearts)
    // =============================================

    [Test]
    public async Task AfterStayman_2S_SpadeFit_GameForcing_Bids4S()
    {
        // Opener: 12-14 balanced with 4+ spades, <4 hearts
        Func<Hand, bool> openerWith4SpadesNoHearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4;

        // Responder: 13+ HCP, exactly 4 spades (has the fit)
        // Use GameForcing_WithSpades since responder has 4 spades
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4SpadesNoHearts,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_GameForcing_WithSpades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2S"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("4S"),
                $"Expected 4S with spade fit and 13+ HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task AfterStayman_2S_SpadeFit_Invitational_Bids3S()
    {
        // Opener: 12-14 balanced with 4+ spades, <4 hearts
        Func<Hand, bool> openerWith4SpadesNoHearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4;

        // Responder: 11-12 HCP, exactly 4 spades (has the fit)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4SpadesNoHearts,
            HandSpecification.PassingOpponent,
            HandSpecification.AfterStayman_Invitational_WithSpades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"), $"S: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2S"), $"N: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("3S"),
                $"Expected 3S invite with spade fit and 11-12 HCP. S: {deal[Seat.South]}, N: {deal[Seat.North]}");
        }
    }
}
