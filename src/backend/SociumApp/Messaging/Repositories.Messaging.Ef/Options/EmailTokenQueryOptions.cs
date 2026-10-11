using Datasource.Messaging.Ef.Models;

namespace Repositories.Messaging.Ef.Options;

public class EmailTokenQueryOptions
{
    public Guid? UserId { get; set; }
    public EmailTokenPurpose? Purpose { get; set; }
    public bool? UnusedOnly { get; set; }
}
