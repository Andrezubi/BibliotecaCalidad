using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Shared
{
    public abstract class AuthorizedPageModel : PageModel
    {
        protected string? CurrentUsername =>
            HttpContext.Session.GetString("Username");

        protected string? CurrentRole =>
            HttpContext.Session.GetString("Role");

        protected bool IsInRole(string role)
        {
            return string.Equals(
                CurrentRole,
                role,
                StringComparison.OrdinalIgnoreCase
            );
        }

        protected bool IsInAnyRole(params string[] roles)
        {
            return roles.Any(IsInRole);
        }

        protected IActionResult RequireRole(
            params string[] roles)
        {
            if (!IsInAnyRole(roles))
            {
                return RedirectToPage("/AccessDenied");
            }

            return Page();
        }
    }
}
