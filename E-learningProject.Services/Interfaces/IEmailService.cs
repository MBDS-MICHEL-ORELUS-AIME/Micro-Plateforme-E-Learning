using E_learningProject.Core.Entities;

namespace E_learningProject.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch new inbound messages from the IMAP server and persist them. Returns saved EmailMessage entries.
    /// </summary>
    Task<IEnumerable<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken = default);
}
