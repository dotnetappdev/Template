#if (IncludeSerilog)
using Serilog;
#endif
#if (UseRaygun)
using Mindscape.Raygun4Net.AspNetCore;
#endif
#if (UseWindowsAuth)
using Microsoft.AspNetCore.Authentication.Negotiate;
#elseif (UseEntraId)
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
#endif
using TemplateApp.Internal.ApiClients;

var builder = WebApplication.CreateBuilder(args);
#if (IncludeSerilog)

// Serilog: configured from the "Serilog" section of appsettings.json.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));
#endif

#if (UseSentry)

// Sentry (sentry.io): unhandled exceptions and logged errors. Settings: "Sentry" section (Dsn). An empty Dsn turns it off.
// The browser SDK is started in Pages/Shared/_Scripts.cshtml with the same Dsn.
builder.WebHost.UseSentry();
#elseif (UseRaygun)

// Raygun (raygun.com): unhandled exceptions. Settings: "RaygunSettings" section (ApiKey). Off until an ApiKey is set.
// raygun4js (browser errors) is started in Pages/Shared/_Scripts.cshtml with the same ApiKey.
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

// Razor Pages (no Areas): pages live in /Pages and map to URLs by folder.
builder.Services.AddRazorPages();
#if (UseWindowsAuth)

// Windows authentication (Negotiate/Kerberos/NTLM). Every page requires a signed-in user.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);
#elseif (UseEntraId)

// Microsoft Entra ID sign-in (settings in the "AzureAd" section). Every page requires a signed-in user.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);
#endif


// Typed HttpClients for TemplateApp.API. This site never talks to the database directly.
builder.Services.AddApiClients(builder.Configuration);
#if (IncludeHealthChecks)
builder.Services.AddHealthChecks();
#endif

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
#if (UseRaygun)

if (raygunEnabled)
{
    // After the exception handler, so Raygun sees exceptions before they are turned into the error page.
    app.UseRaygun();
}
#endif
#if (IncludeSerilog)

app.UseSerilogRequestLogging();
#endif

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
#if (UseInternalAuth)
app.UseAuthentication();
#endif

app.UseAuthorization();

app.MapRazorPages();
#if (IncludeHealthChecks)
#if (UseInternalAuth)
app.MapHealthChecks("/health").AllowAnonymous();
#else
app.MapHealthChecks("/health");
#endif
#endif

app.Run();
