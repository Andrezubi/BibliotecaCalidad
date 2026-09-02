using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryBookCrud.Pages.Books;

public class IndexModel : PageModel
{
    private readonly IBookService _bookService;

    public IndexModel(IBookService bookService)
    {
        _bookService = bookService;
    }

    public List<BookDto> Books { get; set; } = new();

    public async Task OnGetAsync()
    {
        var books = await _bookService.GetAllAsync();

        Books = books.ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await _bookService.DeleteAsync(id);

        if (!result)
        {
            TempData["Error"] = "No se encontró el libro.";

            return RedirectToPage();
        }

        TempData["Success"] = "Libro eliminado correctamente.";

        return RedirectToPage();
    }
}