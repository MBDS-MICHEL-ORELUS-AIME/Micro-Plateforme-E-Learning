namespace E_learningProject.Web.Models;

public class CourseProgressViewModel
{
    public int ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public decimal CompletionPercentage { get; set; }
    public bool IsModuleCompleted { get; set; }
    public List<LessonProgressItemViewModel> Lessons { get; set; } = new();
}

public class LessonProgressItemViewModel
{
    public int LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? PdfPath { get; set; }
    public int Order { get; set; }
    public bool IsRead { get; set; }
}
