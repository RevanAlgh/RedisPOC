using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace RedisPOC.Services
{
    public class RedisCacheService : IRedisCacheService, IDisposable
    {
        private readonly string _connectionString;
        private ConnectionMultiplexer? _muxer;
        private IDatabase? _db;

        public bool Enabled => _db is not null;

        public RedisCacheService(IConfiguration config)
        {
            _connectionString = config["Redis:ConnectionString"];
            TryConnect();
        }

        private void TryConnect()
        {
            try
            {
                _muxer = ConnectionMultiplexer.Connect(_connectionString);
                _db = _muxer.GetDatabase();
            }
            catch
            {
                _muxer = null;
                _db = null;
            }
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken ct)
        {
            if (_db is null) return null;

            try
            {
                var val = await _db.StringGetAsync(key);
                return val.HasValue ? val.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken ct)
        {
            if (_db is null) return;

            try
            {
                await _db.StringSetAsync(key, value, ttl);
            }
            catch
            {
                // ignore
            }
        }

        public void Dispose()
        {
            _muxer?.Dispose();
        }
    }
}
