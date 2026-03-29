using System.Collections.Concurrent;
using BridgeIt.Analysis.Models;
using BridgeIt.Analysis.Parsers;
using BridgeIt.Api.Models;
using BridgeIt.Api.Services;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Players;
using BridgeIt.Dds;
using BridgeIt.Dds.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Api.Controllers;

[ApiController]
[Route("api/match")]
public class MatchAnalysisController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, ParsedMatch> Matches = new();

    private readonly IEnumerable<IBiddingRule> _rules;
    private readonly IRuleLookupService _ruleLookupService;
    private readonly IDdsService _ddsService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly PartnershipSimulationService _simulationService;

    public MatchAnalysisController(
        IEnumerable<IBiddingRule> rules,
        IRuleLookupService ruleLookupService,
        IDdsService ddsService,
        ILoggerFactory loggerFactory,
        PartnershipSimulationService simulationService)
    {
        _rules = rules;
        _ruleLookupService = ruleLookupService;
        _ddsService = ddsService;
        _loggerFactory = loggerFactory;
        _simulationService = simulationService;
    }

    [HttpGet("{matchId}")]
    public ActionResult<MatchDto> GetMatch(string matchId)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var boardDtos = match.Boards.Select(b => ToBoardDto(b, match)).ToList();
        return Ok(new MatchDto(match.MatchId, match.Filename, match.Boards.Count, boardDtos));
    }

    [HttpPost("upload")]
    public async Task<ActionResult<MatchDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        var parser = new PbnParser();
        var boards = parser.ParseString(content).ToList();

        if (boards.Count == 0)
            return BadRequest("No boards found in PBN file");

        var matchId = Guid.NewGuid().ToString("N")[..8];
        var parsedMatch = new ParsedMatch(matchId, file.FileName, boards);
        Matches[matchId] = parsedMatch;

        var boardDtos = boards.Select(b => ToBoardDto(b, parsedMatch)).ToList();

        return Ok(new MatchDto(matchId, file.FileName, boards.Count, boardDtos));
    }

    [HttpGet("{matchId}/board/{boardNumber}/engine-auction")]
    public async Task<ActionResult<EngineAuctionDto>> GetEngineAuction(string matchId, string boardNumber)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var board = match.Boards.FirstOrDefault(b => b.BoardNumber == boardNumber);
        if (board == null)
            return NotFound("Board not found");

        if (!IsCompleteDeal(board.Hands))
            return BadRequest("Board does not have complete hands (need 4 hands of 13 cards each)");

        try
        {
            var result = await RunEngineAuction(board.Hands, board.Dealer, board.Vulnerability);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Engine auction failed for board {boardNumber}: {ex}");
            return StatusCode(500, $"Engine auction failed: {ex.Message}");
        }
    }

    [HttpGet("{matchId}/board/{boardNumber}")]
    public ActionResult<BoardDto> GetBoard(string matchId, string boardNumber)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var board = match.Boards.FirstOrDefault(b => b.BoardNumber == boardNumber);
        if (board == null)
            return NotFound("Board not found");

        return Ok(ToBoardDto(board, match));
    }

    [HttpGet("{matchId}/board/{boardNumber}/engine-auction-detail")]
    public async Task<ActionResult<EngineAuctionDetailDto>> GetEngineAuctionDetail(string matchId, string boardNumber)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var board = match.Boards.FirstOrDefault(b => b.BoardNumber == boardNumber);
        if (board == null)
            return NotFound("Board not found");

        if (!IsCompleteDeal(board.Hands))
            return BadRequest("Board does not have complete hands (need 4 hands of 13 cards each)");

        try
        {
            var result = await RunEngineAuctionDetail(board.Hands, board.Dealer, board.Vulnerability);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Engine auction detail failed for board {boardNumber}: {ex}");
            return StatusCode(500, $"Engine auction failed: {ex.Message}");
        }
    }

    [HttpGet("{matchId}/board/{boardNumber}/dds")]
    public ActionResult GetDds(string matchId, string boardNumber)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var board = match.Boards.FirstOrDefault(b => b.BoardNumber == boardNumber);
        if (board == null)
            return NotFound("Board not found");

        if (!IsCompleteDeal(board.Hands))
            return BadRequest("Board does not have complete hands (need 4 hands of 13 cards each)");

        try
        {
            var analysis = _ddsService.Analyse(board.Hands, board.Dealer);

            var nsMax = MaxMakeableCalculator.ForSide(analysis.TrickTable, isNorthSouth: true);
            var ewMax = MaxMakeableCalculator.ForSide(analysis.TrickTable, isNorthSouth: false);

            MaxMakeableDto ToDto(MaxMakeableContract? m) =>
                m != null ? new MaxMakeableDto(m.Strain, m.Level, m.Declarer, m.Tricks)
                          : new MaxMakeableDto(null, null, null, null);

            return Ok(new
            {
                analysis.TrickTable,
                analysis.Par,
                MaxMakeable = new MaxMakeableAnalysisDto(ToDto(nsMax), ToDto(ewMax)),
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = $"DDS analysis failed: {ex.Message}" });
        }
    }

    [HttpGet("{matchId}/board/{boardNumber}/simulate-partnership")]
    public async Task<ActionResult<PartnershipSimulationDto>> SimulatePartnership(string matchId, string boardNumber)
    {
        if (!Matches.TryGetValue(matchId, out var match))
            return NotFound("Match not found");

        var board = match.Boards.FirstOrDefault(b => b.BoardNumber == boardNumber);
        if (board == null)
            return NotFound("Board not found");

        if (!IsCompleteDeal(board.Hands))
            return BadRequest("Board does not have complete hands (need 4 hands of 13 cards each)");

        var partnership = PbnParser.IdentifyPartnership(board.PlayerNames);
        if (partnership == null)
        {
            return Ok(new PartnershipSimulationDto(
                OurSeat1: null, OurSeat2: null,
                Bids: new(), FinalContract: null, Declarer: null,
                SimulatedScore: null, SimulatedTricks: null, SimulatedResultDisplay: null,
                DebugLogs: new(), Conflicts: new(),
                IdentificationFailureReason: "Could not identify partnership — no known player name found in board metadata"));
        }

        try
        {
            var result = await _simulationService.Simulate(
                board.Hands, board.ActualAuction, board.Dealer, board.Vulnerability, partnership.Value);

            // Compute contract and score from simulated auction
            var lastNonPass = result.AuctionHistory.Bids
                .LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps);

            string? finalContract = null;
            string? declarer = null;
            int? simScore = null;
            int? simTricks = null;
            string? simResultDisplay = null;

            if (lastNonPass != null)
            {
                var declarerSeat = FindDeclarer(result.AuctionHistory, lastNonPass);
                var contractStr = lastNonPass.Bid.ToString();

                var isDoubled = false;
                var isRedoubled = false;
                for (var i = result.AuctionHistory.Bids.Count - 1; i >= 0; i--)
                {
                    var b = result.AuctionHistory.Bids[i].Bid;
                    if (b.Type == BidType.Suit || b.Type == BidType.NoTrumps) break;
                    if (b.Type == BidType.Redouble) { isRedoubled = true; break; }
                    if (b.Type == BidType.Double) { isDoubled = true; break; }
                }
                finalContract = contractStr + (isRedoubled ? "XX" : isDoubled ? "X" : "");
                declarer = declarerSeat.ToString();

                try
                {
                    var dds = _ddsService.Analyse(board.Hands, board.Dealer);
                    var seatKey = declarerSeat switch
                    {
                        Seat.North => "N", Seat.East => "E", Seat.South => "S", _ => "W"
                    };
                    var strainKey = lastNonPass.Bid.Type == BidType.NoTrumps ? "notrump" :
                        lastNonPass.Bid.Suit!.Value switch
                        {
                            Suit.Clubs => "clubs", Suit.Diamonds => "diamonds",
                            Suit.Hearts => "hearts", _ => "spades"
                        };

                    if (dds.TrickTable.Tricks.TryGetValue(seatKey, out var seatTricks) &&
                        seatTricks.TryGetValue(strainKey, out var ddsTricks))
                    {
                        simTricks = ddsTricks;
                        var declarerVul = IsDeclarerVulnerable(declarerSeat.ToString(), board.Vulnerability);
                        var rawScore = BridgeScorer.ComputeScore(finalContract, ddsTricks, declarerVul);
                        if (rawScore != null && IsEwDeclarer(declarerSeat.ToString()))
                            rawScore = -rawScore;
                        simScore = rawScore;

                        var level = lastNonPass.Bid.Level;
                        var diff = ddsTricks - (level + 6);
                        simResultDisplay = diff == 0 ? $"{finalContract} =" :
                            diff > 0 ? $"{finalContract} + {diff}" :
                            $"{finalContract} - {Math.Abs(diff)}";
                    }
                }
                catch { /* DDS not critical */ }
            }

            // Only include debug logs for our seats
            var ourSeatNames = new HashSet<string>
                { partnership.Value.Seat1.ToString(), partnership.Value.Seat2.ToString() };
            var filteredLogs = result.DebugLogs
                .Where(l => ourSeatNames.Contains(l.Seat))
                .Select(ToLogDto)
                .ToList();

            return Ok(new PartnershipSimulationDto(
                OurSeat1: partnership.Value.Seat1.ToString(),
                OurSeat2: partnership.Value.Seat2.ToString(),
                Bids: result.Bids,
                FinalContract: finalContract,
                Declarer: declarer,
                SimulatedScore: simScore,
                SimulatedTricks: simTricks,
                SimulatedResultDisplay: simResultDisplay,
                DebugLogs: filteredLogs,
                Conflicts: result.Conflicts,
                IdentificationFailureReason: null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Partnership simulation failed for board {boardNumber}: {ex}");
            return StatusCode(500, $"Partnership simulation failed: {ex.Message}");
        }
    }

    private async Task<EngineAuctionDto> RunEngineAuction(
        Dictionary<Seat, Hand> hands, Seat dealer, Vulnerability vulnerability)
    {
        var observer = new CollectingEngineObserver();
        var logger = _loggerFactory.CreateLogger<BiddingEngine>();
        var engine = new BiddingEngine(_rules, logger, observer, new BidValidityChecker());

        var players = new Dictionary<Seat, IPlayer>();
        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
            players[seat] = new RobotPlayer(engine, _ruleLookupService);

        var noopObserver = new NoopBiddingObserver();
        var auctionRules = new StandardAuctionRules();
        var table = new BiddingTable(auctionRules, noopObserver);

        var history = await table.RunAuction(hands, players, dealer);

        var bids = new List<EngineBidDto>();
        for (int i = 0; i < history.Bids.Count; i++)
        {
            var auctionBid = history.Bids[i];
            var log = i < observer.Logs.Count ? observer.Logs[i] : null;

            string? ruleName = null;
            int? priority = null;
            string? explanation = null;

            if (log != null)
            {
                var winningRule = log.EvaluatedRules.FirstOrDefault(r => r.WasSelected);
                if (winningRule != null)
                {
                    ruleName = winningRule.RuleName;
                    priority = winningRule.Priority;
                    explanation = BuildExplanation(winningRule);
                }
            }

            bids.Add(new EngineBidDto(
                Seat: auctionBid.Seat.ToString(),
                Call: auctionBid.Bid.ToString(),
                RuleName: ruleName,
                Priority: priority,
                Explanation: explanation,
                IsAlerted: auctionBid.IsAlerted
            ));
        }

        // Determine final contract
        var lastNonPass = history.Bids
            .LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps);

        int? engineScore = null;
        int? engineTricks = null;
        string? engineResultDisplay = null;

        if (lastNonPass != null)
        {
            var declarerSeat = FindDeclarer(history, lastNonPass);
            var contractStr = lastNonPass.Bid.ToString();

            // Check if doubled/redoubled in the auction
            var isDoubled = false;
            var isRedoubled = false;
            for (var i = history.Bids.Count - 1; i >= 0; i--)
            {
                var b = history.Bids[i].Bid;
                if (b.Type == BidType.Suit || b.Type == BidType.NoTrumps) break;
                if (b.Type == BidType.Redouble) { isRedoubled = true; break; }
                if (b.Type == BidType.Double) { isDoubled = true; break; }
            }
            var fullContract = contractStr + (isRedoubled ? "XX" : isDoubled ? "X" : "");

            // Use DDS to get the tricks declarer can make
            try
            {
                var dds = _ddsService.Analyse(hands, dealer);
                var seatKey = declarerSeat switch
                {
                    Seat.North => "N", Seat.East => "E", Seat.South => "S", _ => "W"
                };
                var strainKey = lastNonPass.Bid.Type == BidType.NoTrumps ? "notrump" :
                    lastNonPass.Bid.Suit!.Value switch
                    {
                        Suit.Clubs => "clubs", Suit.Diamonds => "diamonds",
                        Suit.Hearts => "hearts", _ => "spades"
                    };

                if (dds.TrickTable.Tricks.TryGetValue(seatKey, out var seatTricks) &&
                    seatTricks.TryGetValue(strainKey, out var ddsTricks))
                {
                    engineTricks = ddsTricks;
                    var declarerVul = IsDeclarerVulnerable(declarerSeat.ToString(), vulnerability);
                    var rawEngineScore = BridgeScorer.ComputeScore(fullContract, ddsTricks, declarerVul);
                    // Normalise to N/S perspective (par is always N/S)
                    if (rawEngineScore != null && IsEwDeclarer(declarerSeat.ToString()))
                        rawEngineScore = -rawEngineScore;
                    engineScore = rawEngineScore;

                    var level = lastNonPass.Bid.Level;
                    var diff = ddsTricks - (level + 6);
                    engineResultDisplay = diff == 0 ? $"{fullContract} =" :
                        diff > 0 ? $"{fullContract} + {diff}" :
                        $"{fullContract} - {Math.Abs(diff)}";
                }
            }
            catch
            {
                // DDS not critical
            }

            return new EngineAuctionDto(
                Bids: bids,
                FinalContract: fullContract,
                Declarer: declarerSeat.ToString(),
                EngineScore: engineScore,
                EngineTricks: engineTricks,
                EngineResultDisplay: engineResultDisplay
            );
        }

        return new EngineAuctionDto(
            Bids: bids,
            FinalContract: null,
            Declarer: null,
            EngineScore: null,
            EngineTricks: null,
            EngineResultDisplay: null
        );
    }

    private async Task<EngineAuctionDetailDto> RunEngineAuctionDetail(
        Dictionary<Seat, Hand> hands, Seat dealer, Vulnerability vulnerability)
    {
        var observer = new CollectingEngineObserver();
        var logger = _loggerFactory.CreateLogger<BiddingEngine>();
        var engine = new BiddingEngine(_rules, logger, observer, new BidValidityChecker());

        var players = new Dictionary<Seat, IPlayer>();
        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
            players[seat] = new RobotPlayer(engine, _ruleLookupService);

        var noopObserver = new NoopBiddingObserver();
        var auctionRules = new StandardAuctionRules();
        var table = new BiddingTable(auctionRules, noopObserver);

        var history = await table.RunAuction(hands, players, dealer);

        var bids = new List<EngineBidDto>();
        for (int i = 0; i < history.Bids.Count; i++)
        {
            var auctionBid = history.Bids[i];
            var log = i < observer.Logs.Count ? observer.Logs[i] : null;

            string? ruleName = null;
            int? priority = null;
            string? explanation = null;

            if (log != null)
            {
                var winningRule = log.EvaluatedRules.FirstOrDefault(r => r.WasSelected);
                if (winningRule != null)
                {
                    ruleName = winningRule.RuleName;
                    priority = winningRule.Priority;
                    explanation = BuildExplanation(winningRule);
                }
            }

            bids.Add(new EngineBidDto(
                Seat: auctionBid.Seat.ToString(),
                Call: auctionBid.Bid.ToString(),
                RuleName: ruleName,
                Priority: priority,
                Explanation: explanation,
                IsAlerted: auctionBid.IsAlerted
            ));
        }

        // Determine final contract
        var lastNonPass = history.Bids
            .LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps);

        int? engineScore = null;
        int? engineTricks = null;
        string? engineResultDisplay = null;

        if (lastNonPass != null)
        {
            var declarerSeat = FindDeclarer(history, lastNonPass);
            var contractStr = lastNonPass.Bid.ToString();

            var isDoubled = false;
            var isRedoubled = false;
            for (var i = history.Bids.Count - 1; i >= 0; i--)
            {
                var b = history.Bids[i].Bid;
                if (b.Type == BidType.Suit || b.Type == BidType.NoTrumps) break;
                if (b.Type == BidType.Redouble) { isRedoubled = true; break; }
                if (b.Type == BidType.Double) { isDoubled = true; break; }
            }
            var fullContract = contractStr + (isRedoubled ? "XX" : isDoubled ? "X" : "");

            try
            {
                var dds = _ddsService.Analyse(hands, dealer);
                var seatKey = declarerSeat switch
                {
                    Seat.North => "N", Seat.East => "E", Seat.South => "S", _ => "W"
                };
                var strainKey = lastNonPass.Bid.Type == BidType.NoTrumps ? "notrump" :
                    lastNonPass.Bid.Suit!.Value switch
                    {
                        Suit.Clubs => "clubs", Suit.Diamonds => "diamonds",
                        Suit.Hearts => "hearts", _ => "spades"
                    };

                if (dds.TrickTable.Tricks.TryGetValue(seatKey, out var seatTricks) &&
                    seatTricks.TryGetValue(strainKey, out var ddsTricks))
                {
                    engineTricks = ddsTricks;
                    var declarerVul = IsDeclarerVulnerable(declarerSeat.ToString(), vulnerability);
                    var rawEngineScore = BridgeScorer.ComputeScore(fullContract, ddsTricks, declarerVul);
                    if (rawEngineScore != null && IsEwDeclarer(declarerSeat.ToString()))
                        rawEngineScore = -rawEngineScore;
                    engineScore = rawEngineScore;

                    var level = lastNonPass.Bid.Level;
                    var diff = ddsTricks - (level + 6);
                    engineResultDisplay = diff == 0 ? $"{fullContract} =" :
                        diff > 0 ? $"{fullContract} + {diff}" :
                        $"{fullContract} - {Math.Abs(diff)}";
                }
            }
            catch { /* DDS not critical */ }

            return new EngineAuctionDetailDto(
                Bids: bids,
                FinalContract: fullContract,
                Declarer: declarerSeat.ToString(),
                EngineScore: engineScore,
                EngineTricks: engineTricks,
                EngineResultDisplay: engineResultDisplay,
                DebugLogs: observer.Logs.Select(ToLogDto).ToList()
            );
        }

        return new EngineAuctionDetailDto(
            Bids: bids,
            FinalContract: null,
            Declarer: null,
            EngineScore: null,
            EngineTricks: null,
            EngineResultDisplay: null,
            DebugLogs: observer.Logs.Select(ToLogDto).ToList()
        );
    }

    private static RuleEvaluationLogDto ToLogDto(RuleEvaluationLog log)
    {
        return new RuleEvaluationLogDto(
            Seat: log.Seat,
            Hand: log.Hand,
            Hcp: log.Hcp,
            IsBalanced: log.IsBalanced,
            Shape: log.Shape,
            SeatRole: log.SeatRole,
            AuctionPhase: log.AuctionPhase,
            BiddingRound: log.BiddingRound,
            PartnerLastBid: log.PartnerLastBid,
            TableKnowledge: log.TableKnowledge.ToDictionary(
                kv => kv.Key,
                kv => new TableKnowledgeEntryDto(
                    kv.Value.HcpMin, kv.Value.HcpMax, kv.Value.IsBalanced,
                    kv.Value.MinShape, kv.Value.MaxShape)),
            WinningBid: log.WinningBid,
            EvaluatedRules: log.EvaluatedRules.Select(ToRuleDto).ToList()
        );
    }

    private static RuleEvaluationDto ToRuleDto(RuleEvaluation rule)
    {
        return new RuleEvaluationDto(
            RuleName: rule.RuleName,
            Priority: rule.Priority,
            IsApplicableToAuction: rule.IsApplicableToAuction,
            IsHandApplicable: rule.IsHandApplicable,
            ProducedBid: rule.ProducedBid,
            WasSelected: rule.WasSelected,
            WasInvalidBid: rule.WasInvalidBid,
            ForwardConstraints: rule.ForwardConstraints?.Select(ToConstraintDto).ToList(),
            ConstraintResults: rule.ConstraintResults?.Select(r =>
                new ConstraintEvalResultDto(ToConstraintDto(r.Constraint), r.Passed, r.ActualValue)).ToList()
        );
    }

    private static ConstraintDetailDto ToConstraintDto(ConstraintDetail c)
    {
        return new ConstraintDetailDto(
            Type: c.Type,
            Description: c.Description,
            Min: c.Min,
            Max: c.Max,
            Suit: c.Suit,
            Children: c.Children?.Select(ToConstraintDto).ToList()
        );
    }

    private static Seat FindDeclarer(AuctionHistory history, AuctionBid contractBid)
    {
        // Declarer = first player in the contracting partnership to bid the contract strain
        var partnerSeat = contractBid.Seat.GetPartner();
        var strain = contractBid.Bid.Suit;
        var bidType = contractBid.Bid.Type;

        foreach (var bid in history.Bids)
        {
            if (bid.Seat != contractBid.Seat && bid.Seat != partnerSeat) continue;
            if (bid.Bid.Type != bidType) continue;
            if (bidType == BidType.Suit && bid.Bid.Suit != strain) continue;
            return bid.Seat;
        }

        return contractBid.Seat;
    }

    private static string BuildExplanation(RuleEvaluation rule)
    {
        var parts = new List<string> { rule.RuleName };

        if (rule.ForwardConstraints != null)
        {
            foreach (var c in rule.ForwardConstraints)
            {
                if (!string.IsNullOrEmpty(c.Description))
                    parts.Add(c.Description);
            }
        }

        return string.Join(" — ", parts);
    }

    private BoardDto ToBoardDto(PbnBoard board, ParsedMatch match)
    {
        var hands = new Dictionary<string, HandDto>();
        foreach (var (seat, hand) in board.Hands)
        {
            var eval = HandEvaluator.Evaluate(hand);
            hands[seat.ToString()] = new HandDto(
                Display: hand.ToString(),
                Hcp: eval.Hcp,
                Suits: BuildSuitHoldings(hand)
            );
        }

        var playerNames = board.PlayerNames.ToDictionary(
            kv => kv.Key.ToString(),
            kv => kv.Value);

        var partnership = PbnParser.IdentifyPartnership(board.PlayerNames);

        // Build played result display and compute score
        string? playedResultDisplay = null;
        int? playedScore = null;
        if (board.Contract != null && board.TricksTaken != null)
        {
            var contractLevel = ParseContractLevel(board.Contract);
            if (contractLevel > 0)
            {
                var requiredTricks = contractLevel + 6;
                var diff = board.TricksTaken.Value - requiredTricks;
                playedResultDisplay = diff == 0 ? $"{board.Contract} =" :
                    diff > 0 ? $"{board.Contract} + {diff}" :
                    $"{board.Contract} - {Math.Abs(diff)}";
            }

            // Compute score from contract + vulnerability + tricks
            // BridgeScorer returns declarer-positive; normalise to N/S perspective
            // so it's directly comparable to par score (which is always N/S perspective)
            var declarerVul = IsDeclarerVulnerable(board.DeclarerSeat, board.Vulnerability);
            var rawScore = BridgeScorer.ComputeScore(board.Contract, board.TricksTaken, declarerVul);
            if (rawScore != null && IsEwDeclarer(board.DeclarerSeat))
                rawScore = -rawScore;
            playedScore = rawScore;
        }
        // Fall back to PBN [Score] tag if our computation failed
        // PBN [Score] is typically "NS xxx" or just a number from N/S perspective
        if (playedScore == null && board.Score != null)
        {
            playedScore = ParseScore(board.Score);
        }

        // Par is NOT computed here — it's fetched on-demand via /dds endpoint
        // to avoid a native DDS crash on one board killing the entire upload
        return new BoardDto(
            BoardNumber: board.BoardNumber,
            Vulnerability: board.Vulnerability.ToString(),
            Dealer: board.Dealer.ToString(),
            Hands: hands,
            PlayerNames: playerNames,
            DdsTable: null,
            ParContract: null,
            ParScore: null,
            ParDeclarer: null,
            PlayedAuction: board.ActualAuction,
            PlayedContract: board.Contract,
            PlayedDeclarer: board.DeclarerSeat,
            PlayedTricks: board.TricksTaken,
            PlayedResultDisplay: playedResultDisplay,
            PlayedScore: playedScore,
            OurSeat1: partnership?.Seat1.ToString(),
            OurSeat2: partnership?.Seat2.ToString()
        );
    }

    private static List<SuitHoldingDto> BuildSuitHoldings(Hand hand)
    {
        var suits = new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
        var symbols = new Dictionary<Suit, string>
        {
            [Suit.Spades] = "\u2660", [Suit.Hearts] = "\u2665",
            [Suit.Diamonds] = "\u2666", [Suit.Clubs] = "\u2663"
        };
        var colors = new Dictionary<Suit, string>
        {
            [Suit.Spades] = "black", [Suit.Hearts] = "red",
            [Suit.Diamonds] = "red", [Suit.Clubs] = "black"
        };

        return suits.Select(suit =>
        {
            var cards = hand.Cards
                .Where(c => c.Suit == suit)
                .OrderByDescending(c => c.Rank)
                .Select(c => RankToChar(c.Rank).ToString())
                .ToArray();

            return new SuitHoldingDto(
                Suit: suit.ToString(),
                Symbol: symbols[suit],
                Cards: string.Join("", cards),
                Color: colors[suit]
            );
        }).ToList();
    }

    private static char RankToChar(Rank rank) => rank switch
    {
        Rank.Ace => 'A',
        Rank.King => 'K',
        Rank.Queen => 'Q',
        Rank.Jack => 'J',
        Rank.Ten => 'T',
        _ => (char)('0' + (int)rank)
    };

    private static string VulnerabilityToKey(Vulnerability v) => v switch
    {
        Vulnerability.None => "none",
        Vulnerability.NS => "nsVul",
        Vulnerability.EW => "ewVul",
        Vulnerability.Both => "bothVul",
        _ => "none"
    };

    /// <summary>
    /// Validates that a deal has exactly 4 hands with exactly 13 cards each (52 total).
    /// </summary>
    private static bool IsCompleteDeal(Dictionary<Seat, Hand> hands)
    {
        if (hands.Count != 4) return false;
        foreach (var hand in hands.Values)
        {
            if (hand.Cards.Count != 13) return false;
        }
        return true;
    }

    private static bool IsDeclarerVulnerable(string? declarerSeat, Vulnerability vulnerability)
    {
        if (declarerSeat == null) return false;
        var isNS = declarerSeat is "N" or "S" or "North" or "South";
        return vulnerability switch
        {
            Vulnerability.Both => true,
            Vulnerability.NS => isNS,
            Vulnerability.EW => !isNS,
            _ => false
        };
    }

    private static bool IsEwDeclarer(string? declarerSeat)
        => declarerSeat is "E" or "W" or "East" or "West";

    private static int ParseContractLevel(string contract)
    {
        // Contract formats: "2D", "4HX", "3NTXX", "Pass" etc.
        if (string.IsNullOrEmpty(contract) || contract == "Pass") return 0;
        return char.IsDigit(contract[0]) ? contract[0] - '0' : 0;
    }

    private static int? ParseScore(string score)
    {
        // PBN Score formats: "NS -100", "EW 620", "-100", "620", "NS 0"
        if (string.IsNullOrWhiteSpace(score)) return null;
        var parts = score.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Take the last part which should be the numeric score
        var numStr = parts[^1];
        return int.TryParse(numStr, out var val) ? val : null;
    }

    private class NoopBiddingObserver : IBiddingObserver
    {
        public Task OnBid(AuctionHistory auctionHistory) => Task.CompletedTask;
    }

    private record ParsedMatch(string MatchId, string Filename, List<PbnBoard> Boards);
}
