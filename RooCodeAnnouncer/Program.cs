// See https://aka.ms/new-console-template for more information

using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RooCodeAnnouncer;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Implementations;
using RooCodeAnnouncer.Infrastructure;
using RooCodeAnnouncer.Publishers;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration(config =>
{
    config.AddEnvironmentVariables();
    config.AddJsonFile("appsettings.json", false);
});

builder.ConfigureServices(
    (_, services) =>
    {
        var connectionString = _.Configuration.GetConnectionString("Default");
        services.AddDbContext<CodeAnnouncerDbContext>(
            c =>
                c.UseSqlite(connectionString)
                    .EnableSensitiveDataLogging());
        services.AddMediatR(
            c => c.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddHangfire(c =>
            c.UseMemoryStorage());
        services.AddHangfireServer();
        services.AddLogging(c =>
            c.AddConsole());
        services.AddHttpClient(
            nameof(HttpCodeReader),
            client => client.BaseAddress =
                new Uri("https://gamingph.com/2023/04/redeem-codes-for-ragnarok-origin-roo-guide/"));
        services.AddHttpClient(
            nameof(LinePublisher),
            client => client.BaseAddress = new Uri("https://notify-api.line.me/api/notify"));
        services.AddScoped<ICodeReader, HttpCodeReader>();
        services.AddHostedService<CodeReaderHostedService>();
    });

var app = builder.Build();

await ApplyMigrationsAsync(app);

app.Run();

static async Task ApplyMigrationsAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CodeAnnouncerDbContext>();

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        await dbContext.Database.MigrateAsync();
    }
}