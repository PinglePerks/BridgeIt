using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol;

/// <summary>
/// System tests for competitive bidding: overcalls, takeout doubles,
/// negative doubles, and responses to competitive bids.
/// </summary>
[TestFixture]
public class CompetitiveBiddingTests
{
    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer();
    }

    // ═══════════════════════════════════════════════════════════════
    // HAND SPECIFICATIONS FOR COMPETITIVE DEALS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Simple overcall hand: 8-15 HCP, 5+ in target suit, no other 5+ suit
    /// (to avoid the engine choosing the other suit).
    /// </summary>
    private static Func<Hand, bool> SimpleOvercallHand(Suit suit) => h =>
    {
        var hcp = HighCardPoints.Count(h);
        var shape = ShapeEvaluator.GetShape(h);
        return hcp >= 8 && hcp <= 15
               && shape[suit] >= 5
               && !ShapeEvaluator.IsBalanced(h)
               // Ensure target suit is the longest (no competing 5+ suits)
               && Enum.GetValues<Suit>().Where(s => s != suit).All(s => shape[s] < shape[suit]);
    };

    /// <summary>
    /// Takeout double via strong override: 17+ HCP, no 5+ suit, not balanced.
    /// Above overcall range, no long suit for jump overcall, not balanced for NT overcall.
    /// </summary>
    private static Func<Hand, bool> StrongTakeoutDoubleHand => h =>
    {
        var hcp = HighCardPoints.Count(h);
        if (hcp < 17 || hcp > 20) return false;
        var shape = ShapeEvaluator.GetShape(h);
        if (ShapeEvaluator.IsBalanced(h)) return false;
        // No 6+ suit (would trigger jump overcall)
        if (shape.Values.Any(v => v >= 6)) return false;
        return true;
    };

    /// <summary>Weak hand that should pass.</summary>
    private static Func<Hand, bool> WeakPassHand => h =>
        HighCardPoints.Count(h) < 8
        && ShapeEvaluator.GetShape(h).Values.All(v => v < 6);

    // ═══════════════════════════════════════════════════════════════
    // SIMPLE OVERCALL TESTS
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public async Task East_Overcalls1S_WhenNorthOpens1H()
    {
        // North opens 1H, East has overcall hand with 5+ spades.
        var deals = _dealer.GenerateMultipleConstrainedDeals(20,
            northConstraint: HandSpecification.AcolMajor1LevelOpening(Suit.Hearts),
            eastConstraint: SimpleOvercallHand(Suit.Spades),
            southConstraint: WeakPassHand,
            westConstraint: WeakPassHand);

        foreach (var deal in deals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            // North should open 1H
            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1H"),
                $"N should open 1H. Hand: {deal[Seat.North]}");

            // East should overcall 1S
            Assert.That(auction.Bids[1].Bid.ToString(), Is.EqualTo("1S"),
                $"E should overcall 1S with hand: {deal[Seat.East]}");
        }
    }

    [Test]
    public async Task East_Overcalls2C_WhenNorthOpens1S()
    {
        // North opens 1S, East has 5+ clubs for 2C overcall.
        var deals = _dealer.GenerateMultipleConstrainedDeals(20,
            northConstraint: HandSpecification.AcolMajor1LevelOpening(Suit.Spades),
            eastConstraint: SimpleOvercallHand(Suit.Clubs),
            southConstraint: WeakPassHand,
            westConstraint: WeakPassHand);

        foreach (var deal in deals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1S"),
                $"N should open 1S. Hand: {deal[Seat.North]}");

            Assert.That(auction.Bids[1].Bid.ToString(), Is.EqualTo("2C"),
                $"E should overcall 2C with hand: {deal[Seat.East]}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // TAKEOUT DOUBLE TESTS
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public async Task East_Doubles_WhenNorthOpens1H_AndEastHasTakeoutShape()
    {
        // Use strong override (16+ HCP) — easier to generate than classic 4-4-4-1 shape
        var deals = _dealer.GenerateMultipleConstrainedDeals(20,
            northConstraint: HandSpecification.AcolMajor1LevelOpening(Suit.Hearts),
            eastConstraint: StrongTakeoutDoubleHand,
            southConstraint: WeakPassHand,
            westConstraint: WeakPassHand);

        foreach (var deal in deals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1H"),
                $"N should open 1H. Hand: {deal[Seat.North]}");

            // East should double (takeout)
            Assert.That(auction.Bids[1].Bid.ToString(), Is.EqualTo("X"),
                $"E should double (takeout) with hand: {deal[Seat.East]}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // COMPETITIVE AUCTION COMPLETION
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public async Task CompetitiveAuction_Completes_WithBothSidesBidding()
    {
        // North opens 1H, East overcalls 1S. Verify the auction terminates properly.
        var deals = _dealer.GenerateMultipleConstrainedDeals(10,
            northConstraint: HandSpecification.AcolMajor1LevelOpening(Suit.Hearts),
            eastConstraint: SimpleOvercallHand(Suit.Spades));

        foreach (var deal in deals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            // Auction should have at least 4 bids (opening + overcall + 2 more)
            Assert.That(auction.Bids.Count, Is.GreaterThanOrEqualTo(4),
                "Competitive auction should have at least 4 bids");

            // Should end with 3 consecutive passes
            var lastThree = auction.Bids.TakeLast(3).ToList();
            Assert.That(lastThree.All(b => b.Bid.Type == BidType.Pass), Is.True,
                "Auction should end with 3 consecutive passes");
        }
    }
}
