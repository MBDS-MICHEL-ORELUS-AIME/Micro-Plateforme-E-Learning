using E_learningProject.Core.Entities;
using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class DiscussionController : Controller
{
    private const string DemoStudentId = "student.demo";
    private readonly ApplicationDbContext _dbContext;

    public DiscussionController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string studentId = DemoStudentId, CancellationToken cancellationToken = default)
    {
        var viewModel = new DiscussionIndexViewModel
        {
            StudentId = studentId,
            Threads = await _dbContext.DiscussionThreads
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new DiscussionThreadListItemViewModel
                {
                    ThreadId = t.Id,
                    Title = t.Title,
                    StudentId = t.StudentId,
                    ReplyCount = t.Replies.Count,
                    IsResolved = t.IsResolved,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string studentId = DemoStudentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return RedirectToAction(nameof(Index), new { studentId });
        }

        var thread = new DiscussionThread
        {
            Title = title.Trim(),
            StudentId = studentId,
            CreatedAt = DateTime.Now,
            IsResolved = false
        };

        _dbContext.DiscussionThreads.Add(thread);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = thread.Id, studentId });
    }

    public async Task<IActionResult> Details(int id, string studentId = DemoStudentId, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .Include(t => t.Replies)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (thread is null)
        {
            return NotFound();
        }

        var viewModel = new DiscussionDetailsViewModel
        {
            ThreadId = thread.Id,
            Title = thread.Title,
            StudentId = thread.StudentId,
            IsResolved = thread.IsResolved,
            CreatedAt = thread.CreatedAt,
            Replies = thread.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new DiscussionReplyItemViewModel
                {
                    AuthorId = r.AuthorId,
                    Message = r.Message,
                    CreatedAt = r.CreatedAt
                })
                .ToList()
        };

        ViewData["ActiveStudentId"] = studentId;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int threadId, string message, string authorId = DemoStudentId, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return RedirectToAction(nameof(Details), new { id = threadId, studentId = authorId });
        }

        var reply = new DiscussionReply
        {
            DiscussionThreadId = threadId,
            AuthorId = authorId,
            Message = message.Trim(),
            CreatedAt = DateTime.Now
        };

        _dbContext.DiscussionReplies.Add(reply);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = threadId, studentId = authorId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleResolved(int threadId, string studentId = DemoStudentId, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return NotFound();
        }

        thread.IsResolved = !thread.IsResolved;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = threadId, studentId });
    }
}
