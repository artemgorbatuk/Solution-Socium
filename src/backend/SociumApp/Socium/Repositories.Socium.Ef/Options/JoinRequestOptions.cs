using Datasource.Socium.Ef.Enums;

namespace Repositories.Socium.Ef.Options;

public class JoinRequestGetNewOptions
{
    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid CreatedByUserId { get; set; }
}

public class JoinRequestQueryOptions
{
    public Guid? Id { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? UserId { get; set; }
    public JoinRequestStatus? Status { get; set; }
}
