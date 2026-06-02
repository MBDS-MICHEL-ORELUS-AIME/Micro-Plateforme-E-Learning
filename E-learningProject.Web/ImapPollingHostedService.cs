using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using E_learningProject.Services.Interfaces;

namespace E_learningProject.Web;

public class ImapPollingHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ImapPollingHostedService> _logger;
    private readonly IConfiguration _configuration;

    public ImapPollingHostedService(IServiceProvider services, ILogger<ImapPollingHostedService> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int?>("Email:ImapPollIntervalSeconds") ?? 60;
        var enableImapPolling = _configuration.GetValue<bool?>("Email:EnableImapPolling") ?? true;

        if (!enableImapPolling)
        {
            _logger.LogInformation("IMAP polling is disabled by configuration (Email:EnableImapPolling=false).");
            return;
        }

        _logger.LogInformation("IMAP polling hosted service started, interval {Interval}s", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var messages = await emailService.FetchNewMessagesAsync(stoppingToken);
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
