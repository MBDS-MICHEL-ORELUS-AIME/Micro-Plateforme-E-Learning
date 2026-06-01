using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class QuizService : IQuizService
{
    public int CalculateScore(int totalQuestions, int correctAnswers)
    {
        // Guard against division by zero for edge cases (empty quiz).
        if (totalQuestions <= 0)
        {
            return 0;
        }

        var ratio = (double)correctAnswers / totalQuestions;
        // AwayFromZero avoids under-scoring borderline decimals like 66.5.
        return (int)Math.Round(ratio * 100, MidpointRounding.AwayFromZero);
    }
}