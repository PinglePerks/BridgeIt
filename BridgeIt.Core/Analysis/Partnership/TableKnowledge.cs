using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Partnership;

/// <summary>
/// Holds inferred knowledge about all other players at the table,
/// keyed by seat. Built by replaying the auction and extracting
/// constraints from each bid.
/// </summary>
public class TableKnowledge
{
    private readonly Seat _mySeat;
    public Dictionary<Seat, PlayerKnowledge> Players { get; }

    public TableKnowledge(Seat mySeat)
    {
        _mySeat = mySeat;
        Players = new Dictionary<Seat, PlayerKnowledge>
        {
            { Seat.North, new PlayerKnowledge() },
            { Seat.East, new PlayerKnowledge() },
            { Seat.South, new PlayerKnowledge() },
            { Seat.West, new PlayerKnowledge() }
        };
    }

    /// <summary>Partner's inferred hand knowledge</summary>
    public PlayerKnowledge Partner => Players[_mySeat.GetPartner()];

    /// <summary>Left-hand opponent's inferred hand knowledge</summary>
    public PlayerKnowledge LeftOpponent => Players[_mySeat.GetNextSeat()];

    /// <summary>Right-hand opponent's inferred hand knowledge</summary>
    public PlayerKnowledge RightOpponent => Players[_mySeat.GetNextSeat().GetNextSeat().GetNextSeat()];

    /// <summary>
    /// Apply cross-table HCP inference: total HCP in deck is 40,
    /// so knowing about some players constrains others.
    /// Narrows both HcpMax (from others' minimums) and HcpMin (from others' maximums).
    /// </summary>
    public void ApplyCrossTableInferences(int myHcp)
    {
        foreach (var seat in Players.Keys)
        {
            if (seat == _mySeat) continue;

            var othersMin = Players
                .Where(p => p.Key != seat && p.Key != _mySeat)
                .Sum(p => p.Value.HcpMin) + myHcp;

            Players[seat].HcpMax = Math.Min(Players[seat].HcpMax, 40 - othersMin);

            var othersMax = Players
                .Where(p => p.Key != seat && p.Key != _mySeat)
                .Sum(p => p.Value.HcpMax) + myHcp;

            Players[seat].HcpMin = Math.Max(Players[seat].HcpMin, 40 - othersMax);
        }
    }

    /// <summary>
    /// Apply cross-table suit-length inference: each suit has exactly 13 cards,
    /// so knowing cards held by some players constrains what others can hold.
    /// </summary>
    public void ApplyCrossTableSuitInferences(Dictionary<Suit, int> myShape)
    {
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (var seat in Players.Keys)
            {
                if (seat == _mySeat) continue;

                var otherPlayersMin = Players
                    .Where(p => p.Key != seat && p.Key != _mySeat)
                    .Sum(p => p.Value.MinShape[suit]);

                var derivedMax = 13 - myShape[suit] - otherPlayersMin;
                Players[seat].MaxShape[suit] = Math.Min(Players[seat].MaxShape[suit], derivedMax);

                var otherPlayersMax = Players
                    .Where(p => p.Key != seat && p.Key != _mySeat)
                    .Sum(p => p.Value.MaxShape[suit]);

                var derivedMin = 13 - myShape[suit] - otherPlayersMax;
                Players[seat].MinShape[suit] = Math.Max(Players[seat].MinShape[suit], Math.Max(0, derivedMin));
            }
        }
    }
}
