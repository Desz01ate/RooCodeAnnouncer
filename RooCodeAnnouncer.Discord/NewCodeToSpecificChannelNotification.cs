using MediatR;
using RooCodeAnnouncer.Contracts;

namespace RooCodeAnnouncer.Discord;

public record NewCodeToSpecificChannelNotification(ulong ServerId, string Code, Reward[] Items) : INotification;