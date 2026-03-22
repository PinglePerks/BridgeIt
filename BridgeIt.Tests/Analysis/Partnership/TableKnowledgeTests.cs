using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class TableKnowledgeTests
{
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
}
