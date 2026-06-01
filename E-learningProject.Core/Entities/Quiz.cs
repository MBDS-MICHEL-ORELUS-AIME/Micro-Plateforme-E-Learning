namespace E_learningProject.Core.Entities;

public class Quiz
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PassingScore { get; set; } = 70;
    public int? CreatedByUserId { get; set; }

    public User? CreatedBy { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}