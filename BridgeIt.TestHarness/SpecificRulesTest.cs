using BridgeIt.TestHarness.DealerIntegrationTests;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness;

[TestFixture]
public class SpecificRulesTest
{
    [Test]
    public void Check1NTOpener()
    {
        var basicAcol = "BridgeIt.CLI/BiddingRules/Opener/Acol-Basic_Openings.yaml";
        var environment = TestBridgeEnvironment.Create()
            .WithSpecificRules(basicAcol);
        
        var dealer = new Dealer.Deal.Dealer();
        var hands = dealer.GenerateConstrainedDeal(HandSpecifications.Open1NT, HandSpecifications.WeakPass);
    }
    
}