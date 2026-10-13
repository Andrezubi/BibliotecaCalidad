using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Loans
{
    public class IndexModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;

        public IndexModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        public List<LoanListDto> Loans { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            try
            {
                Loans = await _loanService.GetAllLoansAsync();
                return Page();
            }
            catch
            {
                ErrorMessage = "No se pudieron cargar los préstamos.";
                return Page();
            }
        }
    }
}