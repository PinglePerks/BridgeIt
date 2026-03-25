using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Domain;

[TestFixture]
public class BidTests
{
    // =============================================
    // Equals & GetHashCode
    // =============================================

    [Test]
    public void Equals_SameSuitBids_AreEqual()
    {
        var bid1 = Bid.SuitBid(1, Suit.Hearts);
        var bid2 = Bid.SuitBid(1, Suit.Hearts);
        Assert.That(bid1, Is.EqualTo(bid2));
    }

    [Test]
    public void Equals_SameNTBids_AreEqual()
    {
        var bid1 = Bid.NoTrumpsBid(3);
        var bid2 = Bid.NoTrumpsBid(3);
        Assert.That(bid1, Is.EqualTo(bid2));
    }

    [Test]
    public void Equals_DifferentLevels_AreNotEqual()
    {
        var bid1 = Bid.SuitBid(1, Suit.Hearts);
        var bid2 = Bid.SuitBid(2, Suit.Hearts);
        Assert.That(bid1, Is.Not.EqualTo(bid2));
    }

    [Test]
    public void Equals_DifferentSuits_AreNotEqual()
    {
        var bid1 = Bid.SuitBid(1, Suit.Hearts);
        var bid2 = Bid.SuitBid(1, Suit.Spades);
        Assert.That(bid1, Is.Not.EqualTo(bid2));
    }

    [Test]
    public void Equals_PassBids_AreEqual()
    {
        var pass1 = Bid.Pass();
        var pass2 = Bid.Pass();
        Assert.That(pass1, Is.EqualTo(pass2));
    }

    [Test]
    public void Equals_PassAndSuitBid_AreNotEqual()
    {
        Assert.That(Bid.Pass(), Is.Not.EqualTo(Bid.SuitBid(1, Suit.Clubs)));
    }

    [Test]
    public void Equals_NullHandling()
    {
        var bid = Bid.SuitBid(1, Suit.Hearts);
        Assert.That(bid.Equals(null), Is.False);
    }

    [Test]
    public void GetHashCode_EqualBids_HaveSameHash()
    {
        var bid1 = Bid.SuitBid(2, Suit.Diamonds);
        var bid2 = Bid.SuitBid(2, Suit.Diamonds);
        Assert.That(bid1.GetHashCode(), Is.EqualTo(bid2.GetHashCode()));
    }

    [Test]
    public void OperatorEquals_Works()
    {
        var bid1 = Bid.SuitBid(1, Suit.Hearts);
        var bid2 = Bid.SuitBid(1, Suit.Hearts);
        Assert.That(bid1 == bid2, Is.True);
        Assert.That(bid1 != Bid.Pass(), Is.True);
    }

    [Test]
    public void HashSet_ContainsBid()
    {
        var set = new HashSet<Bid> { Bid.SuitBid(2, Suit.Clubs) };
        Assert.That(set.Contains(Bid.SuitBid(2, Suit.Clubs)), Is.True);
        Assert.That(set.Contains(Bid.SuitBid(2, Suit.Diamonds)), Is.False);
    }

    // =============================================
    // ToString
    // =============================================

    [Test]
    [TestCase(BidType.Pass, 0, null, "Pass")]
    [TestCase(BidType.Double, 0, null, "X")]
    [TestCase(BidType.Redouble, 0, null, "XX")]
    public void ToString_SpecialBids_ReturnsCorrectString(BidType type, int level, Suit? suit, string expected)
    {
        Bid bid = type switch
        {
            BidType.Pass => Bid.Pass(),
            BidType.Double => Bid.Double(),
            BidType.Redouble => Bid.Redouble(),
            _ => throw new ArgumentException()
        };
        Assert.That(bid.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(1, Suit.Clubs, "1C")]
    [TestCase(1, Suit.Diamonds, "1D")]
    [TestCase(1, Suit.Hearts, "1H")]
    [TestCase(1, Suit.Spades, "1S")]
    [TestCase(3, Suit.Hearts, "3H")]
    [TestCase(7, Suit.Spades, "7S")]
    public void ToString_SuitBids_ReturnsCorrectString(int level, Suit suit, string expected)
    {
        Assert.That(Bid.SuitBid(level, suit).ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(1, "1NT")]
    [TestCase(3, "3NT")]
    [TestCase(7, "7NT")]
    public void ToString_NTBids_ReturnsCorrectString(int level, string expected)
    {
        Assert.That(Bid.NoTrumpsBid(level).ToString(), Is.EqualTo(expected));
    }

    // =============================================
    // NextLevelForSuit
    // =============================================

    [Test]
    public void NextLevelForSuit_NoContract_Returns1()
    {
        Assert.That(Bid.NextLevelForSuit(Suit.Hearts, null), Is.EqualTo(1));
    }

    [Test]
    public void NextLevelForSuit_SameSuit_ReturnsNextLevel()
    {
        var contract = Bid.SuitBid(2, Suit.Hearts);
        Assert.That(Bid.NextLevelForSuit(Suit.Hearts, contract), Is.EqualTo(3));
    }

    [Test]
    public void NextLevelForSuit_HigherSuit_ReturnsSameLevel()
    {
        // Current contract is 2C, want to bid Spades — Spades > Clubs, same level is valid
        var contract = Bid.SuitBid(2, Suit.Clubs);
        Assert.That(Bid.NextLevelForSuit(Suit.Spades, contract), Is.EqualTo(2));
    }

    [Test]
    public void NextLevelForSuit_LowerSuit_ReturnsNextLevel()
    {
        // Current contract is 2S, want to bid Clubs — Clubs < Spades, must go up a level
        var contract = Bid.SuitBid(2, Suit.Spades);
        Assert.That(Bid.NextLevelForSuit(Suit.Clubs, contract), Is.EqualTo(3));
    }

    [Test]
    public void NextLevelForSuit_AfterNT_ReturnsNextLevel()
    {
        var contract = Bid.NoTrumpsBid(1);
        Assert.That(Bid.NextLevelForSuit(Suit.Clubs, contract), Is.EqualTo(2));
    }

    [Test]
    public void NextLevelForSuit_AfterNT_AllSuitsGoUp()
    {
        var contract = Bid.NoTrumpsBid(2);
        Assert.That(Bid.NextLevelForSuit(Suit.Spades, contract), Is.EqualTo(3));
        Assert.That(Bid.NextLevelForSuit(Suit.Clubs, contract), Is.EqualTo(3));
    }

    // =============================================
    // NextLevelForNoTrumps
    // =============================================

    [Test]
    public void NextLevelForNoTrumps_NoContract_Returns1()
    {
        Assert.That(Bid.NextLevelForNoTrumps(null), Is.EqualTo(1));
    }

    [Test]
    public void NextLevelForNoTrumps_AfterSuitBid_ReturnsSameLevel()
    {
        // NT outranks all suits at the same level
        var contract = Bid.SuitBid(2, Suit.Spades);
        Assert.That(Bid.NextLevelForNoTrumps(contract), Is.EqualTo(2));
    }

    [Test]
    public void NextLevelForNoTrumps_AfterNT_ReturnsNextLevel()
    {
        var contract = Bid.NoTrumpsBid(1);
        Assert.That(Bid.NextLevelForNoTrumps(contract), Is.EqualTo(2));
    }
}
