using EmailServiceAPI.Models;
using EmailServiceAPI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailServiceAPI.Tests.Functions;

public class SendQueuedEmailFunctionTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<SendQueuedEmailFunction>> _mockLogger;
    private readonly SendQueuedEmailFunction _function;

    public SendQueuedEmailFunctionTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<SendQueuedEmailFunction>>();
        _function = new SendQueuedEmailFunction(_mockEmailService.Object, _mockLogger.Object);
    }

    private static FunctionContext CreateContext(int dequeueCount = 1)
    {
        var bindingData = new Dictionary<string, object?>
        {
            ["DequeueCount"] = dequeueCount
        };

        var bindingContext = new Mock<BindingContext>();
        bindingContext.SetupGet(b => b.BindingData).Returns(bindingData);

        var context = new Mock<FunctionContext>();
        context.SetupGet(c => c.BindingContext).Returns(bindingContext.Object);
        return context.Object;
    }

    private static ContactFormRequest CreateValidRequest() => new()
    {
        FirstName = "John",
        SurName = "Doe",
        Email = "john@example.com",
        QueryType = "general",
        Message = "Hello, queued world."
    };

    [Fact]
    public async Task Valid_Message_Calls_EmailService_Once()
    {
        var request = CreateValidRequest();

        await _function.Run(request, CreateContext());

        _mockEmailService.Verify(
            x => x.SendContactEmailAsync(It.Is<ContactFormRequest>(r => r.Email == request.Email)),
            Times.Once);
    }

    [Fact]
    public async Task EmailService_Throws_Propagates_Exception_For_Retry()
    {
        // Propagation is the contract: the Functions runtime needs the
        // exception to bubble out so it can increment DequeueCount and
        // eventually move the message to email-outbox-poison.
        _mockEmailService
            .Setup(x => x.SendContactEmailAsync(It.IsAny<ContactFormRequest>()))
            .ThrowsAsync(new InvalidOperationException("ACS down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _function.Run(CreateValidRequest(), CreateContext()));
    }
}
