#if (UseServices)
using TemplateApp.Services.Extensions;
#endif
#if (UseSqlRepository)
using TemplateApp.SqlRepository.Extensions;
#endif

var builder = WebApplication.CreateBuilder(args);

// Razor Pages (no Areas): pages live in /Pages and map to URLs by folder.
builder.Services.AddRazorPages();
#if (UseServices)

// ProjectName.Services (and ProjectName.SqlRepository under it).
builder.Services.AddApplicationServices(builder.Configuration);
#elseif (UseSqlRepository)

// ProjectName.SqlRepository: ApplicationDbContext + repositories.
builder.Services.AddSqlRepository(builder.Configuration);
#endif

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
#if (UseSqlRepository)
else
{
    // Creates/migrates and seeds the development database.
    await app.Services.InitialiseDatabaseAsync();
}
#endif

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
