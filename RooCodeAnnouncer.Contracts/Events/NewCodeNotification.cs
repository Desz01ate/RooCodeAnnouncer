using MediatR;

namespace RooCodeAnnouncer.Contracts.Events;

public sealed record NewCodeNotification(string Code, Reward[] Items) : INotification;