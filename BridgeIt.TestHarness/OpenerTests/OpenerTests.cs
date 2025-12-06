using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.TestHarness.Setup;
using Moq;
using NUnit.Framework;

namespace BridgeIt.TestHarness.DebugTests;

[TestFixture]
public class OpenerTests
{
    [Test]
    public void SimpleRulesTest()
    {
        var basicAcol = "../../../../BridgeIt.CLI/BiddingRules/Opener/Acol-Basic_Openings.yaml";
        var env = TestBridgeEnvironment.Create().WithSpecificRules(basicAcol);

        var ctx = new Mock<BiddingContext>();
        
        var result = env.Engine.ChooseBid(ctx.Object);
        
        
    }   


    
    
}