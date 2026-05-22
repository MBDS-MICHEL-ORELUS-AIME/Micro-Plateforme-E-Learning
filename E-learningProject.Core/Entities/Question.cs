using E_learningProject.Core.Enums;

namespace E_learningProject.Core.Entities;

public class Question
{
    public int Id { get; set; }
    public string Statement { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int QuizId { get; set; }

    public Quiz? Quiz { get; set; }
    public List<Option> Options { get; set; } = new();
}