namespace Kioskitos.Domain.Entities;

/// <summary>Lifecycle status shared by projects and their stages.</summary>
public enum ProjectStatus
{
    Planning,
    InProgress,
    Blocked,
    InReview,
    Finished,
    Discarded
}
