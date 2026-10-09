using Microsoft.AspNetCore.Http;
using WebApi.Middleware;

namespace Tests.Units.WebApi;

public sealed class CurrentUserFromHeaderTests
{
    private static CurrentUserFromHeader Create(string? headerValue)
    {
        var context = new DefaultHttpContext();
        if (headerValue is not null)
        {
            context.Request.Headers[CurrentUserFromHeader.HeaderName] = headerValue;
        }

        return new CurrentUserFromHeader(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void UserId_Read_WithGuidHeader_ShouldReturnId()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, Create(id.ToString()).UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void UserId_Read_WithMissingInvalidOrEmptyHeader_ShouldReturnNull(string? headerValue)
    {
        Assert.Null(Create(headerValue).UserId);
    }

    [Fact]
    public void UserId_Read_WithSeveralHeaderValues_ShouldReturnNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CurrentUserFromHeader.HeaderName] = new[] { Guid.CreateVersion7().ToString(), Guid.CreateVersion7().ToString() };

        var currentUser = new CurrentUserFromHeader(new HttpContextAccessor { HttpContext = context });

        Assert.Null(currentUser.UserId);
    }

    [Fact]
    public void UserId_Read_WithoutHttpContext_ShouldReturnNull()
    {
        var currentUser = new CurrentUserFromHeader(new HttpContextAccessor());

        Assert.Null(currentUser.UserId);
    }
}
