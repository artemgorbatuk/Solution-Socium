using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Services.Security.Api;
using Services.Security.Models;
using WebApi.Controllers.Shared;
using WebApi.Middleware;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ApiControllerBase
{
    private readonly IServiceUser serviceUser;

    public UserController(IServiceUser serviceUser)
    {
        this.serviceUser = serviceUser;
    }

    [AllowAnonymous]
    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromQuery] UserCreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayCreatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationRegistration.AuthRateLimitPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("update")]
    public async Task<IActionResult> Update(
        [FromQuery] UserUpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayUpdatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.UpdateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> DeleteConfirm(
        [FromQuery] UserDeletePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayDeletePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] UserDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("info")]
    public async Task<IActionResult> Info(
        [FromQuery] UserInfoPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayInfoPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] UserListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] UserSearchRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.SearchAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
