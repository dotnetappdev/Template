# TemplateApp

An ASP.NET Core solution targeting **net8.0**. The front end is two independent Razor Pages sites (no Areas), and every piece of data goes through one API.

## Architecture

```
<!--#if (IncludeInternal) -->
  TemplateApp.Internal (Razor Pages) --HTTP--+
<!--#endif -->
<!--#if (IncludeExternal) -->
  TemplateApp.External (Razor Pages) --HTTP--+
<!--#endif -->
                                              |
                                              v
                                   TemplateApp.API (controllers)
<!--#if (UseServices) -->
                                              |
                                              v
                                   TemplateApp.Services (ICustomerService ...)
<!--#endif -->
<!--#if (UseSqlRepository) -->
                                              |
                                              v
                                   TemplateApp.SqlRepository (IRepository<T> -> ApplicationDbContext -> database)
<!--#endif -->
```

The sites do **not** reference Services, SqlRepository or EF Core. Each one calls the API through its own typed `HttpClient` clients in `ApiClients/` (for example `ICustomerApiClient` and `IStatusApiClient`), so the two sites can be deployed, scaled and versioned on their own.

| Project | Purpose |
| --- | --- |
<!--#if (IncludeInternal) -->
| `src/TemplateApp.Internal` | Internal (staff / back-office) Razor Pages site. Calls the API only. |
<!--#endif -->
<!--#if (IncludeExternal) -->
| `src/TemplateApp.External` | External (public / customer facing) Razor Pages site. Calls the API only. |
<!--#endif -->
<!--#if (UseApi) -->
| `src/TemplateApp.API` | ASP.NET Core Web API: controllers, Swagger UI at `/swagger`. The only project that uses the data layer. |
<!--#endif -->
<!--#if (UseServices) -->
| `src/TemplateApp.Services` | Business logic: service interfaces (`Interfaces/`) and implementations. |
<!--#endif -->
<!--#if (UseSqlRepository) -->
| `src/TemplateApp.SqlRepository` | EF Core: `ApplicationDbContext`, entities, configurations, `IRepository<T>` / `Repository<T>`, `ICustomerRepository` / `CustomerRepository`. |
<!--#endif -->

Shared build settings are in `Directory.Build.props`. Every NuGet version is set once, in `Directory.Packages.props` (Central Package Management).

## Run it

Start the API first, then the site(s).

**Visual Studio:** right-click the solution → **Configure Startup Projects…** → **Multiple startup projects**. Set `TemplateApp.API` and the site project(s) to **Start**, with the API at the top of the list, then press F5. Visual Studio saves this choice in the `.vs` folder, so you only do it once.

**Command line** (one terminal each):

```
dotnet run --project src/TemplateApp.API --launch-profile https
<!--#if (IncludeInternal) -->
dotnet run --project src/TemplateApp.Internal --launch-profile https
<!--#endif -->
<!--#if (IncludeExternal) -->
dotnet run --project src/TemplateApp.External --launch-profile https
<!--#endif -->
```

If the development HTTPS certificate is not trusted yet, run `dotnet dev-certs https --trust` once.

### How the sites find the API

* `Api:BaseUrl` in each site's `appsettings.Development.json` points at the API's **https** launch profile (`https://localhost:__HTTPS_PORT_API__/`).
* The sites' **http** launch profile overrides it with `Api__BaseUrl=http://localhost:__HTTP_PORT_API__/`, so http-only runs work too.
* For production, set `Api:BaseUrl` in `appsettings.json`, as an environment variable (`Api__BaseUrl`) or in your hosting configuration.

### Adding an API client to a site

1. Add the request and response models to `ApiClients/Models`.
2. Add an interface (`IOrderApiClient`) and an implementation that takes `HttpClient` in its constructor.
3. Register it in `ApiClients/ApiClientServiceCollectionExtensions.cs`: `services.AddApiClient<IOrderApiClient, OrderApiClient>();`
4. Inject the interface into your page model.

<!--#if (UseSqlRepository) -->
## Data layer

