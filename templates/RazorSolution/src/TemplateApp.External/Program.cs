#if (IncludeSerilog)
using Serilog;
#endif
using TemplateApp.External.ApiClients;

var builder = WebApplication.CreateBuilder(args);
#if (IncludeSerilog)

// Serilog: configured from the "Serilog" section of appsettings.json.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));
#endif

// Razor Pages (no Areas): pages live in /Pages and map to URLs by folder.
builder.Services.AddRazorPages();


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
#if (IncludeSerilog)

app.UseSerilogRequestLogging();
#endif

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
#if (IncludeHealthChecks)
app.MapHealthChecks("/health");
#endif

app.Run();
