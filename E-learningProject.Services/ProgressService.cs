using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class ProgressService : IProgressService
{
    public decimal CalculateCompletion(int completedLessons, int totalLessons)
    {
        // Un module vide renvoie 0 % pour éviter des valeurs d'avancement trompeuses.
        if (totalLessons <= 0)
        {
            return 0;
        }

        var percentage = (decimal)completedLessons / totalLessons * 100m;
        // Le bornage garantit une sortie stable entre 0 et 100, même si les compteurs d'entrée dérivent.
        return Math.Clamp(decimal.Round(percentage, 2), 0m, 100m);
    }
}