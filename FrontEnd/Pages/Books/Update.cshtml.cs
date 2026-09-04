using FrontEnd.DTOs;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Books;

public class UpdateModel : PageModel
{
    private readonly BookService _bookService;

    public UpdateModel(BookService bookService)
    {
        _bookService = bookService;
    }

    [BindProperty]
    public UpdateBookDto Book { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var book = await _bookService.GetByIdAsync(Id);

        if (book == null)
        {
            return NotFound();
        }

        Book = new UpdateBookDto
        {
            Title = book.Title,
            EditionNumber = book.EditionNumber,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Description = book.Description,
            UserId = null
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se encontró el usuario autenticado.");

            return Page();
        }

        Book.UserId = userId.Value;

        var result = await _bookService.UpdateAsync(Id, Book);

        if (!result)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el libro.");

            return Page();
        }

        TempData["Success"] =
            "Libro actualizado correctamente.";

        return RedirectToPage("Index");
    }
}