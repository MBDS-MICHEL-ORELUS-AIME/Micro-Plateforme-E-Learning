namespace E_learningProject.Web.Models;

public class AdminLessonPdfSourcesViewModel
{
    public int TotalLessons { get; set; }
    public int OpenSourcePdfCount { get; set; }
    public int NonOpenSourcePdfCount { get; set; }
    public List<AdminLessonPdfSourceItemViewModel> Lessons { get; set; } = new();
}

public class AdminLessonPdfSourceItemViewModel
{
    public int LessonId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public string SourceLabel { get; set; } = string.Empty;
    public bool IsOpenSourcePdf { get; set; }
}
