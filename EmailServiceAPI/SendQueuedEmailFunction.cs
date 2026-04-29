using EmailServiceAPI.Models;
using EmailServiceAPI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailServiceAPI;

public class SendQueuedEmailFunction
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendQueuedEmailFunction> _logger;

    public SendQueuedEmailFunction(IEmailService emailService, ILogger<SendQueuedEmailFunction> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [Function("SendQueuedEmail")]
    public async Task Run(
        [QueueTrigger("email-outbox", Connection = "AzureWebJobsStorage")] ContactFormRequest request,
        FunctionContext context)
    {
        var dequeueCount = context.BindingContext.BindingData.TryGetValue("DequeueCount", out var dc)
            ? dc?.ToString() ?? "?"
            : "?";

        _logger.LogInformation(
            "Processing queued email for {Email} (attempt {DequeueCount})",
            request.Email,
            dequeueCount);

        await _emailService.SendContactEmailAsync(request);
    }
}
