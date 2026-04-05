using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TemperAI.NeuralCore.Domain.Entities.Observations;
using TemperAI.NeuralCore.Domain.Entities.Observations.Enums;
using TemperAI.NeuralCore.Domain.Entities.Sessions;
using TemperAI.NeuralCore.Domain.Entities.Sessions.Enums;
using TemperAI.NeuralCore.Infrastructure.Persistence;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

namespace TemperAI.NeuralCore.Mcp.Tools;

[McpServerToolType]
public sealed class MemSaveTool
{
    private readonly ILogger<MemSaveTool> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public MemSaveTool(
        ILogger<MemSaveTool> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [McpServerTool(Name = "mem_save")]
    [Description("Saves an observation to the project memory. Use this to record decisions, bugs, patterns, or discoveries.")]
    public async Task<string> ExecuteAsync(
        [Description("Title of the observation (verb + what, e.g., 'Fix null reference in ProductController')")] string title,
        [Description("Type of observation: Bugfix, Decision, Architecture, Discovery, Pattern, Config, Preference")] string type,
        [Description("Content in What/Why/Where/Learned format")] string content,
        [Description("Optional topic key to group related observations")] string? topicKey = null)
    {
        try
        {
            if (!Enum.TryParse<ObservationType>(type, ignoreCase: true, out var observationType))
            {
                return $"Error: Invalid type '{type}'. Valid types are: Bugfix, Decision, Architecture, Discovery, Pattern, Config, Preference.";
            }

            var (errors, observation) = Observation.Create(
                sessionId: await GetOrCreateSessionIdAsync(),
                type: observationType,
                title: title,
                content: content,
                project: Path.GetFileName(Directory.GetCurrentDirectory()),
                topicKey: topicKey);

            if (observation is null)
            {
                return $"Error: {string.Join(", ", errors)}";
            }

            await _unitOfWork.ObservationRepository.AddAsync(observation);

            var saveResult = await _unitOfWork.CompleteAsync();

            if (!saveResult.IsSuccess)
            {
                return $"Error: {saveResult.ErrorMessage}";
            }

            _logger.LogInformation("Saved observation: {Title} (Type: {Type})", title, type);

            return $"Observation saved successfully. ID: {observation.Id}";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save observation: {Title}", title);
            return $"Error: {exception.Message}";
        }
    }

    private async Task<Guid> GetOrCreateSessionIdAsync()
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync();
        var activeSession = sessions.FirstOrDefault(s => s.Status == SessionStatus.Active);

        if (activeSession is not null)
        {
            return activeSession.Id;
        }

        var (errors, session) = Session.Create(
            project: Path.GetFileName(Directory.GetCurrentDirectory()),
            directory: Directory.GetCurrentDirectory());

        if (session is null)
        {
            throw new InvalidOperationException($"Failed to create session: {string.Join(", ", errors)}");
        }

        await _unitOfWork.SessionRepository.AddAsync(session);
        await _unitOfWork.CompleteAsync();

        return session.Id;
    }
}
