using BridgeIt.Api.Hubs;
using BridgeIt.Api.Services;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Dds;
using BridgeIt.Systems;

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

// --- 3. Register DDS ---
builder.Services.AddSingleton<IDdsService, DdsService>();

// --- 4. Register Game Logic ---
// GameService needs to be a Singleton to hold state across SignalR requests
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<IBiddingObserver, SignalRBiddingObserver>();
builder.Services.AddSingleton<BiddingTable>(); 
builder.Services.AddSingleton<IBidValidityChecker, BidValidityChecker>();
builder.Services.AddSingleton<PartnershipSimulationService>();
builder.Services.AddSingleton<PracticeService>();




// --- 4. Register Bidding System ---
builder.Services.AddSingleton<BiddingSystemLoader>();
builder.Services.AddSingleton(sp =>
{
    var loader = sp.GetRequiredService<BiddingSystemLoader>();
    var systemPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "BridgeIt.Systems", "Systems", "acol-modern.json");
    return loader.LoadFromFile(systemPath);
});
builder.Services.AddSingleton<IEnumerable<IBiddingRule>>(sp =>
    sp.GetRequiredService<LoadedSystem>().Rules);

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

app.MapControllers();
app.MapHub<GameHub>("/gamehub");
app.MapHub<PracticeHub>("/practicehub");
app.MapGet("/", () => "BridgeIt API is running!");

app.Run();