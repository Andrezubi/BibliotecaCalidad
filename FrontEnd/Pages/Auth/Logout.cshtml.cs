using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Auth;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.Clear();

        return RedirectToPage("/Auth/Login");
    }
}