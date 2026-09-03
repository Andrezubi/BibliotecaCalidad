using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly AuthApiService _authService;

    public LoginModel(AuthApiService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginDto Login { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var response =
            await _authService.LoginAsync(Login);

        if (response is null)
        {
            ErrorMessage =
                "Usuario o contraseña incorrectos.";

            return Page();
        }

        HttpContext.Session.SetString(
            "AuthToken",
            response.Token
        );

        HttpContext.Session.SetString(
            "Username",
            response.Username
        );

        HttpContext.Session.SetString(
            "Role",
            response.Role
        );

        return RedirectToPage("/Index");
    }
}