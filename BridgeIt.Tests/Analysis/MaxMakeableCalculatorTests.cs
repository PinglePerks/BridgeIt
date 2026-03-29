using BridgeIt.Dds.Models;

namespace BridgeIt.Tests.Analysis;

[TestFixture]
public class MaxMakeableCalculatorTests
{
    private static DdsTrickTable BuildTrickTable(
        Dictionary<string, Dictionary<string, int>> tricks)
    {
        return new DdsTrickTable { Tricks = tricks };
    }

    [Test]
    public void NormalGameHand_FindsCorrectMaxMakeable()
    {
        // N can make 10 tricks in spades, S can make 10 tricks in spades
        // N can make 9 tricks in NT
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 5, ["diamonds"] = 6, ["hearts"] = 7, ["spades"] = 10, ["notrump"] = 9 },
            ["S"] = new() { ["clubs"] = 5, ["diamonds"] = 6, ["hearts"] = 7, ["spades"] = 10, ["notrump"] = 9 },
            ["E"] = new() { ["clubs"] = 8, ["diamonds"] = 7, ["hearts"] = 6, ["spades"] = 3, ["notrump"] = 4 },
            ["W"] = new() { ["clubs"] = 8, ["diamonds"] = 7, ["hearts"] = 6, ["spades"] = 3, ["notrump"] = 4 },
        });

        var nsMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: true);
        Assert.That(nsMax, Is.Not.Null);
        Assert.That(nsMax!.Level, Is.EqualTo(4));       // 10 - 6 = 4
        Assert.That(nsMax.Strain, Is.EqualTo("spades"));
        Assert.That(nsMax.Tricks, Is.EqualTo(10));

        var ewMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: false);
        Assert.That(ewMax, Is.Not.Null);
        Assert.That(ewMax!.Level, Is.EqualTo(2));        // 8 - 6 = 2
        Assert.That(ewMax.Strain, Is.EqualTo("clubs"));
        Assert.That(ewMax.Tricks, Is.EqualTo(8));
    }

    [Test]
    public void SlamHand_FindsSmallSlam()
    {
        // N can make 12 tricks in hearts, S can make 12 in hearts
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 6, ["diamonds"] = 8, ["hearts"] = 12, ["spades"] = 9, ["notrump"] = 10 },
            ["S"] = new() { ["clubs"] = 6, ["diamonds"] = 8, ["hearts"] = 12, ["spades"] = 9, ["notrump"] = 10 },
            ["E"] = new() { ["clubs"] = 7, ["diamonds"] = 5, ["hearts"] = 1, ["spades"] = 4, ["notrump"] = 3 },
            ["W"] = new() { ["clubs"] = 7, ["diamonds"] = 5, ["hearts"] = 1, ["spades"] = 4, ["notrump"] = 3 },
        });

        var nsMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: true);
        Assert.That(nsMax, Is.Not.Null);
        Assert.That(nsMax!.Level, Is.EqualTo(6));        // 12 - 6 = 6
        Assert.That(nsMax.Strain, Is.EqualTo("hearts"));
        Assert.That(nsMax.Tricks, Is.EqualTo(12));
    }

    [Test]
    public void NoMakeableContract_ReturnsNull()
    {
        // E/W can only make 6 or fewer tricks in any strain
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 9, ["diamonds"] = 9, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 9 },
            ["S"] = new() { ["clubs"] = 9, ["diamonds"] = 9, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 9 },
            ["E"] = new() { ["clubs"] = 4, ["diamonds"] = 4, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 4 },
            ["W"] = new() { ["clubs"] = 4, ["diamonds"] = 4, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 4 },
        });

        var ewMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: false);
        Assert.That(ewMax, Is.Null);
    }

    [Test]
    public void TieBreakByStrainRank_PrefersHigherStrain()
    {
        // N can make 10 tricks in both hearts and spades — spades wins
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 6, ["diamonds"] = 6, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 8 },
            ["S"] = new() { ["clubs"] = 6, ["diamonds"] = 6, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 8 },
            ["E"] = new() { ["clubs"] = 7, ["diamonds"] = 7, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 5 },
            ["W"] = new() { ["clubs"] = 7, ["diamonds"] = 7, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 5 },
        });

        var nsMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: true);
        Assert.That(nsMax, Is.Not.Null);
        Assert.That(nsMax!.Level, Is.EqualTo(4));
        Assert.That(nsMax.Strain, Is.EqualTo("spades")); // higher strain rank
    }

    [Test]
    public void DifferentDeclarersBetterFromOneSeat()
    {
        // N makes 10 in spades, S makes 11 in spades — S wins
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 5, ["diamonds"] = 5, ["hearts"] = 7, ["spades"] = 10, ["notrump"] = 8 },
            ["S"] = new() { ["clubs"] = 5, ["diamonds"] = 5, ["hearts"] = 7, ["spades"] = 11, ["notrump"] = 8 },
            ["E"] = new() { ["clubs"] = 8, ["diamonds"] = 8, ["hearts"] = 6, ["spades"] = 2, ["notrump"] = 5 },
            ["W"] = new() { ["clubs"] = 8, ["diamonds"] = 8, ["hearts"] = 6, ["spades"] = 3, ["notrump"] = 5 },
        });

        var nsMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: true);
        Assert.That(nsMax, Is.Not.Null);
        Assert.That(nsMax!.Level, Is.EqualTo(5));        // 11 - 6 = 5
        Assert.That(nsMax.Declarer, Is.EqualTo("S"));
    }

    [Test]
    public void GrandSlam_Level7()
    {
        var table = BuildTrickTable(new()
        {
            ["N"] = new() { ["clubs"] = 13, ["diamonds"] = 10, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 12 },
            ["S"] = new() { ["clubs"] = 13, ["diamonds"] = 10, ["hearts"] = 10, ["spades"] = 10, ["notrump"] = 12 },
            ["E"] = new() { ["clubs"] = 0, ["diamonds"] = 3, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 1 },
            ["W"] = new() { ["clubs"] = 0, ["diamonds"] = 3, ["hearts"] = 3, ["spades"] = 3, ["notrump"] = 1 },
        });

        var nsMax = MaxMakeableCalculator.ForSide(table, isNorthSouth: true);
        Assert.That(nsMax, Is.Not.Null);
        Assert.That(nsMax!.Level, Is.EqualTo(7));        // 13 - 6 = 7
        Assert.That(nsMax.Strain, Is.EqualTo("clubs"));
    }
}
