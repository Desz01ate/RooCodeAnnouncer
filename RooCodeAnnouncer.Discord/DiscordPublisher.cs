using MediatR;
using Microsoft.Extensions.Logging;
using RooCodeAnnouncer.Contracts.Events;

namespace RooCodeAnnouncer.Discord;

public class DiscordPublisher : INotificationHandler<NewCodeNotification>,
    INotificationHandler<NewCodeToSpecificChannelNotification>
{
    private const string ChannelName = "ragnarok_origins_item_code";
    private readonly CodeAnnouncerDiscordClient _client;
    private readonly ILogger<DiscordPublisher> _logger;

    public DiscordPublisher(CodeAnnouncerDiscordClient client, ILogger<DiscordPublisher> logger)
    {
        this._client = client;
        this._logger = logger;
    }

    public async Task Handle(NewCodeNotification notification, CancellationToken cancellationToken)
    {
        var servers = this._client.Guilds;

        var retryCount = 0;
        while (retryCount++ < 10 && servers.Count == 0)
        {
            servers = this._client.Guilds;
            await Task.Delay(1000, cancellationToken);
        }

        if (servers.Count == 0)
        {
            this._logger.LogDebug("Unable to read message within {RetryCount} attempts", retryCount);
            return;
        }

        foreach (var server in servers)
        {
            var channel =
                server.Channels
                    .FirstOrDefault(c => c.Value.Name == ChannelName)
                    .Value;

            if (channel is null)
            {
                try
                {
                    channel = await server.CreateTextChannelAsync(ChannelName);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Unable to publish a message for server {Server}", server.Name);

                    continue;
                }
            }

            var code = notification.Code.Replace('\n', '\0').Replace('\r', ' ');
            var itemText = string.Join(" ", notification.Items.Select(r => $"**{r.Name}** x {r.Quantity:N0}"));
            var content = $":star:CODE: `{code.PadRight(30, '\0')}` Items: {itemText}";

            await channel.SendMessageAsync(content);
        }
    }

    public async Task Handle(NewCodeToSpecificChannelNotification notification, CancellationToken cancellationToken)
    {
        var servers = this._client.Guilds;

        var retryCount = 0;
        while (retryCount++ < 10 && servers.Count == 0)
        {
            servers = this._client.Guilds;
            await Task.Delay(1000, cancellationToken);
        }

        if (servers.Count == 0)
        {
            this._logger.LogDebug("Unable to read message within {RetryCount} attempts", retryCount);
            return;
        }

        var server = servers.SingleOrDefault(s => s.Id == notification.ServerId);

        if (server is null)
        {
            this._logger.LogDebug("Unable to find server id {Id}", notification.ServerId);
            return;
        }

        var channel =
            server.Channels
                .FirstOrDefault(c => c.Value.Name == ChannelName)
                .Value;

        if (channel is null)
        {
            try
            {
                channel = await server.CreateTextChannelAsync(ChannelName);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Unable to publish a message for server {Server}", server.Name);

                return;
            }
        }

        var itemText = string.Join(" ", notification.Items.Select(r => $"**{r.Name}** x {r.Quantity:N0}"));
        var content = $":star:CODE: `{notification.Code.PadRight(30, '\0')}` Items: {itemText}";

        await channel.SendMessageAsync(content);
    }
}