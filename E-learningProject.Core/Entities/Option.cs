namespace E_learningProject.Core.Entities;

public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public Question? Question { get; set; }
}