using E_learningProject.Core.Entities;
using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace E_learningProject.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public EmailService(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<EmailService> logger, IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = _configuration.GetValue<int?>("Email:SmtpPort") ?? 587;
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPass"];
        var fromAddress = _configuration["Email:From"] ?? smtpUser ?? "noreply@localhost";

        // In production, strict validation. In development, allow localhost for testing.
        var isDevEnvironment = _hostEnvironment.IsDevelopment();
        if (string.IsNullOrWhiteSpace(smtpHost) || IsPlaceholderHost(smtpHost))
        {
            var errorMessage = "SMTP host is not configured or is using a placeholder value. Please set a valid Email:SmtpHost in appsettings or environment variables.";
            if (!isDevEnvironment)
            {
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            else
            {
                _logger.LogWarning($"Email sending skipped in development: {errorMessage}");
                // Persist message without sending in dev mode
                await PersistEmailAsync(to, subject, htmlBody, fromAddress, cancellationToken);
                return;
            }
        }

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(fromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(to));
        mimeMessage.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        mimeMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                if (!string.IsNullOrEmpty(smtpPass))
                {
                    await client.AuthenticateAsync(smtpUser, smtpPass, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("SMTP username is configured but Email:SmtpPass is empty. Skipping authentication.");
                }
            }
            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            // persist outgoing message record
            await PersistEmailAsync(to, subject, htmlBody, fromAddress, cancellationToken, mimeMessage.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            // In development, persist the message anyway even if SMTP fails
            if (isDevEnvironment)
            {
                _logger.LogWarning("Persisting email in database for development (SMTP not available)");
                await PersistEmailAsync(to, subject, htmlBody, fromAddress, cancellationToken);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task PersistEmailAsync(string to, string subject, string htmlBody, string fromAddress, CancellationToken cancellationToken = default, string? messageId = null)
    {
        var email = new EmailMessage
        {
            MessageId = messageId ?? string.Empty,
            From = fromAddress,
            To = to,
            Subject = subject,
            BodyHtml = htmlBody,
            IsInbound = false,
            ReceivedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.EmailMessages.Add(email);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsPlaceholderHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        return host.Contains("example.com", StringComparison.OrdinalIgnoreCase)
            || host.Contains("example.net", StringComparison.OrdinalIgnoreCase)
            || host.Contains("example.org", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken = default)
    {
        var imapHost = _configuration["Email:ImapHost"];
        var imapPort = _configuration.GetValue<int?>("Email:ImapPort") ?? 993;
        var imapUser = _configuration["Email:ImapUser"];
        var imapPass = _configuration["Email:ImapPass"];

        var results = new List<EmailMessage>();

        if (string.IsNullOrWhiteSpace(imapHost) || IsPlaceholderHost(imapHost))
        {
            _logger.LogWarning("IMAP host is not configured or is using a placeholder value. Skipping IMAP polling until a valid Email:ImapHost is provided.");
            return results;
        }

        using var client = new ImapClient();
        try
        {
            await client.ConnectAsync(imapHost, imapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, cancellationToken);
            if (!string.IsNullOrWhiteSpace(imapUser))
            {
                if (!string.IsNullOrEmpty(imapPass))
                {
                    await client.AuthenticateAsync(imapUser, imapPass, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("IMAP username is configured but Email:ImapPass is empty. Skipping authentication.");
                }
            }

            var inbox = client.Inbox!;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadWrite, cancellationToken);

            // Search for unseen messages
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);

                var messageDate = message.Date.UtcDateTime;
                var email = new EmailMessage
                {
                    MessageId = message.MessageId ?? string.Empty,
                    InReplyTo = message.InReplyTo,
                    From = message.From.ToString(),
                    To = message.To.ToString(),
                    Cc = message.Cc.ToString(),
                    Subject = message.Subject ?? string.Empty,
                    BodyHtml = message.HtmlBody,
                    BodyText = message.TextBody,
                    RawHeaders = message.Headers.ToString(),
                    IsInbound = true,
                    ReceivedAt = messageDate,
                    CreatedAt = DateTime.UtcNow
                };

                // Try to associate with an existing discussion thread via InReplyTo -> MessageId
                if (!string.IsNullOrWhiteSpace(email.InReplyTo))
                {
                    var parent = await _dbContext.EmailMessages
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.MessageId == email.InReplyTo, cancellationToken);
                    if (parent != null && parent.DiscussionThreadId.HasValue)
                    {
                        email.DiscussionThreadId = parent.DiscussionThreadId;
                    }
                }

                // If still not associated, create a new DiscussionThread and a first reply
                if (!email.DiscussionThreadId.HasValue)
                {
                    // extract simple address from mailbox
                    var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? message.From.ToString();

                    // try to map to an existing app user by email
                    var user = await _dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == fromAddress, cancellationToken);
                    var studentId = user?.UserName ?? fromAddress;

                    var thread = new DiscussionThread
                    {
                        Title = string.IsNullOrWhiteSpace(message.Subject) ? $"Email from {fromAddress}" : (message.Subject.Length > 200 ? message.Subject[..200] : message.Subject),
                        AuthorId = user?.Id ?? 0,
                        CreatedAt = messageDate,
                        IsResolved = false
                    };

                    _dbContext.DiscussionThreads.Add(thread);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    email.DiscussionThreadId = thread.Id;

                    // create a discussion reply representing the email body
                    var bodyText = message.TextBody ?? message.HtmlBody ?? string.Empty;
                    var reply = new DiscussionReply
                    {
                        DiscussionThreadId = thread.Id,
                        AuthorId = user?.Id ?? 0,
                        Message = bodyText.Length > 2000 ? bodyText[..2000] : bodyText,
                        CreatedAt = messageDate
                    };
                    _dbContext.DiscussionReplies.Add(reply);
                }

                _dbContext.EmailMessages.Add(email);
                results.Add(email);

                // Save attachments if any
                if (message.Attachments != null)
                {
                    var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "emails", Guid.NewGuid().ToString());
                    Directory.CreateDirectory(uploadRoot);

                    foreach (var attachment in message.Attachments)
                    {
                        try
                        {
                            var mimePart = attachment as MimePart;
                            var fileName = mimePart?.FileName ?? attachment.ContentDisposition?.FileName ?? "attachment";
                            var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                            var filePath = Path.Combine(uploadRoot, safeFileName);

                            if (mimePart?.Content != null)
                            {
                                await using var stream = File.Create(filePath);
                                await mimePart.Content.DecodeToAsync(stream, cancellationToken);
                            }
                            else if (attachment is MessagePart rfc2231Part && rfc2231Part.Message != null)
                            {
                                var nested = rfc2231Part.Message;
                                var nestedPath = Path.Combine(uploadRoot, "forwarded.eml");
                                await using var stream = File.Create(nestedPath);
                                nested.WriteTo(stream);
                                filePath = nestedPath;
                            }

                            var fileInfo = new FileInfo(filePath);
                            var attachEntity = new EmailAttachment
                            {
                                EmailMessage = email,
                                FileName = safeFileName,
                                ContentType = mimePart?.ContentType?.MimeType ?? attachment.ContentType?.MimeType ?? string.Empty,
                                Size = fileInfo.Length,
                                FilePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath).Replace("\\", "/"),
                                CreatedAt = DateTime.UtcNow
                            };
                            _dbContext.EmailAttachments.Add(attachEntity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to save email attachment");
                        }
                    }
                }

                // mark as seen
                await inbox.StoreAsync(uid, new MailKit.StoreFlagsRequest(MailKit.StoreAction.Add, MailKit.MessageFlags.Seen), cancellationToken);
            }

            if (results.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch messages from IMAP");
            // swallow errors so background service can retry later
        }

        return results;
    }
}
