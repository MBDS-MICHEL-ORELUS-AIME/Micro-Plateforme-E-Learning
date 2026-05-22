namespace E_learningProject.Services.Interfaces;

public interface IQuizService
{
    int CalculateScore(int totalQuestions, int correctAnswers);
}