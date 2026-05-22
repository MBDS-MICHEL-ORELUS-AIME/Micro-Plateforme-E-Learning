namespace E_learningProject.Core.Entities;

public class DiscussionThread
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; }

    public List<DiscussionReply> Replies { get; set; } = new();
}
