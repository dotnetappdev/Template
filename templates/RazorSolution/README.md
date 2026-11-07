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

Both sites use these libraries, chosen when the solution was created:

| Library | Version | Used for |
| --- | --- | --- |
| jQuery | __JQUERY_VERSION__ | everything below |
| jQuery UI | __JQUERYUI_VERSION__, theme **__JQUERYUI_THEME__** (cdnjs) | date picker (`.js-datepicker`), dialogs (`.js-open-dialog`), autocomplete |
<!--#if (UseBootswatch) -->
| Bootstrap + Bootswatch | __BOOTSTRAP_VERSION__, theme **__BOOTSWATCH_THEME__** | layout and components |
<!--#else -->
| Bootstrap | __BOOTSTRAP_VERSION__ | layout and components |
<!--#endif -->
<!--#if (UseFontAwesome) -->
| Font Awesome **Free** | __FONTAWESOME_VERSION__ | icons (`<i class="fas fa-...">`); the free `@fortawesome/fontawesome-free` package, no Pro licence needed |
<!--#endif -->
<!--#if (IncludeJQueryValidation) -->
| jQuery Validation + Unobtrusive | 1.22.1 / 4.0.0 | client-side form validation (`_ValidationScriptsPartial`) |
<!--#endif -->
<!--#if (IncludeDataTables) -->
| DataTables | 2.3.8 | sorting, paging and search for `<table class="js-datatable">` |
<!--#endif -->
<!--#if (IncludeSelect2) -->
| Select2 | 4.1.0 | searchable drop-downs for `<select class="js-select2">` |
<!--#endif -->
<!--#if (IncludeToastr) -->
| Toastr | 2.1.4 | `TempData["Message"]` is shown as a toast (see `_Layout.cshtml`) |
<!--#endif -->
<!--#if (IncludeSweetAlert) -->
| SweetAlert2 | 11.26.25 | confirm dialogs for `<form class="js-confirm" data-confirm="...">` |
<!--#endif -->
<!--#if (IncludeChartJs) -->
| Chart.js | 4.5.1 | charts (sample on the Internal dashboard) |
<!--#endif -->

They are listed in each site's `libman.json` and restored into `wwwroot/lib`:

* **On build:** the `Microsoft.Web.LibraryManager.Build` package restores them, so a fresh clone builds with no extra steps. `wwwroot/lib` is git-ignored.
* **In Visual Studio:** saving `libman.json` restores straight away. Right-click `libman.json` → **Restore Client-Side Libraries**, or right-click the project → **Add → Client-Side Library…** to add one.
* **From the command line:** `dotnet tool install -g Microsoft.Web.LibraryManager.Cli`, then `libman restore`.

To upgrade a library, change its `@version` in `libman.json` in **both** sites and build.

