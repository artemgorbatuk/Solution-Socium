namespace Datasource.Socium.Ef.Models;

public class User
{
    public required Guid Id { get; set; }

    public required string Login { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
}
