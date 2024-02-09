using RooCodeAnnouncer.Models;

namespace RooCodeAnnouncer.Abstractions;

public interface ICodeReader
{
    IAsyncEnumerable<ItemCode> ReadAsync(CancellationToken cancellationToken = default);
}