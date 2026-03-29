using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dds.Models;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Dds;

/// <summary>
/// Real DDS service using P/Invoke to the native DDS library (v2.9.0).
/// Computes exact double-dummy trick tables and par contracts.
/// </summary>
public class DdsService : IDdsService
{
    private readonly ILogger<DdsService> _logger;
    private readonly object _ddsLock = new();
    private bool _initialised;

    public DdsService(ILogger<DdsService> logger)
    {
        _logger = logger;
    }

    private void EnsureInitialised()
    {
        if (_initialised) return;

        DdsInterop.SetMaxThreads(0); // auto-detect thread count
        _initialised = true;

        DdsInterop.GetDDSInfo(out var info);
        _logger.LogInformation(
            "DDS {Major}.{Minor}.{Patch} — {Cores} cores, {Threads} threads, threading={Threading}",
            info.Major, info.Minor, info.Patch, info.NumCores, info.NoOfThreads, info.Threading);
    }

    public DdsAnalysis Analyse(Dictionary<Seat, Hand> deal, Seat dealer)
    {
        // Native DDS library is not thread-safe — serialize all access
        lock (_ddsLock)
        {
            return AnalyseInternal(deal, dealer);
        }
    }

    private DdsAnalysis AnalyseInternal(Dictionary<Seat, Hand> deal, Seat dealer)
    {
        EnsureInitialised();

        // 0. Validate deal before passing to native DDS (it calls abort() on bad input)
        ValidateDeal(deal);

        // 1. Convert deal to PBN
        var pbn = DealConverter.ToPbn(deal);
        _logger.LogInformation("DDS PBN: {Pbn}", pbn);

        // 2. Calculate trick table
        var tableDeal = new DdTableDealPbn(pbn);
        var rc = DdsInterop.CalcDDtablePBN(tableDeal, out var rawTable);
        if (rc != DdsInterop.ReturnNoFault)
        {
            var msg = DdsInterop.GetErrorMessage(rc);
            _logger.LogError("CalcDDtablePBN failed ({Code}): {Message}", rc, msg);
            throw new InvalidOperationException($"DDS CalcDDtablePBN failed: {msg} ({rc})");
        }

        // 3. Convert raw table to our model
        var trickTable = ConvertTrickTable(rawTable);

        // 4. Calculate par for all 4 vulnerabilities
        var par = new Dictionary<string, ParResult>();
        var dealerInt = (int)dealer; // North=0, East=1, etc.

        foreach (var (vulKey, vulInt) in VulnerabilityMap)
        {
            rc = DdsInterop.DealerParBin(ref rawTable, out var rawPar, dealerInt, vulInt);
            if (rc != DdsInterop.ReturnNoFault)
            {
                var msg = DdsInterop.GetErrorMessage(rc);
                _logger.LogWarning("DealerParBin failed for {Vul} ({Code}): {Message}", vulKey, rc, msg);
                par[vulKey] = FallbackPar();
                continue;
            }

            par[vulKey] = ConvertParResult(rawPar, trickTable);
        }

        return new DdsAnalysis { TrickTable = trickTable, Par = par };
    }

    // ─── Conversion helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// DDS vulnerability encoding:
    ///   0 = None, 1 = Both, 2 = NS only, 3 = EW only
    /// </summary>
    private static readonly (string key, int ddsCode)[] VulnerabilityMap =
    [
        ("none",    0),
        ("bothVul", 1),
        ("nsVul",   2),
        ("ewVul",   3),
    ];

    /// <summary>
    /// Convert DDS raw table (strain-major: resTable[strain*4+hand]) to our model.
    /// DDS strains: 0=Spades, 1=Hearts, 2=Diamonds, 3=Clubs, 4=NT.
    /// DDS hands:   0=North,  1=East,   2=South,    3=West.
    /// </summary>
    private static DdsTrickTable ConvertTrickTable(DdTableResults raw)
    {
        var tricks = new Dictionary<string, Dictionary<string, int>>();

        string[] seatKeys = ["N", "E", "S", "W"];
        // DDS strain order → our strain names
        (int ddsIndex, string name)[] strainMap =
        [
            (0, "spades"),
            (1, "hearts"),
            (2, "diamonds"),
            (3, "clubs"),
            (4, "notrump"),
        ];

        for (var hand = 0; hand < 4; hand++)
        {
            var seatTricks = new Dictionary<string, int>();
            foreach (var (ddsStrain, strainName) in strainMap)
            {
                seatTricks[strainName] = raw.ResTable[ddsStrain * 4 + hand];
            }
            tricks[seatKeys[hand]] = seatTricks;
        }

        return new DdsTrickTable { Tricks = tricks };
    }

    /// <summary>
    /// Convert DealerParBin output to our ParResult model.
    /// </summary>
    private static ParResult ConvertParResult(ParResultsMaster raw, DdsTrickTable trickTable)
    {
        if (raw.Number == 0 || raw.Contracts == null || raw.Contracts.Length == 0)
        {
            return new ParResult
            {
                Type = ParType.Make,
                Contract = "Pass",
                Score = 0,
                ScoringSide = "NS",
                Declarer = "N",
                Tricks = 0,
            };
        }

        var first = raw.Contracts[0];
        var isSacrifice = first.UnderTricks > 0;
        var contract = $"{first.Level}{DenomToStr(first.Denom)}";
        var declarer = SeatToStr(first.Seats);
        var tricks = first.Level + 6 - first.UnderTricks + first.OverTricks;

        var result = new ParResult
        {
            Type = isSacrifice ? ParType.Sacrifice : ParType.Make,
            Contract = contract,
            Doubled = isSacrifice, // sacrifices are always doubled at par
            Declarer = declarer,
            Tricks = tricks,
            UnderTricks = isSacrifice ? first.UnderTricks : null,
            Score = raw.Score,
            ScoringSide = raw.Score >= 0 ? "NS" : "EW",
        };

        // For sacrifices, find the best make for the opposing side
        if (isSacrifice)
        {
            result.NsBestMake = FindBestMake(trickTable, raw.Score >= 0 ? "N" : "E");
        }

        return result;
    }

