using E_learningProject.Core.Entities;
using E_learningProject.Data.Context;
using E_learningProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Web.Controllers;

public class DiscussionController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public DiscussionController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? q = null, string status = "all", int page = 1, int pageSize = 8, CancellationToken cancellationToken = default)
    {
        var currentUserId = ResolveCurrentUserId();
        var currentUserName = GetCurrentUserName();
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 40 ? 8 : pageSize;
        status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        var baseQuery = _dbContext.DiscussionThreads.AsNoTracking().AsQueryable();
        var totalThreads = await baseQuery.CountAsync(cancellationToken);
        var openThreads = await baseQuery.CountAsync(t => !t.IsResolved, cancellationToken);
        var resolvedThreads = await baseQuery.CountAsync(t => t.IsResolved, cancellationToken);
        var totalReplies = await baseQuery.Select(t => t.Replies.Count).SumAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            baseQuery = baseQuery.Include(t => t.Author).Where(t => t.Title.ToLower().Contains(search) || (t.Author != null && t.Author.UserName.ToLower().Contains(search)));
        }
        baseQuery = status switch
        {
            "open" => baseQuery.Where(t => !t.IsResolved),
            "resolved" => baseQuery.Where(t => t.IsResolved),
            _ => baseQuery
        };
        var totalItems = await baseQuery.CountAsync(cancellationToken);
        var threads = await baseQuery.OrderByDescending(t => t.CreatedAt).Include(t => t.Replies).Include(t => t.Author).Skip((page-1)*pageSize).Take(pageSize)
            .Select(t => new DiscussionThreadListItemViewModel { ThreadId = t.Id, Title = t.Title, StudentId = t.Author != null ? t.Author.UserName : t.AuthorId.ToString(), ReplyCount = t.Replies.Count, IsResolved = t.IsResolved, CreatedAt = t.CreatedAt })
            .ToListAsync(cancellationToken);
        var viewModel = new DiscussionIndexViewModel
        {
            StudentId = currentUserName ?? string.Empty,
            SearchTerm = q?.Trim() ?? string.Empty,
            StatusFilter = status, CurrentPage = page, PageSize = pageSize, TotalItems = totalItems,
            TotalThreads = totalThreads, OpenThreads = openThreads,
            ResolvedThreads = resolvedThreads, TotalReplies = totalReplies,
            Threads = threads
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, CancellationToken cancellationToken = default)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null) return RedirectToAction("Login", "Account");
        if (string.IsNullOrWhiteSpace(title)) return RedirectToAction(nameof(Index));
        var thread = new DiscussionThread { Title = title.Trim(), AuthorId = userId.Value, CreatedAt = DateTime.UtcNow, IsResolved = false };
        _dbContext.DiscussionThreads.Add(thread);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id = thread.Id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = ResolveCurrentUserId();
        var currentUserName = GetCurrentUserName();
        var thread = await _dbContext.DiscussionThreads.AsNoTracking().Include(t => t.Replies).ThenInclude(r => r.Author).Include(t => t.Author).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (thread is null) return NotFound();
        var viewModel = new DiscussionDetailsViewModel
        {
            ThreadId = thread.Id, Title = thread.Title,
            StudentId = thread.Author?.UserName ?? thread.AuthorId.ToString(), IsResolved = thread.IsResolved, CreatedAt = thread.CreatedAt,
            Replies = thread.Replies.OrderBy(r => r.CreatedAt).Select(r => new DiscussionReplyItemViewModel { AuthorId = r.Author?.UserName ?? r.AuthorId.ToString(), Message = r.Message, CreatedAt = r.CreatedAt }).ToList()
        };
        ViewData["ActiveStudentId"] = currentUserName ?? string.Empty;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int threadId, string message, CancellationToken cancellationToken = default)
    {
        var authorId = ResolveCurrentUserId();
        if (authorId is null) return RedirectToAction("Login", "Account");
        var thread = await _dbContext.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);
        if (thread is null) return NotFound();
        if (string.IsNullOrWhiteSpace(message)) return RedirectToAction(nameof(Details), new { id = threadId });
        _dbContext.DiscussionReplies.Add(new DiscussionReply { DiscussionThreadId = threadId, AuthorId = authorId.Value, Message = message.Trim(), CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id = threadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleResolved(int threadId, CancellationToken cancellationToken = default)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null) return RedirectToAction("Login", "Account");
        var thread = await _dbContext.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);
        if (thread is null) return NotFound();
        if (thread.AuthorId != userId) return Forbid();
        thread.IsResolved = !thread.IsResolved;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id = threadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int threadId, string reason, CancellationToken cancellationToken = default)
    {
        var reporterId = ResolveCurrentUserId();
        if (reporterId is null) return RedirectToAction("Login", "Account");
        if (string.IsNullOrWhiteSpace(reason)) return RedirectToAction(nameof(Details), new { id = threadId });
        var already = await _dbContext.DiscussionReports.AnyAsync(r => r.ThreadId == threadId && r.ReporterId == reporterId, cancellationToken);
        if (!already)
        {
            _dbContext.DiscussionReports.Add(new Core.Entities.DiscussionReport { ThreadId = threadId, ReporterId = reporterId.Value, Reason = reason.Trim(), ReportedAt = DateTime.UtcNow, IsHandled = false, HandlerNote = string.Empty });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        TempData["ReportSent"] = "true";
        return RedirectToAction(nameof(Details), new { id = threadId });
    }

    private int? ResolveCurrentUserId()
    {
        var userIdStr = HttpContext.Session.GetString("CurrentUserId");
        if (!string.IsNullOrWhiteSpace(userIdStr) && int.TryParse(userIdStr, out var id))
            return id;
        return null;
    }

    private string? GetCurrentUserName()
    {
        var name = HttpContext.Session.GetString("CurrentUserName");
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}

