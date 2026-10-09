using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParticipantController : ApiControllerBase
{
    private readonly IServiceParticipant serviceParticipant;

    public ParticipantController(IServiceParticipant serviceParticipant)
    {
        this.serviceParticipant = serviceParticipant;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ParticipantCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceParticipant.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] ParticipantUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceParticipant.UpdateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> DeleteConfirm(
        [FromQuery] ParticipantDeletePageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceParticipant.DisplayDeletePageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] ParticipantDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceParticipant.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ParticipantListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceParticipant.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
