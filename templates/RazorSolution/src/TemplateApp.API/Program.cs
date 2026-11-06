#if (UseServices)
using TemplateApp.Services.Extensions;
#endif
#if (UseSqlRepository)
using TemplateApp.SqlRepository.Extensions;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
#if (UseServices)

// ProjectName.Services (and ProjectName.SqlRepository under it).
builder.Services.AddApplicationServices(builder.Configuration);
#elseif (UseSqlRepository)

// ProjectName.SqlRepository: ApplicationDbContext + repositories.
builder.Services.AddSqlRepository(builder.Configuration);
#endif

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
#if (UseSqlRepository)

    // Creates/migrates and seeds the development database.
    await app.Services.InitialiseDatabaseAsync();
#endif
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
