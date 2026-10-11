using Datasource.Files.Ef.Contexts;
using Datasource.Messaging.Ef.Contexts;
using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContextExt(builder.Configuration);
builder.Services.AddDependencyInjectionExt(
    builder.Configuration,
    captureMail: builder.Environment.IsEnvironment("Testing"));
builder.Services.AddAuthenticationExt(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Порядок: Socium → Security → Messaging → Files.
    using var scope = app.Services.CreateScope();
    var sociumFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSocium>>();
    await using var sociumDb = await sociumFactory.CreateDbContextAsync();
    await sociumDb.Database.MigrateAsync();

    var securityFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextSecurity>>();
    await using var securityDb = await securityFactory.CreateDbContextAsync();
    await securityDb.Database.MigrateAsync();

    var messagingFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextMessaging>>();
    await using var messagingDb = await messagingFactory.CreateDbContextAsync();
    await messagingDb.Database.MigrateAsync();

    var filesFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DbContextFiles>>();
    await using var filesDb = await filesFactory.CreateDbContextAsync();
    await filesDb.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

await app.RunAsync();

public partial class Program;
