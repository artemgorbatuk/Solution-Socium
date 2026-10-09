namespace Repositories.Socium.Ef.Options;

public class UserQueryOptions
{
    public string? Login { get; set; }
    public Guid? ExcludeId { get; set; }
    public bool? IsDeleted { get; set; }
}
