using E_learningProject.Core.Enums;
using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace E_learningProject.Web.Controllers;

public class QuizController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQuizService _quizService;
    private readonly ICertificateService _certificateService;

    public QuizController(ApplicationDbContext dbContext, IQuizService quizService, ICertificateService certificateService)
    {
        _dbContext = dbContext;
        _quizService = quizService;
        _certificateService = certificateService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        var quizEntities = await _dbContext.Quizzes
            .AsNoTracking()
            .OrderBy(q => q.Title)
            .Include(q => q.Questions)
            .ToListAsync(cancellationToken);

        Dictionary<int, DateTime> passedByQuiz = new();
        Dictionary<int, DateTime> failedByQuiz = new();
        if (currentUserId.HasValue)
        {
            passedByQuiz = await _dbContext.QuizResults
                .AsNoTracking()
                .Where(r => r.UserId == currentUserId && r.IsPassed)
                .GroupBy(r => r.QuizId)
                .Select(g => new { QuizId = g.Key, PassedAt = g.Max(x => x.AttemptDate) })
                .ToDictionaryAsync(x => x.QuizId, x => x.PassedAt, cancellationToken);

            failedByQuiz = await _dbContext.QuizResults
                .AsNoTracking()
                .Where(r => r.UserId == currentUserId && !r.IsPassed)
                .GroupBy(r => r.QuizId)
                .Select(g => new { QuizId = g.Key, FailedAt = g.Max(x => x.AttemptDate) })
                .ToDictionaryAsync(x => x.QuizId, x => x.FailedAt, cancellationToken);
        }

        var quizzes = quizEntities.Select(q => new QuizListItemViewModel
        {
            QuizId = q.Id,
            QuizTitle = q.Title,
            PassingScore = q.PassingScore,
            QuestionCount = q.Questions.Count,
            ModuleTitle = _dbContext.Modules.Where(m => m.QuizId == q.Id).Select(m => m.Title).FirstOrDefault(),
            AlreadyPassed = passedByQuiz.ContainsKey(q.Id),
            PassedAt = passedByQuiz.ContainsKey(q.Id) ? passedByQuiz[q.Id] : null,
            HasFailedAttempt = !passedByQuiz.ContainsKey(q.Id) && failedByQuiz.ContainsKey(q.Id),
            FailedAt = !passedByQuiz.ContainsKey(q.Id) && failedByQuiz.ContainsKey(q.Id) ? failedByQuiz[q.Id] : null
        }).ToList();

        return View(quizzes);
    }

    [HttpGet]
    public IActionResult Public()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Start(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Start), "Quiz", new { id }) });
        }

        return RedirectToAction(nameof(Take), new { id });
    }

    public async Task<IActionResult> Take(int id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Take), "Quiz", new { id }) });
        }
        var userName = HttpContext.Session.GetString("CurrentUserName") ?? userId.Value.ToString();

        var quiz = await _dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var viewModel = new QuizTakeViewModel
        {
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            PassingScore = quiz.PassingScore,
            StudentId = userName,
            TotalQuestions = quiz.Questions.Count
        };

        ResetStoredAnswers(quiz.Id, userId.Value.ToString());

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Question(
        int quizId,
        int index,
        string? studentId = null,
        int? questionId = null,
        int? selectedOptionId = null,
        string? answerText = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Start), "Quiz", new { id = quizId }) });
        }
        var userKey = userId.Value.ToString();

        var quiz = await _dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        PersistAnswer(quizId, userKey, questionId, selectedOptionId, answerText);

        var orderedQuestions = quiz.Questions.OrderBy(q => q.Id).ToList();
        if (!orderedQuestions.Any())
        {
            return Content("<div class='alert alert-warning'>Aucune question n'est disponible pour ce quiz.</div>", "text/html");
        }

        index = Math.Clamp(index, 0, orderedQuestions.Count - 1);
        var current = orderedQuestions[index];
        var answers = GetStoredAnswers(quizId, userKey);
        answers.TryGetValue(current.Id, out var stored);

        var viewModel = new QuizQuestionStepViewModel
        {
            QuizId = quizId,
            StudentId = HttpContext.Session.GetString("CurrentUserName") ?? userKey,
            CurrentIndex = index,
            TotalQuestions = orderedQuestions.Count,
            SelectedOptionId = stored?.SelectedOptionId,
            AnswerText = stored?.AnswerText,
            HasAnswer = HasAnswer(current.Type, stored?.SelectedOptionId, stored?.AnswerText),
            ProgressPercent = (int)Math.Round(((index + 1) / (double)orderedQuestions.Count) * 100),
            Question = new QuizQuestionViewModel
            {
                QuestionId = current.Id,
                Statement = current.Statement,
                Type = current.Type,
                Options = current.Options
                    .OrderBy(o => o.Id)
                    .Select(o => new QuizOptionViewModel
                    {
                        OptionId = o.Id,
                        Text = o.Text
                    })
                    .ToList()
            }
        };

        return PartialView("_QuizQuestionStep", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Finish(
        int quizId,
        string? studentId = null,
        int? questionId = null,
        int? selectedOptionId = null,
        string? answerText = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Start), "Quiz", new { id = quizId }) });
        }
        var userKey = userId.Value.ToString();

        PersistAnswer(quizId, userKey, questionId, selectedOptionId, answerText);

        var quiz = await _dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var answers = GetStoredAnswers(quizId, userKey);
        var correctAnswers = 0;
        var corrections = new List<QuizQuestionCorrectionItemViewModel>();

        foreach (var question in quiz.Questions)
        {
            answers.TryGetValue(question.Id, out var answer);

            if (!HasAnswer(question.Type, answer?.SelectedOptionId, answer?.AnswerText))
            {
                corrections.Add(new QuizQuestionCorrectionItemViewModel
                {
                    Statement = question.Statement,
                    QuestionType = question.Type.ToString(),
                    UserAnswer = "Aucune réponse",
                    CorrectAnswer = GetCorrectAnswerText(question),
                    IsCorrect = false
                });
                continue;
            }

            var isCorrect = question.Type switch
            {
                QuestionType.ShortAnswer => IsShortAnswerCorrect(question, answer!.AnswerText),
                _ => IsOptionAnswerCorrect(question, answer!.SelectedOptionId)
            };

            if (isCorrect)
            {
                correctAnswers++;
            }

            corrections.Add(new QuizQuestionCorrectionItemViewModel
            {
                Statement = question.Statement,
                QuestionType = question.Type.ToString(),
                UserAnswer = GetUserAnswerText(question, answer!),
                CorrectAnswer = GetCorrectAnswerText(question),
                IsCorrect = isCorrect
            });
        }

        // La notation est déléguée au service pour garder une logique de contrôleur légère et réutilisable.
        var score = _quizService.CalculateScore(quiz.Questions.Count, correctAnswers);
        var isPassed = score >= quiz.PassingScore;

        var result = new Core.Entities.QuizResult
        {
            UserId = userId.Value,
            QuizId = quiz.Id,
            Score = score,
            IsPassed = isPassed,
            AttemptDate = DateTime.UtcNow
        };

        _dbContext.QuizResults.Add(result);
        await _dbContext.SaveChangesAsync(cancellationToken);

        PersistCorrections(result.Id, corrections);
        ResetStoredAnswers(quizId, userKey);

        // La redirection HTMX garde le parcours par étapes dynamique sans postback complet de la page.
        Response.Headers["HX-Redirect"] = Url.Action(nameof(Result), new { id = result.Id }) ?? Url.Action(nameof(Index)) ?? "/Quiz";
        return new EmptyResult();
    }

    public async Task<IActionResult> Result(int id, CancellationToken cancellationToken)
    {
        var result = await _dbContext.QuizResults
            .AsNoTracking()
            .Include(r => r.Quiz)
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (result is null || result.Quiz is null)
        {
            return NotFound();
        }

        var corrections = GetStoredCorrections(result.Id);
        var displayScore = (int)Math.Round(result.Score);
        var displayIsPassed = result.IsPassed;

        if (corrections.Count > 0)
        {
            var correctCount = corrections.Count(c => c.IsCorrect);
            displayScore = _quizService.CalculateScore(corrections.Count, correctCount);
            displayIsPassed = displayScore >= result.Quiz.PassingScore;
        }

        var viewModel = new QuizResultViewModel
        {
            QuizResultId = result.Id,
            QuizId = result.QuizId,
            QuizTitle = result.Quiz.Title,
            StudentId = result.Student?.UserName ?? result.UserId.ToString(),
            Score = displayScore,
            PassingScore = result.Quiz.PassingScore,
            IsPassed = displayIsPassed,
            AttemptDate = result.AttemptDate,
            Corrections = corrections
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> LastAttemptForUser(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return BadRequest("userName is required");
        }

        var last = await _dbContext.QuizResults
            .AsNoTracking()
            .Include(r => r.Quiz)
            .Include(r => r.Student)
            .Where(r => r.Student != null && r.Student.UserName == userName)
            .OrderByDescending(r => r.AttemptDate)
            .Select(r => new
            {
                r.Id,
                r.QuizId,
                QuizTitle = r.Quiz != null ? r.Quiz.Title : string.Empty,
                r.Score,
                r.IsPassed,
                r.AttemptDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (last is null)
        {
            return NotFound();
        }

        return Ok(last);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCertificate(int quizResultId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Result), "Quiz", new { id = quizResultId }) });
        }
        var userName = HttpContext.Session.GetString("CurrentUserName") ?? userId.Value.ToString();

        var result = await _dbContext.QuizResults
            .AsNoTracking()
            .Include(r => r.Quiz)
            .FirstOrDefaultAsync(r => r.Id == quizResultId && r.UserId == userId, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        if (!result.IsPassed)
        {
            return BadRequest("Le quiz doit être réussi pour générer un certificat.");
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.QuizId == result.QuizId, cancellationToken);

        if (module is null)
        {
            return BadRequest("Aucun module lié à ce quiz n'a été trouvé.");
        }

        var recipientName = await ResolveCertificateRecipientName(userName, cancellationToken);
        var viewModel = new CertificateDownloadConfirmationViewModel
        {
            ModuleId = module.Id,
            ModuleTitle = module.Title,
            RecipientName = recipientName,
            QuizResultId = quizResultId
        };

        return View(viewModel);
    }

    [HttpPost]
    [ActionName("DownloadCertificate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadCertificateConfirmed(int quizResultId, CancellationToken cancellationToken = default)
    {
        var studentId = GetCurrentUserName();
        if (studentId is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Result), "Quiz", new { id = quizResultId }) });
        }
        var userId = GetCurrentUserId();

        var result = await _dbContext.QuizResults
            .AsNoTracking()
            .Include(r => r.Quiz)
            .FirstOrDefaultAsync(r => r.Id == quizResultId && r.UserId == userId, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        if (!result.IsPassed)
        {
            return BadRequest("Le quiz doit être réussi pour générer un certificat.");
        }

        var module = await _dbContext.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.QuizId == result.QuizId, cancellationToken);

        if (module is null)
        {
            return BadRequest("Aucun module lié à ce quiz n'a été trouvé.");
        }

        var certificate = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.ModuleId == module.Id && c.UserId == userId, cancellationToken);

        if (certificate is null)
        {
            certificate = new Core.Entities.Certificate
            {
                ModuleId = module.Id,
                UserId = userId.Value,
                UniqueCode = _certificateService.GenerateCertificateNumber(studentId, module.Id),
                IssueDate = DateTime.UtcNow
            };

            _dbContext.Certificates.Add(certificate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var recipientName = await ResolveCertificateRecipientName(studentId, cancellationToken);
        var pdfBytes = _certificateService.GenerateCertificatePdf(recipientName, module.Title, certificate.UniqueCode, certificate.IssueDate);
        var fileName = $"certificate-{certificate.UniqueCode}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private async Task<string> ResolveCertificateRecipientName(string studentId, CancellationToken cancellationToken)
    {
        string? fullName = null;

        var currentUserIdRaw = HttpContext.Session.GetString("CurrentUserId");
        if (int.TryParse(currentUserIdRaw, out var currentUserId))
        {
            fullName = await _dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.Id == currentUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = await _dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.UserName == studentId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return string.IsNullOrWhiteSpace(fullName)
            ? "Apprenant"
            : fullName.Trim();
    }

    private Dictionary<int, QuizAnswerInputViewModel> GetStoredAnswers(int quizId, string studentId)
    {
        var key = GetAnswerSessionKey(quizId, studentId);
        var json = HttpContext.Session.GetString(key);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<int, QuizAnswerInputViewModel>();
        }

        // La charge utile de session stocke une réponse par question pour prendre en charge la navigation précédent/suivant.
        return JsonSerializer.Deserialize<Dictionary<int, QuizAnswerInputViewModel>>(json)
            ?? new Dictionary<int, QuizAnswerInputViewModel>();
    }

    private void PersistAnswer(int quizId, string studentId, int? questionId, int? selectedOptionId, string? answerText)
    {
        if (questionId is null)
        {
            return;
        }

        var answers = GetStoredAnswers(quizId, studentId);
        answers[questionId.Value] = new QuizAnswerInputViewModel
        {
            QuestionId = questionId.Value,
            SelectedOptionId = selectedOptionId,
            AnswerText = answerText
        };

        var key = GetAnswerSessionKey(quizId, studentId);
        HttpContext.Session.SetString(key, JsonSerializer.Serialize(answers));
    }

    private void ResetStoredAnswers(int quizId, string studentId)
    {
        HttpContext.Session.Remove(GetAnswerSessionKey(quizId, studentId));
    }

    private static string GetAnswerSessionKey(int quizId, string studentId)
        => $"quiz-answers:{quizId}:{studentId}";

    private static bool IsOptionAnswerCorrect(Core.Entities.Question question, int? selectedOptionId)
    {
        if (selectedOptionId is null)
        {
            return false;
        }

        return question.Options.Any(o => o.Id == selectedOptionId && o.IsCorrect);
    }

    private static bool IsShortAnswerCorrect(Core.Entities.Question question, string? answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
        {
            return false;
        }

        var normalizedAnswer = answerText.Trim();
        return question.Options.Any(o =>
            o.IsCorrect &&
            string.Equals(o.Text.Trim(), normalizedAnswer, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAnswer(QuestionType type, int? selectedOptionId, string? answerText)
    {
        return type switch
        {
            QuestionType.ShortAnswer => !string.IsNullOrWhiteSpace(answerText),
            _ => selectedOptionId.HasValue
        };
    }

    private static string GetCorrectAnswerText(Core.Entities.Question question)
    {
        var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Text.Trim()).ToList();
        return correctOptions.Count == 0 ? "N/D" : string.Join(" / ", correctOptions);
    }

    private static string GetUserAnswerText(Core.Entities.Question question, QuizAnswerInputViewModel answer)
    {
        if (question.Type == QuestionType.ShortAnswer)
        {
            return string.IsNullOrWhiteSpace(answer.AnswerText) ? "Aucune réponse" : answer.AnswerText.Trim();
        }

        if (!answer.SelectedOptionId.HasValue)
        {
            return "Aucune réponse";
        }

        var option = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);
        return option?.Text ?? "Option inconnue";
    }

    private void PersistCorrections(int resultId, List<QuizQuestionCorrectionItemViewModel> corrections)
    {
        var key = GetCorrectionSessionKey(resultId);
        HttpContext.Session.SetString(key, JsonSerializer.Serialize(corrections));
    }

    private List<QuizQuestionCorrectionItemViewModel> GetStoredCorrections(int resultId)
    {
        var key = GetCorrectionSessionKey(resultId);
        var json = HttpContext.Session.GetString(key);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<QuizQuestionCorrectionItemViewModel>();
        }

        return JsonSerializer.Deserialize<List<QuizQuestionCorrectionItemViewModel>>(json)
            ?? new List<QuizQuestionCorrectionItemViewModel>();
    }

    private static string GetCorrectionSessionKey(int resultId)
        => $"quiz-corrections:{resultId}";

    private int? GetCurrentUserId()
    {
        var userIdStr = HttpContext.Session.GetString("CurrentUserId");
        if (!string.IsNullOrWhiteSpace(userIdStr) && int.TryParse(userIdStr, out var id))
            return id;
        return null;
    }

    private string? GetCurrentUserName()
    {
        var userName = HttpContext.Session.GetString("CurrentUserName");
        return string.IsNullOrWhiteSpace(userName) ? null : userName;
    }
}
