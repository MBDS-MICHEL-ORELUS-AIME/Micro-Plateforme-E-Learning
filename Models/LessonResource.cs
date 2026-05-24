using System.ComponentModel.DataAnnotations;

namespace eLearning_Project.Models;

public class LessonResource
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    [Required]
    [StringLength(40)]
    public string ResourceType { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Url]
    public string Url { get; set; } = string.Empty;

    public Lesson? Lesson { get; set; }
}