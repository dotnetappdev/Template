# Razor Pages Solution Template

A `dotnet new` / Visual Studio template that creates an ASP.NET Core **Razor Pages** solution (no Areas) with an optional multi-project layout, EF Core and the repository pattern, plus a choice of front-end library versions.

```
MyCompany.Portal/
├── MyCompany.Portal.sln
├── Directory.Build.props         shared build settings (nullable, implicit usings, ...)
├── Directory.Packages.props      every NuGet version, in one place (Central Package Management)
└── src/
    ├── MyCompany.Portal.Web             Razor Pages web site (always created)
    ├── MyCompany.Portal.API             Web API: controllers + Swagger            [checkbox]
    ├── MyCompany.Portal.Internal        Internal / staff Razor Pages site         [checkbox]
    ├── MyCompany.Portal.External        External / public Razor Pages site        [checkbox]
    ├── MyCompany.Portal.Services        Business services: ICustomerService ...   [checkbox]
    └── MyCompany.Portal.SqlRepository   EF Core ApplicationDbContext, entities,   [checkbox]
                                         IRepository<T>, ICustomerRepository, Repository<T>
```

Dependencies flow one way: **sites / API → Services → SqlRepository → ApplicationDbContext**. Consumers depend only on interfaces (`ICustomerService`, `ICustomerRepository`, `IRepository<T>`), which is the repository pattern.

## Options in the New Project dialog

