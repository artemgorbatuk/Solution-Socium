using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

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

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromQuery] UserCreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceUser.DisplayCreatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

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
}
