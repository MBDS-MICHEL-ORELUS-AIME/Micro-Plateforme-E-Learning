namespace E_learningProject.Services;

public sealed class OpenContentImportResult
{
    public int ImportedModules { get; init; }
    public int ImportedLessons { get; init; }
    public int ImportedQuizzes { get; init; }
    public int SkippedDuplicates { get; init; }
    public int SkippedInvalidLicense { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
