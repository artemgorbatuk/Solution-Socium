using Tests.Infrastructure;

namespace Tests.Integrations.Security;

public sealed class SecurityServiceFixture : ServiceFixtureBase
{
    public SecurityServiceFixture() : base("security_service") { }
}

[CollectionDefinition(Name)]
public sealed class SecurityServiceCollection : ICollectionFixture<SecurityServiceFixture>
{
    public const string Name = "Security.Services";
}
