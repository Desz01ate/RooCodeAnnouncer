using Microsoft.Extensions.Hosting;
using RooCodeAnnouncer.Discord;
using RooCodeAnnouncer.Implementations;

namespace RooCodeAnnouncer;

public class DiscordBotHostingService : IHostedService
{
    private readonly CodeAnnouncerDiscordClient _client;

    public DiscordBotHostingService(CodeAnnouncerDiscordClient client)
    {
        this._client = client;
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