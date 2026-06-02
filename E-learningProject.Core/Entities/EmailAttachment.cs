using System;

namespace E_learningProject.Core.Entities;

public class EmailAttachment
{
    public int Id { get; set; }
    public int EmailMessageId { get; set; }
    public EmailMessage? EmailMessage { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string FilePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
