# TemplateApp

An ASP.NET Core Razor Pages solution (no Areas) targeting **net8.0**.

## Projects

| Project | Purpose |
| --- | --- |
| `src/TemplateApp.Web` | Main Razor Pages web site. |
<!--#if (IncludeApi) -->
| `src/TemplateApp.API` | ASP.NET Core Web API (controllers + Swagger UI at `/swagger`). |
<!--#endif -->
<!--#if (IncludeInternal) -->
| `src/TemplateApp.Internal` | Internal (staff / back-office) Razor Pages web site. |
<!--#endif -->
<!--#if (IncludeExternal) -->
| `src/TemplateApp.External` | External (public / customer facing) Razor Pages web site. |
<!--#endif -->
<!--#if (UseServices) -->
| `src/TemplateApp.Services` | Business logic. Service interfaces (`Interfaces/`) and their implementations. Hosts depend on `ICustomerService` and so on, never on EF Core. |
<!--#endif -->
<!--#if (UseSqlRepository) -->
| `src/TemplateApp.SqlRepository` | EF Core: `ApplicationDbContext`, entities, entity configurations, and the repository pattern (`IRepository<T>`, `ICustomerRepository`, `Repository<T>`, `CustomerRepository`). |
<!--#endif -->

Shared build settings are in `Directory.Build.props`. Every NuGet version is set once, in `Directory.Packages.props` (Central Package Management).

<!--#if (UseSqlRepository) -->
## Layers

```
<!--#if (UseServices) -->
Web / Internal / External / API  ->  Services (ICustomerService)  ->  SqlRepository (ICustomerRepository -> ApplicationDbContext)
<!--#else -->
Web / Internal / External / API  ->  SqlRepository (ICustomerRepository -> ApplicationDbContext)
<!--#endif -->
```

<!--#if (UseServices) -->
* Registration: `builder.Services.AddApplicationServices(builder.Configuration)` in each host's `Program.cs`. It also calls `AddSqlRepository(...)`.
<!--#else -->
* Registration: `builder.Services.AddSqlRepository(builder.Configuration)` in each host's `Program.cs`.
<!--#endif -->
* Add an entity: create it in `SqlRepository/Entities` (inherit `BaseEntity`), add an `IEntityTypeConfiguration<T>` in `Configurations`, add a `DbSet` to `ApplicationDbContext`, add `IXxxRepository : IRepository<Xxx>` and `XxxRepository : Repository<Xxx>, IXxxRepository`, then register it in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.

## Database

<!--#if (UseSqlServer) -->
* Provider: **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`), compatibility level **__SQL_COMPAT_LEVEL__**. To change the level, edit `UseCompatibilityLevel(...)` in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.
<!--#elseif (UsePostgreSql) -->
* Provider: **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), version **__POSTGRES_MAJOR__**. To change it, edit `SetPostgresVersion(...)` in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.
<!--#else -->
* Provider: **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`).
<!--#endif -->
* Connection string: `ConnectionStrings:DefaultConnection` in each host's `appsettings.json`. Keep real passwords out of source control. Use user-secrets, for example:

  ```
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project src/TemplateApp.Web
  ```

* In Development, every host calls `InitialiseDatabaseAsync()` at startup. While there are no migrations, it creates the database with `EnsureCreated`. Once a migration exists, it switches to `Migrate`. It also seeds a few sample customers.

### Migrations

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/TemplateApp.SqlRepository
dotnet ef database update --project src/TemplateApp.SqlRepository
```

At design time, `ApplicationDbContextFactory` reads the `ConnectionStrings__DefaultConnection` environment variable, or uses the default from `DatabaseDefaults.cs`.
If you created the database with `EnsureCreated` before adding your first migration, drop the development database first.

In Visual Studio's Package Manager Console, choose **TemplateApp.SqlRepository** as the default project and run `Add-Migration InitialCreate`, then `Update-Database`.
<!--#endif -->

## Front end

Razor Pages sites use:

* jQuery **__JQUERY_VERSION__**
* jQuery UI **__JQUERYUI_VERSION__** (base theme)
* Bootstrap **__BOOTSTRAP_VERSION__**
* Font Awesome Free **__FONTAWESOME_VERSION__**
* jQuery Validation + jQuery Validation Unobtrusive (forms)

<!--#if (UseLibMan) -->
The libraries are listed in each site's `libman.json`. They are restored into `wwwroot/lib` on build (the `Microsoft.Web.LibraryManager.Build` package) and when you save `libman.json` in Visual Studio. To change a version, edit the `library` entry, for example `"bootstrap@5.3.8"`. Right-click `libman.json` → **Manage Client-Side Libraries** to browse versions. `wwwroot/lib` is git-ignored.
<!--#else -->
The libraries load from the jsDelivr CDN in `Pages/Shared/_Styles.cshtml`, `_Scripts.cshtml` and `_ValidationScriptsPartial.cshtml`. To change a version, edit the URLs. Consider adding `integrity` (SRI) hashes for production.
<!--#endif -->

Site-wide scripts are in `wwwroot/js/site.js`: the jQuery UI date picker (`.js-datepicker`), dialogs (`.js-open-dialog`) and autocomplete.

## Run

```
dotnet run --project src/TemplateApp.Web
<!--#if (IncludeApi) -->
dotnet run --project src/TemplateApp.API
<!--#endif -->
<!--#if (IncludeInternal) -->
dotnet run --project src/TemplateApp.Internal
<!--#endif -->
<!--#if (IncludeExternal) -->
dotnet run --project src/TemplateApp.External
<!--#endif -->
```

In Visual Studio, right-click the solution → **Configure Startup Projects** → **Multiple startup projects** to run several sites at once.
