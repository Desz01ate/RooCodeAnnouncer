using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Contracts.Events;
using RooCodeAnnouncer.Infrastructure;
using RooCodeAnnouncer.Infrastructure.Entities;

namespace RooCodeAnnouncer;

public class CodeReaderHostedService(
    CodeAnnouncerDbContext dbContext,
    ICodeReader codeReader,
    IMediator mediator,
    ILogger<CodeReaderHostedService> logger) : IHostedService
{
    private const string CodeReaderJobId = "code_reader_recurring";
    private const string ZenyShopSnapTimeJobId = "zeny_shop_snap_time_recurring";
    private const string EndOfWeekJobId = "eow_recurring";
    private const string EndOfMonthJobId = "eom_recurring";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Fire immediately after restart
        ReadCodeAndPublishAsync();

        RecurringJob.AddOrUpdate(CodeReaderJobId, () => ReadCodeAndPublishAsync(), Cron.Hourly);
        RecurringJob.AddOrUpdate($"{ZenyShopSnapTimeJobId}_1", () => NotifySnapTimeAsync(), Cron.Daily(5)); // At 12:00 GMT+7
        RecurringJob.AddOrUpdate($"{ZenyShopSnapTimeJobId}_2", () => NotifySnapTimeAsync(), Cron.Daily(9)); // At 16:00 GMT+7
        RecurringJob.AddOrUpdate($"{ZenyShopSnapTimeJobId}_3", () => NotifySnapTimeAsync(), Cron.Daily(13)); // At 20:00 GMT+7
        RecurringJob.AddOrUpdate(EndOfWeekJobId, () => NotifyEndOfWeekAsync(), Cron.Weekly(DayOfWeek.Sunday, 5));
        RecurringJob.AddOrUpdate(EndOfMonthJobId, () => NotifyEndOfMonthAsync(), Cron.Daily(5));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        RecurringJob.RemoveIfExists(CodeReaderJobId);

        return Task.CompletedTask;
    }

    public async Task ReadCodeAndPublishAsync()
    {
        var publishedCodes = new List<PublishedItemCode>();

        await foreach (var code in codeReader.ReadAsync())
        {
            try
            {
                var codeExist = await dbContext.PublishedItemCodes.AnyAsync(c => c.Id == code.Code);

                if (codeExist)
                {
                    logger.LogWarning("Code {Id} already published, skip", code.Code);
                    continue;
                }

                var itemCode = new PublishedItemCode(code.Code, code.RawRewards);
                publishedCodes.Add(itemCode);

                await mediator.Publish(new NewCodeNotification(code.Code, code.Rewards));

                logger.LogInformation("Code {Id} published", code.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred for {Code}", code.Code);
            }
        }

        if (publishedCodes.Any())
        {
            var distinctCodes = publishedCodes.DistinctBy(c => c.Id);

            dbContext.AddRange(distinctCodes);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task NotifySnapTimeAsync()
    {
        await mediator.Publish(new ZenyShopSnapTimeNotification());

        logger.LogInformation("Published {MessageType}", nameof(ZenyShopSnapTimeNotification));
    }

    public async Task NotifyEndOfWeekAsync()
    {
        await mediator.Publish(new EndOfWeekNotification());
        
        logger.LogInformation("Published {MessageType}", nameof(EndOfWeekNotification));
    }

    public async Task NotifyEndOfMonthAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var tomorrow = now.AddDays(1);

        if (now.Month < tomorrow.Month)
        {
            await mediator.Publish(new EndOfMonthNotification());

            logger.LogInformation("Published {MessageType}", nameof(EndOfMonthNotification));
        }
    }
}