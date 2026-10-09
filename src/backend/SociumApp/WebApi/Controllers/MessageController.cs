using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ApiControllerBase
{
    private readonly IServiceMessage serviceMessage;

    public MessageController(IServiceMessage serviceMessage)
    {
        this.serviceMessage = serviceMessage;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromQuery] MessageCreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DisplayCreatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] MessageCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("update")]
    public async Task<IActionResult> Update(
        [FromQuery] MessageUpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DisplayUpdatePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] MessageUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.UpdateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> DeleteConfirm(
        [FromQuery] MessageDeletePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DisplayDeletePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] MessageDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("info")]
    public async Task<IActionResult> Info(
        [FromQuery] MessageInfoPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DisplayInfoPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] MessageListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceMessage.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
