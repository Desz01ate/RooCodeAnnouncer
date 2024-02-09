using MediatR;
using RooCodeAnnouncer.Events;

namespace RooCodeAnnouncer.Publishers;

public class DiscordPublisher : INotificationHandler<NewCodeNotification>
{
    public Task Handle(NewCodeNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}