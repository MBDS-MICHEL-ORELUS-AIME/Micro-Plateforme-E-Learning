namespace E_learningProject.Core.Entities;

public class StudentBadge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconCss { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; }

    public User? Student { get; set; }
}
