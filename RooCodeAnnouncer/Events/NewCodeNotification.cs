using MediatR;

namespace RooCodeAnnouncer.Events;

public sealed record NewCodeNotification(string Code, string Items) : INotification;