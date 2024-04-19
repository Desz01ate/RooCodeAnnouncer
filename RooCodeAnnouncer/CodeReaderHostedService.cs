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
    private const string JobId = "code_reader_recurring";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Fire immediately after restart
        ReadCodeAndPublishAsync();

        RecurringJob.AddOrUpdate(JobId, () => ReadCodeAndPublishAsync(), Cron.Hourly);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        RecurringJob.RemoveIfExists(JobId);

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
}