using Tests.Infrastructure;

namespace Tests.Integrations.Security;

public sealed class SecurityApiFixture : ApiFixtureBase
{
    public SecurityApiFixture() : base("security_api") { }
}

[CollectionDefinition(Name)]
public sealed class SecurityApiCollection : ICollectionFixture<SecurityApiFixture>
{
    public const string Name = "Security.Api";
}
