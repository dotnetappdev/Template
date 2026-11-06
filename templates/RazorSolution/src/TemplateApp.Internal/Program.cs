using TemplateApp.Internal.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages (no Areas): pages live in /Pages and map to URLs by folder.
builder.Services.AddRazorPages();

// Typed HttpClients for TemplateApp.API. This site never talks to the database directly.
builder.Services.AddApiClients(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
