using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisPOC;
using RedisPOC.Data;
using RedisPOC.Services;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = "HH:mm:ss ";
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContext<AppDbContext>(o =>
                    o.UseSqlServer(ctx.Configuration.GetConnectionString("SqlServer")));

                services.AddScoped<UserRepository>();

                services.AddSingleton<IRedisCacheService, RedisCacheService>();

                services.AddScoped<UserService>();
                services.AddScoped<App>();
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        using (var scope = host.Services.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<App>();
            await app.RunAsync(CancellationToken.None);
        }
    }
}