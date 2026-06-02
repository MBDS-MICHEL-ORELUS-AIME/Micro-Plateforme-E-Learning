using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using E_learningProject.Services.Interfaces;

namespace E_learningProject.Services;

public class ImapPollingService : BackgroundService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ImapPollingService> _logger;
    private readonly IConfiguration _configuration;

    public ImapPollingService(IEmailService emailService, ILogger<ImapPollingService> logger, IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int?>("Email:ImapPollIntervalSeconds") ?? 60;
        _logger.LogInformation("IMAP polling service started, interval {Interval}s", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _emailService.FetchNewMessagesAsync(stoppingToken);
                var count = messages?.Count() ?? 0;
                if (count > 0)
                {
                    _logger.LogInformation("Imported {Count} new email(s) from IMAP", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while polling IMAP");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
