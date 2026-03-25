using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class PlayerKnowledgeTests
{
    [Test]
    public void Defaults_HcpRange_Is0To37()
    {
        var pk = new PlayerKnowledge();
        Assert.That(pk.HcpMin, Is.EqualTo(0));
        Assert.That(pk.HcpMax, Is.EqualTo(37));
    }

    [Test]
    public void Defaults_ShapeRange_Is0To13()
    {
        var pk = new PlayerKnowledge();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            Assert.That(pk.MinShape[suit], Is.EqualTo(0));
            Assert.That(pk.MaxShape[suit], Is.EqualTo(13));
        }
    }

    [Test]
    public void Defaults_NotBalanced()
    {
        var pk = new PlayerKnowledge();
        Assert.That(pk.IsBalanced, Is.False);
    }

    [Test]
    public void Defaults_NoDeniedSuits()
    {
        var pk = new PlayerKnowledge();
        Assert.That(pk.DeniedSuits, Is.Empty);
    }

    [Test]
    public void HasMinimumInSuit_ReturnsTrueWhenMet()
    {
        var pk = new PlayerKnowledge();
        pk.MinShape[Suit.Hearts] = 5;
        Assert.That(pk.HasMinimumInSuit(Suit.Hearts, 4), Is.True);
        Assert.That(pk.HasMinimumInSuit(Suit.Hearts, 5), Is.True);
    }

    [Test]
    public void HasMinimumInSuit_ReturnsFalseWhenNotMet()
    {
        var pk = new PlayerKnowledge();
        pk.MinShape[Suit.Hearts] = 3;
        Assert.That(pk.HasMinimumInSuit(Suit.Hearts, 4), Is.False);
    }

    [Test]
    public void CouldHaveInSuit_ReturnsTrueWhenPossible()
    {
        var pk = new PlayerKnowledge();
        pk.MaxShape[Suit.Spades] = 5;
        Assert.That(pk.CouldHaveInSuit(Suit.Spades, 5), Is.True);
        Assert.That(pk.CouldHaveInSuit(Suit.Spades, 4), Is.True);
    }

    [Test]
    public void CouldHaveInSuit_ReturnsFalseWhenImpossible()
    {
        var pk = new PlayerKnowledge();
        pk.MaxShape[Suit.Spades] = 3;
        Assert.That(pk.CouldHaveInSuit(Suit.Spades, 4), Is.False);
    }
}
