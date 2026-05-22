namespace E_learningProject.Services.Interfaces;

public interface IProgressService
{
    decimal CalculateCompletion(int completedLessons, int totalLessons);
}