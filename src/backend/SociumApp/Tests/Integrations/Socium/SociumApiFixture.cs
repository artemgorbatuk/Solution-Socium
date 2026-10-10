using Tests.Infrastructure;

namespace Tests.Integrations.Socium;

public sealed class SociumApiFixture : ApiFixtureBase
{
    public SociumApiFixture() : base("socium_api") { }
}

[CollectionDefinition(Name)]
public sealed class SociumApiCollection : ICollectionFixture<SociumApiFixture>
{
    public const string Name = "Socium.Api";
}
