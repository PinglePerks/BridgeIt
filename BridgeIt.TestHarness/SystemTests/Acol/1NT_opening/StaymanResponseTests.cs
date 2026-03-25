using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol._1NT_opening;

/// <summary>
/// System tests for the full Stayman sequence after 1NT:
/// 1NT -> 2C -> 2D/2H/2S (opener responds based on shape).
/// Verifies the opener's Stayman response correctly shows/denies majors.
/// </summary>
[TestFixture]
public class StaymanResponseTests
{
    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer();
    }

    [Test]
    public async Task StaymanResponse_ShowsHearts_WhenOpenerHas4Hearts()
    {
        // Opener: 12-14 balanced with 4+ hearts
        Func<Hand, bool> openerWith4Hearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_Stayman);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"));
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"),
                $"Expected 2H with 4+ hearts. Opener: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task StaymanResponse_ShowsSpades_WhenOpenerHas4SpadesButNot4Hearts()
    {
        // Opener: 12-14 balanced with 4+ spades but <4 hearts
        Func<Hand, bool> openerWith4SpadesNo4Hearts = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerWith4SpadesNo4Hearts,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_Stayman);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"));
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2S"),
                $"Expected 2S with 4+ spades, <4 hearts. Opener: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task StaymanResponse_Denies_WhenOpenerHasNoMajor()
    {
        // Opener: 12-14 balanced with <4 in both majors
        Func<Hand, bool> openerNoMajor = h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 14
            && ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            openerNoMajor,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_Stayman);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2C"));
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2D"),
                $"Expected 2D denial with no 4-card major. Opener: {deal[Seat.North]}");
        }
    }
}
