namespace Kioskitos.Application.Tags;

public sealed record TagDto(Guid Id, string Name);

public sealed class TagService(ITagRepository tags)
{
    /// <summary>Lists the user's tags sorted by name, to offer them when creating a project.</summary>
    public async Task<IReadOnlyList<TagDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var userTags = await tags.ListByUserAsync(userId, ct);

        return userTags
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new TagDto(t.Id, t.Name))
            .ToList();
    }
}
