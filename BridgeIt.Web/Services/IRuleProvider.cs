using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Web.Services;

public interface IRuleProvider
{
    Task<IEnumerable<IBiddingRule>> LoadRulesAsync();
}