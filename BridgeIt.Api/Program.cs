using BridgeIt.Api.Hubs;
using BridgeIt.Api.Services;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Openings;
using BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Table;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Register Infrastructure Services ---
builder.Services.AddSignalR();
builder.Services.AddControllers(); // Good to have if you add REST endpoints later
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. Register BridgeIt Core ---
// This registers BiddingEngine, BiddingTable, RuleLoader, etc.
builder.Services.AddBridgeItCore();

// Override the file-based EngineObserver with the SignalR version
builder.Services.AddSingleton<IEngineObserver, SignalREngineObserver>();

// --- 3. Register Game Logic ---
// GameService needs to be a Singleton to hold state across SignalR requests
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<IBiddingObserver, SignalRBiddingObserver>();
builder.Services.AddSingleton<BiddingTable>(); 
builder.Services.AddSingleton<IBidValidityChecker, BidValidityChecker>();




// --- 4. Register Bidding Rules ---
builder.Services.AddSingleton<IEnumerable<IBiddingRule>>(_ => new List<IBiddingRule>
{
    // Openings
    new Acol1NTOpeningRule(),
    new Acol1SuitOpeningRule(),
    new Acol2NTOpeningRule(),
    new AcolStrongOpening(),
    new WeakOpeningRule(new[] { Bid.SuitBid(2, Suit.Clubs) }), // 2C reserved for strong opening

    // Responses to 1-suit
    new AcolJacoby2NTOver1Major(),
    new AcolRaiseMajorOver1Suit(),
    new AcolRaiseMinorOver1Suit(),
    new AcolNewSuitOver1Suit(),
    new Acol1NTResponseTo1Suit(),

    // Responses to 1NT
    new AcolStaymanOver1NT(),
    new AcolRedSuitTransferOver1NT(),
    new AcolNTRaiseOver1NT(),
    // InvitationOver1NT — not yet implemented

    // Opener rebids
    new AcolStaymanResponse(),
    new AcolOpenerAfterNTInvite(),
    new AcolOpenerAfterMajorRaise(),
    new AcolRebidBalanced(),
    new AcolRebidSuit(),
    new CompleteTransfer(),
}.OrderByDescending(r => r.Priority).ToList());

// --- 5. Configure CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)// Add your Blazor client URL here explicitly!
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Critical for SignalR
    });
});

// --- 6. Configure Logging ---
builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();

// --- 7. Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.MapHub<GameHub>("/gamehub");
app.MapGet("/", () => "BridgeIt API is running!");

app.Run();