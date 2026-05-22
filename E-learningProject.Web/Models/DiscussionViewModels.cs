namespace E_learningProject.Web.Models;

public class DiscussionIndexViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public int TotalThreads { get; set; }
    public int OpenThreads { get; set; }
    public int ResolvedThreads { get; set; }
    public int TotalReplies { get; set; }
    public List<DiscussionThreadListItemViewModel> Threads { get; set; } = new();
}

public class DiscussionThreadListItemViewModel
{
    public int ThreadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public int ReplyCount { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DiscussionDetailsViewModel
{
    public int ThreadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DiscussionReplyItemViewModel> Replies { get; set; } = new();
}

public class DiscussionReplyItemViewModel
{
    public string AuthorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
