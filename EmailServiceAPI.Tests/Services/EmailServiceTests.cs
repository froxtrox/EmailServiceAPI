using System.Net;
using Azure;
using Azure.Communication.Email;
using EmailServiceAPI.Models;
using EmailServiceAPI.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EmailServiceAPI.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<EmailClient> _mockEmailClient;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;


    public EmailServiceTests()
    {
        _mockEmailClient = new Mock<EmailClient>();
        _mockEmailClient.Setup(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.IsAny<EmailMessage>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((EmailSendOperation)null!));

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENDER_EMAIL"] = "DoNotReply@test.azurecomm.net",
                ["RECIPIENT_EMAIL"] = "admin@example.com"
            })
            .Build();

        _emailService = new EmailService(_mockEmailClient.Object, _configuration);
    }

    private static ContactFormRequest CreateValidRequest() => new()
    {
        FirstName = "John",
        SurName = "Doe",
        Email = "john@example.com",
        QueryType = "general",
        Message = "Hello, this is a test message."
    };

    [Fact]
    public async Task SendContactEmailAsync_Calls_EmailClient_SendAsync()
    {
        await _emailService.SendContactEmailAsync(CreateValidRequest());

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.IsAny<EmailMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContactEmailAsync_Subject_Format_Is_Correct()
    {
        await _emailService.SendContactEmailAsync(CreateValidRequest());

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),  
            It.Is<EmailMessage>(msg => msg.Content.Subject == "Portfolio Contact: general from John Doe"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContactEmailAsync_Html_Body_Contains_Encoded_Fields()
    {
        await _emailService.SendContactEmailAsync(CreateValidRequest());

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.Is<EmailMessage>(msg =>
                msg.Content.Html.Contains("John") &&
                msg.Content.Html.Contains("Doe") &&
                msg.Content.Html.Contains("john@example.com") &&
                msg.Content.Html.Contains("general") &&
                msg.Content.Html.Contains("Hello, this is a test message.")
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContactEmailAsync_PlainText_Body_Contains_Fields()
    {
        await _emailService.SendContactEmailAsync(CreateValidRequest());

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.Is<EmailMessage>(msg =>
                msg.Content.PlainText.Contains("John Doe") &&
                msg.Content.PlainText.Contains("john@example.com") &&
                msg.Content.PlainText.Contains("general") &&
                msg.Content.PlainText.Contains("Hello, this is a test message.")
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContactEmailAsync_ReplyTo_Set_To_Submitter()
    {
        await _emailService.SendContactEmailAsync(CreateValidRequest());

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.Is<EmailMessage>(msg =>
                msg.ReplyTo.Count == 1 &&
                msg.ReplyTo[0].Address == "john@example.com" &&
                msg.ReplyTo[0].DisplayName == "John Doe"
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContactEmailAsync_Html_Escapes_Script_Tags()
    {
        var request = CreateValidRequest();
        request.FirstName = "<script>alert('xss')</script>";

        await _emailService.SendContactEmailAsync(request);

        _mockEmailClient.Verify(x => x.SendAsync(
            It.IsAny<WaitUntil>(),
            It.Is<EmailMessage>(msg =>
                !msg.Content.Html.Contains("<script>") &&
                msg.Content.Html.Contains(WebUtility.HtmlEncode("<script>alert('xss')</script>"))
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_Missing_SenderEmail_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RECIPIENT_EMAIL"] = "admin@example.com"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(_mockEmailClient.Object, config));
        Assert.Contains("SENDER_EMAIL", ex.Message);
    }

    [Fact]
    public void Constructor_Missing_RecipientEmail_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENDER_EMAIL"] = "DoNotReply@test.azurecomm.net"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new EmailService(_mockEmailClient.Object, config));
        Assert.Contains("RECIPIENT_EMAIL", ex.Message);
    }
}
