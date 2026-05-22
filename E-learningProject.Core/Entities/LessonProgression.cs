namespace E_learningProject.Core.Entities;

public class LessonProgression
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int LessonId { get; set; }
    public bool IsRead { get; set; }
    public DateTime ReadDate { get; set; }

    public Lesson? Lesson { get; set; }
}