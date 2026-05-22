namespace E_learningProject.Web.Models;

public class CourseCatalogViewModel
{
    public int TotalModules { get; set; }
    public int TotalLessons { get; set; }
    public int TotalQuizzes { get; set; }
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public IReadOnlyList<E_learningProject.Core.Entities.Module> Modules { get; set; } = [];
}

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
