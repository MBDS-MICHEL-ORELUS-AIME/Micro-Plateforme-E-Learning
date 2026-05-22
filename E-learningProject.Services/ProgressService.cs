using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class ProgressService : IProgressService
{
    public decimal CalculateCompletion(int completedLessons, int totalLessons)
    {
        if (totalLessons <= 0)
        {
            return 0;
        }

        var percentage = (decimal)completedLessons / totalLessons * 100m;
        return Math.Clamp(decimal.Round(percentage, 2), 0m, 100m);
    }
}