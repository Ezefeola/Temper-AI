using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TemperAI.NeuralCore.Infrastructure.Persistence;

namespace TemperAI.NeuralCore.Mcp.Tools;

[McpServerToolType]
public sealed class MemSearchTool
{
    private readonly ILogger<MemSearchTool> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public MemSearchTool(
        ILogger<MemSearchTool> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [McpServerTool(Name = "mem_search")]
    [Description("Searches observations in the project memory. Returns matching observations based on topic or content.")]
    public async Task<string> ExecuteAsync(
        [Description("Search query (topic key, keyword, or partial title)")] string query,
        [Description("Maximum number of results to return (default: 5)")] int limit = 5)
    {
        try
        {
            var observations = await _unitOfWork.ObservationRepository.GetAllAsNoTrackingAsync();

            var matches = observations
                .Where(obs =>
                    obs.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    obs.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (obs.TopicKey != null && obs.TopicKey.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(obs => obs.CreatedAt)
                .Take(limit)
                .ToList();

            if (matches.Count == 0)
            {
                return $"No observations found matching '{query}'.";
            }

            var result = $"Found {matches.Count} observation(s) matching '{query}':\n\n";

            foreach (var obs in matches)
            {
                result += $"- [{obs.Type}] {obs.Title} ({obs.CreatedAt:yyyy-MM-dd})\n";
                result += $"  Topic: {obs.TopicKey ?? "none"}\n";
                result += $"  Content: {obs.Content}\n\n";
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to search observations: {Query}", query);
            return $"Error: {exception.Message}";
        }
    }
}
