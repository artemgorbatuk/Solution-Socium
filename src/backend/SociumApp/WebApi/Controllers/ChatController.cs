using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ApiControllerBase
{
    private readonly IServiceChat serviceChat;

    public ChatController(IServiceChat serviceChat)
    {
        this.serviceChat = serviceChat;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromQuery] ChatCreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DisplayCreatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ChatCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("update")]
    public async Task<IActionResult> Update(
        [FromQuery] ChatUpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DisplayUpdatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] ChatUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.UpdateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> DeleteConfirm(
        [FromQuery] ChatDeletePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DisplayDeletePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] ChatDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("info")]
    public async Task<IActionResult> Info(
        [FromQuery] ChatInfoPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DisplayInfoPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ChatListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceChat.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
