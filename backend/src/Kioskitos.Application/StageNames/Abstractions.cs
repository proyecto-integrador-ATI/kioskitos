using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.StageNames;

public interface IStageNameRepository
{
    Task<IReadOnlyList<StageName>> ListByUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns the user's stage names matching any of <paramref name="names"/>, ignoring case.
    /// Returned entities must be tracked so they are not inserted again.
    /// </summary>
    Task<IReadOnlyList<StageName>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct);

    /// <summary>Adds a new stage name. It is persisted by the next SaveChanges of the unit of work.</summary>
    Task AddAsync(StageName stageName, CancellationToken ct);
}
