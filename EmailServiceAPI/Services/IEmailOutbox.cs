using System.Text.Json;
using Azure.Storage.Queues;
using EmailServiceAPI.Models;
using Microsoft.Extensions.Logging;

namespace EmailServiceAPI.Services;

public interface IEmailOutbox
{
    Task EnqueueAsync(ContactFormRequest request, CancellationToken cancellationToken = default);
}

public class EmailOutbox : IEmailOutbox
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<EmailOutbox> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EmailOutbox(QueueClient queueClient, ILogger<EmailOutbox> logger)
    {
        _queueClient = queueClient;
        _logger = logger;
    }

    public async Task EnqueueAsync(ContactFormRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        await _queueClient.SendMessageAsync(json, cancellationToken);
        _logger.LogInformation(
            "Enqueued contact form to {Queue} for {Email}",
            _queueClient.Name,
            request.Email);
    }
}
