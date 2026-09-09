using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class IndexModel : PageModel
{
    private readonly BookService _bookService;
    private readonly LoanService _loanService;

    public IndexModel(
        BookService bookService,
        LoanService loanService)
    {
        _bookService = bookService;
        _loanService = loanService;
    }

    public List<BookDto> Books { get; set; } = new();

    // =====================================================
    // FILTROS DE BÚSQUEDA
    // =====================================================

    [BindProperty(SupportsGet = true)]
    public string? Title { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Author { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ISBN { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Publisher { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyAvailable { get; set; }

    // =====================================================
    // GET
    // =====================================================

    public async Task OnGetAsync()
    {
        // Solo libros disponibles
        if (OnlyAvailable)
        {
            Books = (await _bookService.GetAvailableAsync())
                .ToList();

            return;
        }

        // Sin filtros
        if (string.IsNullOrWhiteSpace(Title) &&
            string.IsNullOrWhiteSpace(Author) &&
            string.IsNullOrWhiteSpace(Category) &&
            string.IsNullOrWhiteSpace(ISBN) &&
            string.IsNullOrWhiteSpace(Publisher))
        {
            Books = (await _bookService.GetAllAsync())
                .ToList();

            return;
        }

        // Con filtros
        Books = (await _bookService.SearchAsync(
            Title,
            Author,
            Category,
            ISBN,
            Publisher))
            .ToList();
    }

    // =====================================================
    // ELIMINAR LIBRO
    // =====================================================

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await _bookService.DeleteAsync(id);

        if (!result)
        {
            TempData["Error"] =
                "No se pudo eliminar el libro.";

            return RedirectToPage();
        }

        TempData["Success"] =
            "Libro eliminado correctamente.";

        return RedirectToPage();
    }

    // =====================================================
    // PRESTAR LIBRO
    // =====================================================

    public async Task<IActionResult> OnPostLoanAsync(int bookId)
    {
        var ok = await _loanService.RegisterLoanAsync(bookId);

        if (ok)
        {
            TempData["Success"] =
                "Préstamo registrado. El libro ahora está prestado.";
        }
        else
        {
            TempData["Error"] =
                "No se pudo registrar el préstamo. El libro no tiene copias disponibles.";
        }

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostAddCopyAsync(int bookId, string internalCode)
    {
        var (ok, message) = await _bookService.AddCopyAsync(bookId, internalCode);

        if (ok)
            TempData["Success"] = message;
        else
            TempData["Error"] = message;

        return RedirectToPage();
    }
}