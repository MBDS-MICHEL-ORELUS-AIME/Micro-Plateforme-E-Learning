using E_learningProject.Core.Entities;
using E_learningProject.Core.Enums;
using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class TeacherController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public TeacherController(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Workspace(CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Workspace), "Teacher") });
        }

        var viewModel = await BuildWorkspaceViewModel(cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MyQuizzes(CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(MyQuizzes), "Teacher") });
        }

        var quizzes = await _dbContext.Quizzes
            .AsNoTracking()
            .OrderByDescending(q => q.Id)
            .Select(q => new TeacherQuizManageItemViewModel
            {
                QuizId = q.Id,
                Title = q.Title,
                PassingScore = q.PassingScore,
                QuestionCount = q.Questions.Count,
                ModuleTitle = _dbContext.Modules.Where(m => m.QuizId == q.Id).Select(m => m.Title).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var quizIds = quizzes.Select(q => q.QuizId).ToList();
        var quizResultsQuery = _dbContext.QuizResults.AsNoTracking().Where(r => quizIds.Contains(r.QuizId));
        var totalAttempts = await quizResultsQuery.CountAsync(cancellationToken);
        var passedAttempts = await quizResultsQuery.CountAsync(r => r.IsPassed, cancellationToken);
        var averageScore = totalAttempts == 0
            ? 0
            : await quizResultsQuery.AverageAsync(r => r.Score, cancellationToken);

        var viewModel = new TeacherMyQuizzesViewModel
        {
            TotalAttempts = totalAttempts,
            PassedAttempts = passedAttempts,
            AverageScore = Math.Round(averageScore, 1),
            Quizzes = quizzes
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateModule(TeacherModuleCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Workspace), "Teacher") });
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildWorkspaceViewModel(cancellationToken);
            invalidViewModel.ModuleForm = form;
            return View("Workspace", invalidViewModel);
        }

        _dbContext.Modules.Add(new Module
        {
            Title = form.Title.Trim(),
            Description = form.Description.Trim()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["TeacherSuccess"] = "Module créé avec succès.";
        return RedirectToAction(nameof(Workspace));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson(TeacherLessonCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Workspace), "Teacher") });
        }

        var moduleExists = await _dbContext.Modules.AnyAsync(m => m.Id == form.ModuleId, cancellationToken);
        if (!moduleExists)
        {
            ModelState.AddModelError(nameof(form.ModuleId), "Veuillez sélectionner un module valide.");
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildWorkspaceViewModel(cancellationToken);
            invalidViewModel.LessonForm = form;
            return View("Workspace", invalidViewModel);
        }

        _dbContext.Lessons.Add(new Lesson
        {
            ModuleId = form.ModuleId,
            Title = form.Title.Trim(),
            TextContent = form.TextContent.Trim(),
            Order = form.Order
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["TeacherSuccess"] = "Leçon créée avec succès.";
        return RedirectToAction(nameof(Workspace));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuiz(TeacherQuizCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Workspace), "Teacher") });
        }

        NormalizeQuizForm(form);
        ValidateQuizForm(form);

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildWorkspaceViewModel(cancellationToken);
            invalidViewModel.QuizForm = form;
            invalidViewModel.MediaForm = new TeacherMediaUploadViewModel();
            return View("Workspace", invalidViewModel);
        }

        var quiz = new Quiz
        {
            Title = form.Title.Trim(),
            PassingScore = form.PassingScore,
            Questions = form.Questions.Select(q => new Question
            {
                Statement = q.Statement.Trim(),
                Type = q.Type,
                Options = BuildOptions(q)
            }).ToList()
        };

        _dbContext.Quizzes.Add(quiz);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == form.ModuleId, cancellationToken);
        if (module is not null)
        {
            module.QuizId = quiz.Id;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        TempData["TeacherSuccess"] = "Quiz créé avec succès.";
        return RedirectToAction(nameof(Workspace));
    }

    [HttpGet]
    public async Task<IActionResult> EditQuiz(int id, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(EditQuiz), "Teacher", new { id }) });
        }

        var quiz = await _dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var moduleId = await _dbContext.Modules
            .AsNoTracking()
            .Where(m => m.QuizId == id)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var form = new TeacherQuizCreateViewModel
        {
            QuizId = quiz.Id,
            Title = quiz.Title,
            PassingScore = quiz.PassingScore,
            ModuleId = moduleId,
            Questions = quiz.Questions
                .OrderBy(q => q.Id)
                .Select(q => new TeacherQuestionInputViewModel
                {
                    Statement = q.Statement,
                    Type = q.Type,
                    Options = q.Options
                        .OrderBy(o => o.Id)
                        .Select(o => new TeacherOptionInputViewModel
                        {
                            Text = o.Text,
                            IsCorrect = o.IsCorrect
                        })
                        .ToList()
                })
                .ToList()
        };

        var viewModel = await BuildWorkspaceViewModel(cancellationToken);
        viewModel.QuizForm = form;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuiz(TeacherQuizCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(MyQuizzes), "Teacher") });
        }

        if (!form.QuizId.HasValue)
        {
            return BadRequest();
        }

        var quiz = await _dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == form.QuizId.Value, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        NormalizeQuizForm(form);
        ValidateQuizForm(form);

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildWorkspaceViewModel(cancellationToken);
            invalidViewModel.QuizForm = form;
            return View("EditQuiz", invalidViewModel);
        }

        quiz.Title = form.Title.Trim();
        quiz.PassingScore = form.PassingScore;

        _dbContext.Options.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        _dbContext.Questions.RemoveRange(quiz.Questions);

        quiz.Questions = form.Questions.Select(q => new Question
        {
            Statement = q.Statement.Trim(),
            Type = q.Type,
            Options = BuildOptions(q)
        }).ToList();

        var previousModule = await _dbContext.Modules.FirstOrDefaultAsync(m => m.QuizId == quiz.Id, cancellationToken);
        if (previousModule is not null && previousModule.Id != form.ModuleId)
        {
            previousModule.QuizId = null;
        }

        var targetModule = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == form.ModuleId, cancellationToken);
        if (targetModule is not null)
        {
            targetModule.QuizId = quiz.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["TeacherSuccess"] = "Quiz mis à jour avec succès.";
        return RedirectToAction(nameof(MyQuizzes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(int id, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(MyQuizzes), "Teacher") });
        }

        var quiz = await _dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quiz is null)
        {
            return NotFound();
        }

        var relatedModules = await _dbContext.Modules.Where(m => m.QuizId == id).ToListAsync(cancellationToken);
        foreach (var module in relatedModules)
        {
            module.QuizId = null;
        }

        _dbContext.Options.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        _dbContext.Questions.RemoveRange(quiz.Questions);
        _dbContext.Quizzes.Remove(quiz);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["TeacherSuccess"] = "Quiz supprimé.";
        return RedirectToAction(nameof(MyQuizzes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMedia(TeacherMediaUploadViewModel form, CancellationToken cancellationToken)
    {
        if (!CanTeach())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Workspace), "Teacher") });
        }

        if (form.PdfFile is null && form.VideoFile is null && string.IsNullOrWhiteSpace(form.ExternalVideoUrl))
        {
            ModelState.AddModelError(string.Empty, "Importez au moins un média ou fournissez une URL vidéo.");
        }

        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(l => l.Id == form.LessonId, cancellationToken);
        if (lesson is null)
        {
            ModelState.AddModelError(nameof(form.LessonId), "La leçon sélectionnée n'existe pas.");
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildWorkspaceViewModel(cancellationToken);
            invalidViewModel.MediaForm = form;
            return View("Workspace", invalidViewModel);
        }

        if (form.PdfFile is not null && lesson is not null)
        {
            var pdfRelativePath = await SaveFile(form.PdfFile, "uploads/pdfs", cancellationToken);
            lesson.PdfPath = "/" + pdfRelativePath.Replace("\\", "/");
        }

        if (form.VideoFile is not null && lesson is not null)
        {
            var videoRelativePath = await SaveFile(form.VideoFile, "uploads/videos", cancellationToken);
            lesson.VideoUrl = "/" + videoRelativePath.Replace("\\", "/");
        }

        if (!string.IsNullOrWhiteSpace(form.ExternalVideoUrl) && lesson is not null)
        {
            lesson.VideoUrl = form.ExternalVideoUrl.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["TeacherSuccess"] = "Média importé et lié à la leçon.";
        return RedirectToAction(nameof(Workspace));
    }

    private async Task<TeacherWorkspaceViewModel> BuildWorkspaceViewModel(CancellationToken cancellationToken)
    {
        var modules = await _dbContext.Modules
            .AsNoTracking()
            .OrderBy(m => m.Title)
            .Select(m => new TeacherOptionViewModel { Id = m.Id, Label = m.Title })
            .ToListAsync(cancellationToken);

        var lessons = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.Module)
            .OrderBy(l => l.Module!.Title)
            .ThenBy(l => l.Order)
            .Select(l => new TeacherLessonOptionViewModel
            {
                Id = l.Id,
                ModuleId = l.ModuleId,
                Label = (l.Module != null ? l.Module.Title : "Module") + " - " + l.Title
            })
            .ToListAsync(cancellationToken);

        var quizzes = await _dbContext.Quizzes
            .AsNoTracking()
            .OrderByDescending(q => q.Id)
            .Take(8)
            .Select(q => new TeacherQuizSummaryViewModel
            {
                QuizId = q.Id,
                Title = q.Title,
                PassingScore = q.PassingScore,
                QuestionCount = q.Questions.Count
            })
            .ToListAsync(cancellationToken);

        var totalModules = await _dbContext.Modules.AsNoTracking().CountAsync(cancellationToken);
        var totalLessons = await _dbContext.Lessons.AsNoTracking().CountAsync(cancellationToken);
        var totalQuizzes = await _dbContext.Quizzes.AsNoTracking().CountAsync(cancellationToken);
        var openDiscussions = await _dbContext.DiscussionThreads.AsNoTracking().CountAsync(t => !t.IsResolved, cancellationToken);

        var recentDiscussionThreads = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new TeacherDiscussionSnapshotViewModel
            {
                ThreadId = t.Id,
                Title = t.Title,
                ReplyCount = t.Replies.Count,
                IsResolved = t.IsResolved
            })
            .ToListAsync(cancellationToken);

        return new TeacherWorkspaceViewModel
        {
            ModuleForm = new TeacherModuleCreateViewModel(),
            LessonForm = new TeacherLessonCreateViewModel
            {
                ModuleId = modules.FirstOrDefault()?.Id ?? 0,
                Order = 1
            },
            QuizForm = BuildDefaultQuizForm(modules),
            MediaForm = new TeacherMediaUploadViewModel(),
            TotalModules = totalModules,
            TotalLessons = totalLessons,
            TotalQuizzes = totalQuizzes,
            OpenDiscussions = openDiscussions,
            RecentDiscussionThreads = recentDiscussionThreads,
            ModuleOptions = modules,
            LessonOptions = lessons,
            ExistingQuizzes = quizzes
        };
    }

    private static TeacherQuizCreateViewModel BuildDefaultQuizForm(List<TeacherOptionViewModel> modules)
    {
        return new TeacherQuizCreateViewModel
        {
            ModuleId = modules.FirstOrDefault()?.Id ?? 0,
            Questions = new List<TeacherQuestionInputViewModel>
            {
                new()
                {
                    Statement = string.Empty,
                    Type = QuestionType.MultipleChoice,
                    Options = new List<TeacherOptionInputViewModel>
                    {
                        new(), new()
                    }
                }
            }
        };
    }

    private static void NormalizeQuizForm(TeacherQuizCreateViewModel form)
    {
        form.Questions ??= new List<TeacherQuestionInputViewModel>();

        foreach (var question in form.Questions)
        {
            question.Options ??= new List<TeacherOptionInputViewModel>();
        }
    }

    private void ValidateQuizForm(TeacherQuizCreateViewModel form)
    {
        if (!form.Questions.Any())
        {
            ModelState.AddModelError(string.Empty, "Au moins une question est requise.");
            return;
        }

        if (!_dbContext.Modules.Any(m => m.Id == form.ModuleId))
        {
            ModelState.AddModelError(nameof(form.ModuleId), "Veuillez sélectionner un module valide.");
        }

        for (var i = 0; i < form.Questions.Count; i++)
        {
            var question = form.Questions[i];

            if (string.IsNullOrWhiteSpace(question.Statement))
            {
                ModelState.AddModelError($"Questions[{i}].Statement", "L'énoncé de la question est requis.");
            }

            if (question.Type == QuestionType.ShortAnswer)
            {
                var correctShort = question.Options.Count(o => o.IsCorrect && !string.IsNullOrWhiteSpace(o.Text));
                if (correctShort == 0)
                {
                    ModelState.AddModelError($"Questions[{i}].Options", "La réponse courte doit définir une réponse attendue correcte.");
                }
            }
            else
            {
                var nonEmptyOptions = question.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();
                if (nonEmptyOptions.Count < 2)
                {
                    ModelState.AddModelError($"Questions[{i}].Options", "Fournissez au moins 2 options.");
                }

                if (!nonEmptyOptions.Any(o => o.IsCorrect))
                {
                    ModelState.AddModelError($"Questions[{i}].Options", "Marquez au moins une option correcte.");
                }
            }
        }
    }

    private static List<Option> BuildOptions(TeacherQuestionInputViewModel question)
    {
        if (question.Type == QuestionType.TrueFalse)
        {
            var hasTrue = question.Options.Any(o => string.Equals(o.Text.Trim(), "Vrai", StringComparison.OrdinalIgnoreCase) || string.Equals(o.Text.Trim(), "True", StringComparison.OrdinalIgnoreCase));
            var hasFalse = question.Options.Any(o => string.Equals(o.Text.Trim(), "Faux", StringComparison.OrdinalIgnoreCase) || string.Equals(o.Text.Trim(), "False", StringComparison.OrdinalIgnoreCase));

            if (hasTrue && hasFalse)
            {
                return question.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                    .Select(o => new Option { Text = o.Text.Trim(), IsCorrect = o.IsCorrect })
                    .ToList();
            }

            var trueIsCorrect = question.Options.FirstOrDefault(o => o.IsCorrect)?.Text?.Trim().Equals("Vrai", StringComparison.OrdinalIgnoreCase) == true
                || question.Options.FirstOrDefault(o => o.IsCorrect)?.Text?.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) == true;
            return new List<Option>
            {
                new() { Text = "Vrai", IsCorrect = trueIsCorrect },
                new() { Text = "Faux", IsCorrect = !trueIsCorrect }
            };
        }

        return question.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .Select(o => new Option { Text = o.Text.Trim(), IsCorrect = o.IsCorrect })
            .ToList();
    }

    private async Task<string> SaveFile(IFormFile file, string relativeFolder, CancellationToken cancellationToken)
    {
        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var folderPath = Path.Combine(_environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(folderPath, safeFileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine(relativeFolder, safeFileName);
    }

    private bool CanTeach()
    {
        var role = HttpContext.Session.GetString("CurrentUserRole");
        return string.Equals(role, "enseignant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "coordinateur", StringComparison.OrdinalIgnoreCase);
    }
}