This works the same way as the [Shiny .NET templates](https://github.com/shinyorg/templates). Every option is a `template.json` parameter, and `.template.config/ide.host.json` makes it visible in Visual Studio's **Additional information** page. On the command line, the same options are switches.

| Option | Visual Studio | CLI switch | Values (default **bold**) |
| --- | --- | --- | --- |
| .NET version | Framework | `-f, --framework` | **`net8.0`** (LTS), `net9.0` (STS), `net10.0` (LTS), `net11.0` (STS, preview) |
| Create ProjectName.API | checkbox | `--api` | **true** / false |
| Create ProjectName.Internal | checkbox | `--internal` | **true** / false |
| Create ProjectName.External | checkbox | `--external` | **true** / false |
| Create ProjectName.Services | checkbox | `--services` | **true** / false (also adds SqlRepository) |
| Create ProjectName.SqlRepository | checkbox | `--sql-repository` | **true** / false |
| Database | dropdown | `--database` | **`SqlServer`**, `PostgreSQL`, `Sqlite` |
| SQL Server version | dropdown | `--sql-version` | `SqlServer2016`, `SqlServer2017`, `SqlServer2019`, **`SqlServer2022`**, `SqlServer2025`, `AzureSql` |
| PostgreSQL version | dropdown | `--postgres-version` | `14`, `15`, `16`, **`17`**, `18` |
| Database authentication | dropdown | `--db-auth` | **`Integrated`** (Windows; Entra ID on Azure SQL), `SqlLogin` |
| Database server | text | `--db-server` | blank = `(localdb)\MSSQLLocalDB` / `your-server.database.windows.net` / `localhost` |
| Database name | text | `--db-name` | blank = project name |
| Database user / password | text | `--db-user`, `--db-password` | blank = `sa` or `postgres` / `Your_password123` |
| Connection string | text | `--connection-string` | a full connection string. If set, the four fields above are ignored |
| jQuery version | dropdown | `--jquery` | `4.0.0`, **`3.7.1`**, `3.6.4` |
| jQuery UI version | dropdown | `--jquery-ui` | **`1.14.2`**, `1.13.3` |
| Bootstrap version | dropdown | `--bootstrap` | **`5.3.8`**, `5.2.3`, `4.6.2` (the markup adapts to v4 or v5) |
| Font Awesome version | dropdown | `--fontawesome` | `7.3.1`, **`6.7.2`**, `5.15.4` |
| Client library delivery | dropdown | `--client-libs` | **`LibMan`** (local `wwwroot/lib`, restored on build), `Cdn` (jsDelivr) |

What the database options produce:

* EF Core packages that match the chosen .NET version (8.0.x, 9.0.x, 10.0.x or 11.0 preview), set in `Directory.Packages.props`.
* SQL Server: `UseSqlServer(...).UseCompatibilityLevel(130–170)` for the chosen version. Azure SQL also gets `EnableRetryOnFailure`.
* PostgreSQL: `UseNpgsql(...).SetPostgresVersion(n, 0)`.
* The connection string goes into each host's `appsettings.json` and into `DatabaseDefaults.cs`, which the EF design-time factory uses. Values are JSON- and C#-escaped, so `(localdb)\MSSQLLocalDB` and passwords with quotes are safe.

## Install

### From source (this repo)

```bash
# Windows
./build/install-template.ps1
# macOS / Linux
./build/install-template.sh
```

Both scripts run `dotnet pack` into `artifacts/`, then `dotnet new install` the `.nupkg`.

You can also install straight from the folder while you are developing the template:

```bash
dotnet new install ./templates/RazorSolution
```

### From a package feed

```bash
dotnet new install DotNetAppDev.RazorSolution.Templates
```

To uninstall: `dotnet new uninstall DotNetAppDev.RazorSolution.Templates`.

## Use

### Visual Studio 2022 (17.3 or later)

1. Install the template with one of the commands above, then restart Visual Studio.
2. **File → New → Project**, search for **Razor Pages Solution**. You can also filter by C# / Web.
3. On **Additional information**, pick the framework, tick the projects you want, and choose the database, version, connection details, and the jQuery, jQuery UI, Bootstrap and Font Awesome versions.

> Templates installed with `dotnet new install` appear in Visual Studio automatically. If the template is missing, make sure **Tools → Options → Environment → Preview Features → Show all .NET Core templates in the New project dialog** is checked. In older 17.x builds this setting is not a preview feature and is on by default.

### Command line

```bash
dotnet new razorsln -n MyCompany.Portal

# .NET 10, PostgreSQL 17, no External site, Bootstrap 5.3.8, jQuery 4
dotnet new razorsln -n MyCompany.Portal -f net10.0 --database PostgreSQL --postgres-version 17 \
    --external false --bootstrap 5.3.8 --jquery 4.0.0

# SQL Server 2019 with a SQL login
dotnet new razorsln -n MyCompany.Portal --sql-version SqlServer2019 --db-auth SqlLogin \
    --db-server sql01 --db-name Portal --db-user portal_app --db-password "S3cret!"

# Azure SQL with your own connection string, front-end libraries from the CDN
dotnet new razorsln -n MyCompany.Portal --sql-version AzureSql --client-libs Cdn \
    --connection-string "Server=tcp:contoso.database.windows.net,1433;Database=Portal;Authentication=Active Directory Default;Encrypt=True"

# Just the Razor Pages site
dotnet new razorsln -n MyCompany.Site --api false --internal false --external false --services false --sql-repository false

dotnet new razorsln --help   # every option
```

## Repository layout

| Path | What |
| --- | --- |
| `templates/RazorSolution/` | The template content (a source solution named `TemplateApp`; `TemplateApp` is replaced with your project name). |
| `templates/RazorSolution/.template.config/template.json` | Parameters, conditional files, generated values (EF/Npgsql/Swashbuckle versions, SQL compatibility level, escaped connection-string parts, ports). |
| `templates/RazorSolution/.template.config/ide.host.json` | Visual Studio dialog: which options are shown, their labels and descriptions. |
| `templates/RazorSolution/.template.config/dotnetcli.host.json` | CLI switch names (`--api`, `--database`, `--bootstrap`, ...). |
| `DotNetAppDev.RazorSolution.Templates.csproj` | Packs the template into a NuGet template package. |
| `tests/test-template.sh` | Creates a matrix of solutions (all frameworks and databases, CDN/LibMan, Bootstrap 4/5, web-only, custom connection string) and builds each one. |

### Maintaining versions

* **.NET / EF Core / Npgsql / Swashbuckle**: edit the `Framework` choices and the `EfCoreVersion`, `NpgsqlVersion` and `SwashbuckleVersion` switch generators in `template.json`.
* **Front-end libraries**: add or remove `choices` under `JQueryVersion`, `JQueryUIVersion`, `BootstrapVersion` and `FontAwesomeVersion`. All versions are fetched from jsDelivr (npm), so any published npm version works.
* **SQL Server versions**: `SqlServerVersion` choices plus the `SqlCompatibilityLevel` switch.

Template source files contain tokens such as `__BOOTSTRAP_VERSION__` and `__EFCORE_VERSION__`. They are replaced when you create a solution, so build the generated output (see `tests/test-template.sh`), not `templates/RazorSolution` itself.
