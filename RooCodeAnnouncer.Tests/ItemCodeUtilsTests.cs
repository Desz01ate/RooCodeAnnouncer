using RooCodeAnnouncer.Contracts;
using RooCodeAnnouncer.Utils;

namespace RooCodeAnnouncer.Tests;

public class ItemCodeUtilsTests
{
    [Fact]
    public void Rewards_WithAlphabetic_ShouldExtractSuccessfully()
    {
        const string text = "1 x  Gacha Ticket1 x  Super Pet Coupon1 x  Pet Coupon";

        var rewards = ItemCodeUtils.Parse(text);

        Assert.Contains(new Reward("Gacha Ticket", 1), rewards);
        Assert.Contains(new Reward("Super Pet Coupon", 1), rewards);
        Assert.Contains(new Reward("Pet Coupon", 1), rewards);
    }

    [Fact]
    public void Rewards_WithAlphanumeric_ShouldExtractSuccessfully()
    {
        const string text = "1 x Perfect Sigil Crystal1 x Core Reinforcement Device 2.0 (7-day)1 x Precious Sigil Pack";

        var rewards = ItemCodeUtils.Parse(text);

        Assert.Contains(new Reward("Perfect Sigil Crystal", 1), rewards);
        Assert.Contains(new Reward("Core Reinforcement Device 2.0 (7-day)", 1), rewards);
        Assert.Contains(new Reward("Precious Sigil Pack", 1), rewards);
    }

    [Fact]
    public void Rewards_Without_x_ShouldExtractSuccessfully()
    {
        const string text = "200 x Diamond1,000 Zeny100,000 Eden Coins";

        var rewards = ItemCodeUtils.Parse(text);

        Assert.Contains(new Reward("Diamond", 200), rewards);
        Assert.Contains(new Reward("Zeny", 1000), rewards);
        Assert.Contains(new Reward("Eden Coins", 100_000), rewards);
    }
}