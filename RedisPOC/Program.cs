using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RedisPOC.Data;
using RedisPOC.Models;
using StackExchange.Redis;

var ct = CancellationToken.None;
static string ReadRequiredString(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var s = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        Console.WriteLine("Value is required.");
    }
}

static int ReadIntInRange(string prompt, int min, int max)
{
    while (true)
    {
        Console.Write(prompt);
        var s = Console.ReadLine();
        if (int.TryParse(s, out var n) && n >= min && n <= max) return n;
        Console.WriteLine($"Enter a whole number between {min} and {max}.");
    }
}


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var sqlConn = config.GetConnectionString("SqlServer")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer");

var redisConn = config["Redis:ConnectionString"] ?? "localhost:6379";
var ttlSeconds = int.TryParse(config["Redis:TtlSeconds"], out var t) ? t : 600;
var ttl = TimeSpan.FromSeconds(ttlSeconds);

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(sqlConn)
    .Options;

await using var db = new AppDbContext(dbOptions);

await db.Database.EnsureCreatedAsync(ct);

var repo = new UserRepository(db);

ConnectionMultiplexer? muxer = null;
IDatabase? redis = null;

try
{
    muxer = await ConnectionMultiplexer.ConnectAsync(redisConn);
    redis = muxer.GetDatabase();
    Console.WriteLine($"Redis: connected to {redisConn}");
}
catch (Exception ex)
{
    Console.WriteLine($"Redis: NOT connected ({ex.GetType().Name}). Running DB-only.");
}

string UserKey(Guid id) => $"userprofile:v1:{id:D}";
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

Console.WriteLine("Press Enter to INSERT a user into SQL Server...");
Console.ReadLine();

var name = ReadRequiredString("Enter name: ");
var age = ReadIntInRange("Enter age (0-130): ", 0, 130);

var user = new UserProfile
{
    Id = Guid.NewGuid(),
    Name = name,
    Age = age,
    UpdatedAtUtc = DateTime.UtcNow
};

await repo.InsertAsync(user, ct);
Console.WriteLine($"Inserted into SQL: Id={user.Id}");

Console.WriteLine();
Console.WriteLine("Press Enter to READ user (Redis first, then SQL on miss)...");
Console.ReadLine();

var key = UserKey(user.Id);

// 1) Try Redis
if (redis is not null)
{
    var cachedJson = await redis.StringGetAsync(key);
    if (cachedJson.HasValue)
    {
        var fromCache = JsonSerializer.Deserialize<UserProfile>(cachedJson!, jsonOpts)!;
        Console.WriteLine("SOURCE=REDIS");
        Console.WriteLine(JsonSerializer.Serialize(fromCache, jsonOpts));
        goto secondRead;
    }

    Console.WriteLine("CACHE MISS (Redis)");
}
else
{
    Console.WriteLine("Redis disabled; skipping cache read.");
}

var fromDb = await repo.GetAsync(user.Id, ct);
if (fromDb is null)
{
    Console.WriteLine("Not found in SQL (unexpected).");
    goto done;
}

Console.WriteLine("SOURCE=SQL");
Console.WriteLine(JsonSerializer.Serialize(fromDb, jsonOpts));

if (redis is not null)
{
    var json = JsonSerializer.Serialize(fromDb, jsonOpts);
    await redis.StringSetAsync(key, json, ttl);
    Console.WriteLine($"Cached in Redis with TTL={ttl.TotalSeconds}s");
}

secondRead:
Console.WriteLine();
Console.WriteLine("Press Enter to READ again (should hit Redis if enabled)...");
Console.ReadLine();

if (redis is not null)
{
    var cachedJson2 = await redis.StringGetAsync(key);
    Console.WriteLine(cachedJson2.HasValue ? "SECOND READ SOURCE=REDIS" : "SECOND READ still MISS");
    Console.WriteLine(cachedJson2.HasValue ? cachedJson2.ToString() : "(null)");
}
else
{
    var db2 = await repo.GetAsync(user.Id, ct);
    Console.WriteLine("SECOND READ SOURCE=SQL (Redis disabled)");
    Console.WriteLine(JsonSerializer.Serialize(db2, jsonOpts));
}

done:
muxer?.Dispose();
