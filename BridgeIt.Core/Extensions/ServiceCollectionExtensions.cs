using BridgeIt.Core.BiddingEngine.BidDerivation.Factories;
using BridgeIt.Core.BiddingEngine.Constraints;
using Microsoft.Extensions.DependencyInjection;
using BridgeIt.Core.BiddingEngine.Constraints.Factories;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Services;
using BridgeIt.Core.Gameplay.Table;

namespace BridgeIt.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBridgeItCore(this IServiceCollection services)
    {
        // 1. Register Constraint Factories
        services.AddSingleton<IConstraintFactory, HcpConstraintFactory>();
        services.AddSingleton<IConstraintFactory, ShapeConstraintFactory>();
        services.AddSingleton<IConstraintFactory, LosingTrickCountCounstraintFactory>();
        services.AddSingleton<IConstraintFactory, CurrentStateConstraintFactory>();
        services.AddSingleton<IConstraintFactory, HistoryPatternConstraintFactory>();
        services.AddSingleton<IConstraintFactory, PartnerKnowledgeConstraintFactory>();
        
        // Register Bid Derivations
        services.AddSingleton<IBidDerivationFactory, LengthBidDerivationFactory>();
        services.AddSingleton<IBidDerivationFactory, SimpleRaiseDerivationFactor>();

        // services.AddSingleton<IBiddingRule, RespondingToNaturalOpening>();
        // services.AddSingleton<IBiddingRule, RedSuitTransfer>();
        // services.AddSingleton<IBiddingRule, ResponseTo2ntOpening>();

        // 2. Register Core Services
        services.AddSingleton<BiddingEngine.Core.BiddingEngine>();
        services.AddSingleton<IAuctionRules, StandardAuctionRules>();
        services.AddSingleton<ISeatRotationService, ClockwiseSeatRotationService>();
        services.AddSingleton<IBiddingObserver, ConsoleBiddingObserver>();
        services.AddSingleton<IHandFormatter, HandFormatter>();
        services.AddSingleton<BiddingTable>();
        
        // 3. Register the Rule Loader (See next file)
        services.AddSingleton<YamlRuleLoader>();

        return services;
    }
}