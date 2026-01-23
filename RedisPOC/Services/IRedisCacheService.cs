
namespace RedisPOC.Services
{
    public interface IRedisCacheService
    {
        Task<string?> GetStringAsync(string key, CancellationToken ct);
        Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken ct);
    }
}
