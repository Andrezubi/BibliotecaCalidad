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


    // =====================================================
    // FILTROS DE BÚSQUEDA
    // =====================================================

    [BindProperty(SupportsGet = true)]
    public string? Phrase { get; set; }

    // =====================================================
    // GET
    // =====================================================

    public async Task OnGetAsync()
    {
        // Si no se ingresó ningún filtro,
        // obtenemos todos los libros.

        if (string.IsNullOrWhiteSpace(Phrase))
        {
            Books = (await _bookService.GetAllAsync())
                .ToList();

            return;
        }


        // Si existe al menos un filtro,
        // realizamos la búsqueda.

        Books = (await _bookService.SearchAsync(
            Phrase))
            .ToList();
    }


    // =====================================================
    // ELIMINAR LIBRO
    // =====================================================

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result =
            await _bookService.DeleteAsync(id);


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