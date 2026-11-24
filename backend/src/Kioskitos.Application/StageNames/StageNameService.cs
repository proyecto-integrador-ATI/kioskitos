namespace Kioskitos.Application.StageNames;

public sealed record StageNameDto(Guid Id, string Name);

public sealed class StageNameService(IStageNameRepository stageNames)
{
    /// <summary>Lists the user's stage names sorted by name, to offer them when creating a project.</summary>
    public async Task<IReadOnlyList<StageNameDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var names = await stageNames.ListByUserAsync(userId, ct);

        return names
            .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .Select(n => new StageNameDto(n.Id, n.Name))
            .ToList();
    }
}
