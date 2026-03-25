using System.Text.Json;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Conventions;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Knowledge;
using BridgeIt.Core.BiddingEngine.Rules.Openings;
using BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;
using BridgeIt.Core.BiddingEngine.Rules.Responder;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Systems.Config;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Systems;

/// <summary>
/// Loads a bidding system from a <see cref="BridgeSystemConfig"/>, instantiating
/// rule classes with the correct parameters and priorities.
///
/// Config sections without corresponding rule implementations are logged as warnings
/// and skipped. This allows the JSON files to be fully specified from day 1.
/// </summary>
public class BiddingSystemLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly ILogger<BiddingSystemLoader>? _logger;

    public BiddingSystemLoader(ILogger<BiddingSystemLoader>? logger = null)
    {
        _logger = logger;
    }

    public LoadedSystem LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    public LoadedSystem LoadFromJson(string json)
    {
        var config = JsonSerializer.Deserialize<BridgeSystemConfig>(json, JsonOpts)
                     ?? throw new InvalidOperationException("Failed to deserialise system config JSON");
        return Load(config);
    }

    public LoadedSystem Load(BridgeSystemConfig config)
    {
        var rules = new List<IBiddingRule>();
        var warnings = new List<string>();
        var priorities = config.Priorities ?? new Dictionary<string, int>();

        // ── Opening rules ──────────────────────────────────────────────
        if (config.NT1Opening is { } nt1)
        {
            rules.Add(new Acol1NTOpeningRule(
                nt1.MinHcp, nt1.MaxHcp, nt1.Level,
                GetPriority(priorities, "Acol1NTOpening", 20)));
        }

        if (config.NT2Opening is { } nt2)
        {
            rules.Add(new Acol2NTOpeningRule(
                nt2.MinHcp, nt2.MaxHcp, nt2.Level,
                GetPriority(priorities, "Acol2NTOpening", 20)));
        }

        if (config.SuitOpening is { } suit)
        {
            rules.Add(new Acol1SuitOpeningRule(
                suit.MinHcp, suit.MaxHcp, suit.MajorMinLength,
                GetPriority(priorities, "Acol1SuitOpening", 10)));
        }

        if (config.StrongOpening is { } strong)
        {
            var strongBid = ParseBid(strong.Bid);
            rules.Add(new AcolStrongOpening(
                strong.MinHcpUnbalanced, strong.MinHcpBalanced, strong.MaxHcp,
                strongBid?.Level ?? 2, strongBid?.Suit ?? Suit.Clubs,
                GetPriority(priorities, "AcolStrongOpening", 19)));
        }

        if (config.Preempts is { } preempts)
        {
            var reservedBids = (preempts.ReservedBids ?? new List<string>())
                .Select(ParseBid)
                .Where(b => b is not null)
                .Cast<Bid>()
                .ToArray();
            rules.Add(new WeakOpeningRule(
                reservedBids, preempts.MinHcp, preempts.MaxHcp,
                preempts.MinSuitLength3Level - 1, // minSuitLength for any preempt
                priority: GetPriority(priorities, "WeakOpening", 9)));
        }

        // ── NT Convention contexts ─────────────────────────────────────
        // Build NTConventionContexts from config for use by convention rules
        NTConventionContext? after1NT = null;
        NTConventionContext? after2NT = null;
        NTConventionContext? after2C2D2NT = null;

        if (config.NT1Opening is not null && config.ResponseTo1NT is not null)
        {
            var staymanMinHcp = config.ResponseTo1NT.Stayman?.MinHcp ?? 11;
            after1NT = new NTConventionContext
            {
                Name = "1NT",
                ConventionLevel = 2,
                StaymanHcpMin = staymanMinHcp,
                ResponderIsTriggered = a =>
                    a.BiddingRound == 1
                    && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(1)
                    && a.OpeningBid == Bid.NoTrumpsBid(1)
            };
        }

        if (config.NT2Opening is not null && config.ResponseTo2NT is not null)
        {
            var staymanMinHcp = config.ResponseTo2NT.Stayman?.MinHcp ?? 4;
            after2NT = new NTConventionContext
            {
                Name = "2NT",
                ConventionLevel = 3,
                StaymanHcpMin = staymanMinHcp,
                ResponderIsTriggered = a =>
                    a.BiddingRound == 1
                    && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(2)
                    && a.OpeningBid == Bid.NoTrumpsBid(2)
            };
        }

        // After 2C-2D-2NT sequence (strong opening → 2D response → 2NT rebid)
        if (config.StrongOpening is not null && config.NT2Opening is not null)
        {
            after2C2D2NT = new NTConventionContext
            {
                Name = "2C-2D-2NT",
                ConventionLevel = 3,
                StaymanHcpMin = 0,
                ResponderIsTriggered = a =>
                    a.BiddingRound == 2
                    && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(2)
                    && a.OpeningBid == Bid.SuitBid(2, Suit.Clubs)
            };
        }

        // ── Convention rules (responses to NT) ─────────────────────────
        AddNTConventionRules(rules, config.ResponseTo1NT, after1NT, "1NT", priorities);
        AddNTConventionRules(rules, config.ResponseTo2NT, after2NT, "2NT", priorities);

        // 2C-2D-2NT conventions (Stayman/Transfer after strong opening sequence)
        if (after2C2D2NT is not null)
        {
            AddNTConventionRules(rules, config.ResponseTo2NT, after2C2D2NT, "2C2D2NT", priorities);
        }

        // ── Response to 1NT: NT raise ──────────────────────────────────
        if (config.ResponseTo1NT?.Raise is { Enabled: true })
        {
            rules.Add(new AcolNTRaiseOver1NT());
        }

        // ── Response to 2C ─────────────────────────────────────────────
        if (config.ResponseTo2C is { Enabled: true })
        {
            rules.Add(new AcolResponseTo2C());
        }

        // ── Responses to 1-suit ────────────────────────────────────────
        if (config.ResponseTo1Suit is not null)
        {
            rules.Add(new AcolJacoby2NTOver1Major());
            rules.Add(new AcolRaiseMajorOver1Suit());
            rules.Add(new AcolRaiseMinorOver1Suit());
            rules.Add(new AcolNewSuitOver1Suit());
            rules.Add(new Acol1NTResponseTo1Suit());
        }

        // ── Opener rebids ──────────────────────────────────────────────
        if (config.OpenerRebid is not null)
        {
            rules.Add(new AcolOpenerRebidAfter2C());

            // Stayman responses (opener side)
            if (after1NT is not null)
            {
                rules.Add(new StaymanResponse(after1NT, GetPriority(priorities, "StaymanResponse_1NT", 60)));
                rules.Add(new AcolResponderAfterStayman(after1NT));
            }
            if (after2NT is not null)
            {
                rules.Add(new StaymanResponse(after2NT, GetPriority(priorities, "StaymanResponse_2NT", 60)));
                rules.Add(new AcolResponderAfterStayman(after2NT));
            }
            if (after2C2D2NT is not null)
            {
                rules.Add(new StaymanResponse(after2C2D2NT, GetPriority(priorities, "StaymanResponse_2C2D2NT", 60)));
                rules.Add(new AcolResponderAfterStayman(after2C2D2NT));
            }

            // Transfer completion (opener side)
            if (after1NT is not null && config.ResponseTo1NT?.Transfers is { Enabled: true })
                rules.Add(new CompleteTransfer(after1NT, GetPriority(priorities, "CompleteTransfer_1NT", 30)));
            if (after2NT is not null && config.ResponseTo2NT?.Transfers is { Enabled: true })
                rules.Add(new CompleteTransfer(after2NT, GetPriority(priorities, "CompleteTransfer_2NT", 30)));
            if (after2C2D2NT is not null)
                rules.Add(new CompleteTransfer(after2C2D2NT, GetPriority(priorities, "CompleteTransfer_2C2D2NT", 30)));

            rules.Add(new AcolOpenerAfterNTInvite());
            rules.Add(new AcolOpenerAfterMajorRaise());
            rules.Add(new AcolRebidBalanced());
            rules.Add(new AcolRebidNewSuit());
            rules.Add(new AcolRebidRaiseSuit());
            rules.Add(new AcolRebidOwnSuit());
        }

        // ── Responder rebids ───────────────────────────────────────────
        if (config.ResponderRebid is not null)
        {
            rules.Add(new AcolResponderAfterOpenerRaisedSuit());
            rules.Add(new AcolResponderAfterOpener1NTRebid());
            rules.Add(new AcolResponderAfterOpener2NTRebid());
            rules.Add(new AcolResponderAfterOpenerRebidOwnSuit());
            rules.Add(new AcolResponderAfterOpenerNewSuit());
        }

        // ── Knowledge catch-all rules (always included) ────────────────
        rules.Add(new KnowledgeBidGameInSuit());
        rules.Add(new KnowledgeBidGameInNT());
        rules.Add(new KnowledgeInviteInSuit());
        rules.Add(new KnowledgeInviteInNT());
        rules.Add(new KnowledgeSignOffInFit());
        rules.Add(new KnowledgeSignOff());

        // ── Warn about unbuilt sections ────────────────────────────────
        WarnIfPresent(config.Overcalls, "Overcalls", warnings);
        WarnIfPresent(config.NegativeDoubles, "NegativeDoubles", warnings);
        WarnIfPresent(config.CompetitiveAuctions, "CompetitiveAuctions", warnings);
        WarnIfPresent(config.Slam, "Slam", warnings);
        WarnIfPresent(config.FourthSuitForcing, "FourthSuitForcing", warnings);
        WarnIfPresent(config.Splinters, "Splinters", warnings);
        WarnIfPresent(config.TrialBids, "TrialBids", warnings);

        if (config.TwoLevelOpenings?.BenjaminTwos == true)
            warnings.Add("BenjaminTwos: no rule implementation yet — skipped");
        if (config.TwoLevelOpenings?.WeakTwos is not null)
            warnings.Add("WeakTwos: no dedicated rule implementation yet — skipped (preempts cover some cases)");
        if (config.TwoLevelOpenings?.AcolTwos is not null)
            warnings.Add("AcolTwos: no rule implementation yet — skipped");
        if (config.ResponseTo1NT?.WeaknessTakeouts is { Enabled: true })
            warnings.Add("WeaknessTakeouts: no rule implementation yet — skipped");
        if (config.ResponseTo1NT?.MinorTransfers is { Enabled: true })
            warnings.Add("MinorTransfers: no rule implementation yet — skipped");
        if (config.ResponseTo1NT?.Baron is { Enabled: true })
            warnings.Add("Baron: no rule implementation yet — skipped");
        if (config.ResponseTo2NT?.Baron is { Enabled: true })
            warnings.Add("Baron (2NT): no rule implementation yet — skipped");

        foreach (var warning in warnings)
            _logger?.LogWarning("SystemLoader: {Warning}", warning);

        // Sort by priority descending
        var sorted = rules.OrderByDescending(r => r.Priority).ToList();

        return new LoadedSystem(config.Name, sorted, config, warnings);
    }

    private static void AddNTConventionRules(
        List<IBiddingRule> rules,
        NTResponseConfig? responseConfig,
        NTConventionContext? ctx,
        string suffix,
        Dictionary<string, int> priorities)
    {
        if (responseConfig is null || ctx is null) return;

        if (responseConfig.Stayman is { Enabled: true })
        {
            rules.Add(new StandardStayman(ctx, GetPriority(priorities, $"StandardStayman_{suffix}", 29)));
        }

        if (responseConfig.Transfers is { Enabled: true })
        {
            rules.Add(new StandardTransfer(ctx, GetPriority(priorities, $"StandardTransfer_{suffix}", 30)));
        }
    }

    private static int GetPriority(Dictionary<string, int> priorities, string ruleName, int defaultPriority)
    {
        return priorities.TryGetValue(ruleName, out var p) ? p : defaultPriority;
    }

    private static void WarnIfPresent(object? config, string name, List<string> warnings)
    {
        if (config is not null)
            warnings.Add($"{name}: no rule implementation yet — skipped");
    }

    private static Bid? ParseBid(string bidStr)
    {
        if (string.IsNullOrWhiteSpace(bidStr)) return null;
        // Simple parsing for common bid formats like "2C", "3H", etc.
        if (bidStr.Length < 2) return null;

        if (!int.TryParse(bidStr[0].ToString(), out var level)) return null;

        var suitChar = char.ToUpper(bidStr[1]);
        Suit? suit = suitChar switch
        {
            'C' => Suit.Clubs,
            'D' => Suit.Diamonds,
            'H' => Suit.Hearts,
            'S' => Suit.Spades,
            _ => null
        };

        return suit.HasValue ? Bid.SuitBid(level, suit.Value) : null;
    }
}
