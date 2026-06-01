using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class ProgressService : IProgressService
{
    public decimal CalculateCompletion(int completedLessons, int totalLessons)
    {
        // Empty module yields 0% to avoid misleading completion values.
        if (totalLessons <= 0)
        {
            return 0;
        }

        var percentage = (decimal)completedLessons / totalLessons * 100m;
        // Clamp ensures stable output between 0 and 100 even if input counters drift.
        return Math.Clamp(decimal.Round(percentage, 2), 0m, 100m);
    }
}