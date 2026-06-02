using System;

namespace E_learningProject.Core.Entities;

public class EmailMessage
{
    public int Id { get; set; }

    // Optional link to a discussion thread
    public int? DiscussionThreadId { get; set; }
    public DiscussionThread? DiscussionThread { get; set; }

    public string MessageId { get; set; } = string.Empty;
    public string? InReplyTo { get; set; }

    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }

    public string Subject { get; set; } = string.Empty;

    // Store both HTML and plain text when available
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsInbound { get; set; } = true;

    // Raw headers for debugging or provider-specific metadata
    public string? RawHeaders { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
