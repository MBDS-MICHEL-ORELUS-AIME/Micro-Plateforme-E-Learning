namespace E_learningProject.Core.Entities;

public class StudentBadge
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconCss { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; }
}
