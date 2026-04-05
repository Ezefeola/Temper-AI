using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TemperAI.NeuralCore.Infrastructure.Persistence;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;
using TemperAI.NeuralCore.Mcp.Tools;

namespace TemperAI.NeuralCore.Mcp;

public static class McpServer
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var projectDirectory = Directory.GetCurrentDirectory();
        var temperDirectory = Path.Combine(projectDirectory, ".temper");
        Directory.CreateDirectory(temperDirectory);

        var dbPath = Path.Combine(temperDirectory, "neural.db");

        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddDbContext<NeuralCoreDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        builder.Services.AddScoped<ISessionRepository, SessionRepository>();
        builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "TemperAI NeuralCore",
                Version = "0.1.0"
            };
        })
        .WithTools<MemSaveTool>()
        .WithTools<MemSearchTool>()
        .WithTools<MemContextTool>()
        .WithTools<MemSessionSummaryTool>();

        var app = builder.Build();

        await app.RunAsync(cancellationToken);
    }
}
