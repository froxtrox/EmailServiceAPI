using System.Diagnostics;
using Azure.Communication.Email;
using Azure.Identity;
using Azure.Storage.Queues;
using EmailServiceAPI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#if DEBUG
Debugger.Launch();
#endif

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(s =>
    {
        s.AddApplicationInsightsTelemetryWorkerService();
        s.ConfigureFunctionsApplicationInsights();
        s.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule? toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });

        s.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var endpoint = config["ACS_ENDPOINT"]
                ?? throw new InvalidOperationException("ACS_ENDPOINT is not configured.");
            return new EmailClient(new Uri(endpoint), new DefaultAzureCredential());
        });
        s.AddSingleton<IEmailService, EmailService>();

        // QueueClient for the outbox. MessageEncoding=Base64 matches the
        // Functions Queue trigger's default expectation, so messages we send
        // here can be deserialized by SendQueuedEmailFunction without extra
        // configuration in host.json.
        s.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connection = config["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured.");
            return new QueueClient(connection, "email-outbox", new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
        });
        s.AddSingleton<IEmailOutbox, EmailOutbox>();

        s.AddSingleton<InMemoryRateLimiter>();
        s.AddSingleton(TimeProvider.System);
    })
    .Build();

await host.RunAsync();
