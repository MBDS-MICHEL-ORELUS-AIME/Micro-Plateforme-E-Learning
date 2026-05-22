using E_learningProject.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace E_learningProject.Web.Models;

public class LearnerDashboardViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public decimal OverallProgress { get; set; }
    public List<LearnerModuleCardViewModel> Modules { get; set; } = new();
}

public class LearnerModuleCardViewModel
{
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int ReadLessons { get; set; }
    public decimal ProgressPercent { get; set; }
}

public class LearnerReaderViewModel
{
    public int ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public decimal ProgressPercent { get; set; }
    public LessonReaderItemViewModel? CurrentLesson { get; set; }
    public List<LessonReaderItemViewModel> Lessons { get; set; } = new();
}

public class LessonReaderItemViewModel
{
    public int LessonId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? PdfPath { get; set; }
    public bool IsRead { get; set; }
}

public class TeacherWorkspaceViewModel
{
    public TeacherModuleCreateViewModel ModuleForm { get; set; } = new();
    public TeacherLessonCreateViewModel LessonForm { get; set; } = new();
    public TeacherQuizCreateViewModel QuizForm { get; set; } = new();
    public TeacherMediaUploadViewModel MediaForm { get; set; } = new();
    public List<TeacherOptionViewModel> ModuleOptions { get; set; } = new();
    public List<TeacherLessonOptionViewModel> LessonOptions { get; set; } = new();
    public List<TeacherQuizSummaryViewModel> ExistingQuizzes { get; set; } = new();
}

public class TeacherModuleCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
}

public class TeacherLessonCreateViewModel
{
    [Required]
    public int ModuleId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string TextContent { get; set; } = string.Empty;

    [Range(1, 200)]
    public int Order { get; set; } = 1;
}

public class TeacherMyQuizzesViewModel
{
    public List<TeacherQuizManageItemViewModel> Quizzes { get; set; } = new();
}

public class TeacherQuizManageItemViewModel
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public int QuestionCount { get; set; }
    public string? ModuleTitle { get; set; }
}

public class TeacherOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TeacherLessonOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ModuleId { get; set; }
}

public class TeacherQuizSummaryViewModel
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int PassingScore { get; set; }
}

public class TeacherQuizCreateViewModel
{
    public int? QuizId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 100)]
    public int PassingScore { get; set; } = 70;

    [Required]
    public int ModuleId { get; set; }

    public List<TeacherQuestionInputViewModel> Questions { get; set; } = new();
}

public class TeacherQuestionInputViewModel
{
    [Required]
    [StringLength(1000)]
    public string Statement { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    public List<TeacherOptionInputViewModel> Options { get; set; } = new();
}

public class TeacherOptionInputViewModel
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class TeacherMediaUploadViewModel
{
    [Required]
    public int LessonId { get; set; }

    public IFormFile? PdfFile { get; set; }
    public IFormFile? VideoFile { get; set; }

    [StringLength(500)]
    public string? ExternalVideoUrl { get; set; }
}
