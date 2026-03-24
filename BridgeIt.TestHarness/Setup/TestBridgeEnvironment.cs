using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.BiddingEngine.Conventions;
using BridgeIt.Core.BiddingEngine.Rules.Knowledge;
using BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;
using BridgeIt.Core.BiddingEngine.Rules.Openings;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Players;
using Microsoft.Extensions.Logging;


namespace BridgeIt.TestHarness.Setup;

public class TestBridgeEnvironment
{
    public IServiceProvider Provider { get; private set; }
    public BiddingEngine Engine { get; private set; }
    public BiddingTable Table { get; private set; }
    public Dictionary<Seat, IPlayer> Players { get; private set; }

    // Builder Pattern for flexibility
    public static TestBridgeEnvironment Create()
    {
        var env = new TestBridgeEnvironment();
        env.Initialize();
        return env;
    }

    private void Initialize()
    {
        var services = new ServiceCollection();
        
        // 1. Core Dependencies (Factories, etc.)
        services.AddBridgeItCore(); 
        
        // 2. Register Observers
        services.AddSingleton<IBiddingObserver, ConsoleBiddingObserver>();
        
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug); // Set to Debug to see your detailed logs
        });

        Provider = services.BuildServiceProvider();
    }
    
    public TestBridgeEnvironment WithAllRules()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "BiddingRules");
        
        //var loader = Provider.GetRequiredService<YamlRuleLoader>();
        //var rules = loader.LoadRulesFromDirectory(fullPath).ToList();
        
        var rules = new List<IBiddingRule>();
        
        // Opening rules
        rules.Add(new WeakOpeningRule(reservedBids: [Bid.SuitBid(2, Suit.Clubs)]));
        rules.Add(new Acol1SuitOpeningRule());
        rules.Add(new Acol1NTOpeningRule());
        rules.Add(new Acol2NTOpeningRule());
        rules.Add(new AcolStrongOpening());
        
        rules.Add(new StandardTransfer(NTConventionContexts.After1NT));
        rules.Add(new StandardStayman(NTConventionContexts.After1NT));
        rules.Add(new AcolNTRaiseOver1NT());

        rules.Add(new StandardTransfer(NTConventionContexts.After2NT));
        rules.Add(new StandardStayman(NTConventionContexts.After2NT));

        rules.Add(new StandardTransfer(NTConventionContexts.After2C2D2NT));
        rules.Add(new StandardStayman(NTConventionContexts.After2C2D2NT));

        rules.Add(new AcolJacoby2NTOver1Major());
        rules.Add(new AcolRaiseMajorOver1Suit());
        rules.Add(new AcolRaiseMinorOver1Suit());
        rules.Add(new AcolNewSuitOver1Suit());
        rules.Add(new Acol1NTResponseTo1Suit());

        rules.Add(new CompleteTransfer(NTConventionContexts.After1NT));
        rules.Add(new CompleteTransfer(NTConventionContexts.After2NT));
        rules.Add(new CompleteTransfer(NTConventionContexts.After2C2D2NT));

        rules.Add(new StaymanResponse(NTConventionContexts.After1NT));
        rules.Add(new StaymanResponse(NTConventionContexts.After2NT));
        rules.Add(new StaymanResponse(NTConventionContexts.After2C2D2NT));

        rules.Add(new AcolResponderAfterStayman(NTConventionContexts.After1NT));
        rules.Add(new AcolResponderAfterStayman(NTConventionContexts.After2NT));
        rules.Add(new AcolResponderAfterStayman(NTConventionContexts.After2C2D2NT));

        // Opener rebid rules
        rules.Add(new AcolOpenerAfterNTInvite());
        rules.Add(new AcolOpenerAfterMajorRaise());
        rules.Add(new AcolRebidBalanced());
        rules.Add(new AcolRebidNewSuit());
        rules.Add(new AcolRebidRaiseSuit());
        rules.Add(new AcolRebidOwnSuit());

        // Responder rebids (round 2)
        rules.Add(new AcolResponderAfterOpenerRaisedSuit());
        rules.Add(new AcolResponderAfterOpener1NTRebid());
        rules.Add(new AcolResponderAfterOpener2NTRebid());
        rules.Add(new AcolResponderAfterOpenerRebidOwnSuit());
        rules.Add(new AcolResponderAfterOpenerNewSuit());

        // Knowledge-based catch-all rules
        rules.Add(new KnowledgeBidGameInSuit());
        rules.Add(new KnowledgeBidGameInNT());
        rules.Add(new KnowledgeInviteInSuit());
        rules.Add(new KnowledgeInviteInNT());
        rules.Add(new KnowledgeSignOffInFit());
        rules.Add(new KnowledgeSignOff());

        var observer = Provider.GetRequiredService<IEngineObserver>();
        var logger = Provider.GetRequiredService<ILogger<BiddingEngine>>();
        
        Engine = new BiddingEngine(rules, logger, observer);
        
        var robotPlayer = new RobotPlayer(Engine, Provider.GetRequiredService<IRuleLookupService>());

        Players = new Dictionary<Seat, IPlayer>()
        {
            { Seat.North, robotPlayer },
            { Seat.East, robotPlayer },
            { Seat.South, robotPlayer },
            { Seat.West, robotPlayer }
        };
        RebuildTable();
        return this;
    }

    private void RebuildTable()
    {
        Table = Provider.GetRequiredService<BiddingTable>();
    }
}