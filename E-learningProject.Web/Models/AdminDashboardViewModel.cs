namespace E_learningProject.Web.Models;

public class AdminDashboardViewModel
{
    public bool IsSuperAdminView { get; set; }
    public int SelectedPeriodDays { get; set; }
    public int TotalModules { get; set; }
    public int TotalLessons { get; set; }
    public int TotalQuizzes { get; set; }
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public int CertificatesIssued { get; set; }
    public int QuizAttempts { get; set; }
    public double QuizPassRate { get; set; }

    public List<ModuleOverviewItem> RecentModules { get; set; } = new();
    public List<QuizAttemptItem> RecentQuizAttempts { get; set; } = new();
    public List<DiscussionThreadItem> RecentDiscussionThreads { get; set; } = new();
    public List<UserRoleBreakdownItem> UsersByRole { get; set; } = new();
    public List<CertificateIssueItem> RecentCertificates { get; set; } = new();
    public List<string> EnrollmentChartLabels { get; set; } = new();
    public List<int> EnrollmentChartValues { get; set; } = new();
    public List<string> QuizChartLabels { get; set; } = new();
    public List<double> QuizChartValues { get; set; } = new();
}

public class ModuleOverviewItem
{
    public string Title { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public bool HasFinalQuiz { get; set; }
}

public class QuizAttemptItem
{
    public string QuizTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptDate { get; set; }
}

public class DiscussionThreadItem
{
    public string Title { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public int ReplyCount { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserRoleBreakdownItem
{
    public string RoleName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CertificateIssueItem
{
    public string StudentId { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
}