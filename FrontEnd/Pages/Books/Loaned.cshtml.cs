using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Pages.Books
{
    public class LoanedModel : PageModel
    {
        private readonly LoanService _loanService;

        public LoanedModel(LoanService loanService)
        {
            _loanService = loanService;
        }

        public List<LoanedBookDto> LoanedBooks { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                LoanedBooks = await _loanService.GetLoanedBooksAsync();
            }
            catch
            {
                ErrorMessage =
                    "No se pudieron cargar los libros prestados.";
            }
        }
        public async Task<IActionResult> OnPostReturnAsync(int copyId)
        {
            var ok = await _loanService.RegisterReturnAsync(copyId);

            if (ok)
                TempData["Success"] = "Devolución registrada. El libro ahora está disponible.";
            else
                TempData["Error"] = "No se pudo registrar la devolución.";

            return RedirectToPage();
        }
    }
}