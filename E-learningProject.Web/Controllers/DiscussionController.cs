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

    public async Task<IActionResult> Index(string studentId = DemoStudentId, string? q = null, string status = "all", int page = 1, int pageSize = 8, CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 40 ? 8 : pageSize;
        status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();

        var baseQuery = _dbContext.DiscussionThreads
            .AsNoTracking()
            .AsQueryable();

        var totalThreads = await baseQuery.CountAsync(cancellationToken);
        var openThreads = await baseQuery.CountAsync(t => !t.IsResolved, cancellationToken);
        var resolvedThreads = await baseQuery.CountAsync(t => t.IsResolved, cancellationToken);
        var totalReplies = await baseQuery.Select(t => t.Replies.Count).SumAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            baseQuery = baseQuery.Where(t => t.Title.ToLower().Contains(search) || t.StudentId.ToLower().Contains(search));
        }

        baseQuery = status switch
        {
            "open" => baseQuery.Where(t => !t.IsResolved),
            "resolved" => baseQuery.Where(t => t.IsResolved),
            _ => baseQuery
        };

        var totalItems = await baseQuery.CountAsync(cancellationToken);

        var threads = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new DiscussionThreadListItemViewModel
            {
                ThreadId = t.Id,
                Title = t.Title,
                StudentId = t.StudentId,
                ReplyCount = t.Replies.Count,
                IsResolved = t.IsResolved,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var viewModel = new DiscussionIndexViewModel
        {
            StudentId = studentId,
            SearchTerm = q?.Trim() ?? string.Empty,
            StatusFilter = status,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalThreads = totalThreads,
            OpenThreads = openThreads,
            ResolvedThreads = resolvedThreads,
            TotalReplies = totalReplies,
            Threads = threads
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
