namespace Kioskitos.Domain.Entities;

/// <summary>Join entity between <see cref="Project"/> and <see cref="Tag"/>.</summary>
public sealed class ProjectTag
{
    public Guid ProjectId { get; private set; }
    public Guid TagId { get; private set; }
    public Tag Tag { get; private set; } = null!;

    private ProjectTag() { } // EF Core

    // Internal: project tags are only created through Project.AddTag.
    internal ProjectTag(Guid projectId, Tag tag)
    {
        ProjectId = projectId;
        TagId = tag.Id;
        Tag = tag;
    }
}
