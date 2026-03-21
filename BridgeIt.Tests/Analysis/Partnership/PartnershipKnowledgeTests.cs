using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class PartnershipKnowledgeTests
{
    // =============================================
    // Default State
    // =============================================

    [Test]
    public void Defaults_HcpRange_Is0To40()
    {
        var pk = new PartnershipKnowledge();
        Assert.That(pk.PartnerHcpMin, Is.EqualTo(0));
        Assert.That(pk.PartnerHcpMax, Is.EqualTo(40));
    }

    [Test]
    public void Defaults_ShapeRange_Is0To13()
    {
        var pk = new PartnershipKnowledge();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            Assert.That(pk.PartnerMinShape[suit], Is.EqualTo(0));
            Assert.That(pk.PartnerMaxShape[suit], Is.EqualTo(13));
        }
    }

    [Test]
    public void Defaults_NotBalanced()
    {
        var pk = new PartnershipKnowledge();
        Assert.That(pk.PartnerIsBalanced, Is.False);
    }

    // =============================================
    // HasFit
    // =============================================

    [Test]
    [TestCase(4, 4, true, Description = "4+4=8, fit")]
    [TestCase(5, 3, true, Description = "5+3=8, fit")]
    [TestCase(4, 3, false, Description = "4+3=7, no fit")]
    [TestCase(0, 5, false, Description = "Unknown partner + 5 = no confirmed fit")]
    public void HasFit_ReturnsCorrectly(int partnerMin, int myLength, bool expected)
    {
        var pk = new PartnershipKnowledge();
        pk.PartnerMinShape[Suit.Hearts] = partnerMin;

        Assert.That(pk.HasFit(Suit.Hearts, myLength), Is.EqualTo(expected));
    }

    // =============================================
    // HasPossibleFit
    // =============================================

    [Test]
    [TestCase(5, 3, true, Description = "Max 5 + 3 = 8, possible")]
    [TestCase(4, 3, false, Description = "Max 4 + 3 = 7, not possible")]
    public void HasPossibleFit_UsesMaxShape(int partnerMax, int myLength, bool expected)
    {
        var pk = new PartnershipKnowledge();
        pk.PartnerMaxShape[Suit.Spades] = partnerMax;

        Assert.That(pk.HasPossibleFit(Suit.Spades, myLength), Is.EqualTo(expected));
    }

    // =============================================
    // CombinedHcpMin
    // =============================================

    [Test]
    [TestCase(12, 13, 25)]
    [TestCase(0, 10, 10)]
    [TestCase(20, 15, 35)]
    public void CombinedHcpMin_AddsCorrectly(int partnerMin, int myHcp, int expected)
    {
        var pk = new PartnershipKnowledge { PartnerHcpMin = partnerMin };
        Assert.That(pk.CombinedHcpMin(myHcp), Is.EqualTo(expected));
    }

    // =============================================
    // BestFitSuit
    // =============================================

    [Test]
    public void BestFitSuit_FindsFit()
    {
        var pk = new PartnershipKnowledge();
        pk.PartnerMinShape[Suit.Hearts] = 4;

        var myShape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 3 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 }
        };

        Assert.That(pk.BestFitSuit(myShape), Is.EqualTo(Suit.Hearts));
    }

    [Test]
    public void BestFitSuit_NoFit_ReturnsNull()
    {
        var pk = new PartnershipKnowledge();
        // All partner mins are 0 (default)

        var myShape = new Dictionary<Suit, int>
        {
            { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 }
        };

        Assert.That(pk.BestFitSuit(myShape), Is.Null);
    }

    // =============================================
    // PartnerDeniedMajor
    // =============================================

    [Test]
    public void PartnerDeniedMajor_WhenMaxShapeTooLow_ReturnsTrue()
    {
        var pk = new PartnershipKnowledge();
        pk.PartnerMaxShape[Suit.Hearts] = 3;
        pk.PartnerMaxShape[Suit.Spades] = 3;

        // I have 4 hearts, 4 spades — possible fit needs partner max >= 4
        // Hearts: max 3 + 4 = 7 < 8 → no possible fit
        // Spades: max 3 + 4 = 7 < 8 → no possible fit
        Assert.That(pk.PartnerDeniedMajor(4, 4), Is.True);
    }

    [Test]
    public void PartnerDeniedMajor_WhenFitPossible_ReturnsFalse()
    {
        var pk = new PartnershipKnowledge();
        pk.PartnerMaxShape[Suit.Hearts] = 5;
        pk.PartnerMaxShape[Suit.Spades] = 5;

        // Hearts: max 5 + 4 = 9 >= 8 → possible fit exists
        Assert.That(pk.PartnerDeniedMajor(4, 4), Is.False);
    }
}
