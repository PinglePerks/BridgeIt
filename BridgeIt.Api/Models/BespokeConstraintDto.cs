namespace BridgeIt.Api.Models;

/// <summary>
/// Shape and HCP constraints for a bespoke North hand.
/// All other seats receive PassingOpponent constraints.
/// </summary>
public record BespokeConstraintDto(
    int MinHcp,
    int MaxHcp,
    int MinSpades = 0,
    int MaxSpades = 13,
    int MinHearts = 0,
    int MaxHearts = 13,
    int MinDiamonds = 0,
    int MaxDiamonds = 13,
    int MinClubs = 0,
    int MaxClubs = 13);
