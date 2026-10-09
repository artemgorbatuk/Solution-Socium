using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContextExt(builder.Configuration);
builder.Services.AddDependencyInjectionExt();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sociumFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSocium>>();
    await using var sociumDb = await sociumFactory.CreateDbContextAsync();
    await sociumDb.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();

public partial class Program;
