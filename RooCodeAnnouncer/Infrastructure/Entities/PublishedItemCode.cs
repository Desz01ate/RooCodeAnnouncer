namespace RooCodeAnnouncer.Infrastructure.Entities;

public record PublishedItemCode
{
    public PublishedItemCode(string id, string rewards)
    {
        this.Id = id;
        this.Rewards = rewards;
    }

    public string Id { get; private init; }

    public string Rewards { get; private init; }
}