using System.ComponentModel.DataAnnotations;

namespace eLearning_Project.Models;

public class Lesson
{
    public int Id { get; set; }

    public int CourseModuleId { get; set; }

    [Required]
    [StringLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public CourseModule? CourseModule { get; set; }

    public ICollection<LessonResource> Resources { get; set; } = new List<LessonResource>();
}