using Microsoft.Extensions.Configuration;
using RedisPOC.Data;
using RedisPOC.Models;
using System.Text.Json;

namespace RedisPOC.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;
        private readonly IRedisCacheService _cache;
        private readonly string _keyPrefix;
        private readonly TimeSpan _ttl;
        private readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

        public UserService(UserRepository repo, IRedisCacheService cache, IConfiguration config)
        {
            _repo = repo;
            _cache = cache;
            _keyPrefix = config["Redis:KeyPrefix"];
            var ttlSeconds = int.TryParse(config["Redis:TtlSeconds"], out var t) ? t : 600;
            _ttl = TimeSpan.FromSeconds(ttlSeconds);
        }

        private string Key(Guid id) => $"{_keyPrefix}{id:D}";

        public async Task<UserProfile> CreateAsync(string name, int age, CancellationToken ct)
        {
            var user = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = name,
                Age = age,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _repo.InsertAsync(user, ct);
            return user;
        }

        // Cache-aside read: Redis -> SQL -> Redis
        public async Task<(UserProfile? user, string source)> GetAsync(Guid id, CancellationToken ct)
        {
            var key = Key(id);

            var cachedJson = await _cache.GetStringAsync(key, ct);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var fromCache = JsonSerializer.Deserialize<UserProfile>(cachedJson, _jsonOpts);
                if (fromCache is not null) return (fromCache, "redis");
            }

            var fromDb = await _repo.GetAsync(id, ct);
            if (fromDb is null) return (null, "sql-miss");

            var json = JsonSerializer.Serialize(fromDb, _jsonOpts);
            await _cache.SetStringAsync(key, json, _ttl, ct);

            return (fromDb, "sql");
        }
    }
}
