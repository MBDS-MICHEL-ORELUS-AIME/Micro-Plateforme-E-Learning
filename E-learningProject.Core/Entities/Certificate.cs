namespace E_learningProject.Core.Entities;

public class Certificate
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ModuleId { get; set; }
    public string UniqueCode { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public User? Student { get; set; }
    public Module? Module { get; set; }
}