namespace E_learningProject.Core.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    public string StudentId { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public bool IsCompleted { get; set; }

    public Module? Module { get; set; }
}