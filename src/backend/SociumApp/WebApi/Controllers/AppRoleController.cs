using Microsoft.AspNetCore.Mvc;
using Services.Security.Api;
using Services.Security.Models;
using WebApi.Controllers.Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppRoleController : ApiControllerBase
{
    private readonly IServiceAppRole serviceAppRole;

    public AppRoleController(IServiceAppRole serviceAppRole)
    {
        this.serviceAppRole = serviceAppRole;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AppRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAppRole.CreateAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] AppRoleDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var responseInfo = await serviceAppRole.DeleteAsync(request, cancellationToken);
        return FromResponse(responseInfo);
    }
}
