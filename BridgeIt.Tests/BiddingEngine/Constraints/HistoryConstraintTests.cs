using BridgeIt.Core.BiddingEngine.Constraints;

namespace BridgeIt.Tests.BiddingEngine.Constraints;

[TestFixture]
public class HistoryConstraintTests
{
    [Test]
    public void HistoryPattern_Wildcard_AlwaysTrue()
    {
        var constraint = new HistoryPatternConstraint(new List<string> { "*" });
        var ctx = TestHelper.CreateContext(); // Empty or populated history doesn't matter
        Assert.That(constraint.IsMet(ctx));
    }

    [Test]
    public void HistoryPattern_PassStar_EvaluatesCorrectly()
    {
        var constraint = new HistoryPatternConstraint(new List<string> { "Pass*" });

        // Case 1: Dealer (Empty History) -> True
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new string[] { })));

        // Case 2: Two Passes -> True
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] { "Pass", "Pass" })));

        // Case 3: Bid exists -> False
        Assert.That(!constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] { "1H", "Pass" })));
    }

    [Test]
    public void HistoryPattern_ExactMatch_EvaluatesCorrectly()
    {
        var constraint = new HistoryPatternConstraint(new List<string> { "Pass*", "1NT", "Pass", "2H", "Pass", "3NT" });
    
        // Match
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] {  "1NT", "Pass", "2H", "Pass", "3NT" })));
    
        // Mismatch Content
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] { "Pass", "Pass","Pass", "1NT", "Pass", "2H", "Pass", "3NT" })));
    
        // Mismatch Length
        Assert.That(!constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] {"1NT", "Pass", "2H", "Pass", "3NT", "Pass" })));
    }
    
    [Test]
    public void HistoryPattern_ExactMatchWithWildCard_EvaluatesCorrectly()
    {
        var constraint = new HistoryPatternConstraint(new List<string> { "Pass*", "1NT", "*", "2H" });
    
        // Match
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] {  "1NT", "Pass", "2H" })));
    
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] { "Pass", "Pass","Pass", "1NT", "Pass", "2H" })));
    
        // Mismatch Length
        Assert.That(constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] { "Pass", "Pass","Pass", "1NT", "2D", "2H" })));

        Assert.That(!constraint.IsMet(TestHelper.CreateContext(historyStrs: new[] {"1NT", "Pass", "2H", "Pass", "3NT", "Pass" })));
    }
    
}