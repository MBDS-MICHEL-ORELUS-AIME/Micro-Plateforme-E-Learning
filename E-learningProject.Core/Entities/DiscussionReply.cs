namespace E_learningProject.Core.Entities;

public class DiscussionReply
{
    public int Id { get; set; }
    public int DiscussionThreadId { get; set; }
    public int AuthorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Author { get; set; }
    public DiscussionThread? DiscussionThread { get; set; }
}
