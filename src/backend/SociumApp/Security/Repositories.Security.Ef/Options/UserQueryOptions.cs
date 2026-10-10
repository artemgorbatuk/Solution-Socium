namespace Repositories.Security.Ef.Options;

public class UserQueryOptions
{
    public ICollection<Guid>? Ids { get; set; }
    public string? Login { get; set; }
    public Guid? ExcludeId { get; set; }
    public bool? IsDeleted { get; set; }
}
