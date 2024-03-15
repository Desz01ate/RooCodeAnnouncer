using MediatR;
using Microsoft.Extensions.Logging;
using RooCodeAnnouncer.Contracts.Events;

namespace RooCodeAnnouncer.Discord;

public class DiscordPublisher : INotificationHandler<NewCodeNotification>
{
    private readonly CodeAnnouncerDiscordClient _client;
    private readonly ILogger<DiscordPublisher> _logger;

    public DiscordPublisher(CodeAnnouncerDiscordClient client, ILogger<DiscordPublisher> logger)
    {
        this._client = client;
        this._logger = logger;
    }

    public async Task Handle(NewCodeNotification notification, CancellationToken cancellationToken)
    {
        const string channelName = "ragnarok_origins_item_code";

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
                    .FirstOrDefault(c => c.Value.Name == channelName)
                    .Value;

            if (channel is null)
            {
                try
                {
                    channel = await server.CreateTextChannelAsync(channelName);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Unable to publish a message for server {Server}", server.Name);

                    continue;
                }
            }

            var itemText = string.Join(" ", notification.Items.Select(r => $"**{r.Name}** x {r.Quantity:N0}"));
            var content = $":star:CODE: `{notification.Code.PadRight(30, '\0')}` Items: {itemText}";

            await channel.SendMessageAsync(content);
        }
    }
}