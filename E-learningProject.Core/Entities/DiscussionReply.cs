namespace E_learningProject.Core.Entities;

public class DiscussionReply
{
    public int Id { get; set; }
    public int DiscussionThreadId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DiscussionThread? DiscussionThread { get; set; }
}
