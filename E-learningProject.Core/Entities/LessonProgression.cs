namespace E_learningProject.Core.Entities;

public class LessonProgression
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LessonId { get; set; }
    public bool IsRead { get; set; }
    public DateTime ReadDate { get; set; }

    public User? Student { get; set; }
    public Lesson? Lesson { get; set; }
}