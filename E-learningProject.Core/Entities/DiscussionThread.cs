namespace E_learningProject.Core.Entities;

public class DiscussionThread
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public int? ModuleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }

    public User? Author { get; set; }
    public Module? Module { get; set; }
    public List<DiscussionReply> Replies { get; set; } = new();
}
