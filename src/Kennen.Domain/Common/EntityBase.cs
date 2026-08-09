namespace Kennen.Domain.Common;

/// <summary>
/// Base type for all persisted aggregates. Ids are client-generatable GUIDs so that
/// callers can correlate a record before it round-trips through the database.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
