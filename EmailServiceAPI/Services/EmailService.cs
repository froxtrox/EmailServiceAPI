using System.Net;
using Azure.Communication.Email;
using EmailServiceAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailServiceAPI.Services;

public interface IEmailService
{
    Task SendContactEmailAsync(ContactFormRequest request);
}

public class EmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly string _senderEmail;
    private readonly string _recipientEmail;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        EmailClient emailClient,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _emailClient = emailClient;
        _logger = logger;

        _senderEmail = configuration["SENDER_EMAIL"]
            ?? throw new InvalidOperationException("SENDER_EMAIL is not configured.");
        _recipientEmail = configuration["RECIPIENT_EMAIL"]
            ?? throw new InvalidOperationException("RECIPIENT_EMAIL is not configured.");
    }

    public async Task SendContactEmailAsync(ContactFormRequest request)
    {
            var subject = $"Portfolio Contact: {request.QueryType} from {request.FirstName} {request.SurName}";
            var htmlBody = BuildHtmlBody(request);
            var plainTextBody = BuildPlainTextBody(request);

            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                content: new EmailContent(subject) { Html = htmlBody, PlainText = plainTextBody },
                recipients: new EmailRecipients([new EmailAddress(_recipientEmail)]));

            emailMessage.ReplyTo.Add(new EmailAddress(request.Email, $"{request.FirstName} {request.SurName}"));

        _logger.LogInformation(
            "Sending contact email. Sender: {Sender}, Recipient: {Recipient}, ReplyTo: {ReplyTo}, Subject: {Subject}",
            _senderEmail,
            _recipientEmail,
            request.Email,
            subject);

        try
        {
            await _emailClient.SendAsync(Azure.WaitUntil.Started, emailMessage);
            _logger.LogInformation("Email send initiated successfully for {Recipient}", _recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email. Sender: {Sender}, Recipient: {Recipient}, ReplyTo: {ReplyTo}, Subject: {Subject}",
                _senderEmail,
                _recipientEmail,
                request.Email,
                subject);
            throw;
        }
    }

    private static string BuildHtmlBody(ContactFormRequest request)
    {
        var firstName = WebUtility.HtmlEncode(request.FirstName);
        var surName = WebUtility.HtmlEncode(request.SurName);
        var email = WebUtility.HtmlEncode(request.Email);
        var queryType = WebUtility.HtmlEncode(request.QueryType);
        var message = WebUtility.HtmlEncode(request.Message);

        return $"""
            <h2>New Contact Form Submission</h2>
            <table border="1" cellpadding="8" cellspacing="0">
                <tr><td><strong>Name</strong></td><td>{firstName} {surName}</td></tr>
                <tr><td><strong>Email</strong></td><td>{email}</td></tr>
                <tr><td><strong>Query Type</strong></td><td>{queryType}</td></tr>
            </table>
            <h3>Message</h3>
            <p>{message}</p>
            """;
    }

    private static string BuildPlainTextBody(ContactFormRequest request)
    {
        return $"""
            New Contact Form Submission

            Name: {request.FirstName} {request.SurName}
            Email: {request.Email}
            Query Type: {request.QueryType}

            Message:
            {request.Message}
            """;
    }
}
