using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using FrontEnd.Pages.Shared;

namespace FrontEnd.Pages.Books
{
    public class LoanedModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;

        public LoanedModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        public List<LoanedBookDto> LoanedBooks { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            try
            {
                LoanedBooks = await _loanService.GetLoanedBooksAsync();
                return Page();
            }
            catch
            {
                ErrorMessage = "No se pudieron cargar los libros prestados.";
                return Page();
            }
        }

    }
}
