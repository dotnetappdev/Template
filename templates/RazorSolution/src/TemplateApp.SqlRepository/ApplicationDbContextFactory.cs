using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TemplateApp.SqlRepository.Extensions;

namespace TemplateApp.SqlRepository;

/// <summary>
/// Used only by the EF Core tools (dotnet ef / Package Manager Console) at design time, so that
/// migrations can be added from this project without a startup project:
///   dotnet ef migrations add InitialCreate --project src/TemplateApp.SqlRepository
/// The connection string comes from the ConnectionStrings__DefaultConnection environment variable,
/// or the development default below.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DatabaseDefaults.DevelopmentConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseConfiguredDatabase(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
