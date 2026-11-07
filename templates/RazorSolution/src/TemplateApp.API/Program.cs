#if (IncludeSerilog)
using Serilog;
#endif
#if (UseRaygun)
using Mindscape.Raygun4Net.AspNetCore;
#endif
#if (UseApiKey)
using TemplateApp.API.Security;
#endif
#if (UseServices)
using TemplateApp.Services.Extensions;
#endif
#if (UseSqlRepository)
using TemplateApp.SqlRepository;
using TemplateApp.SqlRepository.Extensions;
#endif

var builder = WebApplication.CreateBuilder(args);
#if (IncludeSerilog)

// Serilog: configured from the "Serilog" section of appsettings.json.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));
#endif

#if (UseSentry)

// Sentry (sentry.io): unhandled exceptions and logged errors. Settings: "Sentry" section (Dsn). An empty Dsn turns it off.
builder.WebHost.UseSentry();
#elseif (UseRaygun)

// Raygun (raygun.com): unhandled exceptions. Settings: "RaygunSettings" section (ApiKey). Off until an ApiKey is set.
var raygunEnabled = !string.IsNullOrWhiteSpace(builder.Configuration["RaygunSettings:ApiKey"]);
if (raygunEnabled)
{
    builder.Services.AddRaygun(builder.Configuration);
}
#elseif (UseAppInsights)

// Azure Application Insights: exceptions, requests and dependencies. Settings: "ApplicationInsights" section
// (ConnectionString) or the APPLICATIONINSIGHTS_CONNECTION_STRING variable. Off until a connection string is set.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"] ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}
#endif

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
#if (UseSwagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endif
#if (UseServices)

// ProjectName.Services (and ProjectName.SqlRepository under it).
builder.Services.AddApplicationServices(builder.Configuration);
#elseif (UseSqlRepository)

// ProjectName.SqlRepository: ApplicationDbContext + repositories.
builder.Services.AddSqlRepository(builder.Configuration);
#endif
#if (IncludeHealthChecks)

#if (UseSqlRepository)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");
#else
builder.Services.AddHealthChecks();
#endif
#endif

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
#if (UseSwagger)
    app.UseSwagger();
    app.UseSwaggerUI();
#endif
#if (UseSqlRepository)

    // Creates/migrates the development database (and seeds sample data if enabled).
    await app.Services.InitialiseDatabaseAsync();
#endif
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}
#if (UseRaygun)

if (raygunEnabled)
{
    // After the exception handlers, so Raygun sees exceptions before they are turned into responses.
    app.UseRaygun();
}
#endif
#if (IncludeSerilog)

app.UseSerilogRequestLogging();
#endif

app.UseHttpsRedirection();
#if (UseApiKey)

// Every /api request (except /api/status) must carry the X-Api-Key header. See Security/ApiKeyMiddleware.cs.
app.UseMiddleware<ApiKeyMiddleware>();
#endif

app.UseAuthorization();

app.MapControllers();
#if (IncludeHealthChecks)
app.MapHealthChecks("/health");
#endif

app.Run();
