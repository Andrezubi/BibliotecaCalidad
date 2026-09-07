using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class IndexModel : PageModel
{
    private readonly BookService _bookService;
    private readonly LoanService _loanService;

    public IndexModel(BookService bookService, LoanService loanService)
    {
        _bookService = bookService;
        _loanService = loanService;
    }

    public List<BookDto> Books { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool OnlyAvailable { get; set; } = true;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Books = await _bookService.GetBooksAsync(OnlyAvailable);
    }

    public async Task<IActionResult> OnPostLoanAsync(int bookId)
    {
        var success = await _loanService.LoanBookAsync(bookId);
        if (success)
        {
            SuccessMessage = "Libro marcado como prestado exitosamente.";
        }
        else
        {
            ErrorMessage = "No se pudo prestar el libro. Verifique si tiene copias disponibles.";
        }

        return RedirectToPage(new { OnlyAvailable });
    }
}