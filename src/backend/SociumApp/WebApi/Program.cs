using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContextExt(builder.Configuration);
builder.Services.AddDependencyInjectionExt(builder.Configuration);
builder.Services.AddAuthenticationExt(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Порядок важен: миграция Socium Task0011 удаляет старую таблицу Users, Security создаёт новую.
    using var scope = app.Services.CreateScope();
    var sociumFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSocium>>();
    await using var sociumDb = await sociumFactory.CreateDbContextAsync();
    await sociumDb.Database.MigrateAsync();

    var securityFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSecurity>>();
    await using var securityDb = await securityFactory.CreateDbContextAsync();
    await securityDb.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

await app.RunAsync();

public partial class Program;
