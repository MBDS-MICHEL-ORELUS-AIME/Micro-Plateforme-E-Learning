namespace E_learningProject.Core.Entities;

public class QuizResult
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int QuizId { get; set; }
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptDate { get; set; }

    public Quiz? Quiz { get; set; }
}