using E_learningProject.Core.Enums;

namespace E_learningProject.Web.Models;

public class QuizListItemViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public int QuestionCount { get; set; }
    public string? ModuleTitle { get; set; }
}

public class QuizTakeViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
}

public class QuizQuestionStepViewModel
{
    public int QuizId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int CurrentIndex { get; set; }
    public int TotalQuestions { get; set; }
    public QuizQuestionViewModel Question { get; set; } = new();
    public int? SelectedOptionId { get; set; }
    public string? AnswerText { get; set; }
    public bool HasAnswer { get; set; }
    public int ProgressPercent { get; set; }
}

public class QuizQuestionViewModel
{
    public int QuestionId { get; set; }
    public string Statement { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public List<QuizOptionViewModel> Options { get; set; } = new();
}

public class QuizOptionViewModel
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class QuizSubmissionViewModel
{
    public int QuizId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public List<QuizAnswerInputViewModel> Answers { get; set; } = new();
}

public class QuizAnswerInputViewModel
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? AnswerText { get; set; }
}

public class QuizResultViewModel
{
    public int QuizResultId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int PassingScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime AttemptDate { get; set; }
    public List<QuizQuestionCorrectionItemViewModel> Corrections { get; set; } = new();
}

public class QuizQuestionCorrectionItemViewModel
{
    public string Statement { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
