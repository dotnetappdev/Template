using Microsoft.EntityFrameworkCore;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.SqlRepository;

/// <summary>
/// The EF Core unit of work for the application. Entity configuration lives in
/// the Configurations folder (one <see cref="IEntityTypeConfiguration{TEntity}"/> per entity).
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