<!--#if (UseServices) -->
The API registers everything with `builder.Services.AddApplicationServices(builder.Configuration)` (Services), which calls `AddSqlRepository(...)`.
<!--#else -->
The API registers everything with `builder.Services.AddSqlRepository(builder.Configuration)`.
<!--#endif -->

To add an entity:

1. Create it in `SqlRepository/Entities`, inheriting `BaseEntity`.
2. Add an `IEntityTypeConfiguration<T>` in `Configurations`.
3. Add a `DbSet` to `ApplicationDbContext`.
4. Add `IXxxRepository : IRepository<Xxx>` and `XxxRepository : Repository<Xxx>, IXxxRepository`.
5. Register the repository in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.
<!--#if (UseServices) -->
6. Add a service in Services, then a controller in the API.
<!--#else -->
6. Add a controller in the API.
<!--#endif -->

### Database

<!--#if (UseSqlServer) -->
* Provider: **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`), compatibility level **__SQL_COMPAT_LEVEL__**. To change it, edit `UseCompatibilityLevel(...)` in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.
<!--#elseif (UsePostgreSql) -->
* Provider: **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), version **__POSTGRES_MAJOR__**. To change it, edit `SetPostgresVersion(...)` in `SqlRepository/Extensions/ServiceCollectionExtensions.cs`.
<!--#else -->
* Provider: **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`).
<!--#endif -->
* Connection string: `ConnectionStrings:DefaultConnection` in `src/TemplateApp.API/appsettings.json`. Only the API has one. Keep real passwords out of source control, for example:

  ```
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project src/TemplateApp.API
  ```

* In Development, the API calls `InitialiseDatabaseAsync()` at startup. While there are no migrations, it creates the database with `EnsureCreated`. Once a migration exists, it switches to `Migrate`. It also seeds a few sample customers.

### Migrations

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/TemplateApp.SqlRepository
dotnet ef database update --project src/TemplateApp.SqlRepository
```

At design time, `ApplicationDbContextFactory` reads the `ConnectionStrings__DefaultConnection` environment variable, or uses the default from `DatabaseDefaults.cs`. If you created the database with `EnsureCreated` before adding your first migration, drop the development database first.

In Visual Studio's Package Manager Console, choose **TemplateApp.SqlRepository** as the default project and run `Add-Migration InitialCreate`, then `Update-Database`.
<!--#endif -->

<!--#if (HasSite) -->
## Front end (LibMan)

Both sites use:

* jQuery **__JQUERY_VERSION__**
* jQuery UI **__JQUERYUI_VERSION__** (base theme)
* Bootstrap **__BOOTSTRAP_VERSION__**
* Font Awesome Free **__FONTAWESOME_VERSION__**
* jQuery Validation + jQuery Validation Unobtrusive (forms)

They are listed in each site's `libman.json` and restored into `wwwroot/lib`:

* **On build:** the `Microsoft.Web.LibraryManager.Build` package restores them, so a fresh clone builds with no extra steps. `wwwroot/lib` is git-ignored.
* **In Visual Studio:** saving `libman.json` restores straight away. Right-click `libman.json` → **Restore Client-Side Libraries**, or right-click the project → **Add → Client-Side Library…** to add a new one.
* **From the command line:** `dotnet tool install -g Microsoft.Web.LibraryManager.Cli`, then `libman restore` or `libman install bootstrap@5.3.8 --provider jsdelivr --destination wwwroot/lib/bootstrap`.

To upgrade a library, change the `@version` in `libman.json` in **both** sites and build.

The pages include the libraries in `Pages/Shared/_Styles.cshtml`, `_Scripts.cshtml` and `_ValidationScriptsPartial.cshtml`. Site-wide scripts are in `wwwroot/js/site.js`: the jQuery UI date picker (`.js-datepicker`), dialogs (`.js-open-dialog`) and autocomplete.

`Directory.Build.targets` retries the LibMan restore once. This is in case two sites restore a new library version at the same moment during a parallel build.
<!--#endif -->
