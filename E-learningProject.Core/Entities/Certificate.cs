namespace E_learningProject.Core.Entities;

public class Certificate
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string UniqueCode { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime IssueDate { get; set; } = DateTime.Now;

    public Module? Module { get; set; }
}