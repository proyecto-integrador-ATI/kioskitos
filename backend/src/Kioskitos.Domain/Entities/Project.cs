namespace Kioskitos.Domain.Entities;

public sealed class Project
{
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 2000;

    private readonly List<ProjectTag> _tags = [];
    private readonly List<Stage> _stages = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ClientId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Objectives { get; private set; }
    public string? Scope { get; private set; }
    public ProjectStatus Status { get; private set; }
    public bool HasStages { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public IReadOnlyCollection<ProjectTag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<Stage> Stages => _stages.AsReadOnly();

    private Project() { } // EF Core

    /// <summary>
    /// Creates a project. When <paramref name="hasStages"/> is false a single generic
    /// stage is created with it; otherwise the project starts without stages.
    /// </summary>
    public Project(
        Guid userId,
        Guid clientId,
        string name,
        string? description,
        string? objectives,
        string? scope,
        bool hasStages,
        DateOnly? dueDate,
        ProjectStatus status = ProjectStatus.Planning)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The project must belong to a user.", nameof(userId));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("The project must belong to a client.", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The project name is required.", nameof(name));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid project status.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        ClientId = clientId;
        Name = name.Trim();
        Description = Normalize(description);
        Objectives = Normalize(objectives);
        Scope = Normalize(scope);
        Status = status;
        HasStages = hasStages;
        CreatedAt = DateTime.UtcNow;
        DueDate = dueDate;

        if (!hasStages)
        {
            _stages.Add(Stage.CreateGeneric(Id));
        }
    }

    /// <summary>Associates a tag of the same user. Adding an already associated tag does nothing.</summary>
    public void AddTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (tag.UserId != UserId)
        {
            throw new InvalidOperationException("The tag belongs to another user.");
        }

        if (_tags.Any(t => t.TagId == tag.Id))
        {
            return;
        }

        _tags.Add(new ProjectTag(Id, tag));
    }

    /// <summary>
    /// Adds a named stage at the end. Only for projects with stages; the name must be of the
    /// same user and cannot be repeated in the project.
    /// </summary>
    public Stage AddStage(StageName stageName)
    {
        ArgumentNullException.ThrowIfNull(stageName);

        if (!HasStages)
        {
            throw new InvalidOperationException("A project without stages only has its generic stage.");
        }

        if (stageName.UserId != UserId)
        {
            throw new InvalidOperationException("The stage name belongs to another user.");
        }

        if (_stages.Any(s => s.StageNameId == stageName.Id))
        {
            throw new InvalidOperationException("The project already has a stage with that name.");
        }

        var stage = Stage.CreateNamed(Id, stageName, _stages.Count);
        _stages.Add(stage);
        return stage;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
