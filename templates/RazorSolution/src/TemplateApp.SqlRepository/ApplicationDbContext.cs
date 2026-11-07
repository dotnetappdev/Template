#if (UseIdentity)
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#endif
using Microsoft.EntityFrameworkCore;
using TemplateApp.SqlRepository.Entities;

namespace TemplateApp.SqlRepository;

/// <summary>
/// The EF Core unit of work for the application. Entity configuration lives in
/// the Configurations folder (one <see cref="IEntityTypeConfiguration{TEntity}"/> per entity).
#if (UseIdentity)
/// It also stores ASP.NET Core Identity's users, roles, logins, tokens and (on .NET 10+) passkeys.
#endif
/// </summary>
#if (UseIdentity)
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
#else
public class ApplicationDbContext : DbContext
#endif
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
