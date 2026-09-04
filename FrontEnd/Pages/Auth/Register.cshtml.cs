using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly AuthApiService _authService;

    public RegisterModel(AuthApiService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public RegisterUserDto User { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result =
            await _authService.RegisterAsync(User);

        if (!result.Success)
        {
            ErrorMessage = result.Message;

            return Page();
        }

        TempData["Success"] =
            "Cuenta creada correctamente. Ahora puedes iniciar sesión.";

        return RedirectToPage("/Auth/Login");
    }
}