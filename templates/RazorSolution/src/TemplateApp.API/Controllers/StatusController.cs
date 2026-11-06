using Microsoft.AspNetCore.Mvc;

namespace TemplateApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    /// <summary>
    /// Simple liveness check.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        application = "TemplateApp.API",
        framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        utc = DateTime.UtcNow,
    });
}
