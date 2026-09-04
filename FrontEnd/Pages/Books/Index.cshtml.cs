using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class IndexModel : PageModel
{
    private readonly BookService _bookService;

    public IndexModel(BookService bookService)
    {
        _bookService = bookService;
    }

    public List<BookDto> Books { get; set; } = new();


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

    public async Task OnGetAsync()
    {
        // If there are no search parameters,
        // retrieve all books.
        if (string.IsNullOrWhiteSpace(Title) &&
            string.IsNullOrWhiteSpace(Author) &&
            string.IsNullOrWhiteSpace(Category) &&
            string.IsNullOrWhiteSpace(ISBN) &&
            string.IsNullOrWhiteSpace(Publisher))
        {
            Books = await _bookService.GetAllAsync();
            return;
        }

        Books = await _bookService.SearchAsync(
            Title,
            Author,
            Category,
            ISBN,
            Publisher);
    }

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
}