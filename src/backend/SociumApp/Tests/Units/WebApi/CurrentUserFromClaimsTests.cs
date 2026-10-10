using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApi.Middleware;

namespace Tests.Units.WebApi;

public sealed class CurrentUserFromClaimsTests
{
    private static CurrentUserFromClaims Create(string? sub, bool isAuthenticated = true)
    {
        Claim[] claims = sub is null ? [] : [new Claim("sub", sub)];
        var identity = isAuthenticated ? new ClaimsIdentity(claims, "Bearer") : new ClaimsIdentity(claims);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new CurrentUserFromClaims(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void UserId_Read_WithAuthenticatedSubClaim_ShouldReturnId()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, Create(id.ToString()).UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void UserId_Read_WithMissingInvalidOrEmptySub_ShouldReturnNull(string? sub)
    {
        Assert.Null(Create(sub).UserId);
    }

    [Fact]
    public void UserId_Read_WithUnauthenticatedIdentity_ShouldReturnNull()
    {
        Assert.Null(Create(Guid.CreateVersion7().ToString(), isAuthenticated: false).UserId);
    }

    [Fact]
    public void UserId_Read_WithoutHttpContext_ShouldReturnNull()
    {
        var currentUser = new CurrentUserFromClaims(new HttpContextAccessor());

        Assert.Null(currentUser.UserId);
    }
}
