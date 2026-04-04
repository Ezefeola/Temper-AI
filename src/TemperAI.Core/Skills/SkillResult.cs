using System.IO;

namespace TemperAI.Core.Skills;

public sealed class SkillResult
{
    public bool IsSuccess { get; init; }
    public string SkillPath { get; init; } = string.Empty;
    public List<string> CreatedFiles { get; init; } = [];
    public string Error { get; init; } = string.Empty;

    public static SkillResult Success(string path, List<string> files)
    {
        return new SkillResult { IsSuccess = true, SkillPath = path, CreatedFiles = files };
    }

    public static SkillResult Failure(string error)
    {
        return new SkillResult { IsSuccess = false, Error = error };
    }
}
