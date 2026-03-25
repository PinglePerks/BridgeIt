namespace BridgeIt.Core.Analysis.Hands;

/// <summary>
/// Represents the quality of a stopper in a suit.
/// None: no useful high cards for stopping the suit.
/// Partial: a half-stopper that may combine with partner's holding (e.g. Qx, Jxx, singleton K).
/// Full: a definite stopper (e.g. A, Kx, Qxx, Jxxx).
/// </summary>
public enum StopperQuality
{
    None,
    Partial,
    Full
}
