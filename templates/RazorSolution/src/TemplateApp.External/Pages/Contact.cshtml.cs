using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TemplateApp.External.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(ILogger<ContactModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public ContactInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // TODO: send the message (email, queue, CRM ...).
        _logger.LogInformation("Contact request from {Email}", Input.Email);

        TempData["Message"] = "Thanks, your message has been sent.";
        return RedirectToPage("/Index");
    }

    public class ContactInput
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Preferred date")]
        [StringLength(20)]
        public string? PreferredDate { get; set; }

        [Required, StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
