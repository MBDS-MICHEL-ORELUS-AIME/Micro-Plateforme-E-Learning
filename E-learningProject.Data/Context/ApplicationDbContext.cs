using E_learningProject.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Data.Context;

public class ApplicationDbContext : DbContext
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
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();

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
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.ModuleId }).IsUnique();
        });

        modelBuilder.Entity<LessonProgression>(entity =>
        {
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.Property(x => x.UniqueCode).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiscussionThread>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Module)
                .WithMany()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DiscussionReply>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(x => x.BadgeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300).IsRequired();
            entity.Property(x => x.IconCss).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.BadgeName }).IsUnique();
        });

        modelBuilder.Entity<DiscussionReport>(entity =>
        {
            entity.ToTable("DiscussionReports");
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.HandlerNote).HasMaxLength(500);
            entity.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Thread)
                .WithMany()
                .HasForeignKey(x => x.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.ToTable("EmailMessages");
            entity.Property(x => x.MessageId).HasMaxLength(500).IsRequired();
            entity.Property(x => x.From).HasMaxLength(500).IsRequired();
            entity.Property(x => x.To).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(1000).IsRequired(false);
            entity.HasIndex(x => x.MessageId).IsUnique(false);
            entity.HasIndex(x => x.DiscussionThreadId);
        });

        modelBuilder.Entity<EmailAttachment>(entity =>
        {
            entity.ToTable("EmailAttachments");
            entity.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.EmailMessage)
                .WithMany()
                .HasForeignKey(x => x.EmailMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
