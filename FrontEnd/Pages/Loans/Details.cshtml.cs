using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Loans
{
    public class DetailsModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;

        public DetailsModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        public LoanDto? Loan { get; set; }

        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            try
            {
                Loan = await _loanService.GetLoanByIdAsync(Id);

                if (Loan == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch
            {
                ErrorMessage = "No se pudo cargar el préstamo.";
                return Page();
            }
        }
    }
}