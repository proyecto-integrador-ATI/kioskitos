namespace Kioskitos.Domain.Entities;

/// <summary>
/// Stage of a project. Projects without stages get a single generic stage
/// (<see cref="IsGeneric"/>) that is not shown to the user as a stage.
/// </summary>
public sealed class Stage
{
    public const int DescriptionMaxLength = 2000;
    public const int DeliverableMaxLength = 500;

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? StageNameId { get; private set; }
    public StageName? StageName { get; private set; }

    /// <summary>Order of the stage inside its project, starting at 0.</summary>
    public int Position { get; private set; }

    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public string? Deliverable { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public bool IsGeneric { get; private set; }

    private Stage() { } // EF Core

    private Stage(Guid projectId, StageName? stageName, int position, bool isGeneric)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        StageNameId = stageName?.Id;
        StageName = stageName;
        Position = position;
        Status = ProjectStatus.Planning;
        CreatedAt = DateTime.UtcNow;
        IsGeneric = isGeneric;
    }

    /// <summary>Generic stage for projects without stages: every optional field stays null.</summary>
    internal static Stage CreateGeneric(Guid projectId) =>
        new(projectId, stageName: null, position: 0, isGeneric: true);

    /// <summary>Named stage created with its project. The other fields are filled later (RF 09.01).</summary>
    internal static Stage CreateNamed(Guid projectId, StageName stageName, int position) =>
        new(projectId, stageName, position, isGeneric: false);
}
