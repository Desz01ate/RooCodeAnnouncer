namespace RooCodeAnnouncer.Models;

public record struct Reward(string Name, int? Quantity);

public record struct ItemCode(string Code, string RawRewards, bool IsNew, Reward[] Rewards);