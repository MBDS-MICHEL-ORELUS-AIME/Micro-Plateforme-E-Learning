namespace E_learningProject.Core.Entities;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? PdfPath { get; set; }
    public int Order { get; set; }
    public int ModuleId { get; set; }

    public Module? Module { get; set; }
}