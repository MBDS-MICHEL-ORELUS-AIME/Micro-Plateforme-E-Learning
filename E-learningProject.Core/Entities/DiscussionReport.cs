namespace E_learningProject.Core.Entities;

public class DiscussionReport
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public DiscussionThread? Thread { get; set; }
    public string ReporterStudentId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public bool IsHandled { get; set; }
    public string HandlerNote { get; set; } = string.Empty;
}
