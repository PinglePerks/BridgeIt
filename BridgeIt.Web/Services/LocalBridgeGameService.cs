using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Services;
using BridgeIt.Core.Gameplay.Table;

namespace BridgeIt.Web.Services;

public class LocalBridgeGameService : IBridgeGameService
{
    private readonly BiddingEngine _engine;
    private readonly IAuctionRules _rules;
    private readonly ISeatRotationService _rotation;
    
    private Dictionary<Seat, Hand> _currentDeal;
    private AuctionHistory _auctionHistory;
    private Seat _dealer;

    public LocalBridgeGameService(
        BiddingEngine engine, 
        IAuctionRules rules, 
        ISeatRotationService rotation)
    {
        _engine = engine;
        _rules = rules;
        _rotation = rotation;
    }

    public Task<Dictionary<Seat, Hand>> NewGameAsync()
    {
        // Simple random deal logic (reuse your Dealer logic if you port it to a library)
        var deck = new Deck();
        deck.Shuffle();
        
        _currentDeal = new Dictionary<Seat, Hand>
        {
            { Seat.North, new Hand(deck.Cards.Take(13)) },
            { Seat.East,  new Hand(deck.Cards.Skip(13).Take(13)) },
            { Seat.South, new Hand(deck.Cards.Skip(26).Take(13)) },
            { Seat.West,  new Hand(deck.Cards.Skip(39).Take(13)) }
        };

        _dealer = Seat.North; // Fixed for now, can rotate
        _auctionHistory = new AuctionHistory(new List<BiddingDecision>(), _dealer);
        
        return Task.FromResult(_currentDeal);
    }

    public Task<BiddingDecision> HumanBidAsync(Bid bid)
    {
        // For a human, we just validate (optional) and add to history
        var decision = new BiddingDecision(bid, "Human Player", "human_move");
        _auctionHistory.Add(decision);
        return Task.FromResult(decision);
    }

    public Task<BiddingDecision> BotBidAsync(Seat seat)
    {
        // Setup context for the bot
        var hand = _currentDeal[seat];
        
        // REUSE your core logic here!
        var ctx = new BiddingContext(
            hand: hand,
            auctionHistory: _auctionHistory,
            seat: seat,
            vulnerability: Vulnerability.None,
            handEvaluation: HandEvaluator.Evaluate(hand),
            partnershipKnowledge: AuctionEvaluator.AnalyzeKnowledge(_auctionHistory, seat, hand),
            auctionEvaluation: AuctionEvaluator.Evaluate(_auctionHistory, seat)
        );

        var decision = _engine.ChooseBid(ctx);
        _auctionHistory.Add(decision);
        
        return Task.FromResult(decision);
    }

    public List<BiddingDecision> GetAuctionHistory() => _auctionHistory.Bids.ToList();
    public Seat GetDealer() => _dealer;
}