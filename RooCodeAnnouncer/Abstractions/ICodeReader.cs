using RooCodeAnnouncer.Contracts;

namespace RooCodeAnnouncer.Abstractions;

public interface ICodeReader
{
    IAsyncEnumerable<ItemCode> ReadAsync(CancellationToken cancellationToken = default);
}