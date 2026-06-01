namespace E_learningProject.Core.Entities;

public class QuizResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QuizId { get; set; }
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptDate { get; set; }

    public User? Student { get; set; }
    public Quiz? Quiz { get; set; }
}