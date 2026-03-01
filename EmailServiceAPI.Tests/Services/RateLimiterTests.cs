using EmailServiceAPI.Services;
using Moq;

namespace EmailServiceAPI.Tests.Services;

public class RateLimiterTests : IDisposable
{
    private readonly Mock<TimeProvider> _mockTimeProvider;
    private readonly RateLimiter _rateLimiter;
    private DateTimeOffset _currentTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RateLimiterTests()
    {
        _mockTimeProvider = new Mock<TimeProvider>();
        _mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(() => _currentTime);
        _mockTimeProvider.Setup(tp => tp.CreateTimer(
            It.IsAny<TimerCallback>(),
            It.IsAny<object?>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()))
            .Returns(Mock.Of<ITimer>());

        _rateLimiter = new RateLimiter(_mockTimeProvider.Object);
    }

    [Fact]
    public void First_Request_From_IP_Is_Allowed()
    {
        var (allowed, retryAfter) = _rateLimiter.CheckRateLimit("1.2.3.4");
        Assert.True(allowed);
        Assert.Null(retryAfter);
    }

    [Fact]
    public void Fifth_Request_Within_Window_Is_Allowed()
    {
        for (int i = 0; i < 4; i++)
            _rateLimiter.CheckRateLimit("1.2.3.4");

        var (allowed, retryAfter) = _rateLimiter.CheckRateLimit("1.2.3.4");
        Assert.True(allowed);
        Assert.Null(retryAfter);
    }

    [Fact]
    public void Sixth_Request_Within_Window_Is_Blocked()
    {
        for (int i = 0; i < 5; i++)
            _rateLimiter.CheckRateLimit("1.2.3.4");

        var (allowed, retryAfter) = _rateLimiter.CheckRateLimit("1.2.3.4");
        Assert.False(allowed);
        Assert.NotNull(retryAfter);
        Assert.True(retryAfter > 0);
    }

    [Fact]
    public void Request_After_Window_Expires_Is_Allowed()
    {
        for (int i = 0; i < 6; i++)
            _rateLimiter.CheckRateLimit("1.2.3.4");

        _currentTime = _currentTime.AddMinutes(16);

        var (allowed, retryAfter) = _rateLimiter.CheckRateLimit("1.2.3.4");
        Assert.True(allowed);
        Assert.Null(retryAfter);
    }

    [Fact]
    public void Different_IPs_Get_Own_Limits()
    {
        for (int i = 0; i < 5; i++)
            _rateLimiter.CheckRateLimit("1.2.3.4");

        var (allowed, _) = _rateLimiter.CheckRateLimit("5.6.7.8");
        Assert.True(allowed);
    }

    [Fact]
    public void RetryAfterSeconds_Correctly_Computed()
    {
        for (int i = 0; i < 5; i++)
            _rateLimiter.CheckRateLimit("1.2.3.4");

        _currentTime = _currentTime.AddMinutes(5);

        var (_, retryAfter) = _rateLimiter.CheckRateLimit("1.2.3.4");
        Assert.NotNull(retryAfter);
        Assert.Equal(600, retryAfter);
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
        GC.SuppressFinalize(this);
    }
}
