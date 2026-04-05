using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TemperAI.NeuralCore.Domain.Entities.Sessions;
using TemperAI.NeuralCore.Domain.Entities.Sessions.Enums;
using TemperAI.NeuralCore.Infrastructure.Persistence;

namespace TemperAI.NeuralCore.Mcp.Tools;

[McpServerToolType]
public sealed class MemContextTool
{
    private readonly ILogger<MemContextTool> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public MemContextTool(
        ILogger<MemContextTool> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [McpServerTool(Name = "mem_context")]
    [Description("Returns a summary of the current project session and recent observations. Call this at the start of a new conversation to get context.")]
    public async Task<string> ExecuteAsync()
    {
        try
        {
            var sessions = await _unitOfWork.SessionRepository.GetAllAsync();
            var activeSession = sessions.FirstOrDefault(s => s.Status == SessionStatus.Active);

            if (activeSession is null)
            {
                return "No active session found. Start a new session with mem_session_summary.";
            }

            var observations = await _unitOfWork.ObservationRepository.GetBySessionIdAsync(activeSession.Id);
            var recentObservations = observations
                .OrderByDescending(obs => obs.CreatedAt)
                .Take(10)
                .ToList();

            var result = $"Session: {activeSession.Project}\n";
            result += $"Started: {activeSession.StartedAt:yyyy-MM-dd HH:mm}\n";
            result += $"Total observations: {observations.Count}\n\n";

            if (recentObservations.Count > 0)
            {
                result += "Recent observations:\n";
                foreach (var obs in recentObservations)
                {
                    result += $"- [{obs.Type}] {obs.Title}\n";
                }
            }
            else
            {
                result += "No observations recorded yet.";
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to get context");
            return $"Error: {exception.Message}";
        }
    }
}
