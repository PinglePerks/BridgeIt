using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dds.Models;

/// <summary>
/// Full double-dummy trick table: for each declarer (N/E/S/W) × strain (C/D/H/S/NT),
/// the number of tricks makeable with perfect play by both sides.
/// </summary>
public class DdsTrickTable
{
    /// <summary>
    /// Outer key: seat name ("N","E","S","W").
    /// Inner key: strain name ("clubs","diamonds","hearts","spades","notrump").
    /// Value: tricks makeable (0–13).
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> Tricks { get; set; } = new();

    public int GetTricks(Seat seat, string strain)
        => Tricks[SeatToKey(seat)][strain];

    public static string SeatToKey(Seat seat) => seat switch
    {
        Seat.North => "N",
        Seat.East => "E",
        Seat.South => "S",
        Seat.West => "W",
        _ => throw new ArgumentOutOfRangeException(nameof(seat))
    };

    public static readonly string[] Strains = ["clubs", "diamonds", "hearts", "spades", "notrump"];
}
