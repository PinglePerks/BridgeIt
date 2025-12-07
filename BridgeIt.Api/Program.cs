using BridgeIt.Api.Hubs;
using BridgeIt.Api.Services;
using BridgeIt.Core.Extensions;
using BridgeIt.Core.Configuration.Yaml;
using BridgeIt.Core.BiddingEngine.Core;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Register Infrastructure Services ---
builder.Services.AddSignalR();
builder.Services.AddControllers(); // Good to have if you add REST endpoints later
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. Register BridgeIt Core ---
// This registers BiddingEngine, BiddingTable, RuleLoader, etc.
builder.Services.AddBridgeItCore(); 

// --- 3. Register Game Logic ---
// GameService needs to be a Singleton to hold state across SignalR requests
builder.Services.AddSingleton<GameService>();

// --- 4. Load Rules at Startup (The "Warm-up" Pattern) ---
// We register a hosted service or run a manual scope to load rules *once* at startup.
// This is cleaner than doing it in the main flow.
var rulesDirectory = Path.Combine(AppContext.BaseDirectory, "BiddingRules"); 
// Ensure your YAML files are copied to output! (See .csproj note below)

// Manually resolve loader *once* to populate the engine, 
// OR simpler: Register a factory/config that BiddingEngine uses.
// Given your current BiddingEngine takes IEnumerable<IBiddingRule>, 
// we need to tell DI how to construct that list.

// BEST PRACTICE FIX: Register the Rules List in DI
builder.Services.AddSingleton<IEnumerable<IBiddingRule>>(sp => 
{
    var loader = sp.GetRequiredService<YamlRuleLoader>();
    
    // Check if directory exists, or fallback to a default relative path
    // For local dev, hardcoded path is risky. Better to use relative.
    var path = "/Users/mattyperky/RiderProjects/BridgeIt/BridgeIt.CLI/BiddingRules"; 
    // Or better: Path.Combine(builder.Environment.ContentRootPath, "Rules");

    return loader.LoadRulesFromDirectory(path).ToList();
});

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