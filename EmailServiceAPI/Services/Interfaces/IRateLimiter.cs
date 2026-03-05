namespace EmailServiceAPI.Services.Interfaces
{
    public interface IRateLimiter
    {
        (bool Allowed, int? RetryAfterSeconds) CheckRateLimit(string ip);
        void Dispose();
    }
}