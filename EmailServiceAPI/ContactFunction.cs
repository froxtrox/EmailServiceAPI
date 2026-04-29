using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EmailServiceAPI.Models;
using EmailServiceAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailServiceAPI;

public class ContactFunction
{
    private readonly IEmailOutbox _outbox;
    private readonly ILogger<ContactFunction> _logger;
    private readonly InMemoryRateLimiter _rateLimiter;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ContactFunction(IEmailOutbox outbox, ILogger<ContactFunction> logger, InMemoryRateLimiter rateLimiter)
    {
        _outbox = outbox;
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    [Function("contact")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req)
    {
        if (!req.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
            return new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);

        var ip = GetClientIp(req);
        var (allowed, retryAfter) = _rateLimiter.CheckRateLimit(ip);
        if (!allowed)
        {
            req.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
            return new ObjectResult("Too many requests. Please try again later.")
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
        }

        ContactFormRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<ContactFormRequest>(req.Body, JsonOptions);
            if (request is null)
                return new BadRequestObjectResult("Invalid request body.");
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Invalid JSON format.");
        }

        if (!string.IsNullOrEmpty(request.Website))
        {
            _logger.LogWarning("Honeypot triggered from IP: {Ip}", ip);
            return new OkObjectResult("Message sent successfully.");
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults.Select(r => r.ErrorMessage).ToList();
            return new BadRequestObjectResult(new { errors });
        }

        try
        {
            await _outbox.EnqueueAsync(request);
            return new OkObjectResult("Message sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue contact email");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static string GetClientIp(HttpRequest req)
    {
        if (req.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip)) return ip;
        }

        if (req.Headers.TryGetValue("X-Client-IP", out var clientIp))
        {
            var ip = clientIp.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip)) return ip;
        }

        return "unknown";
    }
}
