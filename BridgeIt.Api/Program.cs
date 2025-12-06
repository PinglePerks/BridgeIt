using BridgeIt.Api.Hubs;
using BridgeIt.Api.Models;
using BridgeIt.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SignalR
builder.Services.AddSignalR();

// 2. Add your Game Logic as a Singleton (Shared Memory)
builder.Services.AddSingleton<GameService>();

// 3. Configure CORS (CRITICAL for connecting from a different port)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // <--- ALLOWS ANY IP/LOCALHOST
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// 4. Use CORS
app.UseCors("AllowAll");

// 5. Map the Hub endpoint
app.MapHub<GameHub>("/gamehub");

// Simple test endpoint to ensure API is running
app.MapGet("/", () => "BridgeIt API is running!");

app.Run();