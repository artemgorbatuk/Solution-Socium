using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JoinRequestController : ApiControllerBase
{
    private readonly IServiceJoinRequest serviceJoinRequest;

    public JoinRequestController(IServiceJoinRequest serviceJoinRequest)
    {
        this.serviceJoinRequest = serviceJoinRequest;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] JoinRequestCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceJoinRequest.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        [FromBody] JoinRequestAcceptRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceJoinRequest.AcceptAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject(
        [FromBody] JoinRequestRejectRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceJoinRequest.RejectAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> PendingList(
        [FromQuery] JoinRequestPendingListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceJoinRequest.DisplayPendingListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
