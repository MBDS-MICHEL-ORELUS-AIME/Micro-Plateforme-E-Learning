using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class QuizService : IQuizService
{
    public int CalculateScore(int totalQuestions, int correctAnswers)
    {
        if (totalQuestions <= 0)
        {
            return 0;
        }

        var ratio = (double)correctAnswers / totalQuestions;
        return (int)Math.Round(ratio * 100, MidpointRounding.AwayFromZero);
    }
}