* To change the **jQuery UI theme**, replace `__JQUERYUI_THEME__` in `libman.json` and `Pages/Shared/_Styles.cshtml` with any [ThemeRoller](https://jqueryui.com/themeroller/) gallery theme (`smoothness`, `redmond`, `flick`, ...).
* To change the **Bootstrap theme**, use a [Bootswatch](https://bootswatch.com/) theme: add `bootswatch@<bootstrap version>` with `dist/<theme>/bootstrap.min.css` to `libman.json`, and point `_Styles.cshtml` at it.

The pages include the libraries in `Pages/Shared/_Styles.cshtml`, `_Scripts.cshtml` and `_ValidationScriptsPartial.cshtml`. The site-wide wiring for the plugins is in `wwwroot/js/site.js`.

`Directory.Build.targets` retries the LibMan restore up to three times. This is in case both sites download a new library version at the same moment during a parallel build.
<!--#endif -->
<!--#if (UseInternalAuth || UseApiKey || IncludeHealthChecks || IncludeSerilog || IncludeResilience || UseErrorMonitoring) -->

## Security and operations

<!--#if (UseWindowsAuth) -->
* **Internal site, Windows authentication:** `AddNegotiate()` plus a fallback policy, so every page needs a signed-in Windows user. On IIS, enable Windows Authentication for the site. On Kestrel (Windows), it works as is. On Linux, Kerberos needs a keytab.
<!--#endif -->
<!--#if (UseEntraId) -->
* **Internal site, Microsoft Entra ID:** `AddMicrosoftIdentityWebApp(...)` plus a fallback policy, so every page needs a signed-in user. Set `AzureAd:TenantId` and `AzureAd:ClientId` in `appsettings.json`, and register `https://localhost:__HTTPS_PORT_INTERNAL__/signin-oidc` as a redirect URI on the app registration.
<!--#endif -->
<!--#if (UseApiKey) -->
* **API key:** the API rejects `/api/*` requests (except `/api/status`) that don't carry `X-Api-Key: <Security:ApiKey>`. The sites send `Api:ApiKey` through `ApiClients/ApiKeyHandler.cs`. A random key was generated for this solution. Move it to user-secrets or Key Vault for real environments (`dotnet user-secrets set "Security:ApiKey" "<key>" --project src/TemplateApp.API`).
<!--#endif -->
<!--#if (IncludeHealthChecks) -->
<!--#if (UseSqlRepository) -->
* **Health checks:** every project maps `/health`. The API's check includes the database (`AddDbContextCheck<ApplicationDbContext>`).
<!--#else -->
* **Health checks:** every project maps `/health`.
<!--#endif -->
<!--#endif -->
<!--#if (IncludeSerilog) -->
* **Serilog:** logging is configured in the `Serilog` section of each `appsettings.json`. It writes to the console and to rolling files in `logs/`, and logs each request with `UseSerilogRequestLogging()`.
<!--#endif -->
<!--#if (UseSentry) -->
* **Error monitoring, Sentry:**
  * **Server side:** the API and both sites call `builder.WebHost.UseSentry()`. They send unhandled exceptions, and anything logged at Error or above, to sentry.io.
  * **Browser side:** each site's `Pages/Shared/_Scripts.cshtml` loads the Sentry browser SDK (restored by LibMan from browser.sentry-cdn.com), which reports JavaScript errors.
  * **Settings:** the `Sentry` section of each `appsettings.json`: `Dsn`, `TracesSampleRate`, `MinimumEventLevel`. An empty `Dsn` turns Sentry off. You can give each project its own Sentry project and DSN.
<!--#elseif (UseRaygun) -->
* **Error monitoring, Raygun:**
  * **Server side:** the API and both sites use `AddRaygun(...)` / `UseRaygun()` (Mindscape.Raygun4Net.AspNetCore) and send unhandled exceptions to raygun.com.
  * **Browser side:** each site's `Pages/Shared/_Scripts.cshtml` starts raygun4js, which reports JavaScript errors.
  * **Settings:** `RaygunSettings:ApiKey` in each `appsettings.json`. Raygun stays off until an `ApiKey` is set.
<!--#elseif (UseAppInsights) -->
* **Error monitoring, Azure Application Insights:** the API and both sites call `AddApplicationInsightsTelemetry()`. This sends exceptions, requests, dependencies (including the sites' calls to the API) and logs to Azure Monitor. Set `ApplicationInsights:ConnectionString` in `appsettings.json`, or the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable (as App Service does). Telemetry stays off until a connection string is set.
<!--#endif -->
<!--#if (IncludeResilience) -->
* **Resilience:** the sites' API clients use `AddStandardResilienceHandler()`, which adds retries with back-off, timeouts and a circuit breaker.
<!--#endif -->
<!--#endif -->
<!--#if (UseTests) -->

## Tests

`tests/TemplateApp.Tests` uses xUnit and NSubstitute. Because the services depend on repository *interfaces*, they are tested with fakes and no database:

```
dotnet test
```
<!--#endif -->
