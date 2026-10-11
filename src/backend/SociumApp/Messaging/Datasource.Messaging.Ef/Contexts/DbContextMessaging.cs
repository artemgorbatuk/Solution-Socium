using Datasource.Messaging.Ef.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Datasource.Messaging.Ef.Contexts;

public class DbContextMessaging : DbContext
{
    public DbContextMessaging(DbContextOptions<DbContextMessaging> options) : base(options) { }

    public virtual DbSet<EmailToken> EmailTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
