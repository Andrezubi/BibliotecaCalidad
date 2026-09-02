using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Users
{
    public class RegisterModel : PageModel
    {
        private readonly UserService _userService;

        public RegisterModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public RegisterUserDto UserData { get; set; } = new();

        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var result =
                    await _userService.RegisterAsync(UserData);

                SuccessMessage = result.Message;

                UserData = new RegisterUserDto();

                ModelState.Clear();

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;

                return Page();
            }
        }
    }
}