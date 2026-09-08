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

    public IEnumerable<BookDto> Books { get; set; }
        = new List<BookDto>();

    public async Task OnGetAsync()
    {
        Books = await _bookService.GetAllAsync();
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