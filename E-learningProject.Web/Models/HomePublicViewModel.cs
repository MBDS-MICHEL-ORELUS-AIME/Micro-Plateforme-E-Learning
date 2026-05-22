namespace E_learningProject.Web.Models;

public class HomePublicViewModel
{
    public int TotalModules { get; set; }

    public int TotalQuizzes { get; set; }

    public int TotalThreads { get; set; }

    public int TotalUsers { get; set; }

    public int OpenThreads { get; set; }

    public int TotalQuizAttempts { get; set; }

    public IReadOnlyList<HomeModuleItemViewModel> RecentModules { get; set; } = [];

    public IReadOnlyList<HomeQuizItemViewModel> RecentQuizzes { get; set; } = [];

    public IReadOnlyList<HomeDiscussionItemViewModel> RecentThreads { get; set; } = [];
}

public class HomeModuleItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int LessonCount { get; set; }

    public bool HasQuiz { get; set; }
}

public class HomeQuizItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public int PassingScore { get; set; }
}

public class HomeDiscussionItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int ReplyCount { get; set; }

    public bool IsResolved { get; set; }
}