    /// <summary>
    /// Find the highest-scoring makeable contract for a given side.
    /// Used to show what the opponents would make if the sacrifice isn't bid.
    /// </summary>
    private static ParMakeResult? FindBestMake(DdsTrickTable trickTable, string seatKey)
    {
        var partnerKey = seatKey == "N" ? "S" : seatKey == "E" ? "W" : seatKey == "S" ? "N" : "E";
        var bestScore = 0;
        var bestContract = "";
        var bestDeclarer = "";

        foreach (var strain in DdsTrickTable.Strains)
        {
            foreach (var seat in new[] { seatKey, partnerKey })
            {
                var tricks = trickTable.Tricks[seat][strain];
                if (tricks < 7) continue;
                var level = tricks - 6;
                var score = EstimateScore(strain, level, tricks);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestContract = $"{level}{StrainToSymbol(strain)}";
                    bestDeclarer = seat;
                }
            }
        }

        return bestScore > 0
            ? new ParMakeResult { Contract = bestContract, Declarer = bestDeclarer, Score = bestScore }
            : null;
    }

    private static int EstimateScore(string strain, int level, int tricks)
    {
        var trickVal = strain is "clubs" or "diamonds" ? 20 : 30;
        var contractPts = strain == "notrump" ? 40 + (level - 1) * 30 : level * trickVal;
        var isGame = contractPts >= 100;
        var score = contractPts + (isGame ? 300 : 50);
        if (level == 6) score += 500;
        if (level == 7) score += 1000;
        score += (tricks - level - 6) * (strain == "notrump" ? 30 : trickVal);
        return score;
    }

    /// <summary>
    /// DDS denom: 0=NT, 1=Spades, 2=Hearts, 3=Diamonds, 4=Clubs
    /// </summary>
    private static string DenomToStr(int denom) => denom switch
    {
        0 => "NT",
        1 => "S",
        2 => "H",
        3 => "D",
        4 => "C",
        _ => "?"
    };

    /// <summary>
    /// DDS seats: 0=N, 1=E, 2=S, 3=W, 4=NS, 5=EW
    /// </summary>
    private static string SeatToStr(int seats) => seats switch
    {
        0 => "N",
        1 => "E",
        2 => "S",
        3 => "W",
        4 => "N", // NS — pick N as declarer
        5 => "E", // EW — pick E as declarer
        _ => "N"
    };

    private static string StrainToSymbol(string strain) => strain switch
    {
        "clubs" => "C",
        "diamonds" => "D",
        "hearts" => "H",
        "spades" => "S",
        "notrump" => "NT",
        _ => "?"
    };

    /// <summary>
    /// Validates a deal before passing to native DDS. DDS calls abort() on invalid input
    /// which kills the entire process — so we must catch all problems here.
    /// </summary>
    private void ValidateDeal(Dictionary<Seat, Hand> deal)
    {
        var requiredSeats = new[] { Seat.North, Seat.East, Seat.South, Seat.West };

        foreach (var seat in requiredSeats)
        {
            if (!deal.TryGetValue(seat, out var hand))
                throw new InvalidOperationException($"DDS validation: missing hand for {seat}");

            if (hand.Cards.Count != 13)
                throw new InvalidOperationException(
                    $"DDS validation: {seat} has {hand.Cards.Count} cards, expected 13");
        }

        // Check for duplicate cards
        var allCards = new HashSet<string>();
        foreach (var (seat, hand) in deal)
        {
            foreach (var card in hand.Cards)
            {
                var key = $"{card.Rank}{card.Suit}";
                if (!allCards.Add(key))
                    throw new InvalidOperationException(
                        $"DDS validation: duplicate card {key} (found in {seat}'s hand)");
            }
        }

        if (allCards.Count != 52)
            throw new InvalidOperationException(
                $"DDS validation: deal has {allCards.Count} unique cards, expected 52");

        // Validate the PBN string is well-formed and fits in the 80-byte buffer
        var pbn = DealConverter.ToPbn(deal);
        if (pbn.Length >= 80)
            throw new InvalidOperationException(
                $"DDS validation: PBN string too long ({pbn.Length} chars, max 79)");

        // Check PBN structure: "N:" + 4 hands separated by spaces, each with 4 dot-separated suits
        var afterPrefix = pbn[2..]; // skip "N:"
        var hands = afterPrefix.Split(' ');
        if (hands.Length != 4)
            throw new InvalidOperationException(
                $"DDS validation: PBN has {hands.Length} hand groups, expected 4. PBN: {pbn}");

        foreach (var handStr in hands)
        {
            var suits = handStr.Split('.');
            if (suits.Length != 4)
                throw new InvalidOperationException(
                    $"DDS validation: hand '{handStr}' has {suits.Length} suit groups, expected 4. PBN: {pbn}");
        }

        _logger.LogDebug("DDS validation passed for deal with {Cards} cards", allCards.Count);
    }

    private static ParResult FallbackPar() => new()
    {
        Type = ParType.Make,
        Contract = "Pass",
        Score = 0,
        ScoringSide = "NS",
        Declarer = "N",
        Tricks = 0,
    };
}
