using System.ComponentModel.DataAnnotations;

namespace eLearning_Project.Models;

public class CourseModule
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public Course? Course { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}