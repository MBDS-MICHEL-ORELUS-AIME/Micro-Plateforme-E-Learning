using System.ComponentModel.DataAnnotations;

namespace eLearning_Project.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Instructor { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(1, 500)]
    public int DurationHours { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // La plateforme stockait au depart uniquement les metadonnees du cours. Le contenu detaille passe maintenant par les modules.
    public ICollection<CourseModule> Modules { get; set; } = new List<CourseModule>();
}