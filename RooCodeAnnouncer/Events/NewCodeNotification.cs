using MediatR;
using RooCodeAnnouncer.Models;

namespace RooCodeAnnouncer.Events;

public sealed record NewCodeNotification(string Code, Reward[] Items) : INotification;