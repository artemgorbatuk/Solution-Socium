using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Datasource.Socium.Ef.Contexts;

public class DbContextSocium : DbContext
{
    public DbContextSocium(DbContextOptions<DbContextSocium> options) : base(options) { }

    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<Chat> Chats { get; set; }
    public virtual DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
