using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class LoanedModel : PageModel
{
    private readonly LoanService _loanService;

    public LoanedModel(LoanService loanService)
    {
        _loanService = loanService;
    }

    public List<LoanedBookDto> LoanedBooks { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            LoanedBooks = await _loanService.GetLoanedBooksAsync();
        }
        catch
        {
            ErrorMessage = "No se pudieron cargar los libros prestados.";
        }
    }

    public async Task<IActionResult> OnPostReturnAsync(int copyId)
    {
        var success = await _loanService.ReturnBookAsync(copyId);
        if (success)
        {
            SuccessMessage = "Libro devuelto y marcado como disponible exitosamente.";
        }
        else
        {
            ErrorMessage = "No se pudo marcar el libro como disponible.";
        }

        return RedirectToPage();
    }
}