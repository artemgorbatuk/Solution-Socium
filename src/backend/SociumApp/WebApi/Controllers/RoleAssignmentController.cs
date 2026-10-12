using Microsoft.AspNetCore.Mvc;
using Services.Socium.Api;
using Services.Socium.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleAssignmentController : ApiControllerBase
{
    private readonly IServiceRoleAssignment serviceRoleAssignment;

    public RoleAssignmentController(IServiceRoleAssignment serviceRoleAssignment)
    {
        this.serviceRoleAssignment = serviceRoleAssignment;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] RoleAssignmentListPageRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoleAssignment.DisplayListPageAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] RoleAssignmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoleAssignment.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] RoleAssignmentDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceRoleAssignment.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
