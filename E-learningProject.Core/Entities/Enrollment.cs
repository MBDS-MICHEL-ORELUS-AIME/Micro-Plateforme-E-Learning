namespace E_learningProject.Core.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public int ModuleId { get; set; }
    public bool IsCompleted { get; set; }

    public User? Student { get; set; }
    public Module? Module { get; set; }
}