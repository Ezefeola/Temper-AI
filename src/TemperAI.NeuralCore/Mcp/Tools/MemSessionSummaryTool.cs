using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TemperAI.NeuralCore.Domain.Entities.Observations;
using TemperAI.NeuralCore.Domain.Entities.Observations.Enums;
using TemperAI.NeuralCore.Domain.Entities.Sessions;
using TemperAI.NeuralCore.Domain.Entities.Sessions.Enums;
using TemperAI.NeuralCore.Infrastructure.Persistence;

namespace TemperAI.NeuralCore.Mcp.Tools;

[McpServerToolType]
public sealed class MemSessionSummaryTool
{
    private readonly ILogger<MemSessionSummaryTool> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public MemSessionSummaryTool(
        ILogger<MemSessionSummaryTool> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [McpServerTool(Name = "mem_session_summary")]
    [Description("Saves a session summary at the end of a work session. Generates/updates neural-export.md.")]
    public async Task<string> ExecuteAsync(
        [Description("What was the goal of this session")] string goal,
        [Description("Key discoveries made")] string discoveries,
        [Description("What was accomplished")] string accomplished,
        [Description("Comma-separated list of files changed")] string filesChanged)
    {
        try
        {
            var sessions = await _unitOfWork.SessionRepository.GetAllAsync();
            var activeSession = sessions.FirstOrDefault(s => s.Status == SessionStatus.Active);

            if (activeSession is not null)
            {
                var (errors, updated) = activeSession.Complete();

                if (updated)
                {
                    await _unitOfWork.CompleteAsync();
                }
            }

            var (sessionErrors, session) = Session.Create(
                project: Path.GetFileName(Directory.GetCurrentDirectory()),
                directory: Directory.GetCurrentDirectory());

            if (session is null)
            {
                return $"Error: {string.Join(", ", sessionErrors)}";
            }

            await _unitOfWork.SessionRepository.AddAsync(session);
            await _unitOfWork.CompleteAsync();

            var observationContent = $"Goal: {goal}\nDiscoveries: {discoveries}\nAccomplished: {accomplished}\nFiles changed: {filesChanged}";

            var (obsErrors, observation) = Observation.Create(
                sessionId: session.Id,
                type: ObservationType.Discovery,
                title: $"Session summary: {goal}",
                content: observationContent,
                project: Path.GetFileName(Directory.GetCurrentDirectory()),
                topicKey: "session-summary");

            if (observation is not null)
            {
                await _unitOfWork.ObservationRepository.AddAsync(observation);
                await _unitOfWork.CompleteAsync();
            }

            await GenerateNeuralExportAsync();

            _logger.LogInformation("Session summary saved: {Goal}", goal);

            return "Session summary saved and neural-export.md updated.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save session summary: {Goal}", goal);
            return $"Error: {exception.Message}";
        }
    }

    private async Task GenerateNeuralExportAsync()
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync();
        var observations = await _unitOfWork.ObservationRepository.GetAllAsNoTrackingAsync();

        var builder = new StringBuilder();
        builder.AppendLine("# NeuralCore — Project Memory Export");
        builder.AppendLine();
        builder.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine($"> Project: {Path.GetFileName(Directory.GetCurrentDirectory())}");
        builder.AppendLine();

        builder.AppendLine("## Sessions");
        builder.AppendLine();

        foreach (var session in sessions)
        {
            builder.AppendLine($"- **{session.Project}** — {session.Status} ({session.StartedAt:yyyy-MM-dd})");
        }

        builder.AppendLine();
        builder.AppendLine("## Observations");
        builder.AppendLine();

        foreach (var obs in observations.OrderByDescending(o => o.CreatedAt))
        {
            builder.AppendLine($"### [{obs.Type}] {obs.Title}");
            builder.AppendLine();
            builder.AppendLine($"- **Date:** {obs.CreatedAt:yyyy-MM-dd HH:mm}");
            if (!string.IsNullOrEmpty(obs.TopicKey))
            {
                builder.AppendLine($"- **Topic:** {obs.TopicKey}");
            }
            builder.AppendLine();
            builder.AppendLine(obs.Content);
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();
        }

        var exportPath = Path.Combine(Directory.GetCurrentDirectory(), ".temper", "neural-export.md");
        var exportDirectory = Path.GetDirectoryName(exportPath);
        if (!string.IsNullOrEmpty(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }
        await File.WriteAllTextAsync(exportPath, builder.ToString());
    }
}
