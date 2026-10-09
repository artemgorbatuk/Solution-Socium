using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ApiControllerBase
{
    private readonly IServiceRoom serviceRoom;

    public RoomController(IServiceRoom serviceRoom)
    {
        this.serviceRoom = serviceRoom;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromQuery] RoomCreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DisplayCreatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] RoomCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("update")]
    public async Task<IActionResult> Update(
        [FromQuery] RoomUpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DisplayUpdatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] RoomUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.UpdateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> DeleteConfirm(
        [FromQuery] RoomDeletePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DisplayDeletePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] RoomDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("info")]
    public async Task<IActionResult> Info(
        [FromQuery] RoomInfoPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DisplayInfoPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] RoomListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoom.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
