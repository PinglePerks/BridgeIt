using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class TableKnowledgeTests
{
    // ── HCP cross-inference (existing + new HcpMin narrowing) ──────────────
    [Test]
    public void Constructor_CreatesKnowledgeForAllSeats()
    {
        var tk = new TableKnowledge(Seat.North);
        Assert.That(tk.Players, Has.Count.EqualTo(4));
        Assert.That(tk.Players.ContainsKey(Seat.North), Is.True);
        Assert.That(tk.Players.ContainsKey(Seat.South), Is.True);
        Assert.That(tk.Players.ContainsKey(Seat.East), Is.True);
        Assert.That(tk.Players.ContainsKey(Seat.West), Is.True);
    }

    [Test]
    public void Partner_ReturnsCorrectSeat()
    {
        var tk = new TableKnowledge(Seat.North);
        tk.Partner.HcpMin = 12;
        Assert.That(tk.Players[Seat.South].HcpMin, Is.EqualTo(12));
    }

    [Test]
    public void LeftOpponent_ReturnsCorrectSeat()
    {
        var tk = new TableKnowledge(Seat.North);
        tk.LeftOpponent.HcpMin = 15;
        // North's LHO is East
        Assert.That(tk.Players[Seat.East].HcpMin, Is.EqualTo(15));
    }

    [Test]
    public void RightOpponent_ReturnsCorrectSeat()
    {
        var tk = new TableKnowledge(Seat.North);
        tk.RightOpponent.HcpMin = 8;
        // North's RHO is West
        Assert.That(tk.Players[Seat.West].HcpMin, Is.EqualTo(8));
    }

    [Test]
    public void ApplyCrossTableInferences_NarrowsPartnerMax()
    {
        var tk = new TableKnowledge(Seat.North);
        // I (North) have 12 HCP, East is known to have 15+ HCP
        tk.Players[Seat.East].HcpMin = 15;

        tk.ApplyCrossTableInferences(myHcp: 12);

        // Partner (South) can have at most 40 - 12(me) - 15(East) - 0(West) = 13
        Assert.That(tk.Partner.HcpMax, Is.LessThanOrEqualTo(13));
    }

    [Test]
    public void ApplyCrossTableInferences_NarrowsOpponentMax()
    {
        var tk = new TableKnowledge(Seat.North);
        // I have 13, partner known to have 12+
        tk.Partner.HcpMin = 12;

        tk.ApplyCrossTableInferences(myHcp: 13);

        // East max: 40 - 13(me) - 12(partner) - 0(West) = 15
        Assert.That(tk.LeftOpponent.HcpMax, Is.LessThanOrEqualTo(15));
        // West max: 40 - 13(me) - 12(partner) - 0(East) = 15
        Assert.That(tk.RightOpponent.HcpMax, Is.LessThanOrEqualTo(15));
    }

    [Test]
    public void ApplyCrossTableInferences_DoesNotNarrowBelowExistingMax()
    {
        var tk = new TableKnowledge(Seat.North);
        // Partner already constrained to max 14
        tk.Partner.HcpMax = 14;

        tk.ApplyCrossTableInferences(myHcp: 10);

        // Cross-table would give 40 - 10 - 0 - 0 = 30, but existing 14 is lower
        Assert.That(tk.Partner.HcpMax, Is.EqualTo(14));
    }

    [Test]
    public void ApplyCrossTableInferences_NarrowsHcpMin_WhenOthersMaxIsKnown()
    {
        var tk = new TableKnowledge(Seat.North);
        // I have 12 HCP.  Partner max = 14, East max = 8.
        // West must have at least 40 - 12 - 14 - 8 = 6 HCP.
        tk.Partner.HcpMax = 14;
        tk.Players[Seat.East].HcpMax = 8;

        tk.ApplyCrossTableInferences(myHcp: 12);

        Assert.That(tk.RightOpponent.HcpMin, Is.GreaterThanOrEqualTo(6));
    }

    [Test]
    public void ApplyCrossTableInferences_HcpMin_DoesNotDropBelowZero()
    {
        var tk = new TableKnowledge(Seat.North);
        // All others have loose maxes; derived min for any player should not go negative.
        tk.ApplyCrossTableInferences(myHcp: 5);

        foreach (var seat in new[] { Seat.South, Seat.East, Seat.West })
            Assert.That(tk.Players[seat].HcpMin, Is.GreaterThanOrEqualTo(0));
    }

    // ── Suit cross-inference ───────────────────────────────────────────────

    [Test]
    public void ApplyCrossTableSuitInferences_NarrowsMaxWhenIHaveManySuit()
    {
        var tk = new TableKnowledge(Seat.North);
        // I hold 5 hearts; no other player has a known minimum.
        // Each opponent's max hearts = 13 - 5 - 0 - 0 = 8.
        var myShape = AllZeroShape();
        myShape[Suit.Hearts] = 5;

        tk.ApplyCrossTableSuitInferences(myShape);

        Assert.That(tk.Partner.MaxShape[Suit.Hearts], Is.EqualTo(8));
        Assert.That(tk.LeftOpponent.MaxShape[Suit.Hearts], Is.EqualTo(8));
        Assert.That(tk.RightOpponent.MaxShape[Suit.Hearts], Is.EqualTo(8));
    }

    [Test]
    public void ApplyCrossTableSuitInferences_NarrowsFurtherWithPartnerMinimum()
    {
        var tk = new TableKnowledge(Seat.North);
        // I have 5 hearts, partner confirmed min 4 hearts.
        // Each opponent's max = 13 - 5 - 4 = 4.
        tk.Partner.MinShape[Suit.Hearts] = 4;
        var myShape = AllZeroShape();
        myShape[Suit.Hearts] = 5;

        tk.ApplyCrossTableSuitInferences(myShape);

        Assert.That(tk.LeftOpponent.MaxShape[Suit.Hearts], Is.EqualTo(4));
        Assert.That(tk.RightOpponent.MaxShape[Suit.Hearts], Is.EqualTo(4));
    }

    [Test]
    public void ApplyCrossTableSuitInferences_DoesNotRaiseExistingLowerMax()
    {
        var tk = new TableKnowledge(Seat.North);
        // Partner is already known to have max 3 hearts (better than derived 8).
        tk.Partner.MaxShape[Suit.Hearts] = 3;
        var myShape = AllZeroShape();
        myShape[Suit.Hearts] = 5;

        tk.ApplyCrossTableSuitInferences(myShape);

        // Derived max is 8, but existing 3 is tighter — must stay at 3.
        Assert.That(tk.Partner.MaxShape[Suit.Hearts], Is.EqualTo(3));
    }

    [Test]
    public void ApplyCrossTableSuitInferences_NarrowsMinWhenOthersMaxesAreKnown()
    {
        var tk = new TableKnowledge(Seat.North);
        // I have 5 hearts.  LHO max = 2, RHO max = 2.
        // Partner must have at least 13 - 5 - 2 - 2 = 4 hearts.
        tk.LeftOpponent.MaxShape[Suit.Hearts] = 2;
        tk.RightOpponent.MaxShape[Suit.Hearts] = 2;
        var myShape = AllZeroShape();
        myShape[Suit.Hearts] = 5;

        tk.ApplyCrossTableSuitInferences(myShape);

        Assert.That(tk.Partner.MinShape[Suit.Hearts], Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void ApplyCrossTableSuitInferences_SuitMinNeverGoesNegative()
    {
        var tk = new TableKnowledge(Seat.North);
        // Extreme case: I have 13 spades.  Others must have 0, min floor = 0.
        var myShape = AllZeroShape();
        myShape[Suit.Spades] = 13;

        tk.ApplyCrossTableSuitInferences(myShape);

        foreach (var seat in new[] { Seat.South, Seat.East, Seat.West })
        {
            Assert.That(tk.Players[seat].MinShape[Suit.Spades], Is.EqualTo(0));
            Assert.That(tk.Players[seat].MaxShape[Suit.Spades], Is.EqualTo(0));
        }
    }

    [Test]
    public void ApplyCrossTableSuitInferences_OtherSuitsUnaffected()
    {
        var tk = new TableKnowledge(Seat.North);
        var myShape = AllZeroShape();
        myShape[Suit.Hearts] = 5;

        tk.ApplyCrossTableSuitInferences(myShape);

        // Spades were untouched — max should still be 13.
        Assert.That(tk.Partner.MaxShape[Suit.Spades], Is.EqualTo(13));
        Assert.That(tk.LeftOpponent.MaxShape[Suit.Spades], Is.EqualTo(13));
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static Dictionary<Suit, int> AllZeroShape()
        => new()
        {
            { Suit.Spades, 0 },
            { Suit.Hearts, 0 },
            { Suit.Diamonds, 0 },
            { Suit.Clubs, 0 }
        };
}
