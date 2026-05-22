namespace E_learningProject.Core.Entities;

public class Module
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Lesson> Lessons { get; set; } = new();
    public int? QuizId { get; set; }
    public Quiz? Quiz { get; set; }
}