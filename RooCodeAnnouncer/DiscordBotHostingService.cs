using MediatR;
using Microsoft.Extensions.Hosting;
using RooCodeAnnouncer.Abstractions;
using RooCodeAnnouncer.Contracts.Events;
using RooCodeAnnouncer.Discord;

namespace RooCodeAnnouncer;

public class DiscordBotHostingService : IHostedService
{
    private readonly CodeAnnouncerDiscordClient _client;

    public DiscordBotHostingService(
        ICodeReader codeReader,
        IMediator mediator,
        CodeAnnouncerDiscordClient client)
    {
        this._client = client;
        this._client.Client.GuildCreated += async (sender, args) =>
        {
            var codes = codeReader.ReadAsync();

            await foreach (var code in codes)
            {
                await mediator.Publish(
                    new NewCodeToSpecificChannelNotification(args.Guild.Id, code.Code, code.Rewards));
            }
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this._client.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await this._client.DisposeAsync();
    }
}