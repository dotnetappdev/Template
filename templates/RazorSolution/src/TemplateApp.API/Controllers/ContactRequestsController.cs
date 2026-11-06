using Microsoft.AspNetCore.Mvc;
using TemplateApp.API.Models;

namespace TemplateApp.API.Controllers;

/// <summary>
/// Receives contact requests from TemplateApp.External.
/// </summary>
[ApiController]
[Route("api/contact-requests")]
[Produces("application/json")]
public class ContactRequestsController : ControllerBase
{
    private readonly ILogger<ContactRequestsController> _logger;

    public ContactRequestsController(ILogger<ContactRequestsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Post(ContactRequest request)
    {
        // TODO: store it, email it or put it on a queue.
        _logger.LogInformation("Contact request from {Name} <{Email}>", request.Name, request.Email);

        return Accepted();
    }
}
