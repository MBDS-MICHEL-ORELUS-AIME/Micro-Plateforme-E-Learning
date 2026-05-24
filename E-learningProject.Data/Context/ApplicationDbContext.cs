using E_learningProject.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Data.Context;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgression> LessonProgressions => Set<LessonProgression>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<DiscussionThread> DiscussionThreads => Set<DiscussionThread>();
    public DbSet<DiscussionReply> DiscussionReplies => Set<DiscussionReply>();
    public DbSet<User> AppUsers => Set<User>();
    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<ContentImportLog> ContentImportLogs => Set<ContentImportLog>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<DiscussionReport> DiscussionReports => Set<DiscussionReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Module>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.HasOne(x => x.Quiz)
                .WithMany()
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(x => x.Statement).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TextContent).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.PdfPath).HasMaxLength(500);
            entity.Property(x => x.VideoUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<LessonProgression>(entity =>
        {
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.UniqueCode).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<DiscussionThread>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<DiscussionReply>(entity =>
        {
            entity.Property(x => x.AuthorId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.HasOne(x => x.DiscussionThread)
                .WithMany(t => t.Replies)
                .HasForeignKey(x => x.DiscussionThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContentImportLog>(entity =>
        {
            entity.ToTable("ContentImportLogs");
            entity.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SourceName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.SourceLicense).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.ImportedAt);
        });

        modelBuilder.Entity<StudentBadge>(entity =>
        {
            entity.ToTable("StudentBadges");
            entity.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.BadgeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300).IsRequired();
            entity.Property(x => x.IconCss).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.StudentId, x.BadgeName }).IsUnique();
        });

        modelBuilder.Entity<DiscussionReport>(entity =>
        {
            entity.ToTable("DiscussionReports");
            entity.Property(x => x.ReporterStudentId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.HandlerNote).HasMaxLength(500);
            entity.HasOne(x => x.Thread)
                .WithMany()
                .HasForeignKey(x => x.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
