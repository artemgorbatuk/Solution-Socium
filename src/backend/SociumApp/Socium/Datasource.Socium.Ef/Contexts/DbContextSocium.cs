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
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Sender> Senders { get; set; }
    public virtual DbSet<Recipient> Recipients { get; set; }
    public virtual DbSet<Participant> Participants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
