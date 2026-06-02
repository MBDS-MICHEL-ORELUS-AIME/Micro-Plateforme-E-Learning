using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class QuizService : IQuizService
{
    public int CalculateScore(int totalQuestions, int correctAnswers)
    {
        // Prévenir la division par zéro dans les cas limites (quiz vide).
        if (totalQuestions <= 0)
        {
            return 0;
        }

        var ratio = (double)correctAnswers / totalQuestions;
        // AwayFromZero évite de sous-noter les décimales limites comme 66,5.
        return (int)Math.Round(ratio * 100, MidpointRounding.AwayFromZero);
    }
}