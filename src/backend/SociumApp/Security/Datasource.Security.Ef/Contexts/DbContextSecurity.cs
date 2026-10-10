using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Datasource.Security.Ef.Contexts;

public class DbContextSecurity : DbContext
{
    public DbContextSecurity(DbContextOptions<DbContextSecurity> options) : base(options) { }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
