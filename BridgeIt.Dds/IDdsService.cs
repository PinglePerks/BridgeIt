using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dds.Models;

namespace BridgeIt.Dds;

public interface IDdsService
{
    /// <summary>
    /// Analyse a deal: compute the full trick table and par results for all 4 vulnerabilities.
    /// Called once per deal (not per auction).
    /// </summary>
    DdsAnalysis Analyse(Dictionary<Seat, Hand> deal, Seat dealer);
}
