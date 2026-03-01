using System.Text;
using System.Text.Json;
using EmailServiceAPI.Models;
using EmailServiceAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailServiceAPI.Tests.Functions;

public class ContactFunctionTests : IDisposable
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<ContactFunction>> _mockLogger;
    private readonly RateLimiter _rateLimiter;
    private readonly ContactFunction _function;

    public ContactFunctionTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<ContactFunction>>();

        var mockTimeProvider = new Mock<TimeProvider>();
        mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow);
        mockTimeProvider.Setup(tp => tp.CreateTimer(
            It.IsAny<TimerCallback>(),
            It.IsAny<object?>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()))
            .Returns(Mock.Of<ITimer>());

        _rateLimiter = new RateLimiter(mockTimeProvider.Object);
        _function = new ContactFunction(_mockEmailService.Object, _mockLogger.Object, _rateLimiter);
    }

    private static HttpRequest CreateRequest(object? body = null, string contentType = "application/json", string ip = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;
        context.Request.Headers["X-Forwarded-For"] = ip;

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        else
        {
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""));
        }

        return context.Request;
    }

    private static object CreateValidBody() => new
    {
        firstName = "John",
        surName = "Doe",
        email = "john@example.com",
        queryType = "general",
        message = "Hello, this is a test message."
    };

    [Fact]
    public async Task Wrong_ContentType_Returns_415()
    {
        var req = CreateRequest(CreateValidBody(), contentType: "text/plain");
        var result = await _function.Run(req);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(415, statusResult.StatusCode);
    }

    [Fact]
    public async Task Rate_Limited_Returns_429_With_RetryAfter()
    {
        for (int i = 0; i < 5; i++)
        {
            var r = CreateRequest(CreateValidBody(), ip: "10.0.0.1");
            await _function.Run(r);
        }

        var req = CreateRequest(CreateValidBody(), ip: "10.0.0.1");
        var result = await _function.Run(req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, objectResult.StatusCode);
        Assert.True(req.HttpContext.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task Invalid_Json_Returns_400()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{invalid json}"));

        var result = await _function.Run(context.Request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Honeypot_Triggered_Returns_200_Without_Sending_Email()
    {
        var body = new
        {
            firstName = "John",
            surName = "Doe",
            email = "john@example.com",
            queryType = "general",
            message = "Hello",
            website = "http://bot.example.com"
        };

        var req = CreateRequest(body);
        var result = await _function.Run(req);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockEmailService.Verify(x => x.SendContactEmailAsync(It.IsAny<ContactFormRequest>()), Times.Never);
    }

    [Fact]
    public async Task Missing_Required_Field_Returns_400()
    {
        var body = new
        {
            firstName = "John",
            email = "john@example.com",
            queryType = "general",
            message = "Hello"
        };

        var req = CreateRequest(body);
        var result = await _function.Run(req);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Invalid_Email_Format_Returns_400()
    {
        var body = new
        {
            firstName = "John",
            surName = "Doe",
            email = "not-an-email",
            queryType = "general",
            message = "Hello"
        };

        var req = CreateRequest(body);
        var result = await _function.Run(req);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Invalid_QueryType_Returns_400()
    {
        var body = new
        {
            firstName = "John",
            surName = "Doe",
            email = "john@example.com",
            queryType = "invalid",
            message = "Hello"
        };

        var req = CreateRequest(body);
        var result = await _function.Run(req);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Valid_Submission_Returns_200_And_Sends_Email()
    {
        var req = CreateRequest(CreateValidBody());
        var result = await _function.Run(req);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockEmailService.Verify(x => x.SendContactEmailAsync(It.IsAny<ContactFormRequest>()), Times.Once);
    }

    [Fact]
    public async Task Email_Service_Throws_Returns_500()
    {
        _mockEmailService.Setup(x => x.SendContactEmailAsync(It.IsAny<ContactFormRequest>()))
            .ThrowsAsync(new Exception("Email send failed"));

        var req = CreateRequest(CreateValidBody());
        var result = await _function.Run(req);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
        GC.SuppressFinalize(this);
    }
}
