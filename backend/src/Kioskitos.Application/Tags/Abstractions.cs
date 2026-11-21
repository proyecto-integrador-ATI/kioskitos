using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Tags;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> ListByUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns the user's tags whose name matches any of <paramref name="names"/>,
    /// ignoring case. Returned tags must be tracked so they are not inserted again.
    /// </summary>
    Task<IReadOnlyList<Tag>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct);

    /// <summary>Adds a new tag. It is persisted by the next SaveChanges of the unit of work.</summary>
    Task AddAsync(Tag tag, CancellationToken ct);
}
