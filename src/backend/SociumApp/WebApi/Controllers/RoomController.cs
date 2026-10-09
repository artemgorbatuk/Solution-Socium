using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    [HttpGet]
    public IActionResult List()
    {
        return Ok();
    }
}
