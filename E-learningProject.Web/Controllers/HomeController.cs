using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Public(CancellationToken cancellationToken)
    {
        var model = new HomePublicViewModel
        {
            TotalModules = await _dbContext.Modules.CountAsync(cancellationToken),
            TotalQuizzes = await _dbContext.Quizzes.CountAsync(cancellationToken),
            TotalThreads = await _dbContext.DiscussionThreads.CountAsync(cancellationToken),
            TotalUsers = await _dbContext.AppUsers.CountAsync(cancellationToken),
            OpenThreads = await _dbContext.DiscussionThreads.CountAsync(t => !t.IsResolved, cancellationToken),
            TotalQuizAttempts = await _dbContext.QuizResults.CountAsync(cancellationToken),
            RecentModules = await _dbContext.Modules
                .AsNoTracking()
                .OrderByDescending(m => m.Id)
                .Take(3)
                .Select(m => new HomeModuleItemViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    LessonCount = m.Lessons.Count,
                    HasQuiz = m.QuizId.HasValue
                })
                .ToListAsync(cancellationToken),
            RecentQuizzes = await _dbContext.Quizzes
                .AsNoTracking()
                .OrderByDescending(q => q.Id)
                .Take(3)
                .Select(q => new HomeQuizItemViewModel
                {
                    Id = q.Id,
                    Title = q.Title,
                    QuestionCount = q.Questions.Count,
                    PassingScore = q.PassingScore
                })
                .ToListAsync(cancellationToken),
            RecentThreads = await _dbContext.DiscussionThreads
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Take(3)
                .Select(t => new HomeDiscussionItemViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    ReplyCount = t.Replies.Count,
                    IsResolved = t.IsResolved
                })
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }
}
