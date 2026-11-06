namespace TemplateApp.SqlRepository.Entities;

/// <summary>
/// Base class for every entity stored by <see cref="ApplicationDbContext"/>.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ModifiedUtc { get; set; }
}
