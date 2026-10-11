using Datasource.Files.Ef.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Datasource.Files.Ef.Contexts;

public class DbContextFiles : DbContext
{
    public DbContextFiles(DbContextOptions<DbContextFiles> options) : base(options) { }

    public virtual DbSet<StoredFile> StoredFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
