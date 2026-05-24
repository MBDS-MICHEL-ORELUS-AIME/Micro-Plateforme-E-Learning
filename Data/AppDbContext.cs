using eLearning_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eLearning_Project.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonResource> LessonResources => Set<LessonResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Le cours reste l'entree principale du catalogue dans lms.Courses, puis les modules, lecons et ressources s'y rattachent.
        modelBuilder.Entity<Course>().ToTable("Courses", "lms");

        modelBuilder.Entity<CourseModule>(entity =>
        {
            entity.ToTable("Module", "lms");
            entity.HasOne(module => module.Course)
                .WithMany(course => course.Modules)
                .HasForeignKey(module => module.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("Lesson", "lms");
            entity.HasOne(lesson => lesson.CourseModule)
                .WithMany(module => module.Lessons)
                .HasForeignKey(lesson => lesson.CourseModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonResource>(entity =>
        {
            entity.ToTable("LessonResource", "lms");
            // Les ressources restent des liens externes pour eviter de stocker de gros fichiers PDF dans la base.
            entity.HasOne(resource => resource.Lesson)
                .WithMany(lesson => lesson.Resources)
                .HasForeignKey(resource => resource.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}